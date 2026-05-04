using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.LeadService
{
    public class LeadService : ILeadService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly IProjectCommandService _projectCommandService;

        public LeadService(IMVPTestDatabaseContext context, IProjectCommandService projectCommandService)
        {
            _context = context;
            _projectCommandService = projectCommandService;
        }

        public async Task<IReadOnlyList<LeadListItemDto>> GetMyLeadsAsync(string ownerUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Lead>()
                .AsNoTracking()
                .Where(l => l.OwnerUserId == ownerUserId)
                .OrderByDescending(l => l.UpdatedAt)
                .Select(l => new LeadListItemDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    CompanyName = l.CompanyName,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt,
                    ConvertedProjectId = l.ConvertedProjectId
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<LeadDetailsDto?> GetDetailsAsync(int id, string ownerUserId, CancellationToken cancellationToken = default)
        {
            return await _context.Set<Lead>()
                .AsNoTracking()
                .Where(l => l.Id == id && l.OwnerUserId == ownerUserId)
                .Select(l => new LeadDetailsDto
                {
                    Id = l.Id,
                    Title = l.Title,
                    CompanyName = l.CompanyName,
                    ContactName = l.ContactName,
                    Phone = l.Phone,
                    Email = l.Email,
                    Notes = l.Notes,
                    Source = l.Source,
                    Status = l.Status,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt,
                    ConvertedProjectId = l.ConvertedProjectId,
                    ConvertedAt = l.ConvertedAt
                })
                .FirstOrDefaultAsync(cancellationToken);
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
                UpdatedAt = now
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
                .FirstOrDefaultAsync(l => l.Id == id && l.OwnerUserId == ownerUserId, cancellationToken);

            if (lead == null)
                throw new InvalidOperationException("لید یافت نشد.");

            _context.Set<Lead>().Remove(lead);
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
            if (!string.IsNullOrWhiteSpace(lead.Notes))
            {
                lines.Add("");
                lines.Add("یادداشت لید:");
                lines.Add(lead.Notes);
            }

            return string.Join("\n", lines);
        }

        /// <summary>فقط مراحل دستی؛ تبدیل‌شده از مسیر تبدیل به پروژه است.</summary>
        private static LeadPipelineStatus NormalizeManualStatus(LeadPipelineStatus status)
        {
            if (status == LeadPipelineStatus.Converted)
                return LeadPipelineStatus.New;
            return status;
        }
    }
}
