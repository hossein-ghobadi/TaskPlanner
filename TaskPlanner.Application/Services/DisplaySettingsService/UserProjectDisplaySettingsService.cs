using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Application.Services.DisplaySettingsService
{
    public class UserProjectDisplaySettingsService : IUserProjectDisplaySettingsService
    {
        private readonly IMVPTestDatabaseContext _context;

        public UserProjectDisplaySettingsService(IMVPTestDatabaseContext context)
        {
            _context = context;
        }

        public async Task<UserProjectDisplaySettingsDto> GetAsync(
            string userId,
            int projectId,
            CancellationToken cancellationToken = default)
        {
            var pref = await _context.UserProjectDisplayPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ProjectId == projectId, cancellationToken);

            if (pref == null)
            {
                return new UserProjectDisplaySettingsDto();
            }

            return new UserProjectDisplaySettingsDto
            {
                ShowTasks = pref.ShowTasks,
                ShowKanban = pref.ShowKanban,
                ShowSprints = pref.ShowSprints,
                ShowFeatures = pref.ShowFeatures,
                ShowTickets = pref.ShowTickets,
                ShowGallery = pref.ShowGallery,
                ShowCategories = pref.ShowCategories
            };
        }

        public async Task SaveAsync(
            string userId,
            int projectId,
            UserProjectDisplaySettingsDto settings,
            CancellationToken cancellationToken = default)
        {
            var pref = await _context.UserProjectDisplayPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ProjectId == projectId, cancellationToken);

            if (pref == null)
            {
                pref = new UserProjectDisplayPreference
                {
                    UserId = userId,
                    ProjectId = projectId
                };
                _context.UserProjectDisplayPreferences.Add(pref);
            }

            pref.ShowTasks = settings.ShowTasks;
            pref.ShowKanban = settings.ShowKanban;
            pref.ShowSprints = settings.ShowSprints;
            pref.ShowFeatures = settings.ShowFeatures;
            pref.ShowTickets = settings.ShowTickets;
            pref.ShowGallery = settings.ShowGallery;
            pref.ShowCategories = settings.ShowCategories;
            pref.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
