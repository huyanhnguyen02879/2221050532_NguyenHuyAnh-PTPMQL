using BTH7.Models;
using BTH7.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net.Mail;

namespace BTH7.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly IEmailSender _emailSender;

    public AccountController(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailSender = emailSender;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        var result = user?.UserName is null
            ? Microsoft.AspNetCore.Identity.SignInResult.Failed
            : await _signInManager.PasswordSignInAsync(
                user.UserName,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

        if (result.Succeeded)
        {
            return RedirectToLocal(model.ReturnUrl);
        }

        ModelState.AddModelError(
            string.Empty,
            result.IsLockedOut
                ? "Tài khoản đang bị khóa. Vui lòng thử lại sau."
                : "Email hoặc mật khẩu không đúng, hoặc email chưa được xác nhận.");
        return View(model);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingUser = await _userManager.FindByEmailAsync(model.Email);
        if (existingUser is not null)
        {
            ModelState.AddModelError(string.Empty, "Email này đã được đăng ký.");
            return View(model);
        }

        var user = new IdentityUser
        {
            UserName = model.Email,
            Email = model.Email
        };
        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        try
        {
            await SendEmailConfirmationAsync(user);
        }
        catch (SmtpException)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(
                string.Empty,
                "Không thể gửi email xác nhận. Vui lòng kiểm tra lại cấu hình SMTP hoặc App Password.");
            return View(model);
        }
        catch (InvalidOperationException exception)
        {
            await _userManager.DeleteAsync(user);
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
        return View("RegisterConfirmation");
    }

    [AllowAnonymous]
    public IActionResult RegisterConfirmation()
    {
        return View();
    }

    [AllowAnonymous]
    public IActionResult ResendConfirmation()
    {
        return View(new EmailViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResendConfirmation(EmailViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is not null && !user.EmailConfirmed)
            {
                try
                {
                    await SendEmailConfirmationAsync(user);
                }
                catch (SmtpException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Không thể gửi email xác nhận. Vui lòng thử lại sau.");
                    return View(model);
                }
                catch (InvalidOperationException exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                    return View(model);
                }
            }

            return View("ResendConfirmationConfirmation");
        }

        return View(model);
    }

    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(token))
        {
            return View("ConfirmEmail", false);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return View("ConfirmEmail", false);
        }

        var result = await _userManager.ConfirmEmailAsync(user, token);
        return View("ConfirmEmail", result.Succeeded);
    }

    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View(new EmailViewModel());
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(EmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null)
        {
            if (!user.EmailConfirmed)
            {
                try
                {
                    await SendEmailConfirmationAsync(user);
                }
                catch (SmtpException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Không thể gửi email xác nhận. Vui lòng thử lại sau.");
                    return View(model);
                }
                catch (InvalidOperationException exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                    return View(model);
                }
            }
            else
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var callbackUrl = Url.ActionLink(
                    nameof(ResetPassword),
                    "Account",
                    new { email = user.Email, token });
                if (callbackUrl is null)
                {
                    throw new InvalidOperationException("Could not generate the password reset URL.");
                }

                try
                {
                    await _emailSender.SendEmailAsync(
                        user.Email!,
                        "Đặt lại mật khẩu",
                        $"<p>Để đặt lại mật khẩu, <a href=\"{callbackUrl}\">bấm vào đây</a>.</p>");
                }
                catch (SmtpException)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Không thể gửi email đặt lại mật khẩu. Vui lòng thử lại sau.");
                    return View(model);
                }
                catch (InvalidOperationException exception)
                {
                    ModelState.AddModelError(string.Empty, exception.Message);
                    return View(model);
                }
            }
        }

        return View("ForgotPasswordConfirmation");
    }

    [AllowAnonymous]
    public IActionResult ResetPassword(string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return View("ResetPasswordInvalid");
        }

        return View(new ResetPasswordViewModel { Email = email, Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is null)
        {
            return View("ResetPasswordConfirmation", false);
        }

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        return View("ResetPasswordConfirmation", true);
    }

    [Authorize]
    public IActionResult ChangePassword()
    {
        return View(new ChangePasswordViewModel());
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            model.CurrentPassword,
            model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }

        await _signInManager.RefreshSignInAsync(user);
        return View("ChangePasswordConfirmation");
    }

    [Authorize]
    public async Task<IActionResult> ChangeEmail()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        return View(new ChangeEmailViewModel { NewEmail = user.Email ?? string.Empty });
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeEmail(ChangeEmailViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.GetUserAsync(User);
        if (user is null)
        {
            return Challenge();
        }

        if (!await _userManager.CheckPasswordAsync(user, model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không đúng.");
            return View(model);
        }

        var token = await _userManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
        var callbackUrl = Url.ActionLink(
            nameof(ConfirmEmailChange),
            "Account",
            new { userId = user.Id, email = model.NewEmail, token });
        if (callbackUrl is null)
        {
            throw new InvalidOperationException("Could not generate the email confirmation URL.");
        }

        try
        {
            await _emailSender.SendEmailAsync(
                model.NewEmail,
                "Xác nhận email mới",
                $"<p>Để xác nhận email mới, <a href=\"{callbackUrl}\">bấm vào đây</a>.</p>");
        }
        catch (SmtpException)
        {
            ModelState.AddModelError(
                string.Empty,
                "Không thể gửi email xác nhận thay đổi địa chỉ. Vui lòng thử lại sau.");
            return View(model);
        }
        catch (InvalidOperationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
        return View("ChangeEmailConfirmation");
    }

    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmailChange(string? userId, string? email, string? token)
    {
        if (string.IsNullOrWhiteSpace(userId) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(token))
        {
            return View(false);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            return View(false);
        }

        var result = await _userManager.ChangeEmailAsync(user, email, token);
        if (result.Succeeded)
        {
            await _signInManager.RefreshSignInAsync(user);
        }

        return View(result.Succeeded);
    }

    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        return View();
    }

    private async Task SendEmailConfirmationAsync(IdentityUser user)
    {
        var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        var callbackUrl = Url.ActionLink(
            nameof(ConfirmEmail),
            "Account",
            new { userId = user.Id, token });
        if (callbackUrl is null || user.Email is null)
        {
            throw new InvalidOperationException("Could not generate the email confirmation URL.");
        }

        await _emailSender.SendEmailAsync(
            user.Email,
            "Xác nhận tài khoản",
            $"<p>Để xác nhận tài khoản, <a href=\"{callbackUrl}\">bấm vào đây</a>.</p>");
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        return !string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl)
            ? LocalRedirect(returnUrl)
            : RedirectToAction("Index", "Home");
    }
}
