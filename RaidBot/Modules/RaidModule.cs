using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RaidBot.BusinessLogic.PermissionStorage;
using RaidBot.BusinessLogic.PermissionStorage.Interfaces;
using RaidBot.BusinessLogic.Raids;
using RaidBot.Entities;
using RaidBot.Helpers;

namespace RaidBot.Modules {
   [Name("Raid")]
   public class RaidModule : ModuleBase {

      RaidService _raidService;
      IPermissionService _permissionService;
      ServerSettings _serverPermissions;

      protected override void BeforeExecute(CommandInfo command) {
         _permissionService = new PermissionBlobService("permission" + Context.Guild.Id.ToString());
         _serverPermissions = _permissionService.GetSettings();
         _raidService = new RaidService($"raid{Context.Guild.Id.ToString()}", _serverPermissions);
      }

      [Command("Get"), Summary("Gets all raids")]
      [Alias("Info", "Raids")]
      public async Task Get() {
         var result = _raidService.GetRaids();
         await ReplyAsync("", false, result.Build());
      }

      [Command("Get"), Summary("Gets the details of a Raid")]
      [Alias("Info", "Raid")]
      public async Task Get([Summary("The name of the raid")] string raidName) {

         var result = _raidService.GetSpecificRaid(raidName);
         if (result.Success) {
            await ReplyAsync("", false, result.RequesterUserBuilder.Build());
         }
         else {
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
         }
      }

      [Command("Join"), Summary("Joins a Raid")]
      [Alias("Attend")]
      public async Task Join([Summary("The name of the raid")] string raidName, [Summary("Number of guests")] int guests = 0) {

         var result = _raidService.JoinRaid(raidName, Context.User, guests);
         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
         await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
      }

      [Command("Join"), Summary("Adds a user to a raid")]
      [Alias("Attend", "AddUser")]
      public async Task Join([Summary("The name of the raid")] string raidName, [Summary("Mention the user you want to add")] IUser user, [Summary("Number of guests")] int guests = 0) {

         var requesterUser = Context.User as IGuildUser;
         if (await CheckPermission(user, requesterUser, _serverPermissions)) {
            var result = _raidService.JoinRaid(raidName, user, guests, Context.User);

            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());

            if (result.Success) {
               var dmChannelRequester = await user.GetOrCreateDMChannelAsync();
               await dmChannelRequester.SendMessageAsync("", false, result.ReferenceUserBuilder.Build());
            }
         }
      }

      [Command("Leave"), Summary("Leaves a Raid")]
      [Alias("Unattend")]
      public async Task Leave([Summary("The name of the raid")] string raidName, IUser user = null) {

         var requesterUser = Context.User as IGuildUser;
         if (await CheckPermission(user, requesterUser, _serverPermissions)) {
            var result = _raidService.LeaveRaid(raidName, Context.User, user);
            var dmChannel = await requesterUser.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
            if (user != null) {
               var dmChannelReferenceUser = await user.GetOrCreateDMChannelAsync();
               await dmChannelReferenceUser.SendMessageAsync("", false, result.ReferenceUserBuilder.Build());
            }
         }
      }



      [Command("MyRaids"), Summary("Gets your raids")]
      [Alias("Me")]
      public async Task MyRaids(IUser user = null) {

         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
         var result = user == null ? _raidService.MyRaids(Context.User) : _raidService.MyRaids(user);
         await dmChannel.SendMessageAsync("", false, result.Build());
      }

