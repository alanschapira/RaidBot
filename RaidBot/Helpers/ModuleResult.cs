using Discord;
using RaidBot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot.Helpers {
   public class ModuleResult {
      public bool Success { get; set; }
      public EmbedBuilder ReferenceUserBuilder { get; set; }
      public EmbedBuilder RequesterUserBuilder { get; set; }
      public List<User> Users { get; set; }
   }
}
