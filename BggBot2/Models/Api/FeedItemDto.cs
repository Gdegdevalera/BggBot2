using System;

namespace BggBot2.Models.Api
{
    public class FeedItemDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTimeOffset PublishDate { get; set; }
        public string Link { get; set; }
    }
}
