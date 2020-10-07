using System.Configuration;

namespace RaidBot.Helpers {
   public static class ConfigVariables {
      public static string DebugToken => GetAppConfig("Token.Debug");
      public static string ReleaseToken => GetAppConfig("Token.Release");
      private static string GetAppConfig(string key) => ConfigurationManager.AppSettings[key];
   }
}
