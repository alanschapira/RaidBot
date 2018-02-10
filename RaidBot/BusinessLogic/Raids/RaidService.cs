using System.Collections.Generic;
using System.Linq;
using System;
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

      public ModuleResult JoinRaid(string raidName, IUser forUser, int guests, IUser requestUser = null) {

         ModuleResult result = new ModuleResult();

         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            bool success = AddUserToRaidIfNotExists(raids.Single(), forUser, guests);

            if (success) {
               result.Success = true;
               if (requestUser != null) {
                  result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();

                  string withGuests = guests == 0 ? string.Empty : $"with {guests} guests";

                  result.RequesterUserBuilder.AddField(x => {
                     x.Name = $"Raid: {raidName}";
                     x.Value = $"You have added {forUser.Username} to the raid {withGuests}";
                     x.IsInline = false;
                  });
                  result.ReferenceUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.ReferenceUserBuilder.AddField(x => {
                     x.Name = $"Raid: {raidName}";
                     x.Value = $"You have been added to the raid by {requestUser.Username} {withGuests}";
                     x.IsInline = false;
                  });
               }
               else {
                  result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
                  result.RequesterUserBuilder.AddField(x => {
                     x.Name = $"Raid: {raidName}";
                     x.Value = $"You have been added to the raid";
                     x.IsInline = false;
                  });
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

      public ModuleResult AddPokemon(string raidName, string raidBoss, IGuildUser user) {
         var result = new ModuleResult();
         string newRaidBossName;

         int raidBossId = 0;
         if (int.TryParse(raidBoss, out raidBossId)) {
            if (!Mons.IsValidId(raidBossId)) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Invalid raid boss Id");
               return result;
            }
            newRaidBossName = Mons.GetNameById(raidBossId);
         }
         else {
            raidBossId = Mons.GetIdByName(raidBoss);
            if (raidBossId == 0) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot recognise raid boss. Check your spelling!");
               return result;
            }
            newRaidBossName = raidBoss;
         }

         var raids = _raidFileService.GetRaidsFromFile().Where(a => a.Name.Equals(raidName, StringComparison.CurrentCultureIgnoreCase));

         if (raids.Count() == 1) {
            var raid = raids.Single();
            if (raid.RaidBossId == raidBossId) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder($"That raid already has a raidboss of: { Mons.GetNameById(raidBossId)}");
               return result;
            }
            if (user.GuildPermissions.Has(GuildPermission.ManageMessages) || raid.Users.FirstOrDefault().Equals(User.FromIUser(user))) {
               result.Users = raid.Users;
               var allRaids = _raidFileService.GetRaidsFromFile();
               var oldRaidBossId = allRaids.Single(a => a.Equals(raid)).RaidBossId;
               allRaids.Single(a => a.Equals(raid)).RaidBossId = raidBossId;
               _raidFileService.PushRaidsToFile(allRaids);

               string raidBossMessage = oldRaidBossId == 0 ? $"Raidboss has been changed to {newRaidBossName}" : $"Raidboss has been changed from {Mons.GetNameById(oldRaidBossId)} to {newRaidBossName}";

               result.Success = true;
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder.WithThumbnailUrl(string.Format(ConfigVariables.PokemonIconURL, raidBossId));
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raidName}";
                  x.Value = raidBossMessage;
                  x.IsInline = false;
               });
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Only the leader can change the raidboss");
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
            result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Somthing went wrong");
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
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raidName}";
                  x.Value = "This raid has been deleted";
                  x.IsInline = false;
               });
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

         if (raidsFiltered.Count() == 0) {
            return EmbedBuilderHelper.ErrorBuilder($"{user.Username} is not attending any raids yet");
         }
         else {
            return EmbedBuilderHelper.RaidEmbedBuilder($"{user.Username} is attending:", raidsFiltered);
         }

      }

      public ModuleResult CreateRaid(string raidName, string raidTime, string raidBoss, IUser user, int guests) {
         ModuleResult result = new ModuleResult();
         var now = DateTime.Now;
         int id = 0;
         if (int.TryParse(raidBoss, out id)) {
            if (!Mons.IsValidId(id)) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Invalid raid boss Id");
               return result;
            }
         }
         else {
            id = Mons.GetIdByName(raidBoss);
            if (id == 0) {
               result.RequesterUserBuilder = EmbedBuilderHelper.ErrorBuilder("Cannot recognise raid boss. Check your spelling!");
               return result;
            }
         }

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
               RaidBossId = id
            };
            if (_permissions.JoinRaidOnCreate) {
               raid.Users.Add(User.FromIUser(user, guests));
            }
            bool success = AddRaids(raid);
            if (success) {
               result.Success = true;
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = "Raid succesfully created:";
                  x.Value = raid.ToString();
                  x.IsInline = false;
               });
               result.RequesterUserBuilder.WithThumbnailUrl(string.Format(ConfigVariables.PokemonIconURL, id));
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
            result.RequesterUserBuilder.AddField(x => {
               x.Name = "Raid succesfully created:";
               x.Value = raid.ToString();
               x.IsInline = false;
            });
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
            result.RequesterUserBuilder.AddField(x => {
               x.Name = $"Raid: {raidName}";
               x.Value = message;
               x.IsInline = false;
            });
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
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {newRaidName}";
                  x.Value = $"Name has been changed from {raidName} to {newRaidName}";
                  x.IsInline = false;
               });
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
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raidName}";
                  x.Value = $"Raid will now expire in {allRaids.Single(a => a.Equals(raid)).ToStringExpire()}";
                  x.IsInline = false;
               });
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
                  result.RequesterUserBuilder.AddField(x => {
                     x.Name = $"Raid: {raidName}";
                     x.Value = $"Time has been changed to {time.ToString("H:mm")}";
                     x.IsInline = false;
                  });
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
                  result.RequesterUserBuilder.AddField(x => {
                     x.Name = $"Raid: {raidName}";
                     x.Value = $"Date has been changed to {date.ToString("yyyy'-'MM'-'dd")}\nPlease note you will need to change the expire seperately";
                     x.IsInline = false;
                  });
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

            if (userToAddGuests == null) {
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raidName}";
                  x.Value = $"Added {guests} guests";
                  x.IsInline = false;
               });
            }
            else {
               result.ReferenceUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.ReferenceUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raidName}";
                  x.Value = $"Added {guests} guests for {userToAddGuests.Username}";
                  x.IsInline = false;
               });
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raidName}";
                  x.Value = $"{requestUser} has added {guests} guests to the raid for you.";
                  x.IsInline = false;
               });
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
            result.RequesterUserBuilder.WithThumbnailUrl(string.Format(ConfigVariables.PokemonIconURL, raid.RaidBossId));
            result.RequesterUserBuilder.AddField(x => {
               x.Name = $"Raid: {raid.ToString()}";
               x.Value = raid.ToStringUsers();
               x.IsInline = false;
            });
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


      #region Private Methods


      private bool AddUserToRaidIfNotExists(Raid raid, IUser user, int guests) {
         if (raid.Users.Any(a => a.Equals(User.FromIUser(user)))) {
            return false;
         }
         else {
            raid.Users.Add(User.FromIUser(user, guests));
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
               result.ReferenceUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.ReferenceUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raid.Name}";
                  x.Value = $"You have been removed from the raid by {requesterUser.Username}";
                  x.IsInline = false;
               });
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raid.Name}";
                  x.Value = $"You have removed {referenceUser.Username} from the raid";
                  x.IsInline = false;
               });
            }
            else {
               result.RequesterUserBuilder = EmbedBuilderHelper.GreenBuilder();
               result.RequesterUserBuilder.AddField(x => {
                  x.Name = $"Raid: {raid.Name}";
                  x.Value = "You have been removed from the raid";
                  x.IsInline = false;
               });
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
