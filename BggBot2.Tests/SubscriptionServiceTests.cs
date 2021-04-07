using AutoFixture;
using BggBot2.Data;
using BggBot2.Infrastructure;
using BggBot2.Models.Api;
using BggBot2.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BggBot2.Tests
{
    [TestClass]
    public class SubscriptionServiceTests
    {
        // Two separated contexts for SaveChanges() call validation
        TestDbContext db = TestDbContext.CreateInMemory();
        TestDbContext serviceContext = TestDbContext.CreateInMemory();

        IReceiverJobScheduler scheduler = Substitute.For<IReceiverJobScheduler>();

        [TestMethod]
        public void When_subsctiption_created_scheduler_should_be_called()
        {
            // act 
            var service = new SubscriptionService(serviceContext, scheduler);
            var subs = service.Create(new CreateSubscriptionModel(), default);

            // assert
            subs.Id.Should().NotBe(default);
            db.Subscriptions.Count().Should().Be(1);
            scheduler.Received().Start(subs.Id);
        }

        [TestMethod]
        public void When_subsctiption_started_scheduler_should_be_called()
        {
            // arrange
            var subscription = new Subscription { IsEnabled = false };
            db.Subscriptions.Add(subscription);
            db.SaveChanges();

            // act 
            var service = new SubscriptionService(serviceContext, scheduler);
            service.Start(subscription.Id, default);

            // assert
            db.Entry(subscription).State = EntityState.Detached;
            db.Subscriptions.Find(subscription.Id).IsEnabled.Should().BeTrue();
            scheduler.Received().Start(subscription.Id);
        }

        [TestMethod]
        public void When_subsctiption_stopped_scheduler_should_be_called()
        {
            // arrange
            var subscription = new Subscription { IsEnabled = true };
            db.Subscriptions.Add(subscription);
            db.SaveChanges();

            // act 
            var service = new SubscriptionService(serviceContext, scheduler);
            service.Stop(subscription.Id, default);

            // assert
            db.Entry(subscription).State = EntityState.Detached;
            db.Subscriptions.Find(subscription.Id).IsEnabled.Should().BeFalse();
            scheduler.Received().Stop(subscription.Id);
        }

        public void Dispose()
        {
            db?.Database?.EnsureDeleted();
            db?.Dispose();
            serviceContext?.Dispose();
        }
    }
}
