using BggBot2.Models;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BggBot2.Data
{
    public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>
    {
        public ApplicationDbContext(
            DbContextOptions options,
            IOptions<OperationalStoreOptions> operationalStoreOptions) : base(options, operationalStoreOptions)
        {
        }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<FeedItem> FeedItems { get; set; }

        public DbSet<ItemError> ItemErrors { get; set; }

        public DbSet<RegistrationCode> RegistrationCodes { get; set; }
    }

    [Index(nameof(ApplicationUserId), nameof(FeedUrl), IsUnique = true)]
    public class Subscription
    {
        public long Id { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        public virtual ApplicationUser ApplicationUser { get; set; }

        [Url]
        public string FeedUrl { get; set; }

        public bool IsEnabled { get; set; }

        [NotMapped]
        public int PendingCount { get; set; }

        [NotMapped]
        public bool HasError { get; set; }
    }

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

    public class ItemError
    {
        public long Id { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public long FeedItemId { get; set; }

        public DateTimeOffset ErrorDate { get; set; }
    }

    [Index(nameof(Code), IsUnique = true)]
    public class RegistrationCode
    {
        public long Id { get; set; }

        public DateTimeOffset ExpirationDate { get; set; }

        public string TelegramUserName { get; set; }

        public long TelegramChatId { get; set; }

        public string Code { get; set; }

        public bool IsUsed { get; set; }
    }

    public enum FeedItemStatus
    {
        Unknown = 0,
        Pending,
        Sent,
        OnDemand,
        Archived,
        Error
    }
}
