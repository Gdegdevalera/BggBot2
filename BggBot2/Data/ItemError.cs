using System;

namespace BggBot2.Data
{
    public class ItemError
    {
        public long Id { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public long FeedItemId { get; set; }

        public DateTimeOffset ErrorDate { get; set; }
    }
}
