using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Notifications;
using TaskPlanner.Domain.Entities.Boards;

namespace TaskPlanner.Application.Interfaces.Contexts
{
    public interface IMVPTestDatabaseContext
    {
        DbSet<T> Set<T>() where T : class;
        

        DbSet<TaskCategory> TaskCategories { get; set; }
        DbSet<TaskItem> TaskItems { get; set; }
        DbSet<Project> Projects { get; set; }
        DbSet<ProjectMember> ProjectMembers { get; set; }
        DbSet<ProjectInvitation> ProjectInvitations { get; set; }
        DbSet<ProjectNote> ProjectNotes { get; set; }
        DbSet<ProjectNoteAttachment> ProjectNoteAttachments { get; set; }
        DbSet<PersonalNote> PersonalNotes { get; set; }
        DbSet<PersonalNoteAttachment> PersonalNoteAttachments { get; set; }
        DbSet<TaskComment> TaskComments { get; set; }
        DbSet<TaskCommentAttachment> TaskCommentAttachments { get; set; }
        DbSet<ProjectChatGroup> ProjectChatGroups { get; set; }
        DbSet<ProjectChatGroupMember> ProjectChatGroupMembers { get; set; }
        DbSet<ProjectChatMessage> ProjectChatMessages { get; set; }
        DbSet<ProjectChatMessageAttachment> ProjectChatMessageAttachments { get; set; }
        DbSet<Sprint> Sprints { get; set; }
        DbSet<SprintTask> SprintTasks { get; set; }
        DbSet<WorkflowStatus> WorkflowStatuses { get; set; }
        DbSet<WorkflowTransition> WorkflowTransitions { get; set; }
        DbSet<IssueStatusHistory> IssueStatusHistories { get; set; }
        DbSet<Notification> Notifications { get; set; }
        DbSet<ProjectIssueType> ProjectIssueTypes { get; set; }
        
        // Board entities (separate from TaskPlanner)
        DbSet<Board> Boards { get; set; }
        DbSet<BoardMember> BoardMembers { get; set; }
        DbSet<BoardStatus> BoardStatuses { get; set; }
        DbSet<BoardTask> BoardTasks { get; set; }
        DbSet<BoardTaskComment> BoardTaskComments { get; set; }
        DbSet<BoardTaskCommentAttachment> BoardTaskCommentAttachments { get; set; }

        DbSet<Lead> Leads { get; set; }

        void MarkAsModified<T>(T entity) where T : class;
        void MarkPropertyAsModified<T, TProperty>(T entity, Expression<Func<T, TProperty>> property) where T : class;

        int SaveChanges(bool acceptAllChangesOnsuccess);
        int SaveChanges();
        Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellation = new CancellationToken());
        Task<int> SaveChangesAsync(CancellationToken cancellation = new CancellationToken());
        Task<IDbContextTransaction> BeginTransactionAsync();  // Add this method to the interface
    }
}
