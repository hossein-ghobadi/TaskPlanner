using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities.Users;
using Endpoint.Site.Models;
using TaskPlanner.Application.Services.SMS;
using TaskPlanner.Application.Services.SMS.Commands;

namespace Endpoint.Site.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ISmsSendService _smsSendService;
        private readonly ISmsCheckService _smsCheckService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ISmsSendService smsSendService,
            ISmsCheckService smsCheckService,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _smsSendService = smsSendService;
            _smsCheckService = smsCheckService;
            _logger = logger;
        }

        [HttpGet]
        [Route("/login")]
        [Route("/account/login")]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [Route("/login")]
        [Route("/account/login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Login attempt with invalid model state");
                    return View(model);
                }

                var userName = model.UserName;
                var normalizedUserName = userName.ToUpper();

                var user = await _userManager.Users
                    .FirstOrDefaultAsync(u =>
                        (u.NormalizedUserName != null && u.NormalizedUserName == normalizedUserName) ||
                        (u.NormalizedEmail != null && u.NormalizedEmail == normalizedUserName) ||
                        (u.Phone != null && u.Phone == userName) ||
                        (u.PhoneNumber != null && u.PhoneNumber == userName));

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed - user not found: {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "نام کاربری، ایمیل یا شماره تلفن یافت نشد");
                    return View(model);
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("Login attempt failed - user inactive: {UserName}", user.UserName);
                    ModelState.AddModelError(string.Empty, "حساب کاربری شما غیرفعال است");
                    return View(model);
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user.UserName ?? string.Empty,
                    model.Password,
                    model.RememberMe,
                    lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User {UserName} logged in successfully", user.UserName);
                    return RedirectToLocal(returnUrl ?? Url.Action("Index", "Home")!);
                }

                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out: {UserName}", user.UserName);
                    ModelState.AddModelError(string.Empty, "حساب کاربری شما به دلیل تلاش‌های ناموفق متعدد قفل شده است");
                    return View(model);
                }

                _logger.LogWarning("Login attempt failed - invalid password for user: {UserName}", user.UserName);
                ModelState.AddModelError(string.Empty, "نام کاربری یا رمز عبور اشتباه است");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for user: {UserName}", model.UserName);
                ModelState.AddModelError(string.Empty, "خطایی در ورود به سیستم رخ داد. لطفاً دوباره تلاش کنید.");
                return View(model);
            }
        }

        [HttpGet]
        [Route("/register")]
        [Route("/account/register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [Route("/register")]
        [Route("/account/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            model.Phone = PhoneNumberNormalizer.Normalize(model.Phone);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!PhoneNumberNormalizer.IsValidIranianMobile(model.Phone))
            {
                ModelState.AddModelError(nameof(model.Phone), "شماره موبایل معتبر نیست");
                return View(model);
            }

            var phoneExists = await _userManager.Users.AnyAsync(u =>
                u.Phone == model.Phone || u.PhoneNumber == model.Phone);

            if (phoneExists)
            {
                ModelState.AddModelError(nameof(model.Phone), "این شماره موبایل قبلاً ثبت شده است");
                return View(model);
            }

            var smsCheck = await _smsCheckService.CheckAsync(new RequestSmsCheckDto
            {
                PhoneNumber = model.Phone,
                Code = model.SmsCode
            });

            if (!smsCheck.isSuccess || !smsCheck.data)
            {
                ModelState.AddModelError(nameof(model.SmsCode), smsCheck.message ?? "کد تأیید پیامک نامعتبر است");
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Phone = model.Phone,
                PhoneNumber = model.Phone,
                InsertTime = DateTime.Now,
                IsActive = true,
                IsVarify = true,
                EmailConfirmed = false,
                PhoneNumberConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserName} registered with SMS verification", user.UserName);
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [Route("/account/send-register-sms")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendRegisterSms([FromBody] SendSmsCodeRequest request)
        {
            var phone = PhoneNumberNormalizer.Normalize(request.Phone);

            if (!PhoneNumberNormalizer.IsValidIranianMobile(phone))
            {
                return Json(new { success = false, message = "شماره موبایل معتبر نیست" });
            }

            var phoneExists = await _userManager.Users.AnyAsync(u =>
                u.Phone == phone || u.PhoneNumber == phone);

            if (phoneExists)
            {
                return Json(new { success = false, message = "این شماره موبایل قبلاً ثبت شده است" });
            }

            var sendResult = await _smsSendService.SendAsync(new RequestSmsSendDto
            {
                PhoneNumber = phone
            });

            return Json(new
            {
                success = sendResult.isSuccess && sendResult.data,
                message = sendResult.message ?? (sendResult.data ? "کد تأیید ارسال شد" : "ارسال پیامک ناموفق بود")
            });
        }

        [HttpGet]
        [Route("/forgot-password")]
        [Route("/account/forgot-password")]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [Route("/forgot-password")]
        [Route("/account/forgot-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            model.Phone = PhoneNumberNormalizer.Normalize(model.Phone);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            if (!PhoneNumberNormalizer.IsValidIranianMobile(model.Phone))
            {
                ModelState.AddModelError(nameof(model.Phone), "شماره موبایل معتبر نیست");
                return View(model);
            }

            var user = await _userManager.Users.FirstOrDefaultAsync(u =>
                u.Phone == model.Phone || u.PhoneNumber == model.Phone);

            if (user == null)
            {
                ModelState.AddModelError(nameof(model.Phone), "کاربری با این شماره موبایل یافت نشد");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "حساب کاربری شما غیرفعال است");
                return View(model);
            }

            var smsCheck = await _smsCheckService.CheckAsync(new RequestSmsCheckDto
            {
                PhoneNumber = model.Phone,
                Code = model.SmsCode
            });

            if (!smsCheck.isSuccess || !smsCheck.data)
            {
                ModelState.AddModelError(nameof(model.SmsCode), smsCheck.message ?? "کد تأیید پیامک نامعتبر است");
                return View(model);
            }

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await _userManager.ResetPasswordAsync(user, resetToken, model.NewPassword);

            if (resetResult.Succeeded)
            {
                _logger.LogInformation("Password reset via SMS for user {UserName}", user.UserName);
                TempData["Success"] = "رمز عبور با موفقیت تغییر کرد. اکنون می‌توانید وارد شوید.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in resetResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpPost]
        [Route("/account/send-forgot-password-sms")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendForgotPasswordSms([FromBody] SendSmsCodeRequest request)
        {
            var phone = PhoneNumberNormalizer.Normalize(request.Phone);

            if (!PhoneNumberNormalizer.IsValidIranianMobile(phone))
            {
                return Json(new { success = false, message = "شماره موبایل معتبر نیست" });
            }

            var userExists = await _userManager.Users.AnyAsync(u =>
                u.Phone == phone || u.PhoneNumber == phone);

            if (!userExists)
            {
                return Json(new { success = false, message = "کاربری با این شماره موبایل یافت نشد" });
            }

            var sendResult = await _smsSendService.SendAsync(new RequestSmsSendDto
            {
                PhoneNumber = phone
            });

            return Json(new
            {
                success = sendResult.isSuccess && sendResult.data,
                message = sendResult.message ?? (sendResult.data ? "کد تأیید ارسال شد" : "ارسال پیامک ناموفق بود")
            });
        }

        [HttpPost]
        [Route("/account/logout")]
        [Route("/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User signed out");
            return RedirectToAction(nameof(Login), "Account");
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
