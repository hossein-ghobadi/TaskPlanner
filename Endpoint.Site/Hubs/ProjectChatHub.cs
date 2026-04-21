using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Hubs
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    public class ProjectChatHub : Hub
    {
        private readonly MVPTestDatabaseContext _context;

        public ProjectChatHub(MVPTestDatabaseContext context)
        {
            _context = context;
        }

        public async Task JoinProjectGroup(int groupId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            var isMember = await _context.ProjectChatGroupMembers
                .AnyAsync(m => m.ProjectChatGroupId == groupId && m.UserId == userId);

            if (!isMember)
            {
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetSignalRGroupName(groupId));
        }

        public async Task LeaveProjectGroup(int groupId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetSignalRGroupName(groupId));
        }

        public static string GetSignalRGroupName(int groupId)
        {
            return $"project-chat-group-{groupId}";
        }
    }
}
