using AutoFixture;
using BggBot2.Data;
using BggBot2.Infrastructure;
using BggBot2.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BggBot2.Tests
{
    [TestClass]
    public class SenderServiceTests : IDisposable
    {
        // Two separated contexts for SaveChanges() call validation
        TestDbContext db = TestDbContext.CreateInMemory();
        TestDbContext serviceContext = TestDbContext.CreateInMemory();

        SenderSettingsModel settings = new SenderSettingsModel { BatchSize = 1 };
        ITelegramClient telegram = Substitute.For<ITelegramClient>();

        CancellationToken ct = CancellationToken.None;

        [TestMethod]
        public async Task Pending_items_should_be_sent()
        {
            // arrange
            var items = new Fixture().CreateMany<FeedItem>(3).ToList();
            items.ForEach(x => x.Status = FeedItemStatus.Pending);
            db.FeedItems.AddRange(items);
            db.SaveChanges();

            // act
            var service = new SenderService(settings, serviceContext, telegram);
            await service.SendPendingsAsync(ct);

            // assert
            db.FeedItems.All(x => x.Status == FeedItemStatus.Sent).Should().BeTrue();
            items.ForEach(item =>
                telegram.Received().SendMessageAsync(ChatId(item), item.Link).Wait());
        }

        [TestMethod]
        public async Task Items_over_batch_size_should_have_ondemand_status()
        {
            // arrange
            const long chatId = long.MaxValue;
            var items = new Fixture().CreateMany<FeedItem>(3).ToList();
            items.ForEach(x => x.Status = FeedItemStatus.Pending);
            items.ForEach(x => x.Subscription.ApplicationUser.TelegramChatId = chatId); // same chat for all
            db.FeedItems.AddRange(items);
            db.SaveChanges();

            // act
            var service = new SenderService(settings, serviceContext, telegram);
            await service.SendPendingsAsync(ct);

            // assert
            var sentItems = db.FeedItems.Where(x => x.Status == FeedItemStatus.Sent);
            sentItems.Count().Should().Be(1);

            var onDemandItems = db.FeedItems.Where(x => x.Status == FeedItemStatus.OnDemand);
            onDemandItems.Count().Should().Be(2);
            telegram.Received().SendOnDemandCounterAsync(chatId, 2).Wait();
        }

        [TestMethod]
        public async Task Error_must_be_saved()
        {
            // arrange
            var item = new Fixture().Create<FeedItem>();
            item.Status = FeedItemStatus.Pending;
            db.FeedItems.Add(item);
            db.SaveChanges();

            telegram
                .SendMessageAsync(item.Subscription.ApplicationUser.TelegramChatId, item.Link)
                .Throws(new Exception());

            // act
            var service = new SenderService(settings, serviceContext, telegram);
            await service.SendPendingsAsync(ct);

            // assert
            db.FeedItems.Count(x => x.Status == FeedItemStatus.Error).Should().Be(1);
            db.ItemErrors.Count().Should().Be(1);
        }

        [TestMethod]
        public void Ondemand_items_can_be_archived()
        {
            // arrange
            var items = new Fixture().CreateMany<FeedItem>(10).ToList();
            items.ForEach(x => x.Status = FeedItemStatus.Pending);
            items[0].Status = FeedItemStatus.OnDemand;
            db.FeedItems.AddRange(items);
            db.SaveChanges();

            // act
            var service = new SenderService(settings, serviceContext, telegram);
            service.ArchiveItems(ChatId(items[0]));

            // assert
            db.FeedItems.Count(x => x.Status == FeedItemStatus.Archived).Should().Be(1);
        }

        public void Dispose()
        {
            db?.Database?.EnsureDeleted();
            db?.Dispose();
            serviceContext?.Dispose();
        }

        private long ChatId(FeedItem item) => item.Subscription.ApplicationUser.TelegramChatId;
    }
}