      [Command("AddGuests"), Summary("Add guests to youself or another")]
      [Alias("AddGuest", "Guest", "Guests")]
      public async Task AddGuests([Summary("The name of the raid")] string raidName, int guests, IUser user = null) {

         ModuleResult result;
         var requesterUser = Context.User as IGuildUser;
         if (await CheckPermission(user, requesterUser, _serverPermissions)) {
            if (user == null) {
               result = _raidService.AddGuests(raidName, Context.User, guests);
            }
            else {
               result = _raidService.AddGuests(raidName, user, guests, Context.User);
            }

            var dmChannel = await requesterUser.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());

            if (result.Success && user != null) {
               var dmChannelReferenceUser = await user.GetOrCreateDMChannelAsync();
               await dmChannelReferenceUser.SendMessageAsync("", false, result.ReferenceUserBuilder.Build());
            }
         }
      }

      [Command("Create"), Summary("Creates a Raid")]
      [Alias("Add", "New")]
      public async Task Create(
         [Summary("The name of the raid")] string raidName) {
         var user = Context.User as IGuildUser;

         if (await CheckPermission(user, _serverPermissions)) {
            var result = _raidService.CreateRaid(raidName, Context.User);

            if (result.Success) {
               await ReplyAsync("", false, result.RequesterUserBuilder.Build());
            }
            else {
               var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
               await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
            }
         }
      }

      [Command("Create"), Summary("Creates a Raid")]
      [Alias("Add", "New")]
      public async Task Create([Summary("The name of the raid")] string raidName, [Summary("The time of the raid")] string raidTime, [Summary("The name or id of the raid boss")]string raidBoss, [Summary("The number of guests to add to the initial attendee (if autojoin is set to on)")]int guests = 0) {
         var user = Context.User as IGuildUser;

         if (await CheckPermission(user, _serverPermissions)) {
            var result = _raidService.CreateRaid(raidName, raidTime, raidBoss, Context.User, guests);

            if (result.Success) {
               await ReplyAsync("", false, result.RequesterUserBuilder.Build());
            }
            else {
               var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
               await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
            }
         }
      }


      [Command("Pokemon"), Summary("Changes the pokemon")]
      [Alias("Boss", "Raidboss")]
      public async Task RaidBoss([Summary("The name of the raid")] string raidName, [Summary("The name or id of the pokemon")] string pokemonName) {
         var user = Context.User as IGuildUser;

         if (await CheckPermission(user, _serverPermissions)) {
            var result = _raidService.AddPokemon(raidName, pokemonName, user);

            if (result.Success) {
               await ReplyAsync("", false, result.RequesterUserBuilder.Build());
            }
            else {
               var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
               await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
            }
         }
      }

      [Command("Time"), Summary("Changes the time of a raid")]
      [Alias("ChangeTime", "RaidTime")]
      public async Task ChangeTime([Summary("The name of the raid")] string raidName, [Summary("The new time of the raid")] string raidTime) {
         var requesterUser = Context.User as IGuildUser;
         var result = _raidService.ChangeTime(raidName, raidTime, requesterUser);
         if (result.Success) {
            MessageAllUsers(result);
         }
         else {
            var dmChannel = await requesterUser.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
         }
      }

      [Command("Date"), Summary("Changes the date of a raid")]
      [Alias("ChangeDate", "RaidDate")]
      public async Task ChangeDate([Summary("The name of the raid")] string raidName, [Summary("The date of the raid - format must be yyyy-mm-dd")] string raidDate) {
         var requesterUser = Context.User as IGuildUser;
         var result = _raidService.ChangeDate(raidName, raidDate, requesterUser);
         if (result.Success) {
            MessageAllUsers(result);
         }
         else {
            var dmChannel = await requesterUser.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
         }
      }

      [Command("Expire"), Summary("Changes the mins until raid expires")]
      [Alias("Extend", "ChangeExpire")]
      public async Task ChangeExpire([Summary("The name of the raid")] string raidName, [Summary("The amount of mins until expire")] int expireMins) {
         var requesterUser = Context.User as IGuildUser;
         var result = _raidService.ChangeExpire(raidName, expireMins, requesterUser);
         if (result.Success) {
            MessageAllUsers(result);
         }
         else {
            var dmChannel = await requesterUser.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
         }
      }

      [Command("Name"), Summary("Changes the name of a raid")]
      [Alias("ChangeName", "RaidName")]
      public async Task ChangeName([Summary("The name of the raid")] string raidName, [Summary("The new name of the raid")] string newRaidName) {
         var requesterUser = Context.User as IGuildUser;
         var result = _raidService.ChangeName(raidName, newRaidName, requesterUser);
         if (result.Success) {
            MessageAllUsers(result);
         }
         else {
            var dmChannel = await requesterUser.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
         }
      }

      [Command("Message"), Summary("Message everyone in a raid")]
      public async Task Message([Summary("The name of the raid")] string raidName, [Summary("The message"), Remainder] string message) {
         var requesterUser = Context.User as IGuildUser;
         var result = _raidService.MessageUsers(raidName, message);
         if (result.Success) {
            MessageAllUsers(result);
         }
         else {
            var dmChannel = await requesterUser.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
         }
      }


      [Command("Delete"), Summary("Deletes a Raid")]
      [Alias("Remove", "Destroy")]
      public async Task Delete([Summary("The name of the raid")] string raidName) {

         var user = Context.User as IGuildUser;
         if (await CheckPermission(user, _serverPermissions)) {
            var result = _raidService.DeleteRaid(raidName, user);
            if (result.Success) {
               MessageAllUsers(result);
            }
            else {
               var dmChannel = await user.GetOrCreateDMChannelAsync();
               await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
            }
         }
      }

      #region Private Methods
      private async void MessageAllUsers(ModuleResult result) {
         var mentions = new StringBuilder();
         foreach (var user in result.Users) {
            mentions.Append($"{user.Mention} ");
         }
         await ReplyAsync(mentions.ToString(), false, result.RequesterUserBuilder.Build());
      }

      private async Task<bool> CheckPermission(IUser mentionedUser, IGuildUser requesterUser, ServerSettings serverPermissions) {
         var application = await Context.Client.GetApplicationInfoAsync();
         if (mentionedUser != null && serverPermissions.AdminRights != null && !requesterUser.GuildPermissions.Has(serverPermissions.AdminRights.Value) && requesterUser.Id != application.Owner.Id) {
            var dmChannelUser = await requesterUser.GetOrCreateDMChannelAsync();
            EmbedBuilder builder = EmbedBuilderHelper.ErrorBuilder("You do not have permission to do that!");
            await dmChannelUser.SendMessageAsync("", false, builder.Build());
            return false;
         }
         return true;
      }

      private async Task<bool> CheckPermission(IGuildUser requesterUser, ServerSettings serverPermissions) {
         var application = await Context.Client.GetApplicationInfoAsync();
         if (serverPermissions.AdminRights != null && !requesterUser.GuildPermissions.Has(serverPermissions.AdminRights.Value) && requesterUser.Id != application.Owner.Id) {
            var dmChannelUser = await requesterUser.GetOrCreateDMChannelAsync();
            EmbedBuilder builder = EmbedBuilderHelper.ErrorBuilder("You do not have permission to do that!");
            await dmChannelUser.SendMessageAsync("", false, builder.Build());
            return false;
         }
         return true;
      }
      #endregion
   }
}
