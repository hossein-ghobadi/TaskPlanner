using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Endpoint.Site.Models;
using TaskPlanner.Application.Helpers;
using TaskPlanner.Application.Services.LeaveService;

namespace Endpoint.Site.Controllers
{
    [Authorize(Roles = "ADMIN")]
    [Route("Admin/[controller]/[action]")]
    public class AdminLeavesController : Controller
    {
        private readonly ILeaveService _leaveService;

        public AdminLeavesController(ILeaveService leaveService)
        {
            _leaveService = leaveService;
        }

        public async Task<IActionResult> Index(LeaveIndexFilterVm filter)
        {
            var adminId = GetAdminId();
            await PopulateLookupsAsync();

            DateTime? fromDate = null;
            DateTime? toDate = null;

            if (!string.IsNullOrWhiteSpace(filter.FromPersian))
            {
                try { fromDate = PersianDateTimeHelper.ParsePersianDate(filter.FromPersian); }
                catch { ModelState.AddModelError(nameof(filter.FromPersian), "فرمت تاریخ شروع معتبر نیست."); }
            }

            if (!string.IsNullOrWhiteSpace(filter.ToPersian))
            {
                try { toDate = PersianDateTimeHelper.ParsePersianDate(filter.ToPersian); }
                catch { ModelState.AddModelError(nameof(filter.ToPersian), "فرمت تاریخ پایان معتبر نیست."); }
            }

            var result = await _leaveService.GetMyRecordsAsync(adminId, new LeaveRecordFilterDto
            {
                UserId = filter.UserId,
                LeaveTypeId = filter.LeaveTypeId,
                FromDate = fromDate,
                ToDate = toDate,
                SearchNote = filter.SearchNote,
                Page = filter.Page,
                PageSize = 20
            });

            var items = result.Items.Select(MapToListVm).ToList();
            ViewBag.Filter = filter;
            ViewBag.TotalCount = result.TotalCount;
            ViewBag.CurrentPage = filter.Page < 1 ? 1 : filter.Page;
            ViewBag.PageSize = 20;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(result.TotalCount / 20.0));

            return View(items);
        }

