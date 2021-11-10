using CloudNative.CloudEvents;
using Google.Cloud.Functions.Framework;
using Google.Cloud.Functions.Hosting;
using Google.Cloud.PubSub.V1;
using Google.Events.Protobuf.Cloud.PubSub.V1;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HermesMail.Core
{
    public class Startup : FunctionsStartup
    {
        public override void ConfigureServices(WebHostBuilderContext context, IServiceCollection services)
        {
            services.AddScoped<Services>();
            services.AddSingleton(PublisherServiceApiClient.Create());
        }
    }
    /// <summary>
    /// MessagePublishedData is an event triggered by a pubsub topic.
    /// </summary>
    [FunctionsStartup(typeof(Startup))]
    public class Function : ICloudEventFunction<MessagePublishedData>
    {
        private readonly Services _services;

        public Function(Services services)
        {
            _services = services;
        }

        public async Task HandleAsync(CloudEvent cloudEvent, MessagePublishedData data, CancellationToken cancellationToken)
        {
            if (cloudEvent.IsExpiredEvent())
            {
                Console.WriteLine($"--- [{DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}] Publishing Message to Dead Letter Topic. '{data.Message?.TextData}' ---");
                await _services.PublishIntegrationEventToDeadLetterTopic(data.Message?.TextData);
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(data.Message?.TextData))
                    throw new InvalidOperationException($"--- [{DateTime.UtcNow:dd/MM/yyyy HH:mm:ss}] Message TextData is null' ---");

                var message = data.Message.TextData.ToMailMessage();
                _services.SendMail(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }

            return;
        }
    }
}
