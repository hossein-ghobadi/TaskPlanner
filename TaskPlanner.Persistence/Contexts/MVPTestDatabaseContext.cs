using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Notifications;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Domain.Entities.Boards;
using TaskPlanner.Application.Interfaces.Contexts;


namespace TaskPlanner.Persistence.Contexts
{
    public class MVPTestDatabaseContext : IdentityDbContext<User>, IMVPTestDatabaseContext
    {
        public MVPTestDatabaseContext(DbContextOptions<MVPTestDatabaseContext> options) : base(options)
        {
        }
        

       
        public DbSet<TaskCategory> TaskCategories { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectMember> ProjectMembers { get; set; }
        public DbSet<ProjectInvitation> ProjectInvitations { get; set; }
        public DbSet<ProjectNote> ProjectNotes { get; set; }
        public DbSet<ProjectNoteAttachment> ProjectNoteAttachments { get; set; }
        public DbSet<ProjectNoteFolder> ProjectNoteFolders { get; set; }
        public DbSet<PersonalNote> PersonalNotes { get; set; }
        public DbSet<PersonalNoteAttachment> PersonalNoteAttachments { get; set; }
        public DbSet<PersonalNoteFolder> PersonalNoteFolders { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskCommentAttachment> TaskCommentAttachments { get; set; }
        public DbSet<ProjectChatGroup> ProjectChatGroups { get; set; }
        public DbSet<ProjectChatGroupMember> ProjectChatGroupMembers { get; set; }
        public DbSet<ProjectChatMessage> ProjectChatMessages { get; set; }
        public DbSet<ProjectChatMessageAttachment> ProjectChatMessageAttachments { get; set; }
        public DbSet<ProjectBaleGroupLink> ProjectBaleGroupLinks { get; set; }
        public DbSet<BaleBotSyncState> BaleBotSyncStates { get; set; }
        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<SprintTask> SprintTasks { get; set; }
        public DbSet<WorkflowStatus> WorkflowStatuses { get; set; }
        public DbSet<WorkflowTransition> WorkflowTransitions { get; set; }
        public DbSet<IssueStatusHistory> IssueStatusHistories { get; set; }
        public DbSet<ProjectIssueType> ProjectIssueTypes { get; set; }
        public DbSet<ProjectFeature> ProjectFeatures { get; set; }
        public DbSet<FeatureFunction> FeatureFunctions { get; set; }
        public DbSet<FeaturePageState> FeaturePageStates { get; set; }
        public DbSet<FeatureApiContract> FeatureApiContracts { get; set; }
        public DbSet<FeatureBusinessRule> FeatureBusinessRules { get; set; }
        public DbSet<FeatureCodeReview> FeatureCodeReviews { get; set; }
        public DbSet<ProjectTicket> ProjectTickets { get; set; }
        public DbSet<ProjectTicketMessage> ProjectTicketMessages { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        
        // Board entities (separate from TaskPlanner)
        public DbSet<Board> Boards { get; set; }
        public DbSet<BoardMember> BoardMembers { get; set; }
        public DbSet<BoardStatus> BoardStatuses { get; set; }
        public DbSet<BoardTask> BoardTasks { get; set; }
        public DbSet<BoardTaskComment> BoardTaskComments { get; set; }
        public DbSet<BoardTaskCommentAttachment> BoardTaskCommentAttachments { get; set; }
        
        // Project Image Gallery entities
        public DbSet<ProjectImageGallery> ProjectImageGalleries { get; set; }
        public DbSet<ProjectImageGalleryFolder> ProjectImageGalleryFolders { get; set; }

        public DbSet<Lead> Leads { get; set; }
        public DbSet<LeadMember> LeadMembers { get; set; }
        public DbSet<LeadInvitation> LeadInvitations { get; set; }
        public DbSet<LeadSession> LeadSessions { get; set; }
        public DbSet<LeadNote> LeadNotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Identity configurations
            modelBuilder.Entity<IdentityUserLogin<string>>().HasKey(l => new { l.LoginProvider, l.ProviderKey });
            modelBuilder.Entity<IdentityUserRole<string>>().HasKey(r => new { r.UserId, r.RoleId });
            modelBuilder.Entity<IdentityUserToken<string>>().HasKey(t => new { t.UserId, t.LoginProvider, t.Name });

            // TaskPlanner configurations
            modelBuilder.Entity<ProjectInvitation>()
               .HasOne(i => i.Project)
               .WithMany()
               .HasForeignKey(i => i.ProjectId)
               .OnDelete(DeleteBehavior.Cascade).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectInvitation>()
        .HasIndex(i => new { i.ProjectId, i.InviteeId, i.Status });
            modelBuilder.Entity<ProjectMember>()
          .HasOne(pm => pm.Project)
          .WithMany(p => p.Members)
          .HasForeignKey(pm => pm.ProjectId).OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Project>()
      .Property(p => p.CreatorUserId)
      .HasMaxLength(450);
            modelBuilder.Entity<TaskItem>()
               .HasOne(t => t.Project)
               .WithMany(p => p.Tasks)
               .HasForeignKey(t => t.ProjectId)
               .OnDelete(DeleteBehavior.NoAction); // تا با حذف پروژه، Taskها پاک نشوند (دلخواه)

            // رابطه‌ی تو در توی Task (Parent-Child hierarchy)
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.ParentTask)
                .WithMany(t => t.ChildIssues)
                .HasForeignKey(t => t.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // Index برای IssueKey (باید یونیک باشه)
            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => t.IssueKey)
                .IsUnique();

            // Index برای IssueType و ProjectId (برای query‌های سریع‌تر)
            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => new { t.ProjectId, t.IssueType });

            // CategoryId رو nullable کردیم
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Category)
                .WithMany()
                .HasForeignKey(t => t.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // IssueType پروژه (Story-level)
                        modelBuilder.Entity<ProjectIssueType>()
                            .HasOne(pit => pit.Project)
                            .WithMany(p => p.IssueTypes)
                            .HasForeignKey(pit => pit.ProjectId)
                            .OnDelete(DeleteBehavior.Cascade);

                        modelBuilder.Entity<ProjectIssueType>()
                            .HasIndex(pit => new { pit.ProjectId, pit.Name })
                            .IsUnique();

                        modelBuilder.Entity<TaskItem>()
                            .HasOne(t => t.ProjectIssueType)
                            .WithMany(pit => pit.Tasks)
                            .HasForeignKey(t => t.ProjectIssueTypeId)
                            .OnDelete(DeleteBehavior.Restrict);

            // Default value برای IssueType
            modelBuilder.Entity<TaskItem>()
                .Property(t => t.IssueType)
                .HasDefaultValue(IssueType.Task);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.AssignedUser)
                .WithMany()
                .HasForeignKey(t => t.AssignedUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index برای IssueKeyPrefix در Project
            modelBuilder.Entity<Project>()
                .HasIndex(p => p.IssueKeyPrefix);

            // رابطه ProjectNote با Project
            modelBuilder.Entity<ProjectNote>()
                .HasOne(n => n.Project)
                .WithMany()
                .HasForeignKey(n => n.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // رابطه ProjectNoteAttachment با ProjectNote
            modelBuilder.Entity<ProjectNoteAttachment>()
                .HasOne(a => a.ProjectNote)
                .WithMany(n => n.Attachments)
                .HasForeignKey(a => a.ProjectNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // رابطه ProjectNoteFolder با ParentFolder (تودرتو)
            modelBuilder.Entity<ProjectNoteFolder>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict); // جلوگیری از حذف پوشه‌ای که پوشه‌های فرزند دارد

            // رابطه ProjectNoteFolder با Project
            modelBuilder.Entity<ProjectNoteFolder>()
                .HasOne(f => f.Project)
                .WithMany()
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade); // با حذف پروژه، پوشه‌ها هم حذف می‌شوند

            // رابطه ProjectNote با ProjectNoteFolder
            modelBuilder.Entity<ProjectNote>()
                .HasOne(n => n.Folder)
                .WithMany(f => f.Notes)
                .HasForeignKey(n => n.FolderId)
                .OnDelete(DeleteBehavior.NoAction); // جلوگیری از cascade path - باید به صورت دستی مدیریت شود

            // Index برای ProjectId در ProjectNoteFolder
            modelBuilder.Entity<ProjectNoteFolder>()
                .HasIndex(f => f.ProjectId);

            // Index برای ParentFolderId در ProjectNoteFolder
            modelBuilder.Entity<ProjectNoteFolder>()
                .HasIndex(f => f.ParentFolderId);

            // رابطه PersonalNoteAttachment با PersonalNote
            modelBuilder.Entity<PersonalNoteAttachment>()
                .HasOne(a => a.PersonalNote)
                .WithMany(n => n.Attachments)
                .HasForeignKey(a => a.PersonalNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // رابطه PersonalNoteFolder با ParentFolder (تودرتو)
            modelBuilder.Entity<PersonalNoteFolder>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict); // جلوگیری از حذف پوشه‌ای که پوشه‌های فرزند دارد

            // رابطه PersonalNote با PersonalNoteFolder
            modelBuilder.Entity<PersonalNote>()
                .HasOne(n => n.Folder)
                .WithMany(f => f.Notes)
                .HasForeignKey(n => n.FolderId)
                .OnDelete(DeleteBehavior.SetNull); // اگر پوشه حذف شد، یادداشت‌ها بدون پوشه می‌مانند

            // Index برای UserId در PersonalNoteFolder
            modelBuilder.Entity<PersonalNoteFolder>()
                .HasIndex(f => f.UserId);

            // Index برای ParentFolderId در PersonalNoteFolder
            modelBuilder.Entity<PersonalNoteFolder>()
                .HasIndex(f => f.ParentFolderId);

            // رابطه TaskComment با TaskItem
            modelBuilder.Entity<TaskComment>()
                .HasOne(c => c.Task)
                .WithMany()
                .HasForeignKey(c => c.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // رابطه TaskCommentAttachment با TaskComment
            modelBuilder.Entity<TaskCommentAttachment>()
                .HasOne(a => a.TaskComment)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.TaskCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectChatGroup>()
                .HasOne(g => g.Project)
                .WithMany()
                .HasForeignKey(g => g.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectChatGroup>()
                .Property(g => g.Name)
                .HasMaxLength(150);

            modelBuilder.Entity<ProjectChatGroup>()
                .Property(g => g.CreatedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectChatGroup>()
                .HasIndex(g => new { g.ProjectId, g.Name });

            modelBuilder.Entity<ProjectChatGroupMember>()
                .HasOne(m => m.ProjectChatGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(m => m.ProjectChatGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectChatGroupMember>()
                .Property(m => m.UserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectChatGroupMember>()
                .Property(m => m.AddedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectChatGroupMember>()
                .HasIndex(m => new { m.ProjectChatGroupId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<ProjectChatMessage>()
                .HasOne(m => m.ProjectChatGroup)
                .WithMany(g => g.Messages)
                .HasForeignKey(m => m.ProjectChatGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectChatMessage>()
                .HasOne(m => m.ReplyToMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(m => m.ReplyToMessageId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.UserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.UserName)
                .HasMaxLength(200);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.Message)
                .HasMaxLength(4000);

            modelBuilder.Entity<ProjectChatMessage>()
                .HasIndex(m => new { m.ProjectChatGroupId, m.CreatedAt });

            modelBuilder.Entity<ProjectChatMessage>()
                .HasIndex(m => m.ReplyToMessageId);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.ExternalProvider)
                .HasMaxLength(20);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.ExternalSenderUsername)
                .HasMaxLength(100);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.ExternalSenderFirstName)
                .HasMaxLength(100);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.ExternalSenderLastName)
                .HasMaxLength(100);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.ExternalSenderPhone)
                .HasMaxLength(20);

            modelBuilder.Entity<ProjectChatMessage>()
                .Property(m => m.ExternalSenderPhotoPath)
                .HasMaxLength(500);

            modelBuilder.Entity<ProjectChatMessage>()
                .HasIndex(m => new { m.ProjectChatGroupId, m.ExternalProvider, m.ExternalMessageId })
                .IsUnique()
                .HasFilter("[ExternalMessageId] IS NOT NULL");

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .HasOne(l => l.Project)
                .WithMany()
                .HasForeignKey(l => l.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .HasOne(l => l.ProjectChatGroup)
                .WithMany()
                .HasForeignKey(l => l.ProjectChatGroupId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .Property(l => l.BaleChatTitle)
                .HasMaxLength(200);

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .Property(l => l.BotToken)
                .HasMaxLength(120);

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .Property(l => l.BaleChatUsername)
                .HasMaxLength(100);

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .Property(l => l.CreatedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .HasIndex(l => l.BaleChatId);

            modelBuilder.Entity<ProjectBaleGroupLink>()
                .HasIndex(l => new { l.ProjectId, l.BaleChatId })
                .IsUnique();

            modelBuilder.Entity<ProjectChatMessageAttachment>()
                .HasOne(a => a.ProjectChatMessage)
                .WithMany(m => m.Attachments)
                .HasForeignKey(a => a.ProjectChatMessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectChatMessageAttachment>()
                .Property(a => a.FileName)
                .HasMaxLength(500);

            modelBuilder.Entity<ProjectChatMessageAttachment>()
                .Property(a => a.FilePath)
                .HasMaxLength(1000);

            modelBuilder.Entity<ProjectChatMessageAttachment>()
                .Property(a => a.FileType)
                .HasMaxLength(50);

            modelBuilder.Entity<ProjectChatMessageAttachment>()
                .Property(a => a.MimeType)
                .HasMaxLength(100);


            // رابطه WorkflowStatus با Project
            modelBuilder.Entity<WorkflowStatus>()
                .HasOne(w => w.Project)
                .WithMany(p => p.WorkflowStatuses)
                .HasForeignKey(w => w.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // رابطه WorkflowStatus با Sprint (اختیاری)
            modelBuilder.Entity<WorkflowStatus>()
                .HasOne(w => w.Sprint)
                .WithMany()
                .HasForeignKey(w => w.SprintId)
                .OnDelete(DeleteBehavior.SetNull);

            // رابطه TaskItem با Sprint (اختیاری)
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Sprint)
                .WithMany(s => s.Tasks)
                .HasForeignKey(t => t.SprintId)
                .OnDelete(DeleteBehavior.SetNull);

            // رابطه TaskItem با WorkflowStatus (اختیاری)
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Status)
                .WithMany(w => w.Tasks)
                .HasForeignKey(t => t.StatusId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configuration برای WorkflowTransition
            modelBuilder.Entity<WorkflowTransition>()
                .HasOne(wt => wt.FromStatus)
                .WithMany(ws => ws.OutgoingTransitions)
                .HasForeignKey(wt => wt.FromStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WorkflowTransition>()
                .HasOne(wt => wt.ToStatus)
                .WithMany(ws => ws.IncomingTransitions)
                .HasForeignKey(wt => wt.ToStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<WorkflowTransition>()
                .HasOne(wt => wt.Project)
                .WithMany(p => p.WorkflowTransitions)
                .HasForeignKey(wt => wt.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index برای WorkflowTransition
            modelBuilder.Entity<WorkflowTransition>()
                .HasIndex(wt => new { wt.FromStatusId, wt.ToStatusId, wt.ProjectId })
                .IsUnique();

            // Configuration برای IssueStatusHistory
            modelBuilder.Entity<IssueStatusHistory>()
                .HasOne(h => h.Task)
                .WithMany(t => t.StatusHistory)
                .HasForeignKey(h => h.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<IssueStatusHistory>()
                .HasOne(h => h.FromStatus)
                .WithMany()
                .HasForeignKey(h => h.FromStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<IssueStatusHistory>()
                .HasOne(h => h.ToStatus)
                .WithMany()
                .HasForeignKey(h => h.ToStatusId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<IssueStatusHistory>()
                .HasOne(h => h.Transition)
                .WithMany()
                .HasForeignKey(h => h.TransitionId)
                .OnDelete(DeleteBehavior.SetNull);

            // Index برای IssueStatusHistory
            modelBuilder.Entity<IssueStatusHistory>()
                .HasIndex(h => h.TaskId);

            modelBuilder.Entity<IssueStatusHistory>()
                .HasIndex(h => h.ChangedAt);

            
            // Sprint relationships
            modelBuilder.Entity<Sprint>()
                .HasOne(s => s.Project)
                .WithMany(p => p.Sprints)
                .HasForeignKey(s => s.ProjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SprintTask>()
                .HasOne(st => st.Sprint)
                .WithMany(s => s.SprintTasks)
                .HasForeignKey(st => st.SprintId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SprintTask>()
                .HasOne(st => st.Task)
                .WithMany(t => t.SprintTasks)
                .HasForeignKey(st => st.TaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // Unique constraint: یک تسک نمی‌تونه در یک اسپرینت دو بار باشه
            modelBuilder.Entity<SprintTask>()
                .HasIndex(x => new { x.SprintId, x.TaskId })
                .IsUnique();

            // 🚀 Index برای Phone در User (برای بهینه‌سازی لاگین)
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Phone)
                .HasFilter("[Phone] IS NOT NULL");

            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .Property(n => n.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<Notification>()
                .Property(n => n.RelatedEntityType)
                .HasMaxLength(100);

            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead });

            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.CreatedAt);

            // Board configurations (separate from TaskPlanner)
            modelBuilder.Entity<Board>()
                .Property(b => b.CreatorUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<BoardStatus>()
                .HasOne(bs => bs.Board)
                .WithMany(b => b.Statuses)
                .HasForeignKey(bs => bs.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BoardStatus>()
                .HasIndex(bs => new { bs.BoardId, bs.Order });

            modelBuilder.Entity<BoardStatus>()
                .HasIndex(bs => bs.BoardId);

            // Ignore the Tasks navigation property on BoardStatus since we use Status string field instead
            modelBuilder.Entity<BoardStatus>()
                .Ignore(bs => bs.Tasks);

            modelBuilder.Entity<BoardMember>()
                .HasOne(bm => bm.Board)
                .WithMany(b => b.Members)
                .HasForeignKey(bm => bm.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BoardMember>()
                .HasIndex(bm => new { bm.BoardId, bm.UserId })
                .IsUnique();

            modelBuilder.Entity<BoardTask>()
                .HasOne(bt => bt.Board)
                .WithMany(b => b.Tasks)
                .HasForeignKey(bt => bt.BoardId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BoardTask>()
                .Property(bt => bt.CreatorUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<BoardTask>()
                .Property(bt => bt.AssignedUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<BoardTask>()
                .HasIndex(bt => bt.BoardId);

            // رابطه parent-child برای BoardTask (کارک‌ها)
            modelBuilder.Entity<BoardTask>()
                .HasOne(bt => bt.ParentTask)
                .WithMany(bt => bt.ChildTasks)
                .HasForeignKey(bt => bt.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);

            // BoardTaskComment configurations
            modelBuilder.Entity<BoardTaskComment>()
                .HasOne(c => c.BoardTask)
                .WithMany(t => t.Comments)
                .HasForeignKey(c => c.BoardTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BoardTaskComment>()
                .Property(c => c.UserId)
                .HasMaxLength(450);

            modelBuilder.Entity<BoardTaskComment>()
                .Property(c => c.UserName)
                .HasMaxLength(200);

            modelBuilder.Entity<BoardTaskComment>()
                .HasIndex(c => c.BoardTaskId);

            // BoardTaskCommentAttachment configurations
            modelBuilder.Entity<BoardTaskCommentAttachment>()
                .HasOne(a => a.BoardTaskComment)
                .WithMany(c => c.Attachments)
                .HasForeignKey(a => a.BoardTaskCommentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<BoardTaskCommentAttachment>()
                .Property(a => a.FileName)
                .HasMaxLength(500);

            modelBuilder.Entity<BoardTaskCommentAttachment>()
                .Property(a => a.FilePath)
                .HasMaxLength(1000);

            modelBuilder.Entity<BoardTaskCommentAttachment>()
                .Property(a => a.FileType)
                .HasMaxLength(50);

            modelBuilder.Entity<BoardTaskCommentAttachment>()
                .Property(a => a.MimeType)
                .HasMaxLength(100);

            // Project Image Gallery configurations
            // رابطه ProjectImageGallery با Project
            modelBuilder.Entity<ProjectImageGallery>()
                .HasOne(img => img.Project)
                .WithMany()
                .HasForeignKey(img => img.ProjectId)
                .OnDelete(DeleteBehavior.NoAction);

            // رابطه ProjectImageGalleryFolder با ParentFolder (تودرتو)
            modelBuilder.Entity<ProjectImageGalleryFolder>()
                .HasOne(f => f.ParentFolder)
                .WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict); // جلوگیری از حذف پوشه‌ای که پوشه‌های فرزند دارد

            // رابطه ProjectImageGalleryFolder با Project
            modelBuilder.Entity<ProjectImageGalleryFolder>()
                .HasOne(f => f.Project)
                .WithMany()
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.NoAction); // جلوگیری از multiple cascade paths

            // رابطه ProjectImageGallery با ProjectImageGalleryFolder
            modelBuilder.Entity<ProjectImageGallery>()
                .HasOne(img => img.Folder)
                .WithMany(f => f.Images)
                .HasForeignKey(img => img.FolderId)
                .OnDelete(DeleteBehavior.SetNull); // اگر پوشه حذف شد، عکس‌ها بدون پوشه می‌مانند

            // Index برای ProjectId در ProjectImageGalleryFolder
            modelBuilder.Entity<ProjectImageGalleryFolder>()
                .HasIndex(f => f.ProjectId);

            // Index برای ParentFolderId در ProjectImageGalleryFolder
            modelBuilder.Entity<ProjectImageGalleryFolder>()
                .HasIndex(f => f.ParentFolderId);

            // Index برای ProjectId در ProjectImageGallery
            modelBuilder.Entity<ProjectImageGallery>()
                .HasIndex(img => img.ProjectId);

            // Index برای FolderId در ProjectImageGallery
            modelBuilder.Entity<ProjectImageGallery>()
                .HasIndex(img => img.FolderId);

            modelBuilder.Entity<Lead>()
                .Property(l => l.OwnerUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<Lead>()
                .HasOne(l => l.ConvertedProject)
                .WithMany()
                .HasForeignKey(l => l.ConvertedProjectId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Lead>()
                .HasIndex(l => l.OwnerUserId);

            modelBuilder.Entity<Lead>()
                .HasIndex(l => l.Status);

            modelBuilder.Entity<LeadMember>()
                .HasOne(m => m.Lead)
                .WithMany(l => l.LeadMembers)
                .HasForeignKey(m => m.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeadMember>()
                .Property(m => m.UserId)
                .HasMaxLength(450);

            modelBuilder.Entity<LeadMember>()
                .Property(m => m.AddedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<LeadMember>()
                .HasIndex(m => new { m.LeadId, m.UserId })
                .IsUnique();

            modelBuilder.Entity<LeadMember>()
                .HasIndex(m => m.UserId);

            modelBuilder.Entity<LeadInvitation>()
                .HasOne(i => i.Lead)
                .WithMany(l => l.LeadInvitations)
                .HasForeignKey(i => i.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeadInvitation>()
                .Property(i => i.InviterId)
                .HasMaxLength(450);

            modelBuilder.Entity<LeadInvitation>()
                .Property(i => i.InviteeId)
                .HasMaxLength(450);

            modelBuilder.Entity<LeadInvitation>()
                .Property(i => i.InviteePhone)
                .HasMaxLength(50);

            modelBuilder.Entity<LeadInvitation>()
                .HasIndex(i => new { i.LeadId, i.Status });

            modelBuilder.Entity<LeadSession>()
                .HasOne(s => s.Lead)
                .WithMany(l => l.Sessions)
                .HasForeignKey(s => s.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeadSession>()
                .Property(s => s.CreatedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<LeadSession>()
                .Property(s => s.Notes)
                .HasMaxLength(500);

            modelBuilder.Entity<LeadSession>()
                .HasIndex(s => new { s.LeadId, s.ScheduledAt });

            modelBuilder.Entity<LeadNote>()
                .HasOne(n => n.Lead)
                .WithMany(l => l.LeadNotes)
                .HasForeignKey(n => n.LeadId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<LeadNote>()
                .Property(n => n.Title)
                .HasMaxLength(200);

            modelBuilder.Entity<LeadNote>()
                .Property(n => n.CreatedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<LeadNote>()
                .HasIndex(n => new { n.LeadId, n.CreatedAt });

            // ========== Project Features ==========
            modelBuilder.Entity<ProjectFeature>()
                .HasOne(f => f.Project)
                .WithMany(p => p.Features)
                .HasForeignKey(f => f.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectFeature>()
                .HasOne(f => f.CodeReviewerUser)
                .WithMany()
                .HasForeignKey(f => f.CodeReviewerUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectFeature>()
                .HasIndex(f => f.ProjectId);

            modelBuilder.Entity<ProjectFeature>()
                .Property(f => f.CodeReviewerUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectFeature>()
                .Property(f => f.CreatedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<FeatureFunction>()
                .HasOne(x => x.Feature)
                .WithMany(f => f.Functions)
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeaturePageState>()
                .HasOne(x => x.Feature)
                .WithMany(f => f.PageStates)
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeaturePageState>()
                .HasIndex(x => new { x.FeatureId, x.StateType })
                .IsUnique();

            modelBuilder.Entity<FeatureApiContract>()
                .HasOne(x => x.Feature)
                .WithMany(f => f.ApiContracts)
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureBusinessRule>()
                .HasOne(x => x.Feature)
                .WithMany(f => f.BusinessRules)
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureCodeReview>()
                .HasOne(x => x.Feature)
                .WithMany(f => f.CodeReviews)
                .HasForeignKey(x => x.FeatureId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FeatureCodeReview>()
                .HasOne(x => x.ReviewerUser)
                .WithMany()
                .HasForeignKey(x => x.ReviewerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeatureCodeReview>()
                .HasOne(x => x.RevieweeUser)
                .WithMany()
                .HasForeignKey(x => x.RevieweeUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FeatureCodeReview>()
                .HasIndex(x => x.FeatureId);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Feature)
                .WithMany(f => f.Tasks)
                .HasForeignKey(t => t.FeatureId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => t.FeatureId);

            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.Ticket)
                .WithMany(tk => tk.Tasks)
                .HasForeignKey(t => t.TicketId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<TaskItem>()
                .HasIndex(t => t.TicketId);

            // ========== Project Tickets ==========
            modelBuilder.Entity<ProjectTicket>()
                .HasOne(t => t.Project)
                .WithMany(p => p.Tickets)
                .HasForeignKey(t => t.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTicket>()
                .HasOne(t => t.Feature)
                .WithMany(f => f.Tickets)
                .HasForeignKey(t => t.FeatureId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ProjectTicket>()
                .HasOne(t => t.AskedToUser)
                .WithMany()
                .HasForeignKey(t => t.AskedToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectTicket>()
                .HasOne(t => t.CreatedByUser)
                .WithMany()
                .HasForeignKey(t => t.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ProjectTicket>()
                .HasIndex(t => t.ProjectId);

            modelBuilder.Entity<ProjectTicket>()
                .HasIndex(t => t.FeatureId);

            modelBuilder.Entity<ProjectTicket>()
                .HasIndex(t => t.AskedToUserId);

            modelBuilder.Entity<ProjectTicket>()
                .Property(t => t.AskedToUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectTicket>()
                .Property(t => t.CreatedByUserId)
                .HasMaxLength(450);

            modelBuilder.Entity<ProjectTicketMessage>()
                .HasOne(m => m.Ticket)
                .WithMany(t => t.Messages)
                .HasForeignKey(m => m.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ProjectTicketMessage>()
                .HasOne(m => m.AuthorUser)
                .WithMany()
                .HasForeignKey(m => m.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProjectTicketMessage>()
                .HasIndex(m => m.TicketId);

            modelBuilder.Entity<ProjectTicketMessage>()
                .Property(m => m.AuthorUserId)
                .HasMaxLength(450);
        }
        public void MarkAsModified<T>(T entity) where T : class
        {
            Entry(entity).State = EntityState.Modified;
        }
        public void MarkPropertyAsModified<T, TProperty>(T entity, Expression<Func<T, TProperty>> property) where T : class
        {
            Entry(entity).Property(property).IsModified = true;
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await Database.BeginTransactionAsync();  // Expose this method from the DbContext
        }


    }
}


