using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using RaidBot.Entities;

namespace RaidBot.BusinessLogic.PermissionStorage.Interfaces {
   public interface IPermissionService {
      void SetAdminRights(GuildPermission? permission);
      void SetJoinRaidOnCreatePermission(bool permission);
      void SetAutoDeleteRaid(int mins);
      ServerSettings GetSettings();
   }
}
