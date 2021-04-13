using BggBot2.Services;
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

namespace BggBot2.Infrastructure
{
    public interface ITelegramClient
    {
        Task SendMessageAsync(long chatId, string text);
        Task SendOnDemandCounterAsync(long chatId, int onDemandCount);
    }

    public class TelegramClient : ITelegramClient
    {
        private readonly TelegramBotClient _telegramClient;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TelegramClient> _logger;

        const string SendMoreAction = "Send more";
        const string IgnoreAction = "Ignore";

        public TelegramClient(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<TelegramClient> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;

            _telegramClient = new TelegramBotClient(configuration["TelegramService:BotToken"]);
            _telegramClient.StartReceiving(new[] { UpdateType.Message });
            _telegramClient.OnMessage += OnMessage;
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            var text = e.Message.Text;
            var chat = e.Message.Chat;

            _logger.LogDebug($"Received telegram message from {chat.Id}: {text}");

            if (text.ToLower() == "/start")
            {
                using var scope = _serviceProvider.CreateScope();
                var registrationService = scope.ServiceProvider.GetRequiredService<RegistrationCodeService>();
                var code = registrationService.CreateCode(chat.Username, chat.Id, DateTimeOffset.UtcNow);

                if (code != null)
                {
                    SendMessageAsync(chat.Id, "Your registration code: " + code).Wait();
                }
                else
                {
                    var feedSender = _serviceProvider.GetRequiredService<SubscriptionService>();
                    feedSender.StartAll(chat.Id);

                    SendMessageAsync(chat.Id, "Resumed").Wait();
                }

                _logger.LogDebug("Registration code has been sent to " + chat.Username);
            }

            if (text.ToLower() == "/stop")
            {
                var feedSender = _serviceProvider.GetRequiredService<SubscriptionService>();
                feedSender.StopAll(chat.Id);

                SendMessageAsync(chat.Id, "Stopped").Wait();

                _logger.LogDebug("Sending stopped with Telegram command");
            }

            if (text == SendMoreAction)
            {
                var feedSender = _serviceProvider.GetRequiredService<SenderService>();
                feedSender.SendMoreItemsAsync(chat.Id, CancellationToken.None).Wait(); 

                _logger.LogDebug("More feed items are marked as pending");
            }

            if (text == IgnoreAction)
            {
                var feedSender = _serviceProvider.GetRequiredService<SenderService>();
                feedSender.ArchiveItems(chat.Id);
                SendMessageAsync(chat.Id, "The items have been archived").Wait();

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
