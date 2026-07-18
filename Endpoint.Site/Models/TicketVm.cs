using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class TicketIndexVm
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public bool IsProjectCreator { get; set; }
        public int? FeatureId { get; set; }
        public string? AskedToUserId { get; set; }
        public string? CreatedByUserId { get; set; }
        public List<SelectListItem> FeatureOptions { get; set; } = new();
        public List<SelectListItem> MemberOptions { get; set; } = new();
        public List<TicketListItemVm> Tickets { get; set; } = new();
    }

    public class TicketListItemVm
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public TicketType Type { get; set; }
        public string? FeatureName { get; set; }
        public string AskedToUserName { get; set; } = "";
        public string CreatedByUserName { get; set; } = "";
        public TicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TicketCreateVm
    {
        [Required(ErrorMessage = "عنوان سوال الزامی است")]
        [StringLength(300)]
        public string Title { get; set; } = "";

        [StringLength(4000)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "نوع تیکت الزامی است")]
        public TicketType Type { get; set; } = TicketType.Other;

        public int? FeatureId { get; set; }

        [Required(ErrorMessage = "انتخاب فرد مورد سوال الزامی است")]
        public string AskedToUserId { get; set; } = "";

        public List<SelectListItem> FeatureOptions { get; set; } = new();
        public List<SelectListItem> MemberOptions { get; set; } = new();
        public List<SelectListItem> TypeOptions { get; set; } = new();
    }

    public class TicketDetailsVm
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string Title { get; set; } = "";
        public TicketType Type { get; set; }
        public int? FeatureId { get; set; }
        public string? FeatureName { get; set; }
        public string AskedToUserId { get; set; } = "";
        public string AskedToUserName { get; set; } = "";
        public string? CreatedByUserId { get; set; }
        public string CreatedByUserName { get; set; } = "";
        public TicketStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool CanAnswer { get; set; }
        public bool CanAskFollowUp { get; set; }
        public bool CanManage { get; set; }
        public List<TicketMessageItemVm> Messages { get; set; } = new();
    }

    public class TicketMessageItemVm
    {
        public int Id { get; set; }
        public TicketMessageKind Kind { get; set; }
        public string Body { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string? AuthorUserId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class TicketAddMessageVm
    {
        public int TicketId { get; set; }

        [Required(ErrorMessage = "متن پیام الزامی است")]
        [StringLength(4000)]
        public string Body { get; set; } = "";

        public TicketStatus? Status { get; set; }
    }
}
