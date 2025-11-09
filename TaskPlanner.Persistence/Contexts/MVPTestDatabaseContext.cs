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
        public DbSet<PersonalNote> PersonalNotes { get; set; }
        public DbSet<PersonalNoteAttachment> PersonalNoteAttachments { get; set; }
        public DbSet<TaskComment> TaskComments { get; set; }
        public DbSet<TaskCommentAttachment> TaskCommentAttachments { get; set; }
        public DbSet<Sprint> Sprints { get; set; }
        public DbSet<SprintTask> SprintTasks { get; set; }
        public DbSet<WorkflowStatus> WorkflowStatuses { get; set; }
        public DbSet<WorkflowTransition> WorkflowTransitions { get; set; }
        public DbSet<IssueStatusHistory> IssueStatusHistories { get; set; }
        public DbSet<Notification> Notifications { get; set; }


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

            // Default value برای IssueType
            modelBuilder.Entity<TaskItem>()
                .Property(t => t.IssueType)
                .HasDefaultValue(IssueType.Task);

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

            // رابطه PersonalNoteAttachment با PersonalNote
            modelBuilder.Entity<PersonalNoteAttachment>()
                .HasOne(a => a.PersonalNote)
                .WithMany(n => n.Attachments)
                .HasForeignKey(a => a.PersonalNoteId)
                .OnDelete(DeleteBehavior.Cascade);

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
                .WithMany()
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


