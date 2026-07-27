using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Helpers;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.Leaves;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Application.Services.LeaveService
{
    public class LeaveService : ILeaveService
    {
        private const double WorkHoursPerDay = 8;

        private readonly IMVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public LeaveService(IMVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IReadOnlyList<LeaveTypeDto>> GetActiveLeaveTypesAsync(CancellationToken cancellationToken = default)
        {
            await EnsureLeaveTypesSeededAsync(cancellationToken);

            return await _context.LeaveTypes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .OrderBy(t => t.SortOrder)
                .Select(t => new LeaveTypeDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Color = t.Color,
                    IsHourlyAllowed = t.IsHourlyAllowed
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<ColleagueSelectDto>> GetColleaguesAsync(
            string adminUserId,
            CancellationToken cancellationToken = default)
        {
            var colleagueIds = await GetColleagueIdsAsync(adminUserId, cancellationToken);
            if (colleagueIds.Count == 0)
                return Array.Empty<ColleagueSelectDto>();

            return await _userManager.Users
                .AsNoTracking()
                .Where(u => colleagueIds.Contains(u.Id) && u.IsActive && !u.IsRemove)
                .OrderBy(u => u.FullName)
                .Select(u => new ColleagueSelectDto
                {
                    Id = u.Id,
                    DisplayName = string.IsNullOrWhiteSpace(u.FullName) ? u.UserName ?? u.Id : u.FullName
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<LeaveRecordListResultDto> GetMyRecordsAsync(
            string adminUserId,
            LeaveRecordFilterDto filter,
            CancellationToken cancellationToken = default)
        {
            var query = _context.LeaveRecords
                .AsNoTracking()
                .Where(r => r.CreatedByUserId == adminUserId && !r.IsDeleted);

            if (!string.IsNullOrWhiteSpace(filter.UserId))
                query = query.Where(r => r.UserId == filter.UserId);

            if (filter.LeaveTypeId.HasValue)
                query = query.Where(r => r.LeaveTypeId == filter.LeaveTypeId.Value);

            if (filter.FromDate.HasValue)
                query = query.Where(r => r.EndAt >= filter.FromDate.Value);

            if (filter.ToDate.HasValue)
            {
                var toEnd = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(r => r.StartAt < toEnd);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchNote))
            {
                var term = filter.SearchNote.Trim();
                query = query.Where(r => r.Note != null && r.Note.Contains(term));
            }

            var totalCount = await query.CountAsync(cancellationToken);

            var page = filter.Page < 1 ? 1 : filter.Page;
            var pageSize = filter.PageSize < 1 ? 20 : Math.Min(filter.PageSize, 100);

            var items = await query
                .OrderByDescending(r => r.StartAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new LeaveRecordDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    ColleagueName = r.User.FullName ?? r.User.UserName ?? r.UserId,
                    LeaveTypeId = r.LeaveTypeId,
                    LeaveTypeName = r.LeaveType.Name,
                    LeaveTypeColor = r.LeaveType.Color,
                    LeaveTypeIsHourlyAllowed = r.LeaveType.IsHourlyAllowed,
                    StartAt = r.StartAt,
                    EndAt = r.EndAt,
                    DurationDays = r.DurationDays,
                    DurationHours = r.DurationHours,
                    Note = r.Note,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);

            return new LeaveRecordListResultDto
            {
                Items = items,
                TotalCount = totalCount
            };
        }

        public async Task<LeaveRecordDto?> GetByIdAsync(int id, string adminUserId, CancellationToken cancellationToken = default)
        {
            return await _context.LeaveRecords
                .AsNoTracking()
                .Where(r => r.Id == id && r.CreatedByUserId == adminUserId && !r.IsDeleted)
                .Select(r => new LeaveRecordDto
                {
                    Id = r.Id,
                    UserId = r.UserId,
                    ColleagueName = r.User.FullName ?? r.User.UserName ?? r.UserId,
                    LeaveTypeId = r.LeaveTypeId,
                    LeaveTypeName = r.LeaveType.Name,
                    LeaveTypeColor = r.LeaveType.Color,
                    LeaveTypeIsHourlyAllowed = r.LeaveType.IsHourlyAllowed,
                    StartAt = r.StartAt,
                    EndAt = r.EndAt,
                    DurationDays = r.DurationDays,
                    DurationHours = r.DurationHours,
                    Note = r.Note,
                    CreatedAt = r.CreatedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> CreateAsync(string adminUserId, CreateLeaveRecordDto dto, CancellationToken cancellationToken = default)
        {
            await EnsureColleagueIsManagedByAdminAsync(adminUserId, dto.UserId, cancellationToken);

            var leaveType = await GetLeaveTypeOrThrowAsync(dto.LeaveTypeId, cancellationToken);
            var (startAt, endAt, durationDays, durationHours) = ParseAndCalculate(dto, leaveType);

            await EnsureNoOverlapAsync(dto.UserId, startAt, endAt, null, cancellationToken);

            var record = new LeaveRecord
            {
                UserId = dto.UserId,
                LeaveTypeId = dto.LeaveTypeId,
                StartAt = startAt,
                EndAt = endAt,
                DurationDays = durationDays,
                DurationHours = durationHours,
                Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim(),
                CreatedByUserId = adminUserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.LeaveRecords.Add(record);
            await _context.SaveChangesAsync(cancellationToken);
            return record.Id;
        }

        public async Task UpdateAsync(string adminUserId, UpdateLeaveRecordDto dto, CancellationToken cancellationToken = default)
        {
            var record = await _context.LeaveRecords
                .FirstOrDefaultAsync(r => r.Id == dto.Id && r.CreatedByUserId == adminUserId && !r.IsDeleted, cancellationToken);

            if (record == null)
                throw new InvalidOperationException("رکورد مرخصی یافت نشد.");

            await EnsureColleagueIsManagedByAdminAsync(adminUserId, dto.UserId, cancellationToken);

            var leaveType = await GetLeaveTypeOrThrowAsync(dto.LeaveTypeId, cancellationToken);
            var createDto = new CreateLeaveRecordDto
            {
                UserId = dto.UserId,
                LeaveTypeId = dto.LeaveTypeId,
                StartPersian = dto.StartPersian,
                EndPersian = dto.EndPersian,
                IsHourly = dto.IsHourly,
                DurationDays = dto.DurationDays,
                Note = dto.Note
            };
            var (startAt, endAt, durationDays, durationHours) = ParseAndCalculate(createDto, leaveType);

            await EnsureNoOverlapAsync(dto.UserId, startAt, endAt, dto.Id, cancellationToken);

            record.UserId = dto.UserId;
            record.LeaveTypeId = dto.LeaveTypeId;
            record.StartAt = startAt;
            record.EndAt = endAt;
            record.DurationDays = durationDays;
            record.DurationHours = durationHours;
            record.Note = string.IsNullOrWhiteSpace(dto.Note) ? null : dto.Note.Trim();
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedByUserId = adminUserId;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task DeleteAsync(int id, string adminUserId, CancellationToken cancellationToken = default)
        {
            var record = await _context.LeaveRecords
                .FirstOrDefaultAsync(r => r.Id == id && r.CreatedByUserId == adminUserId && !r.IsDeleted, cancellationToken);

            if (record == null)
                throw new InvalidOperationException("رکورد مرخصی یافت نشد.");

            record.IsDeleted = true;
            record.UpdatedAt = DateTime.UtcNow;
            record.UpdatedByUserId = adminUserId;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<LeaveStatsDto> GetStatsAsync(
            string adminUserId,
            DateTime from,
            DateTime to,
            CancellationToken cancellationToken = default)
        {
            var fromDate = from.Date;
            var toEnd = to.Date.AddDays(1);

            var records = await _context.LeaveRecords
                .AsNoTracking()
                .Where(r => r.CreatedByUserId == adminUserId
                            && !r.IsDeleted
                            && r.EndAt >= fromDate
                            && r.StartAt < toEnd)
                .Select(r => new
                {
                    r.UserId,
                    ColleagueName = r.User.FullName ?? r.User.UserName ?? r.UserId,
                    r.LeaveTypeId,
                    LeaveTypeName = r.LeaveType.Name,
                    LeaveTypeColor = r.LeaveType.Color,
                    r.DurationDays,
                    r.DurationHours
                })
                .ToListAsync(cancellationToken);

            var byType = records
                .GroupBy(r => new { r.LeaveTypeId, r.LeaveTypeName, r.LeaveTypeColor })
                .Select(g => new LeaveTypeStatDto
                {
                    LeaveTypeId = g.Key.LeaveTypeId,
                    LeaveTypeName = g.Key.LeaveTypeName,
                    Color = g.Key.LeaveTypeColor,
                    TotalHours = Math.Round(g.Sum(x => x.DurationHours), 2),
                    TotalDays = Math.Round(g.Sum(x => EffectiveDays(x.DurationDays, x.DurationHours)), 2),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalHours)
                .ToList();

            var byColleague = records
                .GroupBy(r => new { r.UserId, r.ColleagueName })
                .Select(g => new ColleagueLeaveStatDto
                {
                    UserId = g.Key.UserId,
                    ColleagueName = g.Key.ColleagueName,
                    TotalHours = Math.Round(g.Sum(x => x.DurationHours), 2),
                    TotalDays = Math.Round(g.Sum(x => EffectiveDays(x.DurationDays, x.DurationHours)), 2),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalHours)
                .Take(20)
                .ToList();

            return new LeaveStatsDto
            {
                TotalHours = Math.Round(records.Sum(r => r.DurationHours), 2),
                TotalDays = Math.Round(records.Sum(r => EffectiveDays(r.DurationDays, r.DurationHours)), 2),
                TotalRecords = records.Count,
                ByType = byType,
                ByColleague = byColleague
            };
        }

        public async Task<IReadOnlyList<LeaveCalendarDayDto>> GetCalendarAsync(
            string adminUserId,
            DateTime startOfWeek,
            DateTime endOfWeek,
            CancellationToken cancellationToken = default)
        {
            var weekStart = startOfWeek.Date;
            var weekEnd = endOfWeek.Date;

            var records = await _context.LeaveRecords
                .AsNoTracking()
                .Where(r => r.CreatedByUserId == adminUserId
                            && !r.IsDeleted
                            && r.StartAt.Date <= weekEnd
                            && r.EndAt.Date >= weekStart)
                .Select(r => new LeaveCalendarItemDto
                {
                    Id = r.Id,
                    ColleagueName = r.User.FullName ?? r.User.UserName ?? r.UserId,
                    LeaveTypeName = r.LeaveType.Name,
                    LeaveTypeColor = r.LeaveType.Color,
                    StartAt = r.StartAt,
                    EndAt = r.EndAt
                })
                .ToListAsync(cancellationToken);

            var days = new List<LeaveCalendarDayDto>();
            for (var day = weekStart; day <= weekEnd; day = day.AddDays(1))
            {
                var dayItems = records
                    .Where(r => r.StartAt.Date <= day && r.EndAt.Date >= day)
                    .OrderBy(r => r.StartAt)
                    .ToList();

                days.Add(new LeaveCalendarDayDto
                {
                    Date = day,
                    Items = dayItems
                });
            }

            return days;
        }

        private async Task EnsureLeaveTypesSeededAsync(CancellationToken cancellationToken)
        {
            if (await _context.LeaveTypes.AnyAsync(cancellationToken))
                return;

            foreach (var seed in LeaveTypeDefaults.CreateSeedTypes())
            {
                _context.LeaveTypes.Add(new LeaveType
                {
                    Name = seed.Name,
                    Color = seed.Color,
                    IsHourlyAllowed = seed.IsHourlyAllowed,
                    IsActive = seed.IsActive,
                    SortOrder = seed.SortOrder
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// همکاران ادمین: دعوت سیستم/پروژه + اعضای پروژه‌های مشترک (همان منطق ProjectsController).
        /// </summary>
        private async Task<List<string>> GetColleagueIdsAsync(string adminUserId, CancellationToken cancellationToken)
        {
            var admin = await _userManager.FindByIdAsync(adminUserId);
            if (admin == null)
                return new List<string>();

            var phone = admin.Phone;

            var acceptedInvitations = await _context.ProjectInvitations
                .AsNoTracking()
                .Where(i =>
                    i.Status == InvitationStatus.Accepted &&
                    (
                        (i.InviterId == adminUserId && (!string.IsNullOrEmpty(i.InviteeId) || !string.IsNullOrEmpty(i.InviteePhone))) ||
                        (!string.IsNullOrWhiteSpace(phone) && i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviterId))
                    ))
                .Select(i => new
                {
                    i.InviterId,
                    i.InviteeId,
                    i.InviteePhone
                })
                .ToListAsync(cancellationToken);

            var inviteeIds = acceptedInvitations
                .Where(i => i.InviterId == adminUserId && !string.IsNullOrEmpty(i.InviteeId))
                .Select(i => i.InviteeId!);

            var inviterIds = acceptedInvitations
                .Where(i => !string.IsNullOrWhiteSpace(phone) && i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviterId))
                .Select(i => i.InviterId);

            var phoneNumbers = acceptedInvitations
                .Where(i => i.InviterId == adminUserId && string.IsNullOrEmpty(i.InviteeId) && !string.IsNullOrEmpty(i.InviteePhone))
                .Select(i => i.InviteePhone!)
                .Distinct()
                .ToList();

            var usersByPhoneIds = phoneNumbers.Count == 0
                ? new List<string>()
                : await _userManager.Users
                    .AsNoTracking()
                    .Where(u => phoneNumbers.Contains(u.Phone))
                    .Select(u => u.Id)
                    .ToListAsync(cancellationToken);

            var membersOfMyProjects = await (
                    from pm in _context.ProjectMembers.AsNoTracking()
                    join p in _context.Projects.AsNoTracking() on pm.ProjectId!.Value equals p.Id
                    where p.CreatorUserId == adminUserId && pm.UserId != adminUserId
                    select pm.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var projectsIMemberOf = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.UserId == adminUserId && pm.ProjectId.HasValue)
                .Select(pm => pm.ProjectId!.Value)
                .ToListAsync(cancellationToken);

            var creatorsOfMyProjects = projectsIMemberOf.Count == 0
                ? new List<string>()
                : await _context.Projects
                    .AsNoTracking()
                    .Where(p => projectsIMemberOf.Contains(p.Id) && p.CreatorUserId != adminUserId)
                    .Select(p => p.CreatorUserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

            var otherMembersOfMyProjects = projectsIMemberOf.Count == 0
                ? new List<string>()
                : await _context.ProjectMembers
                    .AsNoTracking()
                    .Where(pm => pm.ProjectId.HasValue
                                 && projectsIMemberOf.Contains(pm.ProjectId.Value)
                                 && pm.UserId != adminUserId)
                    .Select(pm => pm.UserId)
                    .Distinct()
                    .ToListAsync(cancellationToken);

            return inviteeIds
                .Concat(inviterIds)
                .Concat(usersByPhoneIds)
                .Concat(membersOfMyProjects)
                .Concat(creatorsOfMyProjects)
                .Concat(otherMembersOfMyProjects)
                .Where(id => !string.IsNullOrEmpty(id) && id != adminUserId)
                .Distinct()
                .ToList();
        }

        private async Task EnsureColleagueIsManagedByAdminAsync(
            string adminUserId,
            string colleagueUserId,
            CancellationToken cancellationToken)
        {
            var colleagueIds = await GetColleagueIdsAsync(adminUserId, cancellationToken);
            if (!colleagueIds.Contains(colleagueUserId))
                throw new InvalidOperationException("همکار انتخاب‌شده در لیست همکاران شما نیست.");

            var isActive = await _userManager.Users
                .AnyAsync(u => u.Id == colleagueUserId && u.IsActive && !u.IsRemove, cancellationToken);

            if (!isActive)
                throw new InvalidOperationException("همکار انتخاب‌شده فعال نیست.");
        }

        private async Task<LeaveType> GetLeaveTypeOrThrowAsync(int leaveTypeId, CancellationToken cancellationToken)
        {
            var leaveType = await _context.LeaveTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == leaveTypeId && t.IsActive, cancellationToken);

            if (leaveType == null)
                throw new InvalidOperationException("نوع مرخصی انتخاب‌شده معتبر نیست.");

            return leaveType;
        }

        private static (DateTime StartAt, DateTime EndAt, int? DurationDays, double DurationHours) ParseAndCalculate(
            CreateLeaveRecordDto dto,
            LeaveType leaveType)
        {
            if (dto.IsHourly && !leaveType.IsHourlyAllowed)
                throw new InvalidOperationException("برای این نوع مرخصی، ثبت ساعتی مجاز نیست.");

            DateTime startAt;
            DateTime endAt;

            if (dto.IsHourly)
            {
                startAt = PersianDateTimeHelper.ParsePersianDateTime(dto.StartPersian);
                endAt = PersianDateTimeHelper.ParsePersianDateTime(dto.EndPersian);
            }
            else
            {
                startAt = PersianDateTimeHelper.ParsePersianDate(dto.StartPersian);
                endAt = PersianDateTimeHelper.ParsePersianDate(dto.EndPersian);
            }

            if (endAt < startAt)
                throw new InvalidOperationException("تاریخ/زمان پایان باید بعد از شروع باشد.");

            if (dto.IsHourly)
            {
                var durationHours = Math.Round((endAt - startAt).TotalHours, 2);
                if (durationHours <= 0)
                    throw new InvalidOperationException("مدت مرخصی باید بیشتر از صفر باشد.");

                return (startAt, endAt, null, durationHours);
            }

            var calendarDays = (endAt.Date - startAt.Date).Days + 1;
            if (dto.DurationDays is null or < 1)
                throw new InvalidOperationException("تعداد روز مرخصی الزامی است و باید حداقل ۱ باشد.");

            if (dto.DurationDays > calendarDays)
                throw new InvalidOperationException(
                    $"تعداد روز مرخصی ({dto.DurationDays}) نمی‌تواند بیشتر از بازه تقویمی ({calendarDays} روز) باشد.");

            var durationHoursDaily = Math.Round(dto.DurationDays.Value * WorkHoursPerDay, 2);
            return (startAt, endAt, dto.DurationDays.Value, durationHoursDaily);
        }

        private static double EffectiveDays(int? durationDays, double durationHours) =>
            durationDays ?? Math.Round(durationHours / WorkHoursPerDay, 2);

        private async Task EnsureNoOverlapAsync(
            string userId,
            DateTime startAt,
            DateTime endAt,
            int? excludeId,
            CancellationToken cancellationToken)
        {
            var query = _context.LeaveRecords
                .AsNoTracking()
                .Where(r => r.UserId == userId
                            && !r.IsDeleted
                            && r.StartAt <= endAt
                            && r.EndAt >= startAt);

            if (excludeId.HasValue)
                query = query.Where(r => r.Id != excludeId.Value);

            if (await query.AnyAsync(cancellationToken))
                throw new InvalidOperationException("این همکار در بازه زمانی انتخاب‌شده قبلاً مرخصی دارد.");
        }
    }
}
