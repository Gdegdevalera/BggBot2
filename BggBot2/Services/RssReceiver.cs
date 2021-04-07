using BggBot2.Data;
using Hangfire.Server;
using Hangfire.Console;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Xml;

namespace BggBot2.Services
{
    public class RssReceiver
    {
        private readonly IServiceProvider _serviceProvider;

        public RssReceiver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Read(long subscriptionId, PerformContext performContext, CancellationToken cancellationToken)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var subscription = context.Subscriptions.FirstOrDefault(x => x.Id == subscriptionId);

                if (subscription == null)
                {
                    performContext.WriteLine($"Subscription id:{subscriptionId} is missing!");
                    return;
                }

                if (!subscription.IsEnabled)
                {
                    performContext.WriteLine($"Subscription id:{subscriptionId} is not enabled");
                    return;
                }

                var feed = RssReader.Read(subscription.FeedUrl);

                var items = feed.Items
                    .Select(x => new FeedItem
                    {
                        Title = x.Title.Text,
                        Description = x.Summary.Text,
                        PublishDate = x.PublishDate,
                        ReceivedDate = DateTimeOffset.UtcNow,
                        Link = x.Links?.FirstOrDefault()?.Uri?.ToString(),
                        Status = FeedItemStatus.Pending,
                        SubscriptionId = subscription.Id,
                    })
                    .OrderBy(x => x.PublishDate)
                    .ToList();

                performContext.WriteLine($"Got {items.Count} items from {subscription.FeedUrl}");

                foreach (var item in items)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var itemExists = context.FeedItems.Any(x =>
                        x.SubscriptionId == item.SubscriptionId
                        && x.PublishDate == item.PublishDate
                        && x.Title == item.Title);

                    if (itemExists)
                        continue;

                    context.FeedItems.Add(item);
                    context.SaveChanges();
                }
            }
        }
    }

    public class RssReader
    {
        public static SyndicationFeed Read(string url)
        {
            using var reader = XmlReader.Create(url);
            return SyndicationFeed.Load(reader);
        }
    }
}
