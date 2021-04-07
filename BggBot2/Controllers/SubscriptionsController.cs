using BggBot2.Data;
using BggBot2.Models.Api;
using BggBot2.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace BggBot2.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionService _subscriptionService;

        public SubscriptionsController(SubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpGet]
        public IEnumerable<Subscription> Get(long? lastId = null)
            => _subscriptionService.GetSubscriptions(lastId);

        [HttpGet("{id}")]
        public IEnumerable<FeedItem> Get(long id, long? lastId = null) 
            => _subscriptionService.GetFeedItems(id, lastId);

        [Authorize]
        [HttpPut("{id}/stop")]
        public Subscription Stop(long id) 
            => _subscriptionService.Stop(id, User.GetId());

        [Authorize]
        [HttpPut("{id}/start")]
        public Subscription Start(long id)
            => _subscriptionService.Start(id, User.GetId());

        [Authorize]
        [HttpPost]
        public Subscription Create(CreateSubscriptionModel model) 
            => _subscriptionService.Create(model, User.GetId());

        [HttpPost("test")]
        public IEnumerable<FeedItem> Test(CreateSubscriptionModel model)
            => _subscriptionService.Test(model);
    }
}
