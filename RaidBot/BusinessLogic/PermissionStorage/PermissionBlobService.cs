using Discord;
using Newtonsoft.Json;
using RaidBot.Entities;
using RaidBot.BusinessLogic.PermissionStorage.Interfaces;
using RaidBot.BusinessLogic.BlobStorage;

namespace RaidBot.BusinessLogic.PermissionStorage {
   public class PermissionBlobService : BlobService, IPermissionService {
      public PermissionBlobService(string fileName): base(fileName) {      }
      
      public void SetAdminRights(GuildPermission? permission) {
         var permissions = GetSettings();
         permissions.AdminRights = permission;
         SetPermissions(permissions);
      }
      
      public void SetJoinRaidOnCreatePermission(bool permission) {
         var permissions = GetSettings();
         permissions.JoinRaidOnCreate = permission;
         SetPermissions(permissions);
      }

      public void SetTimeZone(int timeZone) {
         var permissions = GetSettings();
         permissions.TimeZone = timeZone;
         SetPermissions(permissions);
      }
      public void SetAutoDeleteRaid(int mins) {
         var permissions = GetSettings();
         permissions.AutoDeleteRaid = mins;
         SetPermissions(permissions);
      }

      public ServerSettings GetSettings() {
         var json = _blockBlob.DownloadText();
         if (string.IsNullOrEmpty(json)) {
            return null;
         }

         ServerSettings permission = JsonConvert.DeserializeObject<ServerSettings>(json);
         return permission;
      }
      private void SetPermissions(ServerSettings permissions) {
         string json = JsonConvert.SerializeObject(permissions);
         _blockBlob.UploadText(json);
      }
   }
}
