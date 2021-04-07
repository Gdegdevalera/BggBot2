using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace BggBot2.Services
{
    public interface ITelegramService
    {
        Task SendMessageAsync(long chatId, string text);
        Task SendOnDemandCounterAsync(long chatId, int onDemandCount);
    }

    public class TelegramService : ITelegramService
    {
        private readonly TelegramBotClient _telegramClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramService> _logger;

        const string SendMoreAction = "Send more";
        const string IgnoreAction = "Ignore";

        public TelegramService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<TelegramService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            _telegramClient = new TelegramBotClient(configuration["TelegramService:BotToken"]);
            _telegramClient.StartReceiving(new[] { UpdateType.Message });
            _telegramClient.OnMessage += OnMessage;
        }

        private async void OnMessage(object sender, MessageEventArgs e)
        {
            var text = e.Message.Text;
            var chat = e.Message.Chat;

            _logger.LogDebug($"Received telegram message from {chat.Id}: {text}");

            if (text.ToLower() == "/start")
            {
                using var scope = _serviceProvider.CreateScope();
                var registrationService = scope.ServiceProvider.GetRequiredService<RegistrationService>();
                var code = registrationService.CreateCode(chat.Username, chat.Id, DateTimeOffset.UtcNow);

                await SendMessageAsync(chat.Id, "Your registration code: " + code);

                _logger.LogDebug("Registration code has been sent to " + chat.Username);
            }

            if (text == SendMoreAction)
            {
                var feedSender = _serviceProvider.GetRequiredService<FeedSender>();
                await feedSender.SendMoreItemsAsync(chat.Id, CancellationToken.None); 

                _logger.LogDebug("More feed items are marked as pending");
            }

            if (text == IgnoreAction)
            {
                var feedSender = _serviceProvider.GetRequiredService<FeedSender>();
                feedSender.ArchiveItems(chat.Id);
                await SendMessageAsync(chat.Id, "The items have been archived");

                _logger.LogDebug("More feed items are marked as pending");
            }
        }

        public async Task SendMessageAsync(long chatId, string message)
        {
            try
            {
                await _telegramClient.SendTextMessageAsync(chatId, message, ParseMode.Html);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429) // Too many requests
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                throw;
            }
        }

        public async Task SendOnDemandCounterAsync(long chatId, int onDemandCount)
        {
            try
            {
                var rkm = new ReplyKeyboardMarkup(new KeyboardButton[]
                    {
                        new KeyboardButton(SendMoreAction),
                        new KeyboardButton(IgnoreAction)
                    }, 
                    resizeKeyboard: true, 
                    oneTimeKeyboard: true);

                await _telegramClient.SendTextMessageAsync(chatId, 
                    $"You have {onDemandCount} items more", replyMarkup: rkm);
            }
            catch (ApiRequestException ex) when (ex.ErrorCode == 429) // Too many requests
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                throw;
            }
        }
    }
}
