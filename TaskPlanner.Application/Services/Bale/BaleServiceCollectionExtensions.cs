using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace TaskPlanner.Application.Services.Bale
{
    public static class BaleServiceCollectionExtensions
    {
        public static IServiceCollection AddBaleServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient("BaleBot", client => client.Timeout = TimeSpan.FromSeconds(90));

            services.Configure<BaleBotOptions>(configuration.GetSection(BaleBotOptions.SectionName));
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<BaleBotOptions>>().Value;
                var config = sp.GetRequiredService<IConfiguration>();
                options.BotToken = options.ResolveToken(config);
                return options;
            });

            services.AddSingleton<IBaleBotClient, BaleBotClient>();
            services.AddScoped<IBaleMediaIngestService, BaleMediaIngestService>();
            services.AddScoped<IBaleSenderEnrichmentService, BaleSenderEnrichmentService>();
            services.AddHostedService<BaleMessageSyncBackgroundService>();

            return services;
        }
    }
}