        public async Task<IActionResult> Create()
        {
            await PopulateLookupsAsync();
            return View(new LeaveFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeaveFormVm model)
        {
            CombineHourlyDateTimeFields(model);
            ValidateHourlyTimes(model);

            if (!ModelState.IsValid)
            {
                SplitHourlyFieldsForDisplay(model);
                await PopulateLookupsAsync();
                return View(model);
            }

            try
            {
                await _leaveService.CreateAsync(GetAdminId(), new CreateLeaveRecordDto
                {
                    UserId = model.UserId,
                    LeaveTypeId = model.LeaveTypeId,
                    StartPersian = model.StartPersian,
                    EndPersian = model.EndPersian,
                    IsHourly = model.IsHourly,
                    DurationDays = model.DurationDays,
                    Note = model.Note
                });

                TempData["SuccessMessage"] = "مرخصی با موفقیت ثبت شد.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                SplitHourlyFieldsForDisplay(model);
                await PopulateLookupsAsync();
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var record = await _leaveService.GetByIdAsync(id, GetAdminId());
            if (record == null)
                return NotFound();

            await PopulateLookupsAsync();

            var colleagues = ViewBag.Colleagues as IReadOnlyList<ColleagueSelectDto> ?? Array.Empty<ColleagueSelectDto>();
            if (!colleagues.Any(c => c.Id == record.UserId))
            {
                ViewBag.Colleagues = colleagues
                    .Concat(new[] { new ColleagueSelectDto { Id = record.UserId, DisplayName = record.ColleagueName } })
                    .ToList();
            }

            var isHourly = IsHourlyRecord(record);
            var model = new LeaveFormVm
            {
                Id = record.Id,
                UserId = record.UserId,
                LeaveTypeId = record.LeaveTypeId,
                IsHourly = isHourly,
                DurationDays = record.DurationDays ?? (int)Math.Round(record.DurationHours / 8),
                Note = record.Note
            };
            PopulateFormDateTimes(model, record, isHourly);

            return View("Create", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(LeaveFormVm model)
        {
            if (!model.Id.HasValue)
                return NotFound();

            CombineHourlyDateTimeFields(model);
            ValidateHourlyTimes(model);

            if (!ModelState.IsValid)
            {
                SplitHourlyFieldsForDisplay(model);
                await PopulateLookupsAsync();
                return View("Create", model);
            }

            try
            {
                await _leaveService.UpdateAsync(GetAdminId(), new UpdateLeaveRecordDto
                {
                    Id = model.Id.Value,
                    UserId = model.UserId,
                    LeaveTypeId = model.LeaveTypeId,
                    StartPersian = model.StartPersian,
                    EndPersian = model.EndPersian,
                    IsHourly = model.IsHourly,
                    DurationDays = model.DurationDays,
                    Note = model.Note
                });

                TempData["SuccessMessage"] = "مرخصی با موفقیت ویرایش شد.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                SplitHourlyFieldsForDisplay(model);
                await PopulateLookupsAsync();
                return View("Create", model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _leaveService.DeleteAsync(id, GetAdminId());
                TempData["SuccessMessage"] = "مرخصی حذف شد.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Stats(LeaveStatsFilterVm filter)
        {
            var adminId = GetAdminId();
            var today = DateTime.Today;
            var persianCalendar = new PersianCalendar();
            var year = persianCalendar.GetYear(today);
            var month = persianCalendar.GetMonth(today);

            DateTime from;
            DateTime to;

            if (!string.IsNullOrWhiteSpace(filter.FromPersian) && !string.IsNullOrWhiteSpace(filter.ToPersian))
            {
                try
                {
                    from = PersianDateTimeHelper.ParsePersianDate(filter.FromPersian);
                    to = PersianDateTimeHelper.ParsePersianDate(filter.ToPersian);
                }
                catch
                {
                    from = persianCalendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
                    to = persianCalendar.ToDateTime(year, month, persianCalendar.GetDaysInMonth(year, month), 0, 0, 0, 0);
                    filter.FromPersian = PersianDateTimeHelper.FormatPersianDate(from);
                    filter.ToPersian = PersianDateTimeHelper.FormatPersianDate(to);
                    ModelState.AddModelError(string.Empty, "فرمت تاریخ‌های فیلتر آمار معتبر نیست. بازه ماه جاری شمسی نمایش داده شد.");
                }
            }
            else
            {
                from = persianCalendar.ToDateTime(year, month, 1, 0, 0, 0, 0);
                to = persianCalendar.ToDateTime(year, month, persianCalendar.GetDaysInMonth(year, month), 0, 0, 0, 0);
                filter.FromPersian = PersianDateTimeHelper.FormatPersianDate(from);
                filter.ToPersian = PersianDateTimeHelper.FormatPersianDate(to);
            }

            var stats = await _leaveService.GetStatsAsync(adminId, from, to);
            ViewBag.Filter = filter;
            return View(stats);
        }

        public async Task<IActionResult> Calendar(int? offset)
        {
            var adminId = GetAdminId();
            var weekOffset = offset ?? 0;
            var today = DateTime.Today;
            var daysSinceSaturday = ((int)today.DayOfWeek + 1) % 7;
            var startOfWeek = today.AddDays(-daysSinceSaturday).AddDays(7 * weekOffset);
            var endOfWeek = startOfWeek.AddDays(6);

            var days = await _leaveService.GetCalendarAsync(adminId, startOfWeek, endOfWeek);

            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;
            ViewBag.Offset = weekOffset;

            return View(days);
        }

        private string GetAdminId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new InvalidOperationException("کاربر احراز هویت نشده است.");

        private async Task PopulateLookupsAsync()
        {
            ViewBag.Colleagues = await _leaveService.GetColleaguesAsync(GetAdminId());
            ViewBag.LeaveTypes = await _leaveService.GetActiveLeaveTypesAsync();
        }

        private static LeaveRecordListVm MapToListVm(LeaveRecordDto dto) => new()
        {
            Id = dto.Id,
            ColleagueName = dto.ColleagueName,
            LeaveTypeName = dto.LeaveTypeName,
            LeaveTypeColor = dto.LeaveTypeColor,
            StartAt = dto.StartAt,
            EndAt = dto.EndAt,
            DurationDays = dto.DurationDays,
            DurationHours = dto.DurationHours,
            Note = dto.Note,
            CreatedAt = dto.CreatedAt
        };

        private static bool IsHourlyRecord(LeaveRecordDto record) =>
            record.LeaveTypeIsHourlyAllowed && !record.DurationDays.HasValue;

        private static string FormatForForm(DateTime value, bool isHourly) =>
            isHourly
                ? IranDateTimeHelper.FormatLeaveDateTime(value)
                : IranDateTimeHelper.FormatLeaveDate(value);

        private static void PopulateFormDateTimes(LeaveFormVm model, LeaveRecordDto record, bool isHourly)
        {
            var start = FormatForForm(record.StartAt, isHourly);
            var end = FormatForForm(record.EndAt, isHourly);

            if (isHourly)
            {
                (model.StartPersian, model.StartTimePersian) = SplitDateTime(start);
                (model.EndPersian, model.EndTimePersian) = SplitDateTime(end);
            }
            else
            {
                model.StartPersian = start;
                model.EndPersian = end;
            }
        }

        private static void CombineHourlyDateTimeFields(LeaveFormVm model)
        {
            if (!model.IsHourly)
                return;

            model.StartPersian = CombineDateAndTime(model.StartPersian, model.StartTimePersian);
            model.EndPersian = CombineDateAndTime(model.EndPersian, model.EndTimePersian);
        }

        private static void SplitHourlyFieldsForDisplay(LeaveFormVm model)
        {
            if (!model.IsHourly)
                return;

            (model.StartPersian, model.StartTimePersian) = SplitDateTime(model.StartPersian);
            (model.EndPersian, model.EndTimePersian) = SplitDateTime(model.EndPersian);
        }

        private void ValidateHourlyTimes(LeaveFormVm model)
        {
            if (!model.IsHourly)
                return;

            if (string.IsNullOrWhiteSpace(model.StartTimePersian))
                ModelState.AddModelError(nameof(model.StartTimePersian), "ساعت شروع الزامی است.");

            if (string.IsNullOrWhiteSpace(model.EndTimePersian))
                ModelState.AddModelError(nameof(model.EndTimePersian), "ساعت پایان الزامی است.");
        }

        private static string CombineDateAndTime(string date, string? time)
        {
            var datePart = PersianDateTimeHelper.ToLatinDigits(date?.Trim() ?? "");
            var timePart = PersianDateTimeHelper.ToLatinDigits(time?.Trim() ?? "");
            return string.IsNullOrWhiteSpace(timePart) ? datePart : $"{datePart} {timePart}";
        }

        private static (string date, string? time) SplitDateTime(string value)
        {
            var normalized = PersianDateTimeHelper.ToLatinDigits(value?.Trim() ?? "");
            var spaceIdx = normalized.IndexOf(' ');
            if (spaceIdx <= 0)
                return (normalized, null);

            return (normalized[..spaceIdx], normalized[(spaceIdx + 1)..]);
        }
    }
}
