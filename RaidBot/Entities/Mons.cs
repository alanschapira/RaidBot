using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Globalization;
using PokeApiNet;

namespace RaidBot.Entities {
   public static class Mons {

      // Gets a mon's id, name and sprite URL
      public static async Task<(int, string, string)> GetMonInfo(string nameOrId) {
         PokeApiClient pokeClient = new PokeApiClient();

         int id;
         string name;
         string spriteUrl = null;
         TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

         Pokemon mon;
         if (int.TryParse(nameOrId, out id)) {
            mon = await pokeClient.GetResourceAsync<Pokemon>(id);
         }
         else {
            name = nameOrId.ToLower();
            if (GetEggs().ContainsKey(name)) {
               (id, spriteUrl) = GetEggs()[name];
               return (id, textInfo.ToTitleCase(name), spriteUrl);
            }
            else {
               mon = await pokeClient.GetResourceAsync<Pokemon>(name);
            }
         }

         try {
            string url = mon.Forms[0].Url;
            string[] urlSplit = url.Split('/');
            int formNumber = int.Parse(urlSplit[urlSplit.Length - 2]);

            PokemonForm form = await pokeClient.GetResourceAsync<PokemonForm>(formNumber);

            spriteUrl = form.Sprites.FrontDefault;
         }
         catch (Exception e) {
            // Do not error on missing sprite
         }

         return (mon.Id, textInfo.ToTitleCase(mon.Name), spriteUrl);
      }

      private static Dictionary<string, (int, string)> GetEggs() {
         return new Dictionary<string, (int, string)>() {
            {"level1", (
               -1,
               "https://raw.githubusercontent.com/PokeMiners/pogo_assets/master/Images/Raids/raid_egg_0_icon_notification.png"
            )},
            {"level3", (
               -3,
               "https://raw.githubusercontent.com/PokeMiners/pogo_assets/master/Images/Raids/raid_egg_1_icon_notification.png"
            )},
            {"level5", (
               -5,
               "https://raw.githubusercontent.com/PokeMiners/pogo_assets/master/Images/Raids/raid_egg_2_icon_notification.png"
            )},
            {"mega", (
               -10,
               "https://raw.githubusercontent.com/PokeMiners/pogo_assets/master/Images/Raids/raid_egg_3_icon_notification.png"
            )}
         };
      }
   }
}
