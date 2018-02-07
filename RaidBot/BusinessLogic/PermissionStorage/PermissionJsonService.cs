using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;
using RaidBot.BusinessLogic.PermissionStorage.Interfaces;
using RaidBot.Entities;
using static System.Environment.SpecialFolder;
using static System.Environment;
using RaidBot.Helpers;

namespace RaidBot.BusinessLogic.PermissionStorage {
   public class PermissionJsonService : IPermissionService {
      string serializationFile;

      public PermissionJsonService(string fileName) {
         serializationFile = Path.Combine(GetFolderPath(MyDocuments), $"{fileName}.json");
      }

      public ServerSettings GetSettings() {
         JsonHelper.CreateFileIfNotExists(serializationFile);
         string json;
         using (StreamReader stream = new StreamReader(serializationFile)) {
            json = stream.ReadToEnd();
         }
         if (json.Length == 0) {
            return new ServerSettings();
         }
         ServerSettings permissions = JsonConvert.DeserializeObject<ServerSettings>(json);
         return permissions;
      }

      public void SetAdminRights(GuildPermission? permission) {
         var permissions = GetSettings();
         permissions.AdminRights = permission;
         SetPermissions(permissions);
      }

      public void SetAutoDeleteRaid(int mins) {
         var permissions = GetSettings();
         permissions.AutoExpireMins = mins;
         SetPermissions(permissions);
      }

      public void SetJoinRaidOnCreatePermission(bool permission) {
         var permissions = GetSettings();
         permissions.JoinRaidOnCreate = permission;
         SetPermissions(permissions);
      }

      private void SetPermissions(ServerSettings permissions) {
         using (StreamWriter stream = File.CreateText(serializationFile)) {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Serialize(stream, permissions);
         }
      }
   }
}
