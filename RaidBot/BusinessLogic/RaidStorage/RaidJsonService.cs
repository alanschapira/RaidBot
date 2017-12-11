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

         var aliveRaids = raids.Where(a => a.CreateDateTime > DateTime.Now.AddHours(-3)).Where(a => a.Users.Count() > 0);
         if (aliveRaids.Count() < raids.Count()) {
            PushRaidsToFile(aliveRaids.ToList());
            return aliveRaids.ToList();
         }
         return raids;
      }

      public void PushRaidsToFile(List<Raid> raids) {
         using (StreamWriter stream = File.CreateText(serializationFile)) {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(stream, raids);
         }
      }
   }
}
