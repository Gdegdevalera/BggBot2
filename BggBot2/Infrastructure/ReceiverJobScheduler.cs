using System;
using System.Linq.Expressions;
using System.Threading;

namespace BggBot2.Infrastructure
{
    public interface IReceiverJobScheduler
    {
        void Stop(long subscriptionId);

        void Start(long subscriptionId);
    }

    public class ReceiverJobScheduler : IReceiverJobScheduler
    {
        public void Start(long subscriptionId)
        {
            Expression<Action<ReceiverJob>> job = s => s.Execute(subscriptionId, null, CancellationToken.None);

            // run immediately
            Hangfire.BackgroundJob.Enqueue(job); 

            // schedule recurring
            Hangfire.RecurringJob.AddOrUpdate("receiver_" + subscriptionId, job, Hangfire.Cron.Minutely);
        }

        public void Stop(long subscriptionId)
        {
            Hangfire.RecurringJob.RemoveIfExists("receiver_" + subscriptionId);
        }
    }
}
