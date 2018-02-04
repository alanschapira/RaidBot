using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using RaidBot.BusinessLogic.PermissionStorage;
using RaidBot.BusinessLogic.PermissionStorage.Interfaces;
using RaidBot.BusinessLogic.Raids;
using RaidBot.Entities;
using RaidBot.Helpers;

namespace RaidBot.BusinessLogic.Modules {
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

      [Command("create"), Summary("Creates a Raid")]
      [Alias("add", "new")]
      public async Task Create(
         [Summary("The name of the raid to create")] string raidName) {
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

      [Command("get"), Summary("Get a list of all raids")]
      [Alias("info", "raids")]
      public async Task Get() {
         var result = _raidService.GetRaids();
         await ReplyAsync("", false, result.Build());
      }

      [Command("get"), Summary("Gets the details of a specific Raid")]
      [Alias("info", "raid")]
      public async Task Get([Summary("The name of the raid to get details about")] string raidName) {

         var result = _raidService.GetSpecificRaid(raidName);
         if (result.Success) {
            await ReplyAsync("", false, result.RequesterUserBuilder.Build());
         }
         else {
            var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
         }
      }

      [Command("join"), Summary("Joins a specific Raid")]
      [Alias("attend", "adduser")]
      public async Task Join([Summary("The name of the raid to join")] string raidName, [Summary("Number of guests")] int guests = 0) {

         var result = _raidService.JoinRaid(raidName, Context.User, guests);
         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
         await dmChannel.SendMessageAsync("", false, result.RequesterUserBuilder.Build());
      }

      [Command("join"), Summary("Adds a user to a raid")]
      [Alias("attend", "adduser")]
      public async Task Join([Summary("The name of the raid to join")] string raidName, [Summary("Mention the user you want to add")] IUser user, [Summary("Number of guests")] int guests = 0) {

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

      [Command("delete"), Summary("Deletes a specific Raid")]
      [Alias("remove", "destroy")]
      public async Task Delete([Summary("Name of the raid to delete")] string raidName) {

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

      [Command("leave"), Summary("Leaves a specific Raid")]
      [Alias("unattend")]
      public async Task Leave([Summary("The name of the raid to leave")] string raidName, IUser user = null) {

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

      [Command("myRaids"), Summary("Gets the name of the raids you are attending")]
      [Alias("me")]
      public async Task MyRaids(IUser user = null) {

         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
         var result = user == null ? _raidService.MyRaids(Context.User) : _raidService.MyRaids(user);
         await dmChannel.SendMessageAsync("", false, result.Build());
      }

      [Command("addGuests"), Summary("Add guests to an attendee")]
      [Alias("addGuest")]
      public async Task AddGuests([Summary("The name of the raid to add a guest to")] string raidName, int guests, IUser user = null) {

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

      [Command("changeTime"), Summary("Changes the time of a raid")]
      [Alias("Time", "RaidTime")]
      public async Task ChangeTime([Summary("The name of the raid to change the time")] string raidName, [Summary("The new time of the raid")] string raidTime) {
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

      [Command("changeName"), Summary("Changes the name of a raid")]
      [Alias("Name", "RaidName")]
      public async Task ChangeName([Summary("The name of the raid to change the name")] string raidName, [Summary("The new name of the raid")] string newRaidName) {
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

      [Command("message"), Summary("Message everyone in a raid")]
      public async Task Message([Summary("The name of the raid to message")] string raidName, [Summary("The message"), Remainder] string message) {
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
