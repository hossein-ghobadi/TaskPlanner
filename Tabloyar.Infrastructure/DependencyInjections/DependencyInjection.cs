using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;



using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TaskPlanner.Infrastructure.IdentityConfigs;



using Microsoft.Extensions.DependencyInjection.Extensions;

using TaskPlanner.Application.Services;
using TaskPlanner.Application.Services.TaskPlanner;


namespace TaskPlanner.Infrastructure.DependencyInjections
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register infrastructure services, such as external APIs, file storage, etc.


          
            services.AddScoped<IRemoteUploader, RemoteUploader>();
            services.AddScoped<ITaskPlannerFileUploader, TaskPlannerFileUploader>();
            
            // سرویس‌های آپلود جداگانه
            services.AddScoped<IImageUploader, ImageUploader>();
            services.AddScoped<IVoiceUploader, VoiceUploader>();
            services.AddScoped<IDocumentUploader, DocumentUploader>();

            return services;
        }
    }
}
