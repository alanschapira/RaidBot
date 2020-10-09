using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;

namespace RaidBot.Entities {
   [Serializable]
   public class User : IEquatable<User> {
      public string Username { get; set; }
      public string Mention { get; set; }
      public string Discriminator { get; set; }
      public int GuestsCount { get; set; }
      public bool IsRemoteAttendee { get; set; }
      public string GetGuests { get { return GuestsCount == 0 ? string.Empty : $"+{GuestsCount}"; } }

      public static User FromIUser(IUser user, int guests = 0, bool isRemote = false) {
         var guildUser = user as IGuildUser;

         return new User() {
            Username = guildUser.Nickname?? user.Username,
            Mention = user.Mention,
            Discriminator = user.Discriminator,
            GuestsCount = guests,
            IsRemoteAttendee = isRemote,
         };
      }

      public bool Equals(User item) {
         return item.Discriminator == this.Discriminator;
      }
   }
}
