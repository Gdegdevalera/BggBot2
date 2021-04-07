using BggBot2.Models;
using IdentityServer4.EntityFramework.Options;
using Microsoft.AspNetCore.ApiAuthorization.IdentityServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BggBot2.Data
{
    public interface IApplicationDbContext
    {
        DbSet<Subscription> Subscriptions { get; }

        DbSet<FeedItem> FeedItems { get; }

        DbSet<ItemError> ItemErrors { get; }

        DbSet<RegistrationCode> RegistrationCodes { get; }

        int SaveChanges();
    }

    public class ApplicationDbContext : ApiAuthorizationDbContext<ApplicationUser>, IApplicationDbContext
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
}
