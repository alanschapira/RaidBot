using Discord;
using RaidBot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot.Helpers {
   public static class EmbedBuilderHelper {
      public static EmbedBuilder ErrorBuilder(string errorMessage) {
         var builder = new EmbedBuilder() {
            Color = new Color(255, 0, 51),
         };

         builder.AddField(x => {
            x.Name = "The following error occured:";
            x.Value = errorMessage;
            x.IsInline = false;
         });

         return builder;

      }

      public static EmbedBuilder GreenBuilder() {
         return new EmbedBuilder() {
            //Blue: Color = new Color(114, 137, 218),
            Color = new Color(0, 255, 0),
         };
      }

      public static EmbedBuilder BlueBuilder() {
         return new EmbedBuilder() {
            Color = new Color(114, 137, 218),
         };
      }
      public static EmbedBuilder RaidEmbedBuilder(string header, IEnumerable<Raid> raids) {
         StringBuilder result = new StringBuilder();
         foreach (var raid in raids) {
            result.Append(raid.ToString() + "\n");
         }

         EmbedBuilder builder = BlueBuilder();
         builder.AddField(x => {
            x.Name = header;
            x.Value = result.ToString();
            x.IsInline = false;
         });

         return builder;
      }

   }
}
