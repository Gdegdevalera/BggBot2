using Microsoft.AspNetCore.Identity;

namespace BggBot2.Models
{
    public class ApplicationUser : IdentityUser
    {
        public long TelegramChatId { get; set; }
    }
}
