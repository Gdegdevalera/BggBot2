using AutoFixture;
using BggBot2.Data;
using BggBot2.Infrastructure;
using BggBot2.Models.Api;
using BggBot2.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Linq;
using System.Threading;

namespace BggBot2.Tests
{
    [TestClass]
    public class ReceiverServiceTests : IDisposable
    {
        // Two separated contexts for SaveChanges() call validation
        TestDbContext db = TestDbContext.CreateInMemory();
        TestDbContext serviceContext = TestDbContext.CreateInMemory();

        IRssReader rssReader = Substitute.For<IRssReader>();

        CancellationToken ct = CancellationToken.None;

        const string Url = "some_url";
        const int FeedSize = 10;
        
        Subscription subscription = new Subscription
        {
            FeedUrl = Url,
            IsEnabled = true
        };

        public ReceiverServiceTests()
        {
            // arrange
            db.Subscriptions.Add(subscription);
            db.SaveChanges();

            var feed = new Fixture().CreateMany<FeedItemDto>(FeedSize).ToList();
            rssReader.Read(Url).Returns(feed);
        }

        [TestMethod]
        public void ReadFeed_should_fill_FeedItems_table()
        {
            // act
            var service = new ReceiverService(serviceContext, rssReader);
            service.Read(subscription.Id, Log, ct);

            // assert
            db.FeedItems.Should().HaveCount(FeedSize);
            db.FeedItems.All(x => x.Status == FeedItemStatus.Pending).Should().BeTrue();
        }

        [TestMethod]
        public void FeedItems_table_should_not_have_duplicates()
        {
            // act
            var service = new ReceiverService(serviceContext, rssReader);
            service.Read(subscription.Id, Log, ct);
            service.Read(subscription.Id, Log, ct);

            // assert
            db.FeedItems.Should().HaveCount(FeedSize);
        }

        [TestMethod]
        public void Disabled_subscription_should_not_be_processed()
        {
            // arrange
            subscription.IsEnabled = false;
            db.SaveChanges();

            // act
            var service = new ReceiverService(serviceContext, rssReader);
            service.Read(subscription.Id, Log, ct);

            // assert
            db.FeedItems.Should().HaveCount(0);
        }

        [TestMethod]
        public void Missing_subscription_should_not_be_processed()
        {
            // arrange
            subscription.IsEnabled = false;
            db.SaveChanges();

            // act
            var service = new ReceiverService(serviceContext, rssReader);
            service.Read(subscription.Id + 1, Log, ct);

            // assert
            db.FeedItems.Should().HaveCount(0);
        }

        public void Dispose()
        {
            db?.Database?.EnsureDeleted();
            db?.Dispose();
            serviceContext?.Dispose();
        }

        private void Log(string value) { }
    }
}
