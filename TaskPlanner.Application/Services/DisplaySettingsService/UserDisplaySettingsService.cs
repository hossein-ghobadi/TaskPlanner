using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Application.Services.DisplaySettingsService
{
    public class UserDisplaySettingsService : IUserDisplaySettingsService
    {
        private readonly IMVPTestDatabaseContext _context;

        public UserDisplaySettingsService(IMVPTestDatabaseContext context)
        {
            _context = context;
        }

        public async Task<UserDisplaySettingsDto> GetAsync(string userId, CancellationToken cancellationToken = default)
        {
            var pref = await _context.UserDisplayPreferences
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (pref == null)
            {
                return new UserDisplaySettingsDto();
            }

            return new UserDisplaySettingsDto
            {
                ShowLeads = pref.ShowLeads,
                ShowBoards = pref.ShowBoards,
                ShowMindMaps = pref.ShowMindMaps,
                ShowDesigns = pref.ShowDesigns,
                ShowAdminUsers = pref.ShowAdminUsers,
                ShowAdminLeaves = pref.ShowAdminLeaves
            };
        }

        public async Task SaveAsync(
            string userId,
            UserDisplaySettingsDto settings,
            bool isAdmin,
            CancellationToken cancellationToken = default)
        {
            var pref = await _context.UserDisplayPreferences
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (pref == null)
            {
                pref = new UserDisplayPreference { UserId = userId };
                _context.UserDisplayPreferences.Add(pref);
            }

            pref.ShowLeads = settings.ShowLeads;
            pref.ShowBoards = settings.ShowBoards;
            pref.ShowMindMaps = settings.ShowMindMaps;
            pref.ShowDesigns = settings.ShowDesigns;

            if (isAdmin)
            {
                pref.ShowAdminUsers = settings.ShowAdminUsers;
                pref.ShowAdminLeaves = settings.ShowAdminLeaves;
            }

            pref.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
