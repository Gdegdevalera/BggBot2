using Microsoft.EntityFrameworkCore;
using System;

namespace BggBot2.Data
{
    [Index(nameof(Code), IsUnique = true)]
    public class RegistrationCode
    {
        public long Id { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        public string TelegramUserName { get; set; }

        public long TelegramChatId { get; set; }

        public string Code { get; set; }

        public bool IsUsed { get; set; }
    }
}
