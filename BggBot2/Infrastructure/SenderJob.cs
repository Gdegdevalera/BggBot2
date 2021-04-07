using BggBot2.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BggBot2.Infrastructure
{
    public class SenderJob
    {
        private readonly IServiceProvider _serviceProvider;

        public SenderJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Execute(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<SenderService>();
            await service.SendPendingsAsync(cancellationToken);
        }
    }
}
