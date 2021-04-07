using BggBot2.Models.Api;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Syndication;
using System.Xml;

namespace BggBot2.Infrastructure
{
    public interface IRssReader
    {
        List<FeedItemDto> Read(string url);
    }

    public class RssReader : IRssReader
    {
        public List<FeedItemDto> Read(string url)
        {
            using var reader = XmlReader.Create(url);
            var feed = SyndicationFeed.Load(reader);

            var items = feed.Items
                .Select(x => new FeedItemDto
                {
                    Title = x.Title?.Text,
                    Description = x.Summary?.Text,
                    PublishDate = x.PublishDate,
                    Link = x.Links?.FirstOrDefault()?.Uri?.ToString(),
                })
                .ToList();

            return items;
        }
    }
}
