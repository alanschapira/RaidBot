using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RaidBot.Helpers {
   public static class JsonHelper {
      public static void CreateFileIfNotExists(string path) {
         if (!File.Exists(path)) {
            using (var tw = new StreamWriter(path, true)) {
               tw.Close();
            }
         }
      }
   }
}
