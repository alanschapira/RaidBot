using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Globalization;
using Discord;
using RaidBot.Entities;
using RaidBot.Helpers;
using RaidBot.BusinessLogic.RaidStorage.Interfaces;
using RaidBot.BusinessLogic.RaidStorage;

namespace RaidBot.BusinessLogic.Raids {
   public class RaidService {
      IRaidFileService _raidFileService;
      ServerSettings _permissions;
      public RaidService(string guildId, ServerSettings permissions) {
         _raidFileService = new RaidBlobService(guildId, permissions);
         _permissions = permissions;
      }

      public bool AddRaids(Raid raid) {
         var raids = _raidFileService.GetRaidsFromFile();

         if (raids.Any(a => a.Equals(raid))) {
            return false;
         }
         else {
            raids.Add(raid);
            _raidFileService.PushRaidsToFile(raids);
            return true;
         }
      }

      public void AddAttendee(string RaidName, IUser user, int guests) {
         var raids = _raidFileService.GetRaidsFromFile();
         var raid = raids.Single(a => a.Name == RaidName);
         raid.Users.Add(User.FromIUser(user, guests));
      }

      public ModuleResult JoinRaid(string raidName, IUser forUser, int guests, bool isRemote, IUser requestUser = null) {

         ModuleResult result = new ModuleResult();

         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            bool success = AddUserToRaidIfNotExists(raids.Single(), forUser, guests, isRemote);

            if (success) {
               result.Success = true;

               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               string withGuests = guests == 0 ? string.Empty : $"with {guests} guest{(guests == 1 ? "" : "s")}";
               string asRemoteAttendee = isRemote ? " as a remote attendee" : string.Empty;

               if (requestUser != null) {
                  var forUserUsername = (forUser as IGuildUser).Nickname ?? forUser.Username;
                  var requestUserUsername = (requestUser as IGuildUser).Nickname ?? requestUser.Username;

                  result.RequesterUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"You have added {forUserUsername} to the raid{asRemoteAttendee} {withGuests}");

                  result.ReferenceUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.ReferenceUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"You have been added to the raid{asRemoteAttendee} by {requestUserUsername} {withGuests}");
               }
               else {
                  result.RequesterUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"You have been added to the raid{asRemoteAttendee}");
               }
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("User is already in the raid");
            }
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }
         return result;
      }

      public async Task<ModuleResult> AddPokemon(string raidName, string raidBoss, IGuildUser user) {
         var result = new ModuleResult();
         int raidBossId;
         string raidBossName;
         string raidBossSpriteUrl;
         try {
            (raidBossId, raidBossName, raidBossSpriteUrl) = await Mons.GetMonInfo(raidBoss);
         }
         catch (Exception e) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot recognise raid boss. Check your spelling!");
            return result;
         }

         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();
            if (raid.RaidBossId == raidBossId) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder($"That raid already has a raidboss of: {raidBossName}");
               return result;
            }
            if (user.GuildPermissions.Has(GuildPermission.ManageMessages) || raid.Users.FirstOrDefault().Equals(User.FromIUser(user))) {
               result.Users = raid.Users;
               var allRaids = _raidFileService.GetRaidsFromFile();
               var oldRaidBossId = allRaids.Single(a => a.Equals(raid)).RaidBossId;
               allRaids.Single(a => a.Equals(raid)).RaidBossId = raidBossId;
               allRaids.Single(a => a.Equals(raid)).RaidBossName = raidBossName;
               allRaids.Single(a => a.Equals(raid)).RaidBossSpriteUrl = raidBossSpriteUrl;
               _raidFileService.PushRaidsToFile(allRaids);

               string raidBossMessage = oldRaidBossId == 0 ? $"Raid boss has been changed to {raidBossName}" : $"Raid boss has been changed from {raid.RaidBossName} to {raidBossName}";

               result.Success = true;
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle($"Raid: {raidName}")
                  .WithDescription(raidBossMessage)
                  .WithThumbnailUrl(raidBossSpriteUrl);
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Only the leader can change the raid boss");
            }
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }

         return result;
      }

      public ModuleResult LeaveRaid(string raidName, IUser requesterUser, IUser userToUpdate) {
         var result = new ModuleResult();

         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));
         var userFilteredRaids = raids.Where(a => a.Users.Any(b => b.Equals(User.FromIUser(requesterUser))));

         if (userFilteredRaids.Count() == 1) {
            return RemoveUserFromRaid(userFilteredRaids.First(), requesterUser, userToUpdate);
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Could not find that raid");
         }
         else if (userFilteredRaids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Could not find the user in that raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Something went wrong");
         }
         return result;
      }

      public ModuleResult DeleteRaid(string raidName, IGuildUser user) {

         var result = new ModuleResult();
         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();
            if (user.GuildPermissions.Has(GuildPermission.ManageMessages) || raid.Users.Count() == 0 || raid.Users.FirstOrDefault().Equals(User.FromIUser(user))) {
               result.Users = raid.Users;
               var allRaids = _raidFileService.GetRaidsFromFile();
               var index = allRaids.FindIndex(a => a.Equals(raid));
               allRaids.RemoveAt(index);
               _raidFileService.PushRaidsToFile(allRaids);

               result.Success = true;
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle($"Raid: {raidName}")
                  .WithDescription("This raid has been deleted");
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Only the leader can delete a raid");
            }
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }
         return result;
      }

      public EmbedBuilder MyRaids(IUser user) {
         var raids = _raidFileService.GetRaidsFromFile();

         if (raids.Count() == 0) {
            return EmbedBuilderHelper.ErrorBuilder("No raids have been created yet");
         }

         var raidsFiltered = raids.Where(a => a.Users.Any(b => b.Equals(User.FromIUser(user))));

         var username = (user as IGuildUser).Nickname ?? user.Username;

         if (raidsFiltered.Count() == 0) {
            return EmbedBuilderHelper.ErrorBuilder($"{username} is not attending any raids yet");
         }
         else {
            return EmbedBuilderHelper.RaidEmbedBuilder($"{username} is attending:", raidsFiltered);
         }

      }

      public async Task<ModuleResult> CreateRaid(string raidName, string raidTime, string raidBoss, IUser user, int guests) {
         ModuleResult result = new ModuleResult();
         int raidBossId;
         string raidBossName;
         string raidBossSpriteUrl;
         try {
            (raidBossId, raidBossName, raidBossSpriteUrl) = await Mons.GetMonInfo(raidBoss);
         }
         catch (Exception e) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot recognise raid boss. Check your spelling!");
            return result;
         }

         var now = DateTime.Now;
         raidTime = raidTime.Replace(".", ":").Replace(",", ":").Replace(";", ":");
         DateTime time;
         if (DateTime.TryParseExact(raidTime, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) {

            Raid raid = new Raid() {
               Name = raidName,
               Time = time,
               Users = new List<User>(),
               CreateDateTime = now,
               ExpireStart = now,
               Expire = TimeSpan.FromMinutes(_permissions.AutoExpireMins),
               RaidBossId = raidBossId,
               RaidBossName = raidBossName,
               RaidBossSpriteUrl = raidBossSpriteUrl
            };
            if (_permissions.JoinRaidOnCreate) {
               raid.Users.Add(User.FromIUser(user, guests));
            }
            bool success = AddRaids(raid);
            if (success) {
               result.Success = true;
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle("Raid succesfully created:")
                  .WithDescription(raid.ToString())
                  .WithThumbnailUrl(raidBossSpriteUrl);
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Raid already exists. Please join or create a different raid.");
            }
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("I do not understand that time. Try using a format of Hours:Mins e.g. `11:30`");
         }
         return result;
      }

      public ModuleResult CreateRaid(string raidName, IUser user) {
         ModuleResult result = new ModuleResult();
         var now = DateTime.Now;

         Raid raid = new Raid() {
            Name = raidName,
            Users = new List<User>(),
            CreateDateTime = now,
            ExpireStart = now,
            Expire = TimeSpan.FromMinutes(_permissions.AutoExpireMins)
         };
         if (_permissions.JoinRaidOnCreate) {
            raid.Users.Add(User.FromIUser(user));
         }
         bool success = AddRaids(raid);
         if (success) {
            result.Success = true;
            result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
            result.RequesterUserBuilder
               .WithTitle("Raid succesfully created:")
               .WithDescription(raid.ToString());
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Raid already exists. Please join or create a different raid.");
         }
         return result;
      }

      public ModuleResult MessageUsers(string raidName, string message) {
         var result = new ModuleResult();
         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            result.Users = raids.Single().Users;
            result.Success = true;
            result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
            result.RequesterUserBuilder
               .WithTitle($"Raid: {raidName}")
               .WithDescription(message);
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }
         return result;
      }

      public ModuleResult ChangeName(string raidName, string newRaidName, IGuildUser user) {
         var result = new ModuleResult();

         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();
            if (user.GuildPermissions.Has(GuildPermission.ManageMessages) || raid.Users.FirstOrDefault().Equals(User.FromIUser(user))) {
               result.Users = raid.Users;
               var allRaids = _raidFileService.GetRaidsFromFile();
               allRaids.Single(a => a.Equals(raid)).Name = newRaidName;
               _raidFileService.PushRaidsToFile(allRaids);

               result.Success = true;
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle($"Raid: {newRaidName}")
                  .WithDescription($"Name has been changed from {raidName} to {newRaidName}");
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Only the leader can change the name");
            }
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }

         return result;
      }

      public ModuleResult ChangeExpire(string raidName, int expire, IGuildUser user) {
         var result = new ModuleResult();

         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();
               if (user.GuildPermissions.Has(GuildPermission.ManageMessages) || raid.Users.FirstOrDefault().Equals(User.FromIUser(user))) {

                  result.Users = raid.Users;
                  var allRaids = _raidFileService.GetRaidsFromFile();
                  var expireTimeSpan = TimeSpan.FromMinutes(expire);
                  allRaids.Single(a => a.Equals(raid)).Expire = expireTimeSpan;
                  allRaids.Single(a => a.Equals(raid)).ExpireStart = DateTime.Now;
                  _raidFileService.PushRaidsToFile(allRaids);

                  result.Success = true;
                  result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.RequesterUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"Raid will now expire in {allRaids.Single(a => a.Equals(raid)).ToStringExpire()}");
               }
               else {
                  result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Only the leader can delete a raid");
               }
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }


         return result;
      }

      public ModuleResult ChangeTime(string raidName, string raidTime, IGuildUser user) {
         var result = new ModuleResult();

         raidTime = raidTime.Replace(".", ":").Replace(",", ":").Replace(";", ":");
         DateTime time;
         if (DateTime.TryParseExact(raidTime, "H:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out time)) {
            var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

            if (raids.Count() == 1) {
               var raid = raids.Single();
               if (user.GuildPermissions.Has(GuildPermission.ManageMessages) || raid.Users.FirstOrDefault().Equals(User.FromIUser(user))) {

                  result.Users = raid.Users;
                  var allRaids = _raidFileService.GetRaidsFromFile();
                  allRaids.Single(a => a.Equals(raid)).Time = time;
                  _raidFileService.PushRaidsToFile(allRaids);

                  result.Success = true;
                  result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.RequesterUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"Time has been changed to {time.ToString("H:mm")}");
                  }
               else {
                  result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Only the leader can change the time");
               }
            }
            else if (raids.Count() == 0) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
            }

         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("I do not understand that time. Try using a format of Hours:Mins e.g. `11:30`");
         }
         return result;
      }

      public ModuleResult ChangeDate(string raidName, string raidDate, IGuildUser user) {
         var result = new ModuleResult();

         DateTime date;
         if (DateTime.TryParseExact(raidDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date)) {
            var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

            if (raids.Count() == 1) {
               var raid = raids.Single();
               if (user.GuildPermissions.Has(GuildPermission.ManageMessages) || raid.Users.FirstOrDefault().Equals(User.FromIUser(user))) {

                  result.Users = raid.Users;
                  var allRaids = _raidFileService.GetRaidsFromFile();
                  allRaids.Single(a => a.Equals(raid)).Day = date;
                  _raidFileService.PushRaidsToFile(allRaids);

                  result.Success = true;
                  result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.RequesterUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"Date has been changed to {date.ToString("yyyy'-'MM'-'dd")}\nPlease note you will need to change the expire seperately");
                  }
               else {
                  result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Only the leader can change the date");
               }
            }
            else if (raids.Count() == 0) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
            }
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("I do not understand that date. Try using a format of year-month-day e.g.`2018-04-28`");
         }
         return result;
      }

      public ModuleResult AddGuests(string raidName, IUser userToAddGuests, int guests, IUser requestUser = null) {

         var result = new ModuleResult();
         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();
            raid.Users.Single(a => a.Equals(User.FromIUser(userToAddGuests))).GuestsCount = guests;
            UpdateRaidWithDifferentUsers(raid);
            result.Success = true;

            if (requestUser == null || requestUser == null) {
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle($"Raid: {raidName}")
                  .WithDescription($"Added {guests} guest{(guests == 1 ? "" : "s")}");
            }
            else {
               var userToAddGuestsUsername = (userToAddGuests as IGuildUser).Nickname ?? userToAddGuests.Username;
               var requestUserUsername = (requestUser as IGuildUser).Nickname ?? requestUser.Username;

               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle($"Raid: {raidName}")
                  .WithDescription($"Added {guests} guest{(guests == 1 ? "" : "s")} for {userToAddGuestsUsername}");

               result.ReferenceUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.ReferenceUserBuilder
                  .WithTitle($"Raid: {raidName}")
                  .WithDescription($"{requestUserUsername} has added {guests} guest{(guests == 1 ? "" : "s")} to the raid for you");
             }
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }
         return result;
      }

      public ModuleResult GetSpecificRaid(string raidName) {

         var result = new ModuleResult();
         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();

            result.Success = true;
            result.RequesterUserBuilder = EmbedBuilderHelper.BlueBuilder();
            result.RequesterUserBuilder
               .WithTitle($"Raid: {raid.ToString()}")
               .WithDescription(raid.ToStringUsers())
               .WithThumbnailUrl(raid.RaidBossSpriteUrl);
            }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Could not find that raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }
         return result;
      }

      public EmbedBuilder GetRaids() {
         var raids = _raidFileService.GetRaidsFromFile();

         if (raids.Count() == 0) {
            return EmbedBuilderHelper.ErrorBuilder("No raids have been created yet");
         }
         return EmbedBuilderHelper.RaidEmbedBuilder("All current raids:", raids);
      }

      public ModuleResult ChangeAttendanceMode(string raidName, string attendanceMode, IUser requesterUser, IUser userToUpdate) {
         var result = new ModuleResult();
         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            string[] validModes = { "inperson", "in-person", "remote" };
            if (Array.Exists(validModes, el => el == attendanceMode)) {
               var raid = raids.Single();

               if (userToUpdate == null) {
                  raid.Users.Single(a => a.Equals(User.FromIUser(requesterUser))).IsRemoteAttendee = (attendanceMode == "remote");
                  UpdateRaidWithDifferentUsers(raid);
                  result.Success = true;

                  result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.RequesterUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"Updated attendance mode to {attendanceMode}");
               }
               else {
                  raid.Users.Single(a => a.Equals(User.FromIUser(userToUpdate))).IsRemoteAttendee = (attendanceMode == "remote");
                  UpdateRaidWithDifferentUsers(raid);
                  result.Success = true;

                  var userToUpdateUsername = (userToUpdate as IGuildUser).Nickname ?? userToUpdate.Username;
                  var requesterUserUsername = (requesterUser as IGuildUser).Nickname ?? requesterUser.Username;

                  result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.RequesterUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"Updated attendance mode to {attendanceMode} for {userToUpdateUsername}");

                  result.ReferenceUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.ReferenceUserBuilder
                     .WithTitle($"Raid: {raidName}")
                     .WithDescription($"{requesterUserUsername} has updated your attendance mode to {attendanceMode}");
               }
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Invalid mode. Accepted modes are inperson and remote");
            }
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot find raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }
         return result;
      }

      public ModuleResult GetInvitesString(string raidName) {

         var result = new ModuleResult();
         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();
            List<User> remoteUsers = raid.Users.Skip(1).Where(user => user.IsRemoteAttendee).ToList();
            StringBuilder inviteString = new StringBuilder();

            if (remoteUsers.Count() > 0) {
               inviteString.Append($"{remoteUsers.First().Username}");
               foreach (var user in remoteUsers.Skip(1)) {
                  inviteString.Append($",{user.Username}");
               }
               result.Message = inviteString.ToString();
            }
            else {
               result.Message = "No remote raiders to invite";
            }

            result.Success = true;
         }
         else if (raids.Count() == 0) {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Could not find that raid");
         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Unknown Error");
         }
         return result;
      }

      #region Private Methods


      private bool AddUserToRaidIfNotExists(Raid raid, IUser user, int guests, bool isRemote) {
         if (raid.Users.Any(a => a.Equals(User.FromIUser(user)))) {
            return false;
         }
         else {
            raid.Users.Add(User.FromIUser(user, guests, isRemote));
            UpdateRaidWithDifferentUsers(raid);
            return true;//$"You have been added to raid: {raid.Name}";
         }
      }

      private ModuleResult RemoveUserFromRaid(Raid raid, IUser requesterUser, IUser referenceUser) {
         var result = new ModuleResult();

         IUser userToCheck = referenceUser ?? requesterUser;

         if (raid.Users.Any(a => a.Equals(User.FromIUser(userToCheck)))) {
            raid.Users.RemoveAt(raid.Users.FindIndex(a => a.Equals(User.FromIUser(userToCheck))));
            UpdateRaidWithDifferentUsers(raid);

            result.Success = true;

            if (referenceUser != null) {
               var requesterUserUsername = (requesterUser as IGuildUser).Nickname ?? requesterUser.Username;
               var referenceUserUsername = (referenceUser as IGuildUser).Nickname ?? referenceUser.Username;

               result.ReferenceUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.ReferenceUserBuilder
                  .WithTitle($"Raid: {raid.Name}")
                  .WithDescription($"You have been removed from the raid by {requesterUserUsername}");

               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle($"Raid: {raid.Name}")
                  .WithDescription($"You have removed {referenceUserUsername} from the raid");
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder
                  .WithTitle($"Raid: {raid.Name}")
                  .WithDescription("You have been removed from the raid");
            }

         }
         else {
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder($"Could not find user in that raid");
         }
         return result;
      }

      private string UpdateRaidWithDifferentUsers(Raid raid) {
         var raids = _raidFileService.GetRaidsFromFile();
         raids.RemoveAt(raids.FindIndex(a => a.Equals(raid)));
         raids.Add(raid);
         _raidFileService.PushRaidsToFile(raids);
         return "Added user to raid";
      }
      #endregion
   }
}
