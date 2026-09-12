using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.MyWorkService
{
    public class MyWorkService : IMyWorkService
    {
        private readonly IMVPTestDatabaseContext _context;

        public MyWorkService(IMVPTestDatabaseContext context)
        {
            _context = context;
        }

        public async Task<MyWorkPageDto> GetMyWorkAsync(string userId, MyWorkQuery query, CancellationToken cancellationToken = default)
        {
            query ??= new MyWorkQuery();

            var projectOptions = await _context.Projects
                .AsNoTracking()
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .OrderBy(p => p.Name)
                .Select(p => new MyWorkProjectOptionDto { Id = p.Id, Name = p.Name })
                .ToListAsync(cancellationToken);

            var projectIds = projectOptions.Select(p => p.Id).ToList();
            if (query.ProjectId.HasValue && !projectIds.Contains(query.ProjectId.Value))
                query.ProjectId = null;

            if (projectIds.Count == 0)
            {
                return new MyWorkPageDto
                {
                    Scope = query.Scope,
                    ProjectId = query.ProjectId,
                    IncludeCompleted = query.IncludeCompleted,
                    DueFilter = query.DueFilter,
                    Projects = projectOptions
                };
            }

            var scopedProjectIds = query.ProjectId.HasValue
                ? new List<int> { query.ProjectId.Value }
                : projectIds;

            var projectTasks = _context.TaskItems
                .AsNoTracking()
                .Where(t => scopedProjectIds.Contains(t.ProjectId));

            // Story / Task / Bug (سطح قابل نمایش در کارت‌ها)
            var parentLevelTasks = projectTasks.Where(t =>
                (t.ProjectIssueTypeId != null && t.ProjectIssueType != null && t.ProjectIssueType.CanAddToSprint)
                || (t.ProjectIssueTypeId == null
                    && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug)));

            // کارک‌ها — فقط برای پیدا کردن تسک والد
            var subtasks = projectTasks.Where(t =>
                (t.ProjectIssueTypeId != null && t.ProjectIssueType != null && t.ProjectIssueType.Level == IssueTypeLevel.Subtask)
                || (t.ProjectIssueTypeId == null && t.IssueType == IssueType.Subtask));

            var assignedOpenCount = await CountDistinctParentsForScopeAsync(
                parentLevelTasks, subtasks, userId, MyWorkScope.Assigned, openOnly: true, cancellationToken);
            var createdOpenCount = await CountDistinctParentsForScopeAsync(
                parentLevelTasks, subtasks, userId, MyWorkScope.Created, openOnly: true, cancellationToken);

            var localToday = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local).Date;
            var localWeekEnd = localToday.AddDays(7);

            // شناسه والدها از تسک‌های مستقیم مطابق scope
            var directParentsQuery = ApplyScope(parentLevelTasks, userId, query.Scope);
            var directParentSnapshots = await directParentsQuery
                .Select(t => new MatchSnapshot(t.Id, t.IsCompleted, t.DueDate))
                .ToListAsync(cancellationToken);

            // شناسه والدها از کارک‌های مطابق scope (خود کارک نمایش داده نمی‌شود)
            var matchingSubtasksQuery = ApplyScope(subtasks, userId, query.Scope)
                .Where(t => t.ParentTaskId != null);
            var subtaskSnapshots = await matchingSubtasksQuery
                .Select(t => new MatchSnapshot(t.ParentTaskId!.Value, t.IsCompleted, t.DueDate))
                .ToListAsync(cancellationToken);

            var relevantParentIds = ResolveParentIds(
                directParentSnapshots,
                subtaskSnapshots,
                query.IncludeCompleted,
                query.DueFilter,
                localToday,
                localWeekEnd);

            // آمار روی والدها (قبل از فیلتر موعد)، با منطق تکمیل‌شدن کارک/تسک
            var statsParentIds = ResolveParentIds(
                directParentSnapshots,
                subtaskSnapshots,
                includeCompleted: true,
                MyWorkDueFilter.All,
                localToday,
                localWeekEnd);

            var statsRows = statsParentIds.Count == 0
                ? new List<TaskRow>()
                : await LoadTaskRowsAsync(statsParentIds, cancellationToken);

            var openCount = statsRows.Count(t => !t.IsCompleted);
            var completedCount = statsRows.Count(t => t.IsCompleted);
            var overdueCount = statsRows.Count(t =>
                !t.IsCompleted && t.DueDate.HasValue && ToLocalDate(t.DueDate.Value) < localToday);

            var rows = relevantParentIds.Count == 0
                ? new List<TaskRow>()
                : await LoadTaskRowsAsync(relevantParentIds, cancellationToken);

            if (!query.IncludeCompleted)
                rows = rows.Where(t => !t.IsCompleted).ToList();

            var items = rows
                .OrderBy(t => t.IsCompleted)
                .ThenBy(t => t.DueDate.HasValue ? 0 : 1)
                .ThenBy(t => t.DueDate)
                .ThenByDescending(t => t.Priority)
                .ThenBy(t => t.Title)
                .Select(t => new MyWorkItemDto
                {
                    Id = t.Id,
                    IssueKey = t.IssueKey,
                    Title = t.Title,
                    ProjectId = t.ProjectId,
                    ProjectName = t.ProjectName,
                    IssueType = t.IssueType,
                    IssueTypeName = !string.IsNullOrWhiteSpace(t.ProjectIssueTypeName)
                        ? t.ProjectIssueTypeName!
                        : t.IssueType.GetDisplayName(),
                    IssueTypeIcon = !string.IsNullOrWhiteSpace(t.ProjectIssueTypeIcon)
                        ? t.ProjectIssueTypeIcon!
                        : t.IssueType.GetIcon(),
                    Priority = t.Priority,
                    IsCompleted = t.IsCompleted,
                    DueDate = t.DueDate,
                    SprintName = t.SprintName,
                    WorkflowStatusName = t.WorkflowStatusName,
                    WorkflowStatusColor = t.WorkflowStatusColor,
                    AssignedUserName = t.AssignedUserName,
                    IsAssignedToMe = string.Equals(t.AssignedUserId, userId, StringComparison.Ordinal),
                    IsCreatedByMe = string.Equals(t.CreatedByUserId, userId, StringComparison.Ordinal)
                })
                .ToList();

            return new MyWorkPageDto
            {
                Scope = query.Scope,
                ProjectId = query.ProjectId,
                IncludeCompleted = query.IncludeCompleted,
                DueFilter = query.DueFilter,
                AssignedOpenCount = assignedOpenCount,
                CreatedOpenCount = createdOpenCount,
                OpenCount = openCount,
                OverdueCount = overdueCount,
                CompletedCount = completedCount,
                Projects = projectOptions,
                Items = items
            };
        }

        private async Task<int> CountDistinctParentsForScopeAsync(
            IQueryable<TaskItem> parentLevelTasks,
            IQueryable<TaskItem> subtasks,
            string userId,
            MyWorkScope scope,
            bool openOnly,
            CancellationToken cancellationToken)
        {
            var direct = ApplyScope(parentLevelTasks, userId, scope);
            var viaSub = ApplyScope(subtasks, userId, scope).Where(t => t.ParentTaskId != null);

            if (openOnly)
            {
                direct = direct.Where(t => !t.IsCompleted);
                viaSub = viaSub.Where(t => !t.IsCompleted);
            }

            var directIds = direct.Select(t => t.Id);
            var parentIdsFromSub = viaSub.Select(t => t.ParentTaskId!.Value);

            return await directIds.Union(parentIdsFromSub).Distinct().CountAsync(cancellationToken);
        }

        private async Task<List<TaskRow>> LoadTaskRowsAsync(HashSet<int> parentIds, CancellationToken cancellationToken)
        {
            var idList = parentIds.ToList();
            return await _context.TaskItems
                .AsNoTracking()
                .Where(t => idList.Contains(t.Id))
                .Select(t => new TaskRow(
                    t.Id,
                    t.IssueKey,
                    t.Title,
                    t.ProjectId,
                    t.Project.Name,
                    t.IssueType,
                    t.ProjectIssueType != null ? t.ProjectIssueType.Name : null,
                    t.ProjectIssueType != null ? t.ProjectIssueType.Icon : null,
                    t.Priority,
                    t.IsCompleted,
                    t.DueDate,
                    t.Sprint != null ? t.Sprint.Name : null,
                    t.WorkflowStatus != null ? t.WorkflowStatus.Name : (t.Status != null ? t.Status.Name : null),
                    t.WorkflowStatus != null ? t.WorkflowStatus.Color : (t.Status != null ? t.Status.Color : null),
                    t.AssignedUser != null ? t.AssignedUser.UserName : null,
                    t.AssignedUserId,
                    t.CreatedByUserId))
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// والدهایی که یا خودشان با فیلتر جورند، یا از طریق کارک مرتبط وارد می‌شوند.
        /// فیلتر موعد برای مسیر کارک روی موعد خود کارک اعمال می‌شود.
        /// </summary>
        private static HashSet<int> ResolveParentIds(
            List<MatchSnapshot> directParents,
            List<MatchSnapshot> subtaskMatches,
            bool includeCompleted,
            MyWorkDueFilter dueFilter,
            DateTime localToday,
            DateTime localWeekEnd)
        {
            var ids = new HashSet<int>();

            foreach (var t in directParents)
            {
                if (!includeCompleted && t.IsCompleted)
                    continue;
                if (!MatchesDue(t.DueDate, t.IsCompleted, dueFilter, localToday, localWeekEnd))
                    continue;
                ids.Add(t.Id);
            }

            foreach (var s in subtaskMatches)
            {
                if (!includeCompleted && s.IsCompleted)
                    continue;
                if (!MatchesDue(s.DueDate, s.IsCompleted, dueFilter, localToday, localWeekEnd))
                    continue;
                ids.Add(s.Id); // Id اینجا ParentTaskId است
            }

            return ids;
        }

        private static bool MatchesDue(
            DateTime? dueDate,
            bool isCompleted,
            MyWorkDueFilter dueFilter,
            DateTime localToday,
            DateTime localWeekEnd)
        {
            return dueFilter switch
            {
                MyWorkDueFilter.Overdue =>
                    !isCompleted && dueDate.HasValue && ToLocalDate(dueDate.Value) < localToday,
                MyWorkDueFilter.Today =>
                    dueDate.HasValue && ToLocalDate(dueDate.Value) == localToday,
                MyWorkDueFilter.ThisWeek =>
                    dueDate.HasValue
                    && ToLocalDate(dueDate.Value) >= localToday
                    && ToLocalDate(dueDate.Value) < localWeekEnd,
                _ => true
            };
        }

        private static IQueryable<TaskItem> ApplyScope(IQueryable<TaskItem> query, string userId, MyWorkScope scope)
        {
            return scope switch
            {
                MyWorkScope.Created => query.Where(t => t.CreatedByUserId == userId),
                MyWorkScope.All => query.Where(t => t.AssignedUserId == userId || t.CreatedByUserId == userId),
                _ => query.Where(t => t.AssignedUserId == userId)
            };
        }

        private static DateTime ToLocalDate(DateTime dateTime)
        {
            var local = dateTime.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.Local)
                : dateTime.ToLocalTime();
            return local.Date;
        }

        private sealed record MatchSnapshot(int Id, bool IsCompleted, DateTime? DueDate);

        private sealed record TaskRow(
            int Id,
            string? IssueKey,
            string Title,
            int ProjectId,
            string ProjectName,
            IssueType IssueType,
            string? ProjectIssueTypeName,
            string? ProjectIssueTypeIcon,
            TaskPriority Priority,
            bool IsCompleted,
            DateTime? DueDate,
            string? SprintName,
            string? WorkflowStatusName,
            string? WorkflowStatusColor,
            string? AssignedUserName,
            string? AssignedUserId,
            string? CreatedByUserId);
    }
}
