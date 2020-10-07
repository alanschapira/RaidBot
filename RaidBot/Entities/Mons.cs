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
               name = GetCorrectName(name);
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

      private static string GetCorrectName(string name) {
         string newName = name;

         var forms = new Dictionary<string, string>() {
            {"mega", "mega"},
            {"alolan", "alola"},
            {"galarian", "galar"},
            {"alola", "alola"},
            {"galar", "galar"},
            {"zen", "zen"},
            {"normal", "normal"},
            {"speed", "speed"},
            {"attack", "attack"},
            {"defense", "defense"},
            {"defence", "defense"},
            {"altered", "altered"},
            {"rainy", "rainy"},
            {"snowy", "snowy"},
            {"sunny", "sunny"},
            {"black", "black"},
            {"white", "white"},
            {"ordinary", "ordinary"},
            {"resolute", "resolute"},
            {"therian", "therian"},
            {"incarnate", "incarnate"}
         };
         var otherNames = new Dictionary<string, string>() {
            {"mega-charizard-x", "charizard-mega-x"},
            {"mega-charizard-y", "charizard-mega-y"},
            {"mega-charizard", "charizard-mega-x"},
            {"charizard-mega", "charizard-mega-x"},
            {"mega-mewtwo-x", "mewtwo-mega-x"},
            {"mega-mewtwo-y", "mewtwo-mega-y"},
            {"mega-mewtwo", "mewtwo-mega-x"},
            {"mewtwo-mega", "mewtwo-mega-x"},
            {"giratina", "giratina-altered"},
            {"deoxys", "deoxys-normal"},
            {"darmanitan", "darmanitan-standard"},
            {"tornadus", "tornadus-incarnate"},
            {"thundurus", "thundurus-incarnate"},
            {"landorus", "landorus-incarnate"},
            {"rainy", "rainy"},
            {"snowy", "snowy"},
            {"sunny", "sunny"}
         };

         if (otherNames.ContainsKey(newName)) {
            return otherNames[newName];
         }

         string[] nameSplit = newName.Split('-');
         if (forms.ContainsKey(nameSplit[0])) {
            newName = nameSplit[1] + '-' + forms[nameSplit[0]];
         }

         return newName;
      }
   }
}
