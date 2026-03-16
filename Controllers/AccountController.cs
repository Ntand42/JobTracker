using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.WebUtilities;
using System.Text.Encodings.Web;
using System.Text;
using System.Diagnostics;
using JobTracker.Models;

namespace JobTracker.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
        }

        // REGISTER (GET)
        public IActionResult Register()
        {
            return View();
        }

        // REGISTER (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string firstName, string lastName, string phoneNumber, string email, string password)
        {
            // Debugging: Log received values
            Console.WriteLine($"Registering: {firstName} {lastName}, Email: {email}");

             if (!ModelState.IsValid)
             {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors) Console.WriteLine($"Validation Error: {error}");
                return View();
             }

            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = phoneNumber
            };

            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                Console.WriteLine("User created successfully!");
                
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);

                if (string.IsNullOrWhiteSpace(callbackUrl))
                {
                    return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
                }

                TempData["EmailConfirmationLink"] = callbackUrl;
                TempData["EmailConfirmationAddress"] = email;

                await _emailSender.SendEmailAsync(email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                return RedirectToAction("RegisterConfirmation");
            }

            foreach (var error in result.Errors)
            {
                Console.WriteLine($"Error: {error.Description}");
                ModelState.AddModelError("", error.Description);
            }

            return View();
        }

        // REGISTER CONFIRMATION (GET)
        public IActionResult RegisterConfirmation()
        {
            return View();
        }

        // CONFIRM EMAIL (GET)
        public async Task<IActionResult> ConfirmEmail(string userId, string? code, string? token)
        {
            if (string.IsNullOrWhiteSpace(userId) || (string.IsNullOrWhiteSpace(code) && string.IsNullOrWhiteSpace(token)))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            var decodedToken = token;
            if (string.IsNullOrWhiteSpace(decodedToken) && !string.IsNullOrWhiteSpace(code))
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            }

            var result = await _userManager.ConfirmEmailAsync(user, decodedToken!);
            if (!result.Succeeded)
            {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            return View();
        }

        // LOGIN (GET)
        public IActionResult Login()
        {
            return View();
        }

        // LOGIN (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            var result = await _signInManager.PasswordSignInAsync(
                email, password, false, false);

            if (result.Succeeded)
                return RedirectToAction("Index", "Dashboard");

            if (result.IsNotAllowed)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null && await _userManager.CheckPasswordAsync(user, password))
                {
                    if (!await _userManager.IsEmailConfirmedAsync(user))
                    {
                        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
                        var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = user.Id, code = code }, protocol: HttpContext.Request.Scheme);

                        if (string.IsNullOrWhiteSpace(callbackUrl))
                        {
                            return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
                        }

                        ViewData["EmailConfirmationLink"] = callbackUrl;
                        ViewData["EmailConfirmationAddress"] = email;

                        await _emailSender.SendEmailAsync(email, "Confirm your email",
                            $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

                        ModelState.AddModelError(string.Empty, "Your email is not confirmed. A new verification link has been sent to your email.");
                        return View();
                    }
                }
            }

            ModelState.AddModelError("", "Invalid login attempt");
            return View();
        }

        // LOGOUT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Login");
        }

        // PROFILE (GET)
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }
            return View(user);
        }

        // FORGOT PASSWORD (GET)
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // FORGOT PASSWORD (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError("", "Please enter your email.");
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            var callbackUrl = Url.Action("ResetPassword", "Account", new { code, email = user.Email }, protocol: HttpContext.Request.Scheme);

            if (string.IsNullOrWhiteSpace(callbackUrl))
            {
                return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
            }

            await _emailSender.SendEmailAsync(email, "Reset Password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            return RedirectToAction("ForgotPasswordConfirmation");
        }

        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        // RESET PASSWORD (GET)
        public IActionResult ResetPassword(string? code, string? token, string email)
        {
            var value = token ?? code;
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError("", "Invalid password reset token");
            }
            ViewData["Token"] = value;
            ViewData["Email"] = email;
            return View();
        }

        // RESET PASSWORD (POST)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string email, string token, string password, string confirmPassword)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match.");
                ViewData["Token"] = token;
                ViewData["Email"] = email;
                return View();
            }

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                return RedirectToAction("ResetPasswordConfirmation");
            }

            var decodedToken = token;
            try
            {
                decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(token));
            }
            catch
            {
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, password);
            if (result.Succeeded)
            {
                return RedirectToAction("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            
            ViewData["Token"] = token;
            ViewData["Email"] = email;
            return View();
        }

        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // CHANGE PASSWORD (GET)
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        // CHANGE PASSWORD (POST)
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "The new password and confirmation password do not match.");
                return View();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!changePasswordResult.Succeeded)
            {
                foreach (var error in changePasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View();
            }

            await _signInManager.RefreshSignInAsync(user);
            return RedirectToAction("Profile", new { Message = "Your password has been changed." });
        }
    }
}
