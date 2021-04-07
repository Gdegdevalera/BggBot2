using BggBot2.Infrastructure;
using BggBot2.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace BggBot2.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var feed = RssReader.Read("https://www.feedforall.com/sample.xml");
        }

        [TestMethod]
        public void TestTelegramUserSearch()
        {
            
        }
    }
}
