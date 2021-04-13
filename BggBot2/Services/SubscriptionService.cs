using BggBot2.Data;
using BggBot2.Infrastructure;
using BggBot2.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BggBot2.Services
{
    public class SubscriptionService
    {
        private readonly IApplicationDbContext _database;
        private readonly IReceiverJobScheduler _receiver;
        private readonly int _maxSubscriptionsCount;

        public SubscriptionService(
            IApplicationDbContext database, 
            IReceiverJobScheduler receiver,
            UserSettingsModel settings)
        {
            _database = database;
            _receiver = receiver;

            _maxSubscriptionsCount = settings.MaxEnabledSubscriptonsCountPerUser;
        }

        public IEnumerable<Subscription> GetSubscriptions(long? lastId = null, string userId = null)
        {
            var subscriptions = _database.Subscriptions
                .Where(x => lastId == null || x.Id < lastId)
                .OrderByDescending(x => x.Id)
                .Take(10);

            var ids = subscriptions.Select(x => x.Id).ToArray();
            var pendingCounts = _database.FeedItems
                .Where(x => ids.Contains(x.SubscriptionId) && x.Status == FeedItemStatus.Pending)
                .GroupBy(x => x.SubscriptionId)
                .Select(x => new { x.Key, Count = x.Count()} )
                .AsEnumerable()
                .ToDictionary(x => x.Key, x => x.Count);

            var errors = _database.FeedItems
                .Where(x => ids.Contains(x.SubscriptionId) && x.Status == FeedItemStatus.Error)
                .Select(x => x.SubscriptionId)
                .Distinct()
                .ToArray();

            foreach (var subscription in subscriptions)
            {
                subscription.PendingCount = pendingCounts.SafeGetValue(subscription.Id);
                subscription.HasError = errors.Contains(subscription.Id);

                if (subscription.FeedUrl.Length > 50) 
                {
                    subscription.FeedUrl = subscription.FeedUrl.Substring(0, 50) + "...";
                }

                if (subscription.ApplicationUserId == userId)
                {
                    subscription.IsReadOnly = false;
                }
            }

            return subscriptions;
        }

        public IEnumerable<FeedItem> GetFeedItems(long id, long? lastId = null)
        {
            return _database.FeedItems
                .Where(x => x.SubscriptionId == id)
                .Where(x => lastId == null || x.Id < lastId)
                .OrderByDescending(x => x.Id)
                .Take(10);
        }

        public Subscription Create(CreateSubscriptionModel model, string userId)
        {
            var existingSubscriptionsCount = _database.Subscriptions
                .Count(x => x.ApplicationUserId == userId && x.IsEnabled);

            if (existingSubscriptionsCount >= _maxSubscriptionsCount)
                throw new TooManyExistingSubscriptionsException();

            var subscription = new Subscription
            {
                FeedUrl = model.FeedUrl,
                ApplicationUserId = userId,
                IsEnabled = true,
                IsReadOnly = false
            };

            _database.Subscriptions.Add(subscription);
            _database.SaveChanges();
            _receiver.Start(subscription.Id);
            return subscription;
        }

        public Subscription Start(long id, string userId)
        {
            var existingSubscriptionsCount = _database.Subscriptions
                .Count(x => x.ApplicationUserId == userId && x.IsEnabled);

            if (existingSubscriptionsCount >= _maxSubscriptionsCount)
                throw new TooManyExistingSubscriptionsException();

            var newSub = Update(id, userId, x => x.IsEnabled = true);
            _receiver.Start(id);
            return newSub;
        }

        public void StopAll(long chatId)
        {
            var subscriptions = _database.Subscriptions
                .Where(x => x.ApplicationUser.TelegramChatId == chatId)
                .ToList();

            subscriptions.ForEach(x => x.IsEnabled = false);
            _database.SaveChanges();
        }

        public void StartAll(long chatId)
        {
            var subscriptions = _database.Subscriptions
                .Where(x => x.ApplicationUser.TelegramChatId == chatId)
                .ToList();

            subscriptions.ForEach(x => x.IsEnabled = true);
            _database.SaveChanges();
        }

        public Subscription Stop(long id, string userId)
        {
            var newSub = Update(id, userId, x => x.IsEnabled = false);
            _receiver.Stop(id);
            return newSub;
        }

        private Subscription Update(long id, string userId, Action<Subscription> update)
        {
            var subscription = _database.Subscriptions.FirstOrDefault(x => x.Id == id);

            if (subscription.ApplicationUserId != userId)
                throw new NotAllowedException();

            if (subscription == null)
                throw new NotFoundException();

            update(subscription);
            _database.SaveChanges();

            subscription.IsReadOnly = false;
            return subscription;
        }
    }
}
