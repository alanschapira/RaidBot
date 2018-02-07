using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot.Entities {
   public class ServerSettings {
      public ServerSettings() {
         JoinRaidOnCreate = true;
         AutoExpireMins = 120;
      }
      public GuildPermission? AdminRights { get; set; }
      public bool JoinRaidOnCreate { get; set; }
      public int TimeZone { get; set; }
      public int AutoExpireMins { get; set; }

      #region Methods
      public override string ToString() {
         StringBuilder builder = new StringBuilder();

         Type myType = GetType();
         IList<PropertyInfo> props = new List<PropertyInfo>(myType.GetProperties());

         foreach (PropertyInfo prop in props) {
            var name = prop.Name;
            var value = prop.GetValue(this, null) ?? "None";

            builder.Append($"{name}: {value}\n");
         }

         return builder.ToString();
      }
      #endregion
   }
}
