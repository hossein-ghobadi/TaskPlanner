using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TaskPlanner.Application.Services.SMS.Commands;

namespace TaskPlanner.Application.Services.SMS
{
    public static class SmsServiceCollectionExtensions
    {
        public static IServiceCollection AddSmsServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient("LimoSms", client => client.Timeout = TimeSpan.FromSeconds(30));

            services.Configure<SmsOptions>(configuration.GetSection(SmsOptions.SectionName));
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<SmsOptions>>().Value;
                var config = sp.GetRequiredService<IConfiguration>();
                options.ApiKey = options.ResolveApiKey(config);
                options.SendUrl = options.ResolveSendUrl(config);
                options.CheckUrl = options.ResolveCheckUrl(config);
                return options;
            });

            services.AddScoped<ISmsSendService, SmsSendService>();
            services.AddScoped<ISmsCheckService, SmsCheckService>();

            return services;
        }
    }
}
