using System;

namespace BggBot2.Data
{
    public class FeedItem
    {
        public long Id { get; set; }

        public string Original { get; set; }

        public string Link { get; set; }

        public string Title { get; set; }
        
        public string Description { get; set; }

        public DateTimeOffset PublishDate { get; set; }

        public DateTimeOffset ReceivedDate { get; set; }

        public FeedItemStatus Status { get; set; }

        public DateTimeOffset? SentDate { get; set; }

        public long SubscriptionId { get; set; }

        public virtual Subscription Subscription { get; set; }
    }
}
