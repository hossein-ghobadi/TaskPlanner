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
using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Application.Services.NotificationService;
using TaskPlanner.Application.Services.LeadService;
using TaskPlanner.Application.Services.CrmService;
using TaskPlanner.Application.Services.LeaveService;
using TaskPlanner.Application.Services.DisplaySettingsService;
using TaskPlanner.Application.Services.DashboardService;
using TaskPlanner.Application.Services.MyWorkService;
using TaskPlanner.Application.Services.Bale;
using TaskPlanner.Application.Services.SMS;
using TaskPlanner.Application.Services.FileUpload;
using TaskPlanner.Application.Services.Calculation.MaterialCost;
using TaskPlanner.Application.Services.Calculation.EdgeCost;
using TaskPlanner.Application.Services.Calculation.SmdCost;
using TaskPlanner.Application.Services.Calculation.MetalsCost;
using TaskPlanner.Application.Services.Calculation.PvcCost;
using TaskPlanner.Application.Services.Calculation.GlueCost;
using TaskPlanner.Application.Services.Calculation.CrystalCost;
using TaskPlanner.Application.Services.Calculation.PaintCost;
using TaskPlanner.Application.Services.Calculation.WoodCost;
using TaskPlanner.Application.Services.Calculation.VacuumWageCost;
using TaskPlanner.Application.Services.Calculation.CutSpecificCost;


namespace TaskPlanner.Infrastructure.DependencyInjections
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Register infrastructure services, such as external APIs, file storage, etc.

            services.AddSingleton<IFileUrlService, FileUrlService>();
          
            services.AddScoped<IRemoteUploader, RemoteUploader>();
            services.AddScoped<ITaskPlannerFileUploader, TaskPlannerFileUploader>();
            
            // سرویس‌های آپلود جداگانه
            services.AddScoped<IImageUploader, ImageUploader>();
            services.AddScoped<IVoiceUploader, VoiceUploader>();
            services.AddScoped<IDocumentUploader, DocumentUploader>();

            // سرویس‌های Query برای پروژه‌ها
            services.AddScoped<IProjectQueryService, ProjectQueryService>();

            // سرویس‌های Command برای پروژه‌ها
            services.AddScoped<IProjectCommandService, ProjectCommandService>();

            services.AddScoped<ILeadService, LeadService>();
            services.AddScoped<ICrmService, CrmService>();
            services.AddScoped<ILeaveService, LeaveService>();
            services.AddScoped<IUserDisplaySettingsService, UserDisplaySettingsService>();

            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IMyWorkService, MyWorkService>();

            // ماژول‌های سرویس محاسبه (محیط تست)
            services.AddScoped<IMaterialCostCalculator, MaterialCostCalculator>();
            services.AddScoped<IEdgeCostCalculator, EdgeCostCalculator>();
            services.AddScoped<ISmdCostCalculator, SmdCostCalculator>();
            services.AddScoped<IMetalsCostCalculator, MetalsCostCalculator>();
            services.AddScoped<IPvcCostCalculator, PvcCostCalculator>();
            services.AddScoped<IGlueCostCalculator, GlueCostCalculator>();
            services.AddScoped<ICrystalCostCalculator, CrystalCostCalculator>();
            services.AddScoped<IPaintCostCalculator, PaintCostCalculator>();
            services.AddScoped<IWoodCostCalculator, WoodCostCalculator>();
            services.AddScoped<IVacuumWageCostCalculator, VacuumWageCostCalculator>();
            services.AddScoped<ICutSpecificCostCalculator, CutSpecificCostCalculator>();

            // سرویس نوتیفیکیشن
            services.AddScoped<INotificationService, NotificationService>();

            services.AddBaleServices(configuration);

            services.AddSmsServices(configuration);

            return services;
        }
    }
}
