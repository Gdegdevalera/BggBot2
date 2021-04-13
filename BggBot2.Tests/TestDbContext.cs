using BggBot2.Data;
using BggBot2.Models;
using Microsoft.EntityFrameworkCore;

namespace BggBot2.Tests
{
    internal class TestDbContext : DbContext, IApplicationDbContext
    {
        public TestDbContext(DbContextOptions options) : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }

        public DbSet<Subscription> Subscriptions { get; set; }

        public DbSet<FeedItem> FeedItems { get; set; }

        public DbSet<ItemError> ItemErrors { get; set; }

        public DbSet<RegistrationCode> RegistrationCodes { get; set; }

        public static TestDbContext CreateInMemory()
        {
            var builder = new DbContextOptionsBuilder<TestDbContext>();
            builder.UseInMemoryDatabase("test");
            return new TestDbContext(builder.Options);
        }
    }
}
