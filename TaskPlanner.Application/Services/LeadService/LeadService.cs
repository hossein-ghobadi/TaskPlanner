using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Application.Services.NotificationService;
using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Application.Services.LeadService
{
    public class LeadService : ILeadService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly IProjectCommandService _projectCommandService;
        private readonly IProjectQueryService _projectQueryService;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;

        public LeadService(
            IMVPTestDatabaseContext context,
            IProjectCommandService projectCommandService,
            IProjectQueryService projectQueryService,
            UserManager<User> userManager,
            INotificationService notificationService)
        {
            _context = context;
            _projectCommandService = projectCommandService;
            _projectQueryService = projectQueryService;
            _userManager = userManager;
            _notificationService = notificationService;
        }

        public async Task<IReadOnlyList<LeadListItemDto>> GetMyLeadsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var ownedIds = await _context.Set<Lead>()
                .Where(l => l.OwnerUserId == userId)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);

            var memberLeadIds = await _context.Set<LeadMember>()
                .Where(m => m.UserId == userId)
                .Select(m => m.LeadId)
                .ToListAsync(cancellationToken);

            var allIds = ownedIds.Union(memberLeadIds).Distinct().ToList();
            if (!allIds.Any())
                return Array.Empty<LeadListItemDto>();

            return await _context.Set<Lead>()
                .AsNoTracking()
                .Where(l => allIds.Contains(l.Id))
                .OrderByDescending(l => l.UpdatedAt)
                .Select(l => new LeadListItemDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    CompanyName = l.CompanyName,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt,
                    MeetingAt = l.MeetingAt,
                    NextSessionAt =
                        _context.Set<LeadSession>()
                            .Where(s => s.LeadId == l.Id && s.ScheduledAt >= DateTime.UtcNow)
                            .OrderBy(s => s.ScheduledAt)
                            .Select(s => (DateTime?)s.ScheduledAt)
                            .FirstOrDefault()
                        ??
                        _context.Set<LeadSession>()
                            .Where(s => s.LeadId == l.Id)
                            .OrderByDescending(s => s.ScheduledAt)
                            .Select(s => (DateTime?)s.ScheduledAt)
                            .FirstOrDefault(),
                    ConvertedProjectId = l.ConvertedProjectId,
                    IsOwner = l.OwnerUserId == userId
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<LeadDetailsDto?> GetDetailsAsync(int id, string userId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>().AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (lead == null)
                return null;

            var isOwner = lead.OwnerUserId == userId;
            var isMember = await _context.Set<LeadMember>()
                .AnyAsync(m => m.LeadId == id && m.UserId == userId, cancellationToken);
            if (!isOwner && !isMember)
                return null;

            var members = await _context.Set<LeadMember>()
                .AsNoTracking()
                .Where(m => m.LeadId == id)
                .ToListAsync(cancellationToken);

            var memberUserIds = members.Select(m => m.UserId).Distinct().ToList();
            var nameLookup = await _userManager.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : (u.UserName ?? "کاربر"),
                    cancellationToken);

            var pendingInvites = await _context.Set<LeadInvitation>()
                .AsNoTracking()
                .Where(i => i.LeadId == id && i.Status == InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);
            var sessions = await _context.Set<LeadSession>()
                .AsNoTracking()
                .Where(s => s.LeadId == id)
                .OrderBy(s => s.ScheduledAt)
                .ToListAsync(cancellationToken);
            var leadNotes = await _context.ProjectNotes
                .AsNoTracking()
                .Include(n => n.Attachments)
                .Where(n => n.LeadId == id)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync(cancellationToken);

            var invitePhones = pendingInvites.Select(i => i.InviteePhone).Distinct().ToList();
            var usersByPhone = await _userManager.Users
                .Where(u => u.Phone != null && invitePhones.Contains(u.Phone))
                .ToDictionaryAsync(u => u.Phone!, u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : (u.UserName ?? "کاربر"), cancellationToken);

            return new LeadDetailsDto
            {
                Id = lead.Id,
                Title = lead.Title,
                CompanyName = lead.CompanyName,
                ContactName = lead.ContactName,
                Phone = lead.Phone,
                Email = lead.Email,
                Notes = lead.Notes,
                Source = lead.Source,
                Status = lead.Status,
                CreatedAt = lead.CreatedAt,
                UpdatedAt = lead.UpdatedAt,
                MeetingAt = lead.MeetingAt,
                ConvertedProjectId = lead.ConvertedProjectId,
                ConvertedAt = lead.ConvertedAt,
                OwnerUserId = lead.OwnerUserId,
                IsOwner = isOwner,
                Members = members.Select(m => new LeadMemberSummaryDto
                {
                    UserId = m.UserId,
                    DisplayName = nameLookup.GetValueOrDefault(m.UserId, "کاربر")
                }).ToList(),
                PendingInvitations = pendingInvites.Select(i => new LeadPendingInviteDto
                {
                    Id = i.Id,
                    InviteePhone = i.InviteePhone,
                    InviteeDisplayName = usersByPhone.TryGetValue(i.InviteePhone, out var dn) ? dn : null
                }).ToList(),
                Sessions = sessions.Select(s => new LeadSessionDto
                {
                    Id = s.Id,
                    ScheduledAt = s.ScheduledAt,
                    Notes = s.Notes
                }).ToList(),
                NotesList = leadNotes.Select(n => new LeadNoteDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    CreatedAt = n.CreatedAt,
                    Attachments = n.Attachments.Select(a => new LeadNoteAttachmentDto
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FileType = a.FileType,
                        FilePath = a.FilePath
                    }).ToList()
                }).ToList()
            };
        }

        public async Task<int> CreateAsync(CreateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default)
        {
            var status = NormalizeManualStatus(dto.Status);
            var now = DateTime.UtcNow;
            var lead = new Lead
            {
                Title = dto.Title.Trim(),
                CompanyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? null : dto.CompanyName.Trim(),
                ContactName = string.IsNullOrWhiteSpace(dto.ContactName) ? null : dto.ContactName.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Source = string.IsNullOrWhiteSpace(dto.Source) ? null : dto.Source.Trim(),
                Status = status,
                OwnerUserId = ownerUserId,
                CreatedAt = now,
                UpdatedAt = now,
                MeetingAt = NormalizeMeetingAtToUtc(dto.MeetingAt)
            };

            _context.Set<Lead>().Add(lead);
            await _context.SaveChangesAsync(cancellationToken);
            return lead.Id;
        }

        public async Task UpdateAsync(UpdateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == dto.Id && l.OwnerUserId == ownerUserId, cancellationToken);

            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            if (lead.Status == LeadPipelineStatus.Converted)
                throw new InvalidOperationException("لید تبدیل‌شده قابل ویرایش نیست.");

            var newStatus = NormalizeManualStatus(dto.Status);
            if (newStatus == LeadPipelineStatus.Converted)
                throw new InvalidOperationException("وضعیت «تبدیل‌شده» فقط پس از ایجاد پروژه ثبت می‌شود.");

            lead.Title = dto.Title.Trim();
            lead.CompanyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? null : dto.CompanyName.Trim();
            lead.ContactName = string.IsNullOrWhiteSpace(dto.ContactName) ? null : dto.ContactName.Trim();
            lead.Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim();
            lead.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
            lead.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            lead.Source = string.IsNullOrWhiteSpace(dto.Source) ? null : dto.Source.Trim();
            lead.Status = newStatus;
            lead.MeetingAt = NormalizeMeetingAtToUtc(dto.MeetingAt);
            lead.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> ConvertToProjectAsync(int leadId, string ownerUserId, string? projectNameOverride, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId && l.OwnerUserId == ownerUserId, cancellationToken);

            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            if (lead.ConvertedProjectId.HasValue)
                throw new InvalidOperationException("این لید قبلاً به پروژه تبدیل شده است.");

            if (lead.Status != LeadPipelineStatus.Qualified)
                throw new InvalidOperationException("فقط لیدهایی با وضعیت «قطعیت / واجد شرایط» را می‌توان به پروژه تبدیل کرد. ابتدا وضعیت را به «قطعیت» برگردانید.");

            var projectName = string.IsNullOrWhiteSpace(projectNameOverride) ? lead.Title : projectNameOverride.Trim();
            if (string.IsNullOrWhiteSpace(projectName))
                throw new InvalidOperationException("نام پروژه نمی‌تواند خالی باشد.");

            var description = BuildProjectDescriptionFromLead(lead);

            var projectId = await _projectCommandService.CreateProjectAsync(
                new CreateProjectDto
                {
                    Name = projectName,
                    Description = description,
                    SelectedUserIds = new List<string>()
                },
                ownerUserId);

            var notesForProject = await _context.ProjectNotes
                .Where(n => n.LeadId == leadId)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync(cancellationToken);
            foreach (var leadNote in notesForProject)
            {
                leadNote.ProjectId = projectId;
                leadNote.SourceLeadId ??= leadId;
            }

            lead.ConvertedProjectId = projectId;
            lead.ConvertedAt = DateTime.UtcNow;
            lead.Status = LeadPipelineStatus.Converted;
            lead.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
            return projectId;
        }

        public async Task DeleteAsync(int id, string ownerUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");
            if (lead.OwnerUserId != ownerUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند آن را حذف کند.");

            _context.Set<Lead>().Remove(lead);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserSelectDto>> GetAvailableCollaboratorsForLeadAsync(int leadId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>().AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null || lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("لید یافت نشد یا دسترسی ندارید.");

            var existingMemberIds = await _context.Set<LeadMember>()
                .Where(m => m.LeadId == leadId)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

            var pool = await _projectQueryService.GetAvailableUsersForCreateAsync(requesterUserId);
            return pool.Where(u => u.Id != lead.OwnerUserId && !existingMemberIds.Contains(u.Id)).ToList();
        }

        public async Task AddLeadMemberAsync(int leadId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null || lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند همکار اضافه کند.");

            if (string.Equals(memberUserId, lead.OwnerUserId, StringComparison.Ordinal))
                throw new InvalidOperationException("مالک لید نیازی به افزودن خود به‌عنوان همکار ندارد.");

            var exists = await _context.Set<LeadMember>()
                .AnyAsync(m => m.LeadId == leadId && m.UserId == memberUserId, cancellationToken);
            if (exists)
                throw new InvalidOperationException("این کاربر از قبل همکار این لید است.");

            _context.Set<LeadMember>().Add(new LeadMember
            {
                LeadId = leadId,
                UserId = memberUserId,
                AddedByUserId = requesterUserId,
                CreatedAt = DateTime.UtcNow
            });
            lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveLeadMemberAsync(int leadId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null || lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند همکار را حذف کند.");

            var row = await _context.Set<LeadMember>()
                .FirstOrDefaultAsync(m => m.LeadId == leadId && m.UserId == memberUserId, cancellationToken);
            if (row == null)
                throw new InvalidOperationException("همکار در این لید یافت نشد.");

            _context.Set<LeadMember>().Remove(row);
            lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task InviteUserToLeadAsync(int leadId, string phone, string inviterId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("شماره تلفن وارد نشده است.", nameof(phone));
            phone = phone.Trim();

            var lead = await _context.Set<Lead>().FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null || lead.OwnerUserId != inviterId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند دعوت ارسال کند.");

            var inviter = await _userManager.FindByIdAsync(inviterId);
            if (inviter != null && !string.IsNullOrWhiteSpace(inviter.Phone) &&
                string.Equals(inviter.Phone, phone, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("نمی‌توانید خودتان را دعوت کنید.");

            var existsPending = await _context.Set<LeadInvitation>()
                .AnyAsync(i => i.LeadId == leadId && i.InviteePhone == phone && i.Status == InvitationStatus.Pending, cancellationToken);
            if (existsPending)
                throw new InvalidOperationException("برای این شماره قبلاً دعوت در انتظار ارسال شده است.");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("کاربری با این شماره پیدا نشد.");

            if (string.Equals(user.Id, lead.OwnerUserId, StringComparison.Ordinal))
                throw new InvalidOperationException("مالک لید نیازی به دعوت ندارد.");

            var isMember = await _context.Set<LeadMember>()
                .AnyAsync(m => m.LeadId == leadId && m.UserId == user.Id, cancellationToken);
            if (isMember)
                throw new InvalidOperationException("این کاربر هم‌اکنون همکار این لید است.");

            var invite = new LeadInvitation
            {
                LeadId = leadId,
                InviterId = inviterId,
                InviteePhone = phone,
                InviteeId = user.Id,
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.Set<LeadInvitation>().Add(invite);
            lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);

            await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
            {
                UserId = user.Id,
                Title = "دعوت به لید (CRM)",
                Message = $"برای شما دعوتی به لید «{lead.Title}» ارسال شد.",
                RelatedEntityId = invite.Id.ToString(),
                RelatedEntityType = nameof(LeadInvitation),
                Type = NotificationCreateType.LeadInvitation,
                PayloadJson = JsonSerializer.Serialize(new { invitationId = invite.Id, leadId = leadId })
            }, cancellationToken);
        }

        public async Task CancelLeadInvitationAsync(int invitationId, string inviterUserId, CancellationToken cancellationToken = default)
        {
            var invite = await _context.Set<LeadInvitation>()
                .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
            if (invite == null || invite.InviterId != inviterUserId)
                throw new InvalidOperationException("دعوت یافت نشد.");
            if (invite.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("فقط دعوت‌های در انتظار قابل لغو هستند.");

            _context.Set<LeadInvitation>().Remove(invite);
            var lead = await _context.Set<Lead>().FirstOrDefaultAsync(l => l.Id == invite.LeadId, cancellationToken);
            if (lead != null)
                lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RespondToLeadInvitationAsync(int invitationId, string currentUserId, string currentUserPhone, bool accept, CancellationToken cancellationToken = default)
        {
            var invite = await _context.Set<LeadInvitation>()
                .Include(i => i.Lead)
                .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
            if (invite == null)
                throw new InvalidOperationException("دعوت یافت نشد.");

            if (!string.Equals(invite.InviteePhone, currentUserPhone, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("شما مجاز به پاسخ به این دعوت نیستید.");

            if (invite.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("این دعوت قبلاً پاسخ داده شده است.");

            invite.Status = accept ? InvitationStatus.Accepted : InvitationStatus.Rejected;
            invite.RespondedAt = DateTime.UtcNow;
            if (string.IsNullOrEmpty(invite.InviteeId))
                invite.InviteeId = currentUserId;

            if (accept && invite.Lead != null)
            {
                var already = await _context.Set<LeadMember>()
                    .AnyAsync(m => m.LeadId == invite.LeadId && m.UserId == currentUserId, cancellationToken);
                if (!already)
                {
                    _context.Set<LeadMember>().Add(new LeadMember
                    {
                        LeadId = invite.LeadId,
                        UserId = currentUserId,
                        AddedByUserId = invite.InviterId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                invite.Lead.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task AddSessionAsync(CreateLeadSessionDto dto, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == dto.LeadId, cancellationToken);
            if (lead == null || lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند جلسه ثبت کند.");

            var scheduledUtc = NormalizeMeetingAtToUtc(dto.ScheduledAt) ?? throw new InvalidOperationException("زمان جلسه نامعتبر است.");
            _context.Set<LeadSession>().Add(new LeadSession
            {
                LeadId = dto.LeadId,
                ScheduledAt = scheduledUtc,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                CreatedByUserId = requesterUserId,
                CreatedAt = DateTime.UtcNow
            });
            lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task UpdateSessionAsync(UpdateLeadSessionDto dto, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var session = await _context.Set<LeadSession>()
                .Include(s => s.Lead)
                .FirstOrDefaultAsync(s => s.Id == dto.SessionId, cancellationToken);
            if (session == null || session.Lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند جلسه را ویرایش کند.");

            var scheduledUtc = NormalizeMeetingAtToUtc(dto.ScheduledAt) ?? throw new InvalidOperationException("زمان جلسه نامعتبر است.");
            session.ScheduledAt = scheduledUtc;
            session.Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim();
            session.Lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveSessionAsync(int sessionId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var session = await _context.Set<LeadSession>()
                .Include(s => s.Lead)
                .FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);
            if (session == null || session.Lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند جلسه را حذف کند.");

            _context.Set<LeadSession>().Remove(session);
            session.Lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> AddNoteAsync(CreateLeadNoteDto dto, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>().FirstOrDefaultAsync(l => l.Id == dto.LeadId, cancellationToken);
            if (lead == null || lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند یادداشت ثبت کند.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("عنوان یادداشت الزامی است.");

            var note = new ProjectNote
            {
                LeadId = dto.LeadId,
                Title = dto.Title.Trim(),
                Content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim(),
                ProjectId = lead.ConvertedProjectId,
                SourceLeadId = dto.LeadId,
                CreatorUserId = requesterUserId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectNotes.Add(note);
            lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
            return note.Id;
        }

        public async Task UpdateNoteAsync(UpdateLeadNoteDto dto, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var note = await _context.ProjectNotes
                .Include(n => n.Lead)
                .FirstOrDefaultAsync(n => n.Id == dto.NoteId, cancellationToken);
            if (note == null || note.Lead == null || note.Lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند یادداشت را ویرایش کند.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new InvalidOperationException("عنوان یادداشت الزامی است.");

            note.Title = dto.Title.Trim();
            note.Content = string.IsNullOrWhiteSpace(dto.Content) ? null : dto.Content.Trim();
            note.Lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveNoteAsync(int noteId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var note = await _context.ProjectNotes
                .Include(n => n.Lead)
                .FirstOrDefaultAsync(n => n.Id == noteId, cancellationToken);
            if (note == null || note.Lead == null || note.Lead.OwnerUserId != requesterUserId)
                throw new InvalidOperationException("فقط مالک لید می‌تواند یادداشت را حذف کند.");

            _context.ProjectNotes.Remove(note);
            note.Lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string BuildProjectDescriptionFromLead(Lead lead)
        {
            var lines = new List<string> { "این پروژه از بخش لیدها (CRM) ایجاد شده است." };
            if (!string.IsNullOrWhiteSpace(lead.CompanyName))
                lines.Add("شرکت: " + lead.CompanyName);
            if (!string.IsNullOrWhiteSpace(lead.ContactName))
                lines.Add("تماس: " + lead.ContactName);
            if (!string.IsNullOrWhiteSpace(lead.Phone))
                lines.Add("تلفن: " + lead.Phone);
            if (!string.IsNullOrWhiteSpace(lead.Email))
                lines.Add("ایمیل: " + lead.Email);
            if (!string.IsNullOrWhiteSpace(lead.Source))
                lines.Add("منبع لید: " + lead.Source);
            if (lead.MeetingAt.HasValue)
                lines.Add("زمان جلسه (لید): " + lead.MeetingAt.Value.ToLocalTime().ToString("yyyy/MM/dd HH:mm", System.Globalization.CultureInfo.InvariantCulture));
            if (!string.IsNullOrWhiteSpace(lead.Notes))
            {
                lines.Add("");
                lines.Add("یادداشت لید:");
                lines.Add(lead.Notes);
            }

            return string.Join("\n", lines);
        }

        private static LeadPipelineStatus NormalizeManualStatus(LeadPipelineStatus status)
        {
            if (status == LeadPipelineStatus.Converted)
                return LeadPipelineStatus.New;
            return status;
        }

        /// <summary>ورودی فرم (معمولاً Kind=Unspecified به‌معنای زمان محلی) را به UTC ذخیره می‌کند.</summary>
        private static DateTime? NormalizeMeetingAtToUtc(DateTime? value)
        {
            if (!value.HasValue)
                return null;
            var v = value.Value;
            return v.Kind switch
            {
                DateTimeKind.Utc => v,
                DateTimeKind.Local => v.ToUniversalTime(),
                _ => DateTime.SpecifyKind(v, DateTimeKind.Local).ToUniversalTime()
            };
        }
    }
}
