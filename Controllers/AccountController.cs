using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MVCMusicStore.Authentication;
using MVCMusicStore.Utilities;
using MVCMusicStore.ViewModels;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace MVCMusicStore.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly ILogger<AccountController> logger;
        private readonly EmailSender emailSender;

        public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ILogger<AccountController> logger, EmailSender emailSender)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
            this.emailSender = emailSender;
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

            var user = await userManager.FindByNameAsync(loginViewModel.UserName);

            if (user != null)
            {
                var loginResult = await signInManager.PasswordSignInAsync(user, loginViewModel.Password, loginViewModel.RememberMe, false);

                if (loginResult.Succeeded && string.IsNullOrEmpty(loginViewModel.ReturnUrl))
                {
                    logger.LogInformation("User logged in.");

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
            await signInManager.SignOutAsync();

            logger.LogInformation("User logged out.");

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

            var identityResult = await userManager.CreateAsync(user, registerViewModel.Password);

            var emailConfirmationToken = await userManager.GenerateEmailConfirmationTokenAsync(user);

            if (identityResult.Succeeded)
            {
                logger.LogInformation("User created a new account with password");

                var callBackUrl = Url.Page("/Account/ConfirmEmail", null, new { userId = user.Id, emailConfirmationToken }, Request.Scheme);

                await emailSender.SendEmailAsync(registerViewModel.Email, "Confirm your email", $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callBackUrl)}'>clicking here</a>.");

                await signInManager.SignInAsync(user, false);

                return RedirectToAction("Index", "Home");
            }

            foreach (var error in identityResult.Errors)
            {
                logger.LogError($"Failed to register: '{error.Description}'");

                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(registerViewModel);
        }

        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null) return RedirectToAction("Index", "Home");

            var user = await userManager.FindByIdAsync(userId);

            if (user == null) return NotFound($"Unable to load user with ID '{userId}'");

            var confirmResult = await userManager.ConfirmEmailAsync(user, token);

            if (!confirmResult.Succeeded) throw new InvalidOperationException($"Error confirming email for user with ID '{userId}'");

            return View();
        }

        // GET: /Account/ResetPassword

        [AllowAnonymous]
        public IActionResult ResetPassword(string token = null)
        {
            var resetPasswordViewModel = new ResetPasswordViewModel { Token = token };

            return token == null ? View("Error") : View(resetPasswordViewModel);
        }

        // POST: /Account/ResetPassword

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel resetPasswordViewModel)
        {
            if (!ModelState.IsValid) return View(resetPasswordViewModel);

            var user = await userManager.FindByEmailAsync(resetPasswordViewModel.Email);

            if (user == null) return RedirectToAction("ResetPasswordConfirmation", "Account");

            var result = await userManager.ResetPasswordAsync(user, resetPasswordViewModel.Token, resetPasswordViewModel.Password);

            return result.Succeeded ? (IActionResult)RedirectToAction("ResetPasswordConfirmation", "Account") : View();
        }

        // POST: /Account/ForgotPassword

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel forgotPasswordViewModel)
        {
            if (!ModelState.IsValid) return View(forgotPasswordViewModel);

            var user = await userManager.FindByEmailAsync(forgotPasswordViewModel.Email);

            if (user == null || !await userManager.IsEmailConfirmedAsync(user))
            {
                return RedirectToAction("ForgotPasswordConfirmation", "Account");
            }

            var passwordResetToken = await userManager.GeneratePasswordResetTokenAsync(user);

            var callBackUrl = Url.Page("/Account/ResetPassword", null, new { passwordResetToken }, Request.Scheme);

            await emailSender.SendEmailAsync(forgotPasswordViewModel.Email, "Reset Password", $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callBackUrl)}'>clicking here</a>.");

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