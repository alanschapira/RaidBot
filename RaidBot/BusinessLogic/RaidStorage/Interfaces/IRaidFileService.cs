using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using RaidBot.Entities;

namespace RaidBot.BusinessLogic.RaidStorage.Interfaces {
   public interface IRaidFileService {
      List<Raid> GetRaidsFromFile();
      void PushRaidsToFile(List<Raid> raids);
   }
}
