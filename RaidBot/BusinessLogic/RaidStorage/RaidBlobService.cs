using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RaidBot.BusinessLogic.BlobStorage;
using RaidBot.BusinessLogic.RaidStorage.Interfaces;
using RaidBot.Entities;

namespace RaidBot.BusinessLogic.RaidStorage {
   public class RaidBlobService : BlobService, IRaidFileService {
      ServerSettings _serverSettings;

      public RaidBlobService(string guildId, ServerSettings serverSettings) : base(guildId) {
         _serverSettings = serverSettings;
      }
      
      public List<Raid> GetRaidsFromFile() {
         var json = _blockBlob.DownloadText();
         List<Raid> raids = JsonConvert.DeserializeObject<List<Raid>>(json);

         var aliveRaids = raids.Where(a => a.Time?.AddMinutes(_serverSettings.AutoDeleteRaid) > DateTime.Now.AddHours(_serverSettings.TimeZone));
         if (_serverSettings.JoinRaidOnCreate) {
            aliveRaids = aliveRaids.Where(a => a.Users.Count() > 0);
         }
         if (aliveRaids.Count() < raids.Count()) {
            PushRaidsToFile(aliveRaids.ToList());
            return aliveRaids.OrderBy(a => a.Time).ToList();
         }
         return raids;
      }

      public void PushRaidsToFile(List<Raid> raids) {
         string json = JsonConvert.SerializeObject(raids);
         _blockBlob.UploadText(json);
      }
   }
}
