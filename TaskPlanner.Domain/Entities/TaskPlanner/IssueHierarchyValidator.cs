using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// Validator برای بررسی صحت سلسله مراتب Issue (مثل Jira)
    /// </summary>
    public static class IssueHierarchyValidator
    {
        /// <summary>
        /// نتیجه Validation
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public List<string> Errors { get; set; } = new List<string>();

            public static ValidationResult Success()
            {
                return new ValidationResult { IsValid = true };
            }

            public static ValidationResult Failure(params string[] errors)
            {
                return new ValidationResult
                {
                    IsValid = false,
                    Errors = errors.ToList()
                };
            }
        }

        /// <summary>
        /// بررسی صحت Parent-Child relationship
        /// </summary>
        public static ValidationResult ValidateParentChild(IssueType childType, IssueType? parentType)
        {
            // اگر child نیاز به parent نداره
            if (!childType.CanHaveParent())
            {
                if (parentType != null)
                {
                    return ValidationResult.Failure(
                        $"{childType.GetDisplayName()} نمی‌تواند parent داشته باشد."
                    );
                }
                return ValidationResult.Success();
            }

            // اگر child حتماً باید parent داشته باشه (Subtask)
            if (childType.MustHaveParent())
            {
                if (parentType == null)
                {
                    return ValidationResult.Failure(
                        $"{childType.GetDisplayName()} باید حتماً parent داشته باشد."
                    );
                }
            }

            // بررسی نوع parent
            if (parentType != null)
            {
                var allowedParentTypes = childType.GetAllowedParentTypes();
                if (!allowedParentTypes.Contains(parentType.Value))
                {
                    return ValidationResult.Failure(
                        $"{childType.GetDisplayName()} نمی‌تواند زیرمجموعه {parentType.Value.GetDisplayName()} باشد. " +
                        $"انواع مجاز: {string.Join(", ", allowedParentTypes.Select(t => t.GetDisplayName()))}"
                    );
                }
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// بررسی صحت Parent-Child relationship بر اساس Level
        /// </summary>
        public static ValidationResult ValidateParentChildByLevel(IssueTypeLevel childLevel, IssueTypeLevel? parentLevel, string childName, string? parentName = null)
        {
            // Epic نمی‌تواند parent داشته باشد
            if (childLevel == IssueTypeLevel.Epic)
            {
                if (parentLevel != null)
                {
                    return ValidationResult.Failure($"{childName} نمی‌تواند parent داشته باشد.");
                }
                return ValidationResult.Success();
            }

            // Subtask باید حتماً parent داشته باشد (Story-level)
            if (childLevel == IssueTypeLevel.Subtask)
            {
                if (parentLevel == null)
                {
                    return ValidationResult.Failure($"{childName} باید حتماً parent داشته باشد.");
                }
                if (parentLevel != IssueTypeLevel.StoryLevel)
                {
                    return ValidationResult.Failure($"{childName} فقط می‌تواند زیرمجموعه انواع Story-level باشد.");
                }
                return ValidationResult.Success();
            }

            // Story-level types می‌توانند زیر Epic باشند (اختیاری)
            if (childLevel == IssueTypeLevel.StoryLevel)
            {
                if (parentLevel != null && parentLevel != IssueTypeLevel.Epic)
                {
                    return ValidationResult.Failure($"{childName} فقط می‌تواند زیرمجموعه اپیک باشد.");
                }
                return ValidationResult.Success();
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// بررسی اینکه آیا Issue می‌تونه به Sprint اضافه بشه
        /// </summary>
        public static ValidationResult ValidateAddToSprint(TaskItem issue)
        {
            if (!issue.IssueType.CanAddToSprint())
            {
                return ValidationResult.Failure(
                    $"{issue.IssueType.GetDisplayName()} نمی‌تواند مستقیماً به Sprint اضافه شود. " +
                    "فقط Story‌های زیرمجموعه Epic می‌توانند به Sprint اضافه شوند."
                );
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// بررسی اینکه آیا می‌تونیم child اضافه کنیم
        /// </summary>
        public static ValidationResult ValidateAddChild(IssueType parentType, IssueType childType)
        {
            if (!parentType.CanHaveChildren())
            {
                return ValidationResult.Failure(
                    $"{parentType.GetDisplayName()} نمی‌تواند child داشته باشد."
                );
            }

            var allowedChildTypes = parentType.GetAllowedChildTypes();
            if (!allowedChildTypes.Contains(childType))
            {
                return ValidationResult.Failure(
                    $"{parentType.GetDisplayName()} نمی‌تواند {childType.GetDisplayName()} به عنوان child داشته باشد. " +
                    $"انواع مجاز: {string.Join(", ", allowedChildTypes.Select(t => t.GetDisplayName()))}"
                );
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// بررسی جامع یک Issue قبل از ذخیره
        /// </summary>
        public static ValidationResult ValidateIssue(TaskItem issue, TaskItem? parent = null)
        {
            var errors = new List<string>();

            // بررسی Parent
            var parentValidation = ValidateParentChild(
                issue.IssueType,
                parent?.IssueType
            );
            if (!parentValidation.IsValid)
            {
                errors.AddRange(parentValidation.Errors);
            }

            // بررسی Story Points
            if (issue.StoryPoints.HasValue && !issue.IssueType.HasStoryPoints())
            {
                errors.Add($"{issue.IssueType.GetDisplayName()} نمی‌تواند Story Points داشته باشد.");
            }

            // بررسی Subtask: نباید Sprint مستقل داشته باشه
            if (issue.IssueType == IssueType.Subtask && issue.SprintId.HasValue && parent != null)
            {
                if (parent.SprintId != issue.SprintId)
                {
                    errors.Add("Subtask باید در همان Sprint با Parent خود باشد.");
                }
            }

            if (errors.Any())
            {
                return ValidationResult.Failure(errors.ToArray());
            }

            return ValidationResult.Success();
        }

        /// <summary>
        /// بررسی Circular Reference (جلوگیری از حلقه در parent-child)
        /// </summary>
        public static bool HasCircularReference(int issueId, int? newParentId, Func<int, TaskItem?> getIssueById)
        {
            if (!newParentId.HasValue)
                return false;

            var currentId = newParentId.Value;
            var visited = new HashSet<int> { issueId };

            while (currentId != 0)
            {
                if (visited.Contains(currentId))
                    return true;

                visited.Add(currentId);

                var parent = getIssueById(currentId);
                if (parent == null || !parent.ParentTaskId.HasValue)
                    break;

                currentId = parent.ParentTaskId.Value;
            }

            return false;
        }
    }
}

