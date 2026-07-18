using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class FeatureIndexVm
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public List<FeatureListItemVm> Features { get; set; } = new();
    }

    public class FeatureListItemVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int TaskCount { get; set; }
        public int FunctionCount { get; set; }
        public int ApiContractCount { get; set; }
        public int BusinessRuleCount { get; set; }
        public string? CodeReviewerName { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class FeatureCreateVm
    {
        [Required(ErrorMessage = "نام فیچر الزامی است")]
        [StringLength(200)]
        public string Name { get; set; } = "";

        [StringLength(2000)]
        public string? Description { get; set; }
    }

    public class FeatureEditVm : FeatureCreateVm
    {
        public int Id { get; set; }
    }

    public class FeatureSettingsVm
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string FeatureName { get; set; } = "";

        [Display(Name = "مسئول Code Review")]
        public string? CodeReviewerUserId { get; set; }

        public List<SelectListItem> MemberOptions { get; set; } = new();
    }

    public class FeatureFunctionItemVm
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }

        [Required(ErrorMessage = "عنوان کارکرد الزامی است")]
        [StringLength(500)]
        public string Title { get; set; } = "";
    }

    public class FeaturePageStateItemVm
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }
        public FeaturePageStateType StateType { get; set; }

        [Required(ErrorMessage = "توضیح رفتار الزامی است")]
        [StringLength(2000)]
        public string BehaviorDescription { get; set; } = "";

        public bool HasSeparateDesign { get; set; }
    }

    public class FeatureApiContractItemVm
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }

        [Required(ErrorMessage = "Endpoint الزامی است")]
        [StringLength(500)]
        public string Endpoint { get; set; } = "";

        [Required]
        [StringLength(20)]
        public string HttpMethod { get; set; } = "GET";

        [StringLength(4000)]
        public string? RequestDescription { get; set; }

        [StringLength(4000)]
        public string? ResponseDescription { get; set; }

        [StringLength(4000)]
        public string? ErrorResponseDescription { get; set; }

        [StringLength(200)]
        public string? StatusCodes { get; set; }

        [StringLength(1000)]
        public string? Pagination { get; set; }

        [StringLength(1000)]
        public string? Filter { get; set; }

        [StringLength(1000)]
        public string? Sort { get; set; }

        [StringLength(4000)]
        public string? FieldsDescription { get; set; }
    }

    public class FeatureBusinessRuleItemVm
    {
        public int Id { get; set; }
        public int FeatureId { get; set; }

        [Required(ErrorMessage = "توضیح قانون الزامی است")]
        [StringLength(2000)]
        public string Description { get; set; } = "";
    }

    public class FeatureSpecEditVm
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string FeatureName { get; set; } = "";

        public List<FeatureFunctionItemVm> Functions { get; set; } = new();
        public List<FeaturePageStateItemVm> PageStates { get; set; } = new();
        public List<FeatureApiContractItemVm> ApiContracts { get; set; } = new();
        public List<FeatureBusinessRuleItemVm> BusinessRules { get; set; } = new();
    }

    public class FeatureCodeReviewCreateVm
    {
        public int FeatureId { get; set; }

        [Required(ErrorMessage = "انتخاب فرد دریافت‌کننده امتیاز الزامی است")]
        public string RevieweeUserId { get; set; } = "";

        [Required]
        [Range(0, 100, ErrorMessage = "امتیاز باید بین ۰ تا ۱۰۰ باشد")]
        public int Score { get; set; }

        [StringLength(2000)]
        public string? Comment { get; set; }

        public List<SelectListItem> MemberOptions { get; set; } = new();
    }

    public class FeatureCodeReviewItemVm
    {
        public int Id { get; set; }
        public string ReviewerName { get; set; } = "";
        public string RevieweeName { get; set; } = "";
        public int Score { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class FeatureTaskItemVm
    {
        public int Id { get; set; }
        public string? IssueKey { get; set; }
        public string Title { get; set; } = "";
        public IssueType IssueType { get; set; }
        public bool IsCompleted { get; set; }
        public string? AssignedUserName { get; set; }
        public bool InSprint { get; set; }
        public string? SprintName { get; set; }
    }

    public class FeatureDetailsVm
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public string? CodeReviewerUserId { get; set; }
        public string? CodeReviewerName { get; set; }
        public bool CanManageCodeReview { get; set; }
        public bool CanChangeCodeReviewer { get; set; }
        public bool CanManageFeatureSpec { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<FeatureFunctionItemVm> Functions { get; set; } = new();
        public List<FeaturePageStateItemVm> PageStates { get; set; } = new();
        public List<FeatureApiContractItemVm> ApiContracts { get; set; } = new();
        public List<FeatureBusinessRuleItemVm> BusinessRules { get; set; } = new();
        public List<FeatureTaskItemVm> Tasks { get; set; } = new();
        public List<FeatureCodeReviewItemVm> CodeReviews { get; set; } = new();
    }

    public class FeatureMemberOptionVm
    {
        public string UserId { get; set; } = "";
        public string DisplayName { get; set; } = "";
    }
}
