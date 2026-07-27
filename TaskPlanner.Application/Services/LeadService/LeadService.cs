using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Application.Services.CrmService;
using TaskPlanner.Application.Services.FileUpload;
using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.LeadService
{
    public class LeadService : ILeadService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly IProjectCommandService _projectCommandService;
        private readonly ICrmService _crmService;
        private readonly IFileUrlService _fileUrlService;

        public LeadService(
            IMVPTestDatabaseContext context,
            IProjectCommandService projectCommandService,
            ICrmService crmService,
            IFileUrlService fileUrlService)
        {
            _context = context;
            _projectCommandService = projectCommandService;
            _crmService = crmService;
            _fileUrlService = fileUrlService;
        }

        public async Task<IReadOnlyList<LeadListItemDto>> GetMyLeadsAsync(string userId, CancellationToken cancellationToken = default)
        {
            await _crmService.EnsureLegacyLeadsAssignedToCrmAsync(cancellationToken);
            await _crmService.GetOrCreatePersonalCrmAsync(userId, cancellationToken);

            var crmIds = await _crmService.GetAccessibleCrmIdsAsync(userId, cancellationToken);
            if (!crmIds.Any())
                return Array.Empty<LeadListItemDto>();

            var ownerLookup = await _context.Crms.AsNoTracking()
                .Where(c => crmIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.OwnerUserId, cancellationToken);

            var leads = await _context.Set<Lead>()
                .AsNoTracking()
                .Where(l => l.CrmId != null && crmIds.Contains(l.CrmId.Value))
                .OrderByDescending(l => l.UpdatedAt)
                .Select(l => new LeadListItemDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    CompanyName = l.CompanyName,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt,
                    MeetingAt = l.MeetingAt,
                    ProjectAmount = l.ProjectAmount,
                    CrmId = l.CrmId!.Value,
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
                    CanEdit = true,
                    IsCrmOwner = false
                })
                .ToListAsync(cancellationToken);

            var convertedProjectIds = leads
                .Where(l => l.ConvertedProjectId.HasValue)
                .Select(l => l.ConvertedProjectId!.Value)
                .Distinct()
                .ToList();

            HashSet<int> accessibleProjectIds = new();
            if (convertedProjectIds.Count > 0)
            {
                var memberProjectIds = await _context.ProjectMembers.AsNoTracking()
                    .Where(pm => pm.UserId == userId && pm.ProjectId != null && convertedProjectIds.Contains(pm.ProjectId.Value))
                    .Select(pm => pm.ProjectId!.Value)
                    .ToListAsync(cancellationToken);

                var createdProjectIds = await _context.Projects.AsNoTracking()
                    .Where(p => p.CreatorUserId == userId && convertedProjectIds.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync(cancellationToken);

                accessibleProjectIds = memberProjectIds.Union(createdProjectIds).ToHashSet();
            }

            foreach (var lead in leads)
            {
                lead.IsCrmOwner = ownerLookup.TryGetValue(lead.CrmId, out var ownerId) && ownerId == userId;
                lead.CanOpenConvertedProject = lead.ConvertedProjectId.HasValue
                    && accessibleProjectIds.Contains(lead.ConvertedProjectId.Value);
            }

            return leads;
        }

        public async Task<LeadDetailsDto?> GetDetailsAsync(int id, string userId, CancellationToken cancellationToken = default)
        {
            await _crmService.EnsureLegacyLeadsAssignedToCrmAsync(cancellationToken);

            var lead = await _context.Set<Lead>().AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
            if (lead == null || lead.CrmId == null)
                return null;

            var access = await _crmService.GetCrmAccessAsync(lead.CrmId.Value, userId, cancellationToken);
            if (access == null)
                return null;

            var members = await _crmService.GetMembersAsync(lead.CrmId.Value, userId, cancellationToken);
            var pendingInvites = access.CanManageMembers
                ? await _crmService.GetPendingInvitationsAsync(lead.CrmId.Value, userId, cancellationToken)
                : Array.Empty<CrmPendingInviteDto>();

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

            var canOpenProject = false;
            if (lead.ConvertedProjectId is int projectId)
            {
                canOpenProject = await _context.Projects.AsNoTracking()
                        .AnyAsync(p => p.Id == projectId && p.CreatorUserId == userId, cancellationToken)
                    || await _context.ProjectMembers.AsNoTracking()
                        .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId, cancellationToken);
            }

            var result = new LeadDetailsDto
            {
                Id = lead.Id,
                CrmId = lead.CrmId.Value,
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
                ProjectAmount = lead.ProjectAmount,
                ConvertedProjectId = lead.ConvertedProjectId,
                ConvertedAt = lead.ConvertedAt,
                OwnerUserId = lead.OwnerUserId,
                IsCrmOwner = access.IsOwner,
                CanEdit = access.CanEditLeads,
                CanOpenConvertedProject = canOpenProject,
                Members = members.Select(m => new LeadMemberSummaryDto
                {
                    UserId = m.UserId,
                    DisplayName = m.DisplayName
                }).ToList(),
                PendingInvitations = pendingInvites.Select(i => new LeadPendingInviteDto
                {
                    Id = i.Id,
                    InviteePhone = i.InviteePhone,
                    InviteeDisplayName = i.InviteeDisplayName
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

            foreach (var note in result.NotesList)
            {
                foreach (var attachment in note.Attachments)
                {
                    attachment.FilePath = _fileUrlService.ToPublicUrl(attachment.FilePath);
                }
            }

            return result;
        }

        public async Task<int> CreateAsync(CreateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default)
        {
            Crm crm;
            if (dto.CrmId is int crmId)
            {
                var access = await _crmService.GetCrmAccessAsync(crmId, ownerUserId, cancellationToken);
                if (access == null || !access.CanEditLeads)
                    throw new InvalidOperationException("دسترسی ایجاد لید در این CRM را ندارید.");
                crm = await _context.Crms.FirstAsync(c => c.Id == crmId, cancellationToken);
            }
            else
            {
                crm = await _crmService.GetOrCreatePersonalCrmAsync(ownerUserId, cancellationToken);
            }

            var status = NormalizeManualStatus(dto.Status);
            var now = DateTime.UtcNow;
            var lead = new Lead
            {
                CrmId = crm.Id,
                Title = dto.Title.Trim(),
                CompanyName = string.IsNullOrWhiteSpace(dto.CompanyName) ? null : dto.CompanyName.Trim(),
                ContactName = string.IsNullOrWhiteSpace(dto.ContactName) ? null : dto.ContactName.Trim(),
                Phone = string.IsNullOrWhiteSpace(dto.Phone) ? null : dto.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Source = string.IsNullOrWhiteSpace(dto.Source) ? null : dto.Source.Trim(),
                Status = status,
                OwnerUserId = crm.OwnerUserId,
                CreatedByUserId = ownerUserId,
                CreatedAt = now,
                UpdatedAt = now,
                MeetingAt = NormalizeMeetingAtToUtc(dto.MeetingAt),
                ProjectAmount = NormalizeProjectAmount(dto.ProjectAmount)
            };

            _context.Set<Lead>().Add(lead);
            await _context.SaveChangesAsync(cancellationToken);
            return lead.Id;
        }

        public async Task UpdateAsync(UpdateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == dto.Id, cancellationToken);
            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            await EnsureLeadCrmAsync(lead, cancellationToken);
            var access = await _crmService.GetCrmAccessAsync(RequireCrmId(lead), ownerUserId, cancellationToken);
            if (access == null || !access.CanEditLeads)
                throw new InvalidOperationException("دسترسی ویرایش این لید را ندارید.");

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
            lead.ProjectAmount = NormalizeProjectAmount(dto.ProjectAmount);
            lead.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> ConvertToProjectAsync(int leadId, string ownerUserId, string? projectNameOverride, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            await EnsureLeadCrmAsync(lead, cancellationToken);
            var access = await _crmService.GetCrmAccessAsync(RequireCrmId(lead), ownerUserId, cancellationToken);
            if (access == null || !access.CanEditLeads)
                throw new InvalidOperationException("دسترسی تبدیل این لید را ندارید.");

            if (lead.ConvertedProjectId.HasValue)
                throw new InvalidOperationException("این لید قبلاً به پروژه تبدیل شده است.");

            if (lead.Status != LeadPipelineStatus.Qualified)
                throw new InvalidOperationException("فقط لیدهایی با وضعیت «قطعیت / واجد شرایط» را می‌توان به پروژه تبدیل کرد. ابتدا وضعیت را به «قطعیت» برگردانید.");

            var projectName = string.IsNullOrWhiteSpace(projectNameOverride) ? lead.Title : projectNameOverride.Trim();
            if (string.IsNullOrWhiteSpace(projectName))
                throw new InvalidOperationException("نام پروژه نمی‌تواند خالی باشد.");

            var description = BuildProjectDescriptionFromLead(lead);

            // فقط کاربر تبدیل‌کننده عضو پروژه می‌شود؛ اعضای CRM خودکار عضو پروژه نمی‌شوند.
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

            await EnsureLeadCrmAsync(lead, cancellationToken);
            var access = await _crmService.GetCrmAccessAsync(RequireCrmId(lead), ownerUserId, cancellationToken);
            if (access == null || !access.CanEditLeads)
                throw new InvalidOperationException("دسترسی حذف این لید را ندارید.");

            _context.Set<Lead>().Remove(lead);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<UserSelectDto>> GetAvailableCollaboratorsForLeadAsync(int leadId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            await EnsureLeadCrmAsync(lead, cancellationToken);
            return await _crmService.GetAvailableCollaboratorsAsync(RequireCrmId(lead), requesterUserId, cancellationToken);
        }

        public async Task AddLeadMemberAsync(int leadId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            await EnsureLeadCrmAsync(lead, cancellationToken);
            await _crmService.AddMemberAsync(RequireCrmId(lead), memberUserId, requesterUserId, cancellationToken);
            await TouchLeadAsync(leadId, cancellationToken);
        }

        public async Task RemoveLeadMemberAsync(int leadId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            await EnsureLeadCrmAsync(lead, cancellationToken);
            await _crmService.RemoveMemberAsync(RequireCrmId(lead), memberUserId, requesterUserId, cancellationToken);
            await TouchLeadAsync(leadId, cancellationToken);
        }

        public async Task InviteUserToLeadAsync(int leadId, string phone, string inviterId, CancellationToken cancellationToken = default)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            await EnsureLeadCrmAsync(lead, cancellationToken);
            await _crmService.InviteByPhoneAsync(RequireCrmId(lead), phone, inviterId, cancellationToken);
            await TouchLeadAsync(leadId, cancellationToken);
        }

        public async Task CancelLeadInvitationAsync(int invitationId, string inviterUserId, CancellationToken cancellationToken = default)
        {
            await _crmService.CancelInvitationAsync(invitationId, inviterUserId, cancellationToken);
        }

        public async Task RespondToLeadInvitationAsync(int invitationId, string currentUserId, string currentUserPhone, bool accept, CancellationToken cancellationToken = default)
        {
            await _crmService.RespondToInvitationAsync(invitationId, currentUserId, currentUserPhone, accept, cancellationToken);
        }

        public async Task AddSessionAsync(CreateLeadSessionDto dto, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await RequireEditableLeadAsync(dto.LeadId, requesterUserId, cancellationToken);

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
            if (session == null)
                throw new InvalidOperationException("جلسه یافت نشد.");

            await RequireEditableLeadAsync(session.LeadId, requesterUserId, cancellationToken);

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
            if (session == null)
                throw new InvalidOperationException("جلسه یافت نشد.");

            await RequireEditableLeadAsync(session.LeadId, requesterUserId, cancellationToken);

            _context.Set<LeadSession>().Remove(session);
            session.Lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<int> AddNoteAsync(CreateLeadNoteDto dto, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var lead = await RequireEditableLeadAsync(dto.LeadId, requesterUserId, cancellationToken);

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
            if (note?.Lead == null)
                throw new InvalidOperationException("یادداشت یافت نشد.");

            await RequireEditableLeadAsync(note.Lead.Id, requesterUserId, cancellationToken);

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
            if (note?.Lead == null)
                throw new InvalidOperationException("یادداشت یافت نشد.");

            await RequireEditableLeadAsync(note.Lead.Id, requesterUserId, cancellationToken);

            _context.ProjectNotes.Remove(note);
            note.Lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Lead> RequireEditableLeadAsync(int leadId, string userId, CancellationToken cancellationToken)
        {
            var lead = await _context.Set<Lead>()
                .FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            await EnsureLeadCrmAsync(lead, cancellationToken);
            var access = await _crmService.GetCrmAccessAsync(RequireCrmId(lead), userId, cancellationToken);
            if (access == null || !access.CanEditLeads)
                throw new InvalidOperationException("دسترسی لازم برای این عملیات را ندارید.");

            return lead;
        }

        private async Task TouchLeadAsync(int leadId, CancellationToken cancellationToken)
        {
            var lead = await _context.Set<Lead>().FirstOrDefaultAsync(l => l.Id == leadId, cancellationToken);
            if (lead == null)
                return;
            lead.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static int RequireCrmId(Lead lead)
        {
            if (lead.CrmId is not int crmId)
                throw new InvalidOperationException("این لید هنوز به CRM متصل نشده است.");
            return crmId;
        }

        private async Task EnsureLeadCrmAsync(Lead lead, CancellationToken cancellationToken)
        {
            if (lead.CrmId != null)
                return;

            await _crmService.EnsureLegacyLeadsAssignedToCrmAsync(cancellationToken);

            var crmId = await _context.Set<Lead>()
                .AsNoTracking()
                .Where(l => l.Id == lead.Id)
                .Select(l => l.CrmId)
                .FirstOrDefaultAsync(cancellationToken);

            if (crmId != null)
            {
                lead.CrmId = crmId;
                return;
            }

            var crm = await _crmService.GetOrCreatePersonalCrmAsync(lead.OwnerUserId, cancellationToken);
            lead.CrmId = crm.Id;
            lead.CreatedByUserId ??= lead.OwnerUserId;
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

        private static decimal? NormalizeProjectAmount(decimal? value) =>
            value is > 0 ? value : null;
    }
}
