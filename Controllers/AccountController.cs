using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCMusicStore.Authentication;
using MVCMusicStore.ViewModels;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace MVCMusicStore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly IEmailSender _emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger, IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
        }

        // GET: /Account/Login

        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            var loginViewModel = new LoginViewModel { ReturnUrl = returnUrl };

            return View(loginViewModel);
        }

        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel loginViewModel)
        {
            if (!ModelState.IsValid) return View(loginViewModel);

            var user = await _userManager.FindByNameAsync(loginViewModel.UserName);

            if (user != null)
            {
                var loginResult = await _signInManager.PasswordSignInAsync(user, loginViewModel.Password, loginViewModel.RememberMe, false);

                if (loginResult.Succeeded && string.IsNullOrEmpty(loginViewModel.ReturnUrl))
                {
                    _logger.LogInformation("User logged in.");

                    return RedirectToAction("Index", "Home");
                }

                return Redirect(loginViewModel.ReturnUrl);
            }

            ModelState.AddModelError(string.Empty, "Username or Password not found");

            return View(loginViewModel);
        }

        // POST: /Account/Logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            _logger.LogInformation("User logged out.");

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel registerViewModel)
        {
            if (!ModelState.IsValid) return View(registerViewModel);

            var user = new ApplicationUser { UserName = registerViewModel.UserName, Email = registerViewModel.Email };

            var identityResult = await _userManager.CreateAsync(user, registerViewModel.Password);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            if (identityResult.Succeeded)
            {
                _logger.LogInformation("User created a new account with password");

                var callBackUrl = Url.Page("/Account/ConfirmEmail", null, new { userId = user.Id, code }, Request.Scheme);

                await _emailSender.SendEmailAsync(registerViewModel.Email, "Confirm your email", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callBackUrl)}'>clicking here</a>.");

                await _signInManager.SignInAsync(user, false);

                return RedirectToAction("Index", "Home");

            }

            foreach (var error in identityResult.Errors)
            {

                _logger.LogError($"Failed to register: '{error.Description}'");

                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(registerViewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string code)
        {
            if (userId == null || code == null) return RedirectToAction("Index", "Home");

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null) return NotFound($"Unable to load user with ID '{userId}'");

            var confirmResult = await _userManager.ConfirmEmailAsync(user, code);

            if (!confirmResult.Succeeded) throw new InvalidOperationException($"Error confirming email for user with ID '{userId}'");

            return View();
        }

        // GET: /Account/ResetPassword

        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            var resetPasswordViewModel = new ResetPasswordViewModel { Code = code };

            return code == null ? View("Error") : View(resetPasswordViewModel);
        }

        // POST: /Account/ResetPassword

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
        {
            if (!ModelState.IsValid) return View(resetPasswordViewModel);

            var user = await _userManager.FindByEmailAsync(resetPasswordViewModel.Email);

            if (user == null) return RedirectToAction("ResetPasswordConfirmation", "Account");

            var result = await _userManager.ResetPasswordAsync(user, resetPasswordViewModel.Code, resetPasswordViewModel.Password);

            return result.Succeeded ? (IActionResult)RedirectToAction("ResetPasswordConfirmation", "Account") : View();
        }

        // POST: /Account/ForgotPassword

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel)
        {
            if (!ModelState.IsValid) return View(forgotPasswordViewModel);

            var user = await _userManager.FindByEmailAsync(forgotPasswordViewModel.Email);

            if (user == null || !await _userManager.IsEmailConfirmedAsync(user))
            {
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);

            var callBackUrl = Url.Page("/Account/ResetPassword", null, new { code }, Request.Scheme);

            await _emailSender.SendEmailAsync(forgotPasswordViewModel.Email, "Reset Password", $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callBackUrl)}'>clicking here</a>.");

            return RedirectToAction("ForgotPasswordConfirmation", "Account");
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
    }
}