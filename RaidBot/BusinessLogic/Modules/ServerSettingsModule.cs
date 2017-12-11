using Discord;
using Discord.Commands;
using RaidBot.Entities;
using RaidBot.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RaidBot.BusinessLogic.PermissionStorage;
using RaidBot.BusinessLogic.PermissionStorage.Interfaces;

namespace RaidBot.BusinessLogic.Modules {
   [Name("Server Settings")]
   [RequireUserPermission(GuildPermission.Administrator, Group = "Permission"), RequireOwner(Group = "Permission")]
   public class ServerSettingsModule : ModuleBase {
      IPermissionService _permissionService;

      protected override void BeforeExecute(CommandInfo command) {
         _permissionService = new PermissionBlobService("permission" + Context.Guild.Id.ToString());
      }

      [Command("AdminPermission"), Summary("Choose the required permission to create raids")]
      public async Task AdminPermission([Summary("The permission level for raid creation")] string permissionLevel) {

         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

         GuildPermission? permission;
         if (permissionLevel.Equals("off", StringComparison.CurrentCultureIgnoreCase)) {
            permission = null;
         }
         else {
            permission = (GuildPermission)Enum.Parse(typeof(GuildPermission), permissionLevel, true);
         }
         //Check if it failed and return the list of settings you can use

         _permissionService.SetAdminRights(permission);

         var builder = EmbedBuilderHelper.GreenBuilder();

         builder.AddField(x => {
            x.Name = "Setting: Create Raid Permission";
            x.Value = $"Setting has been changed to: {permissionLevel}";
            x.IsInline = false;
         });

         await dmChannel.SendMessageAsync("", false, builder.Build());
      }

      [Command("JoinRaidOnCreate"), Summary("Choose if creating a raid should add the creator automatically")]
      [Alias("JoinRaidOnCreatePermission")]
      public async Task JoinRaidOnCreate([Summary("True or false")] bool shouldJoin) {

         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

         _permissionService.SetJoinRaidOnCreatePermission(shouldJoin);

         var builder = EmbedBuilderHelper.GreenBuilder();

         builder.AddField(x => {
            x.Name = "Setting: Should Join Raid On Raid Creation";
            x.Value = $"Setting has been changed to: {shouldJoin}";
            x.IsInline = false;
         });

         await dmChannel.SendMessageAsync("", false, builder.Build());
      }

      [Command("TimeZone"), Summary("Set the TimeZone you are located using + or - e.g. -4")]
      public async Task SetTimeZone([Summary("TimeZone you are located using + or - e.g. -4")] int timeZone) {

         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

         _permissionService.SetTimeZone(timeZone);

         var builder = EmbedBuilderHelper.GreenBuilder();

         builder.AddField(x => {
            x.Name = "Setting: TimeZone";
            x.Value = $"Setting has been changed to: {timeZone}";
            x.IsInline = false;
         });

         await dmChannel.SendMessageAsync("", false, builder.Build());
      }

      [Command("AutoDeleteRaid"), Summary("Amount of minutes after a raid has finished you want it to auto delete")]
      public async Task AutoDeleteRaid([Summary("Minutes")] int mins) {

         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

         _permissionService.SetAutoDeleteRaid(mins);

         var builder = EmbedBuilderHelper.GreenBuilder();

         builder.AddField(x => {
            x.Name = "Setting: AutoDeleteRaid";
            x.Value = $"Setting has been changed to: {mins} mins";
            x.IsInline = false;
         });

         await dmChannel.SendMessageAsync("", false, builder.Build());
      }

      [Command("GetSettings"), Summary("Get the current server settings")]
      public async Task GetSettings() {

         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

         ServerSettings settings = _permissionService.GetSettings();

         var builder = EmbedBuilderHelper.GreenBuilder();


         builder.AddField(x => {
            x.Name = "Current Settings:";
            x.Value = settings.ToString();
            x.IsInline = false;
         });

         await dmChannel.SendMessageAsync("", false, builder.Build());
      }
   }
}
