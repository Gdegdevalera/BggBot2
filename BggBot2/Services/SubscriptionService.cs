using BggBot2.Data;
using BggBot2.Models.Api;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BggBot2.Services
{
    public class SubscriptionService
    {
        private readonly ApplicationDbContext _database;
        private readonly IHangfireService _hangfireService;

        public SubscriptionService(ApplicationDbContext database, IHangfireService hangfireService)
        {
            _database = database;
            _hangfireService = hangfireService;
        }

        public IEnumerable<Subscription> GetSubscriptions(long? lastId = null)
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
            var subscription = new Subscription
            {
                FeedUrl = model.FeedUrl,
                ApplicationUserId = userId,
                IsEnabled = true
            };

            _database.Add(subscription);
            _database.SaveChanges();
            _hangfireService.Start(subscription.Id);
            return subscription;
        }

        public Subscription Start(long id, string userId)
        {
            var newSub = Update(id, userId, x => x.IsEnabled = true);
            _hangfireService.Start(id);
            return newSub;
        }

        public Subscription Stop(long id, string userId)
        {
            var newSub = Update(id, userId, x => x.IsEnabled = false);
            _hangfireService.Stop(id);
            return newSub;
        }

        public IEnumerable<FeedItem> Test(CreateSubscriptionModel model)
        {
            var feed = RssReader.Read(model.FeedUrl);

            var items = feed.Items.Select(x => new FeedItem
            {
                Title = x.Title.Text,
                Description = x.Summary.Text,
                PublishDate = x.PublishDate,
                Link = x.Links?.FirstOrDefault()?.Uri?.ToString(),
            })
            .OrderBy(x => x.PublishDate)
            .ToList();

            return items;
        }

        private Subscription Update(long id, string userId, Action<Subscription> update)
        {
            var subscription = _database.Subscriptions.FirstOrDefault(x => x.Id == id);
            if (subscription == null)
                throw new NotFoundException();

            update(subscription);
            _database.SaveChanges();
            return subscription;
        }
    }
}
