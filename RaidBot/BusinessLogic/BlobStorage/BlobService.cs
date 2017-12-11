using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using RaidBot.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot.BusinessLogic.BlobStorage {
   public class BlobService {

      CloudStorageAccount _storageAccount;
      CloudBlobClient _blobClient;
      CloudBlobContainer _container;
      public CloudBlockBlob _blockBlob;

      public BlobService(string guildId) {
         _storageAccount = CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting("StorageConnectionString"));
         _blobClient = _storageAccount.CreateCloudBlobClient();
         _container = _blobClient.GetContainerReference("raidbotcontainer");
         _container.CreateIfNotExists();
         _blockBlob = _container.GetBlockBlobReference(guildId);
         if (!_blockBlob.Exists()) {
            if (guildId.Contains("raid")) {
               _blockBlob.UploadText("[]");
            }
            else if (guildId.Contains("permission")) {
               ServerSettings settings = new ServerSettings();
               string json = JsonConvert.SerializeObject(settings);
               _blockBlob.UploadText(json);
            }
         }
      }
   }
}
