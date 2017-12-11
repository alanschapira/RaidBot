using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

namespace RaidBot.BusinessLogic.Modules {
   [Name("Help")]
   public class HelpModule : ModuleBase {
      private CommandService _service;
      public HelpModule(CommandService service) {
         _service = service;
      }

      [Command("echo"), Summary("Echos a message.")]
      [Alias("say", "repeat")]
      public async Task Echo([Remainder, Summary("The text to echo")] string message) {
         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();
         await dmChannel.SendMessageAsync(message);
      }

      [Command("help"), Summary("Shows a list of all available commands per module.")]
      [Alias("command", "commands")]
      public async Task Commands() {
         var dmChannel = await Context.User.GetOrCreateDMChannelAsync();

         string prefix = "$";  /* put your chosen prefix here */
         var builder = new EmbedBuilder() {
            Color = new Color(114, 137, 218),
            Description = "These are the commands you can use:"
         };

         foreach (var module in _service.Modules.OrderByDescending(a => a.Name)) /* we are now going to loop through the modules taken from the service we initiated earlier ! */
         {

            var user = Context.User as IGuildUser;
            if (!user.GuildPermissions.Has(GuildPermission.Administrator) && module.Name == "Server Settings") {
               continue;
            }

            string description = null;
            foreach (var cmd in module.Commands) /* and now we loop through all the commands per module aswell, oh my! */
            {
               var result = await cmd.CheckPreconditionsAsync(Context); /* gotta check if they pass */
               if (result.IsSuccess) {
                  var parameters = cmd.Parameters.Count() == 0? "" : string.Join(" ", cmd.Parameters.Select(p => $"<{p.Name}{(p.IsOptional? "(optional)" : string.Empty)}>"));

                  description += $"{prefix}{cmd.Aliases.First()} {parameters}\n" +
                     $"\tSummary: { cmd.Summary }\n\n";
               }
            }

            if (!string.IsNullOrWhiteSpace(description)) /* if the module wasn't empty, we go and add a field where we drop all the data into! */
            {
               builder.AddField(x => {
                  x.Name = module.Name;
                  x.Value = description;
                  x.IsInline = false;
               });
            }
         }
         builder.AddField(x => {
            x.Name = "RaidBot Server";
            x.Value = "Please come visit for questions, updates and testing https://discord.gg/d4KHHq8";
            x.IsInline = false;
         });
         await dmChannel.SendMessageAsync("", false, builder.Build()); /* then we send it to the user. */
      }

   }
}
