using BggBot2.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace BggBot2.Tests
{
    [TestClass]
    public class RegistrationCodeServiceTests : IDisposable
    {
        // Two separated contexts for SaveChanges() call validation
        TestDbContext db = TestDbContext.CreateInMemory();
        TestDbContext serviceContext = TestDbContext.CreateInMemory();

        const string Username = "userName";
        const long UserId = long.MaxValue;

        [TestMethod]
        public void Generated_code_should_be_saved()
        {
            // act
            var service = new RegistrationCodeService(serviceContext);
            var code = service.CreateCode(Username, UserId, CurrentDate);

            // assert
            code.Should().NotBeEmpty();
            var codeEntity = db.RegistrationCodes.FirstOrDefault(x => x.Code == code);
            codeEntity.Should().NotBeNull();
            codeEntity.IsUsed.Should().BeFalse();
            codeEntity.ExpirationDate.Should().BeAfter(CurrentDate);
        }

        [TestMethod]
        public void Generated_code_should_be_unique()
        {
            // act
            var service = new RegistrationCodeService(serviceContext);
            var code1 = service.CreateCode(Username, UserId, CurrentDate);
            var code2 = service.CreateCode(Username, UserId, CurrentDate.AddMilliseconds(1));

            // assert
            code1.Should().NotBe(code2);
        }

        [TestMethod]
        public void Registration_code_can_be_used_only_once()
        {
            // arrange
            var service = new RegistrationCodeService(serviceContext);
            var code = service.CreateCode(Username, UserId, CurrentDate);

            // act
            var (userName, userId) = service.GetUserByRegistrationCode(code, CurrentDate);
            var (userName2, userId2) = service.GetUserByRegistrationCode(code, CurrentDate);

            // assert
            userName.Should().Be(Username);
            userId.Should().Be(UserId);
            userName2.Should().BeNull();
            userId2.Should().Be(default);
        }

        [TestMethod]
        public void Expired_code_can_not_be_used()
        {
            // arrange
            var service = new RegistrationCodeService(serviceContext);
            var code = service.CreateCode(Username, UserId, CurrentDate.AddMinutes(-10));

            // act
            var (userName, userId) = service.GetUserByRegistrationCode(code, CurrentDate);

            // assert
            userName.Should().BeNull();
            userId.Should().Be(default);
        }

        public void Dispose()
        {
            db?.Database?.EnsureDeleted();
            db?.Dispose();
            serviceContext?.Dispose();
        }

        private DateTimeOffset CurrentDate => DateTimeOffset.Now;
    }
}
