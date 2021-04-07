using BggBot2.Data;
using System;
using System.Linq;

namespace BggBot2.Services
{
    public class RegistrationCodeService
    {
        private readonly IApplicationDbContext _database;

        public RegistrationCodeService(IApplicationDbContext database)
        {
            _database = database;
        }

        public (string, long) GetUserByRegistrationCode(string registrationCode, DateTimeOffset now)
        {
            var item = _database.RegistrationCodes
                .FirstOrDefault(x => x.Code == registrationCode && x.ExpirationDate >= now && !x.IsUsed);

            if (item == null)
                return (null, 0);

            item.IsUsed = true;
            _database.SaveChanges();

            return (item.TelegramUserName, item.TelegramChatId);
        }

        public string CreateCode(string userName, long userId, DateTimeOffset now)
        {
            var code = GenerateUniqueCode(userName, userId, now);
            _database.RegistrationCodes.Add(new RegistrationCode
            {
                TelegramChatId = userId,
                TelegramUserName = userName,
                ExpirationDate = now.AddMinutes(10),
                Code = code
            });
            _database.SaveChanges();
            return code;
        }

        private static string GenerateUniqueCode(string userName, long userId, DateTimeOffset now) 
            => (Math.Abs($"{userName}{userId}{now.Millisecond}".GetHashCode()) % 1000000).ToString();
    }
}
