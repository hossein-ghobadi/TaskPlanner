using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public static class TaskScheduleHelper
    {
        public static int? CalculateDurationDays(DateTime startDate, DateTime? dueDate)
        {
            if (!dueDate.HasValue)
            {
                return null;
            }

            var days = (dueDate.Value.Date - startDate.Date).Days;
            return days >= 0 ? days : null;
        }

        public static void ApplySchedule(TaskItem task, DateTime? startDate, int? durationDays)
        {
            if (startDate.HasValue)
            {
                task.StartDate = startDate.Value.Date;
            }

            if (durationDays.HasValue && durationDays.Value >= 0)
            {
                task.DurationDays = durationDays.Value;
                task.DueDate = task.StartDate.Date.AddDays(durationDays.Value);
                return;
            }

            task.DurationDays = null;
        }
    }
}
