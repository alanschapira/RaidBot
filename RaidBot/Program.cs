using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Globalization;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using RaidBot.Helpers;

namespace RaidBot {
   class Program {
      public static void Main(string[] args)
         => new Program().MainAsync().GetAwaiter().GetResult();

      public CommandService _commandService;
      private DiscordSocketClient _client;
      private IServiceProvider _services;

      public async Task MainAsync() {
         _client = new DiscordSocketClient(new DiscordSocketConfig {
            WebSocketProvider = Discord.Net.Providers.WS4Net.WS4NetProvider.Instance
         });
         _commandService = new CommandService();
#if DEBUG
         string token = "debug bot token goes here";
#else
         string token = "release bot token goes here";
#endif

         _services = new ServiceCollection()
                .BuildServiceProvider();

         await InstallCommands();

         await _client.LoginAsync(TokenType.Bot, token);
         await _client.StartAsync();

         // Block this task until the program is closed.
         await Task.Delay(-1);
      }

      private async Task InstallCommands() {
         _client.MessageReceived += MessageReceived;
         _client.Log += Log;

         await _commandService.AddModulesAsync(Assembly.GetEntryAssembly());
      }

      private async Task MessageReceived(SocketMessage messageParam) {
         var message = messageParam as SocketUserMessage;
         if (message == null) return;

         int argPos = 0;

         if (!(message.HasCharPrefix('$', ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos))) {
            return;
         }

         var context = new CommandContext(_client, message);

         var result = await _commandService.ExecuteAsync(context, argPos, _services);

         if (!result.IsSuccess) {
            string errorMessage;
            switch (result.ErrorReason) {
               case "User requires guild permission Administrator":
                  errorMessage = "Only an admin can do that";
                  break;
               default:
                  errorMessage = "Something was wrong with your request. Please try again. You can use `$help` to see instructions";
                  break;
            }
            var buider = EmbedBuilderHelper.ErrorBuilder(errorMessage);
            var dmChannel = await context.User.GetOrCreateDMChannelAsync();
            await dmChannel.SendMessageAsync("", false, buider.Build());
         }
      }

      private Task Log(LogMessage msg) {
         Console.WriteLine(msg.ToString());
         return Task.CompletedTask;
      }
   }
}
