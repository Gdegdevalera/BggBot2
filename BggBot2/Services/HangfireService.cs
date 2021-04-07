using Hangfire;
using System.Threading;

namespace BggBot2.Services
{
    public interface IHangfireService
    {
        void Stop(long subscriptionId);

        void Start(long subscriptionId);
    }

    public class HangfireService : IHangfireService
    {
        public void Start(long subscriptionId)
        {
            RecurringJob.AddOrUpdate<RssReceiver>("receiver_" + subscriptionId,
                s => s.Read(subscriptionId, null, CancellationToken.None), Cron.Minutely);
        }

        public void Stop(long subscriptionId)
        {
            RecurringJob.RemoveIfExists("receiver_" + subscriptionId);
        }
    }
}
