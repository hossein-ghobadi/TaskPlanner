using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Application.Services.NotificationService;
using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Application.Services.CrmService
{
    public class CrmService : ICrmService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IProjectQueryService _projectQueryService;
        private readonly INotificationService _notificationService;

        public CrmService(
            IMVPTestDatabaseContext context,
            UserManager<User> userManager,
            IProjectQueryService projectQueryService,
            INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _projectQueryService = projectQueryService;
            _notificationService = notificationService;
        }

        public async Task<Crm> GetOrCreatePersonalCrmAsync(string userId, CancellationToken cancellationToken = default)
        {
            var existing = await _context.Crms
                .FirstOrDefaultAsync(c => c.OwnerUserId == userId, cancellationToken);
            if (existing != null)
                return existing;

            var crm = new Crm
            {
                Name = "CRM من",
                OwnerUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Crms.Add(crm);
            await _context.SaveChangesAsync(cancellationToken);

            _context.CrmMembers.Add(new CrmMember
            {
                CrmId = crm.Id,
                UserId = userId,
                Role = CrmMemberRole.Owner,
                AddedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
            return crm;
        }

        public async Task<CrmAccess?> GetCrmAccessAsync(int crmId, string userId, CancellationToken cancellationToken = default)
        {
            var crm = await _context.Crms.AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == crmId, cancellationToken);
            if (crm == null)
                return null;

            var member = await _context.CrmMembers.AsNoTracking()
                .FirstOrDefaultAsync(m => m.CrmId == crmId && m.UserId == userId, cancellationToken);

            var isOwner = crm.OwnerUserId == userId || (member != null && member.Role == CrmMemberRole.Owner);
            if (!isOwner && member == null)
                return null;

            return new CrmAccess
            {
                CrmId = crmId,
                OwnerUserId = crm.OwnerUserId,
                IsOwner = isOwner,
                CanEditLeads = true,
                CanManageMembers = isOwner
            };
        }

        public async Task<CrmAccess?> GetCrmAccessForLeadAsync(int leadId, string userId, CancellationToken cancellationToken = default)
        {
            await EnsureLegacyLeadsAssignedToCrmAsync(cancellationToken);
            var crmId = await _context.Leads.AsNoTracking()
                .Where(l => l.Id == leadId)
                .Select(l => l.CrmId)
                .FirstOrDefaultAsync(cancellationToken);
            if (crmId == null)
                return null;
            return await GetCrmAccessAsync(crmId.Value, userId, cancellationToken);
        }

        public async Task<IReadOnlyList<int>> GetAccessibleCrmIdsAsync(string userId, CancellationToken cancellationToken = default)
        {
            var owned = await _context.Crms.AsNoTracking()
                .Where(c => c.OwnerUserId == userId)
                .Select(c => c.Id)
                .ToListAsync(cancellationToken);

            var memberOf = await _context.CrmMembers.AsNoTracking()
                .Where(m => m.UserId == userId)
                .Select(m => m.CrmId)
                .ToListAsync(cancellationToken);

            return owned.Union(memberOf).Distinct().ToList();
        }

        public async Task EnsureLegacyLeadsAssignedToCrmAsync(CancellationToken cancellationToken = default)
        {
            var orphanOwners = await _context.Leads.AsNoTracking()
                .Where(l => l.CrmId == null)
                .Select(l => l.OwnerUserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var ownerId in orphanOwners)
            {
                var crm = await GetOrCreatePersonalCrmAsync(ownerId, cancellationToken);
                var orphans = await _context.Leads
                    .Where(l => l.OwnerUserId == ownerId && l.CrmId == null)
                    .ToListAsync(cancellationToken);
                foreach (var lead in orphans)
                {
                    lead.CrmId = crm.Id;
                    if (string.IsNullOrWhiteSpace(lead.CreatedByUserId))
                        lead.CreatedByUserId = lead.OwnerUserId;
                }
                await _context.SaveChangesAsync(cancellationToken);
            }

            // ارتقای همکاران قدیمی لید به عضویت CRM
            var legacyMembers = await (
                from lm in _context.LeadMembers.AsNoTracking()
                join l in _context.Leads.AsNoTracking() on lm.LeadId equals l.Id
                where l.CrmId != null && lm.UserId != l.OwnerUserId
                select new { CrmId = l.CrmId!.Value, lm.UserId, lm.AddedByUserId }
            ).Distinct().ToListAsync(cancellationToken);

            foreach (var group in legacyMembers.GroupBy(x => new { x.CrmId, x.UserId }))
            {
                var exists = await _context.CrmMembers
                    .AnyAsync(m => m.CrmId == group.Key.CrmId && m.UserId == group.Key.UserId, cancellationToken);
                if (exists)
                    continue;

                var sample = group.First();
                _context.CrmMembers.Add(new CrmMember
                {
                    CrmId = sample.CrmId,
                    UserId = sample.UserId,
                    Role = CrmMemberRole.Member,
                    AddedByUserId = sample.AddedByUserId,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var pendingLeadInvites = await (
                from li in _context.LeadInvitations
                join l in _context.Leads.AsNoTracking() on li.LeadId equals l.Id
                where li.Status == InvitationStatus.Pending && l.CrmId != null
                select new { Invite = li, CrmId = l.CrmId!.Value }
            ).ToListAsync(cancellationToken);

            foreach (var row in pendingLeadInvites)
            {
                var exists = await _context.CrmInvitations
                    .AnyAsync(ci => ci.CrmId == row.CrmId
                        && ci.InviteePhone == row.Invite.InviteePhone
                        && ci.Status == InvitationStatus.Pending, cancellationToken);
                if (exists)
                    continue;

                _context.CrmInvitations.Add(new CrmInvitation
                {
                    CrmId = row.CrmId,
                    InviterId = row.Invite.InviterId,
                    InviteeId = row.Invite.InviteeId ?? string.Empty,
                    InviteePhone = row.Invite.InviteePhone,
                    ResponseMessage = row.Invite.ResponseMessage,
                    Status = InvitationStatus.Pending,
                    CreatedAt = row.Invite.CreatedAt
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<CrmSelectDto>> GetAccessibleCrmsAsync(string userId, CancellationToken cancellationToken = default)
        {
            await GetOrCreatePersonalCrmAsync(userId, cancellationToken);
            var ids = await GetAccessibleCrmIdsAsync(userId, cancellationToken);
            var crms = await _context.Crms.AsNoTracking()
                .Where(c => ids.Contains(c.Id))
                .OrderByDescending(c => c.OwnerUserId == userId)
                .ThenBy(c => c.Name)
                .ToListAsync(cancellationToken);

            var ownerIds = crms.Select(c => c.OwnerUserId).Distinct().ToList();
            var ownerNames = await _userManager.Users
                .Where(u => ownerIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : (u.UserName ?? "کاربر"),
                    cancellationToken);

            return crms.Select(c => new CrmSelectDto
            {
                Id = c.Id,
                Name = c.Name,
                OwnerUserId = c.OwnerUserId,
                OwnerDisplayName = ownerNames.GetValueOrDefault(c.OwnerUserId),
                IsOwner = c.OwnerUserId == userId
            }).ToList();
        }

        public async Task<IReadOnlyList<CrmMemberSummaryDto>> GetMembersAsync(int crmId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var access = await GetCrmAccessAsync(crmId, requesterUserId, cancellationToken);
            if (access == null)
                throw new InvalidOperationException("دسترسی به این CRM ندارید.");

            var members = await _context.CrmMembers.AsNoTracking()
                .Where(m => m.CrmId == crmId && m.Role != CrmMemberRole.Owner)
                .ToListAsync(cancellationToken);

            var userIds = members.Select(m => m.UserId).Distinct().ToList();
            var names = await _userManager.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : (u.UserName ?? "کاربر"),
                    cancellationToken);

            return members.Select(m => new CrmMemberSummaryDto
            {
                UserId = m.UserId,
                DisplayName = names.GetValueOrDefault(m.UserId, "کاربر"),
                Role = m.Role
            }).ToList();
        }

        public async Task<IReadOnlyList<CrmPendingInviteDto>> GetPendingInvitationsAsync(int crmId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var access = await GetCrmAccessAsync(crmId, requesterUserId, cancellationToken);
            if (access == null || !access.CanManageMembers)
                return Array.Empty<CrmPendingInviteDto>();

            var pending = await _context.CrmInvitations.AsNoTracking()
                .Where(i => i.CrmId == crmId && i.Status == InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync(cancellationToken);

            var phones = pending.Select(i => i.InviteePhone).Distinct().ToList();
            var usersByPhone = await _userManager.Users
                .Where(u => u.Phone != null && phones.Contains(u.Phone))
                .ToDictionaryAsync(
                    u => u.Phone!,
                    u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : (u.UserName ?? "کاربر"),
                    cancellationToken);

            return pending.Select(i => new CrmPendingInviteDto
            {
                Id = i.Id,
                InviteePhone = i.InviteePhone,
                InviteeDisplayName = usersByPhone.TryGetValue(i.InviteePhone, out var dn) ? dn : null
            }).ToList();
        }

        public async Task<IReadOnlyList<UserSelectDto>> GetAvailableCollaboratorsAsync(int crmId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var access = await GetCrmAccessAsync(crmId, requesterUserId, cancellationToken);
            if (access == null || !access.CanManageMembers)
                throw new InvalidOperationException("فقط مالک CRM می‌تواند همکار اضافه کند.");

            var existingMemberIds = await _context.CrmMembers
                .Where(m => m.CrmId == crmId)
                .Select(m => m.UserId)
                .ToListAsync(cancellationToken);

            var pool = await _projectQueryService.GetAvailableUsersForCreateAsync(requesterUserId);
            return pool.Where(u => u.Id != access.OwnerUserId && !existingMemberIds.Contains(u.Id)).ToList();
        }

        public async Task AddMemberAsync(int crmId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var access = await GetCrmAccessAsync(crmId, requesterUserId, cancellationToken);
            if (access == null || !access.CanManageMembers)
                throw new InvalidOperationException("فقط مالک CRM می‌تواند همکار اضافه کند.");

            if (string.Equals(memberUserId, access.OwnerUserId, StringComparison.Ordinal))
                throw new InvalidOperationException("مالک CRM نیازی به افزودن خود به‌عنوان همکار ندارد.");

            var exists = await _context.CrmMembers
                .AnyAsync(m => m.CrmId == crmId && m.UserId == memberUserId, cancellationToken);
            if (exists)
                throw new InvalidOperationException("این کاربر از قبل عضو این CRM است.");

            _context.CrmMembers.Add(new CrmMember
            {
                CrmId = crmId,
                UserId = memberUserId,
                Role = CrmMemberRole.Member,
                AddedByUserId = requesterUserId,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RemoveMemberAsync(int crmId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default)
        {
            var access = await GetCrmAccessAsync(crmId, requesterUserId, cancellationToken);
            if (access == null || !access.CanManageMembers)
                throw new InvalidOperationException("فقط مالک CRM می‌تواند همکار را حذف کند.");

            if (string.Equals(memberUserId, access.OwnerUserId, StringComparison.Ordinal))
                throw new InvalidOperationException("نمی‌توان مالک CRM را حذف کرد.");

            var row = await _context.CrmMembers
                .FirstOrDefaultAsync(m => m.CrmId == crmId && m.UserId == memberUserId, cancellationToken);
            if (row == null)
                throw new InvalidOperationException("همکار در این CRM یافت نشد.");

            _context.CrmMembers.Remove(row);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task InviteByPhoneAsync(int crmId, string phone, string inviterId, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("شماره تلفن وارد نشده است.", nameof(phone));
            phone = phone.Trim();

            var access = await GetCrmAccessAsync(crmId, inviterId, cancellationToken);
            if (access == null || !access.CanManageMembers)
                throw new InvalidOperationException("فقط مالک CRM می‌تواند دعوت ارسال کند.");

            var crm = await _context.Crms.FirstOrDefaultAsync(c => c.Id == crmId, cancellationToken)
                ?? throw new InvalidOperationException("CRM یافت نشد.");

            var inviter = await _userManager.FindByIdAsync(inviterId);
            if (inviter != null && !string.IsNullOrWhiteSpace(inviter.Phone) &&
                string.Equals(inviter.Phone, phone, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("نمی‌توانید خودتان را دعوت کنید.");

            var existsPending = await _context.CrmInvitations
                .AnyAsync(i => i.CrmId == crmId && i.InviteePhone == phone && i.Status == InvitationStatus.Pending, cancellationToken);
            if (existsPending)
                throw new InvalidOperationException("برای این شماره قبلاً دعوت در انتظار ارسال شده است.");

            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
            if (user == null)
                throw new InvalidOperationException("کاربری با این شماره پیدا نشد.");

            if (string.Equals(user.Id, access.OwnerUserId, StringComparison.Ordinal))
                throw new InvalidOperationException("مالک CRM نیازی به دعوت ندارد.");

            var isMember = await _context.CrmMembers
                .AnyAsync(m => m.CrmId == crmId && m.UserId == user.Id, cancellationToken);
            if (isMember)
                throw new InvalidOperationException("این کاربر هم‌اکنون عضو این CRM است.");

            var invite = new CrmInvitation
            {
                CrmId = crmId,
                InviterId = inviterId,
                InviteePhone = phone,
                InviteeId = user.Id,
                Status = InvitationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.CrmInvitations.Add(invite);
            await _context.SaveChangesAsync(cancellationToken);

            await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
            {
                UserId = user.Id,
                Title = "دعوت به CRM",
                Message = $"برای شما دعوتی به CRM ارسال شد. با پذیرش، به همه لیدهای این CRM دسترسی پیدا می‌کنید.",
                RelatedEntityId = invite.Id.ToString(),
                RelatedEntityType = nameof(CrmInvitation),
                Type = NotificationCreateType.CrmInvitation,
                PayloadJson = JsonSerializer.Serialize(new { invitationId = invite.Id, crmId })
            }, cancellationToken);
        }

        public async Task CancelInvitationAsync(int invitationId, string inviterUserId, CancellationToken cancellationToken = default)
        {
            var invite = await _context.CrmInvitations
                .FirstOrDefaultAsync(i => i.Id == invitationId, cancellationToken);
            if (invite == null)
                throw new InvalidOperationException("دعوت یافت نشد.");

            var access = await GetCrmAccessAsync(invite.CrmId, inviterUserId, cancellationToken);
            if (access == null || !access.CanManageMembers)
                throw new InvalidOperationException("دعوت یافت نشد یا دسترسی ندارید.");

            if (invite.Status != InvitationStatus.Pending)
                throw new InvalidOperationException("فقط دعوت‌های در انتظار قابل لغو هستند.");

            _context.CrmInvitations.Remove(invite);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task RespondToInvitationAsync(int invitationId, string currentUserId, string currentUserPhone, bool accept, CancellationToken cancellationToken = default)
        {
            var invite = await _context.CrmInvitations
                .Include(i => i.Crm)
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

            if (accept)
            {
                var already = await _context.CrmMembers
                    .AnyAsync(m => m.CrmId == invite.CrmId && m.UserId == currentUserId, cancellationToken);
                if (!already)
                {
                    _context.CrmMembers.Add(new CrmMember
                    {
                        CrmId = invite.CrmId,
                        UserId = currentUserId,
                        Role = CrmMemberRole.Member,
                        AddedByUserId = invite.InviterId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
