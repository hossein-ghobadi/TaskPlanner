using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Domain.Entities.Users;
using Endpoint.Site.Models;
using System.Security.Claims;
using System;

namespace Endpoint.Site.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
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

                var user = await _userManager.FindByNameAsync(model.UserName) 
                         ?? await _userManager.FindByEmailAsync(model.UserName);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed - user not found: {UserName}", model.UserName);
                    ModelState.AddModelError(string.Empty, "نام کاربری یا ایمیل یافت نشد");
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
                    return RedirectToLocal(returnUrl ?? "/");
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
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new User
            {
                UserName = model.Email,
                Email = model.Email,
                FullName = model.FullName,
                Phone = model.Phone,
                InsertTime = DateTime.Now,
                IsActive = true,
                EmailConfirmed = false
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserName} registered", user.UserName);
                
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
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
