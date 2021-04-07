using BggBot2.Services;
using Hangfire.Console;
using Hangfire.Server;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;

namespace BggBot2.Infrastructure
{
    public class ReceiverJob
    {
        private readonly IServiceProvider _serviceProvider;

        public ReceiverJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Execute(
            long subscriptionId, 
            PerformContext performContext, 
            CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<ReceiverService>();
            service.Read(subscriptionId, performContext.WriteLine, cancellationToken);
        }
    }
}
