using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot.Helpers {
   public static class ConfigVariables {
      public static string DebugToken => GetAppConfig("Token.Debug");
      public static string ReleaseToken => GetAppConfig("Token.Release");
      public static string PokemonIconURL => GetAppConfig("PokemonIconURL");
      private static string GetAppConfig(string key) => ConfigurationManager.AppSettings[key];
   }
}
