using BggBot2.Data;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BggBot2.Services
{
    public class FeedSender
    {
        private readonly int _batchSize;
        private readonly IServiceProvider _serviceProvider;

        public FeedSender(
            FeedSenderSettingsModel settings,
            IServiceProvider serviceProvider)
        {
            _batchSize = settings.BatchSize;
            _serviceProvider = serviceProvider;
        }

        public async Task SendPendingsAsync(PerformContext performContext, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();

            var chats = database.FeedItems
                .OrderByDescending(x => x.Id)
                .Select(x => new FeedDto { Entity = x, ChatId = x.Subscription.ApplicationUser.TelegramChatId })
                .Where(x => x.Entity.Status == FeedItemStatus.Pending)
                .Take(500)
                .AsEnumerable()
                .GroupBy(x => x.ChatId);

            foreach (var chat in chats)
            {
                await ProcessFeed(chat.Key, chat, database, telegram, cancellationToken);
            }
        }

        public async Task SendMoreItemsAsync(long chatId, CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var telegram = scope.ServiceProvider.GetRequiredService<ITelegramService>();

            var chat = database.FeedItems
                .OrderByDescending(x => x.Id)
                .Select(x => new FeedDto { Entity = x, ChatId = x.Subscription.ApplicationUser.TelegramChatId })
                .Where(x => x.ChatId == chatId && x.Entity.Status == FeedItemStatus.OnDemand)
                .ToList();

            await ProcessFeed(chatId, chat, database, telegram, cancellationToken);
        }

        public void ArchiveItems(long chatId)
        {
            using var scope = _serviceProvider.CreateScope();
            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var items = database.FeedItems
                    .Where(x => x.Subscription.ApplicationUser.TelegramChatId == chatId
                        && x.Status == FeedItemStatus.OnDemand);

            foreach (var item in items)
            {
                item.Status = FeedItemStatus.Archived;
            }

            database.SaveChanges();
        }

        private async Task ProcessFeed(
            long chatId,
            IEnumerable<FeedDto> chat,
            ApplicationDbContext database,
            ITelegramService telegram,
            CancellationToken cancellationToken)
        {
            var counter = _batchSize;
            foreach (var item in chat)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                if (counter-- > 0)
                {
                    try
                    {
                        var text = item.Entity.Link ?? FormatText(item.Entity);
                        await telegram.SendMessageAsync(item.ChatId, text);

                        item.Entity.Status = FeedItemStatus.Sent;
                        item.Entity.SentDate = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        database.ItemErrors.Add(FormatError(item.Entity, ex));
                        item.Entity.Status = FeedItemStatus.Error;
                    }
                }
                else
                {
                    item.Entity.Status = FeedItemStatus.OnDemand;
                }

                database.SaveChanges();
            }

            var onDemandCount = database.FeedItems
                .Where(x => x.Subscription.ApplicationUser.TelegramChatId == chatId
                    && x.Status == FeedItemStatus.OnDemand)
                .Count();

            if (onDemandCount > 0)
            {
                await telegram.SendOnDemandCounterAsync(chatId, onDemandCount);
            }
        }

        private static string FormatText(FeedItem entity)
        {
            var description = entity.Description;
            if (description?.Length > 100)
            {
                description = description.Substring(0, 100) + "...";
            }
            return $"{entity.Title}\n{description}";
        }

        private static ItemError FormatError(FeedItem entity, Exception ex)
            => new()
            {
                FeedItemId = entity.Id,
                Message = ex.Message,
                StackTrace = ex.StackTrace,
                ErrorDate = DateTimeOffset.UtcNow,
            };

        private class FeedDto
        {
            public FeedItem Entity { get; set; }
            public long ChatId { get; set; }
        }
    }
}
