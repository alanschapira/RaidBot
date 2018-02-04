using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace RaidBot.Entities {
   [Serializable]
   public class Raid : IEquatable<Raid> {
      public string Name { get; set; }
      public DateTime? Time { get; set; }
      public List<User> Users { get; set; }

      public DateTime CreateDateTime { get; set; }
      public int RaidBossId { get; set; }

      public int UserCount
      {
         get
         {
            return Users.Count() + Users.Sum(a => a.GuestsCount);
         }
      }

      public bool Equals(Raid item) {
         return Name.Equals(item.Name, StringComparison.CurrentCultureIgnoreCase);
      }

      public override string ToString() {
         string time = Time?.ToString("HH:mm");
         string raidBoss = RaidBossId == 0 ? string.Empty : Mons.GetNameById(RaidBossId);
         return $"{Name} {time} {raidBoss} ({UserCount} Attendees)";
      }

      public string ToStringUsers() {
         StringBuilder raidWithUsers = new StringBuilder();

         if (Users.Count() == 0) {
            raidWithUsers.Append("No users are attending this raid");
         }
         else {
            raidWithUsers.Append($"Leader: {Users.First().Username} {Users.First().GetGuests} | ");

            foreach (var user in Users.Skip(1)) {
               raidWithUsers.Append($"{user.Username} {user.GetGuests} | ");
            }

            raidWithUsers.Length-=2;
         }
         

         

         return raidWithUsers.ToString();
      }
   }
}
