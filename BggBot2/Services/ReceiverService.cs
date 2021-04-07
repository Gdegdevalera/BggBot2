using BggBot2.Data;
using System;
using System.Linq;
using System.Threading;
using BggBot2.Infrastructure;

namespace BggBot2.Services
{
    public class ReceiverService
    {
        private readonly IApplicationDbContext _database;
        private readonly IRssReader _rssReader;

        public ReceiverService(IApplicationDbContext database, IRssReader rssReader)
        {
            _database = database;
            _rssReader = rssReader;
        }

        public void Read(long subscriptionId, Extensions.Logger log, CancellationToken cancellationToken)
        {
            var subscription = _database.Subscriptions.FirstOrDefault(x => x.Id == subscriptionId);

            if (subscription == null)
            {
                log($"Subscription id:{subscriptionId} is missing!");
                return;
            }

            if (!subscription.IsEnabled)
            {
                log($"Subscription id:{subscriptionId} is not enabled");
                return;
            }

            var feed = _rssReader.Read(subscription.FeedUrl);

            var items = feed
                .Select(x => new FeedItem
                {
                    Title = x.Title,
                    Description = x.Description,
                    PublishDate = x.PublishDate,
                    Link = x.Link,
                    ReceivedDate = DateTimeOffset.UtcNow,
                    Status = FeedItemStatus.Pending,
                    SubscriptionId = subscription.Id,
                })
                .OrderBy(x => x.PublishDate)
                .ToList();

            log($"Got {items.Count} items from {subscription.FeedUrl}");

            foreach (var item in items)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var itemExists = _database.FeedItems.Any(x =>
                    x.SubscriptionId == item.SubscriptionId
                    && x.PublishDate == item.PublishDate
                    && x.Title == item.Title);

                if (itemExists)
                    continue;

                _database.FeedItems.Add(item);
                _database.SaveChanges();
            }
        }
    }
}
