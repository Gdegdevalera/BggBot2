using BggBot2.Data;
using BggBot2.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BggBot2.Services
{
    public class SenderService
    {
        private readonly int _batchSize;
        private readonly ApplicationDbContext _database;
        private readonly ITelegramClient _telegramClient;

        public SenderService(
            SenderSettingsModel settings,
            ApplicationDbContext database,
            ITelegramClient telegram)
        {
            if (settings.BatchSize <= 0)
                throw new ArgumentException("SenderSettings.BatchSize must be greater than 0");

            _batchSize = settings.BatchSize;
            _database = database;
            _telegramClient = telegram;
        }

        public async Task SendPendingsAsync(CancellationToken cancellationToken)
        {
            var chats = _database.FeedItems
                .OrderByDescending(x => x.Id)
                .Select(x => new FeedDto { Entity = x, ChatId = x.Subscription.ApplicationUser.TelegramChatId })
                .Where(x => x.Entity.Status == FeedItemStatus.Pending)
                .Take(1000)
                .AsEnumerable()
                .GroupBy(x => x.ChatId);

            foreach (var chat in chats)
            {
                await ProcessChat(chat.Key, chat, cancellationToken);
            }
        }

        public Task SendMoreItemsAsync(long chatId, CancellationToken cancellationToken)
        {
            var chat = _database.FeedItems
                .OrderByDescending(x => x.Id)
                .Select(x => new FeedDto { Entity = x, ChatId = x.Subscription.ApplicationUser.TelegramChatId })
                .Where(x => x.ChatId == chatId && x.Entity.Status == FeedItemStatus.OnDemand)
                .ToList();

            return ProcessChat(chatId, chat, cancellationToken);
        }

        public void ArchiveItems(long chatId)
        {
            var items = _database.FeedItems
                    .Where(x => x.Subscription.ApplicationUser.TelegramChatId == chatId
                        && x.Status == FeedItemStatus.OnDemand);

            foreach (var item in items)
            {
                item.Status = FeedItemStatus.Archived;
            }

            _database.SaveChanges();
        }

        private async Task ProcessChat(
            long chatId,
            IEnumerable<FeedDto> chat,
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
                        await _telegramClient.SendMessageAsync(item.ChatId, text);

                        item.Entity.Status = FeedItemStatus.Sent;
                        item.Entity.SentDate = DateTimeOffset.UtcNow;
                    }
                    catch (Exception ex)
                    {
                        _database.ItemErrors.Add(FormatError(item.Entity, ex));
                        item.Entity.Status = FeedItemStatus.Error;
                    }
                }
                else
                {
                    item.Entity.Status = FeedItemStatus.OnDemand;
                }

                _database.SaveChanges();
            }

            var onDemandCount = _database.FeedItems
                .Where(x => x.Subscription.ApplicationUser.TelegramChatId == chatId
                    && x.Status == FeedItemStatus.OnDemand)
                .Count();

            if (onDemandCount > 0)
            {
                await _telegramClient.SendOnDemandCounterAsync(chatId, onDemandCount);
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
