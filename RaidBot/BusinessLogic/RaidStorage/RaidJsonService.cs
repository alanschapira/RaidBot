using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using static System.Environment.SpecialFolder;
using static System.Environment;
using RaidBot.Entities;
using RaidBot.BusinessLogic.RaidStorage.Interfaces;
using RaidBot.Helpers;

namespace RaidBot.BusinessLogic.RaidStorage {
   public class RaidJsonService : IRaidFileService {
      string serializationFile;
      ServerSettings _serverSettings;

      public RaidJsonService(string guildId, ServerSettings serverSettings) {
         _serverSettings = serverSettings;
         serializationFile = Path.Combine(GetFolderPath(MyDocuments), $"{guildId}.json");
      }

      public List<Raid> GetRaidsFromFile() {
         string json;
         JsonHelper.CreateFileIfNotExists(serializationFile);
         using (StreamReader stream = new StreamReader(serializationFile)) {
            json = stream.ReadToEnd();
         }
         if (json.Length == 0) {
            return new List<Raid>();
         }
         List<Raid> raids = JsonConvert.DeserializeObject<List<Raid>>(json);

         var aliveRaids = DeleteExpiredRaids(raids);

         aliveRaids = ChangeOrder(aliveRaids);

         return raids;
      }

      private List<Raid> ChangeOrder(List<Raid> aliveRaids) {
         return aliveRaids
            .OrderByDescending(a => !a.Day.HasValue)
            .ThenBy(a => a.Day)
            .ThenByDescending(a => !a.Time.HasValue)
            .ThenBy(a => a.Time)
            .ToList();
      }

      private List<Raid> DeleteExpiredRaids(List<Raid> raids) {
         var aliveRaids = raids.Where(a => (DateTime.Now - a.ExpireStart).TotalMinutes < a.Expire.TotalMinutes);
         if (_serverSettings.JoinRaidOnCreate) {
            aliveRaids = aliveRaids.Where(a => a.Users.Count() > 0);
         }
         if (aliveRaids.Count() < raids.Count()) {
            PushRaidsToFile(aliveRaids.ToList());
         }
         return aliveRaids.ToList();
      }

      public void PushRaidsToFile(List<Raid> raids) {
         using (StreamWriter stream = File.CreateText(serializationFile)) {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(stream, raids);
         }
      }
   }
}
