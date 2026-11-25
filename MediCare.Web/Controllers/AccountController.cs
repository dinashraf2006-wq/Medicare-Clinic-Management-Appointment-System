using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MediCare.Web.Models;
using MediCare.Data.Entities;
using System.Threading.Tasks;

namespace MediCare.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // ==================== GET: Register ====================
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // ==================== POST: Register ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Check if this is an admin creation (from AdminInvitationController)
            var createAsAdmin = TempData["CreateAsAdmin"] as bool? == true;
            var requestedRole = model.UserRole;

            if (createAsAdmin)
            {
                requestedRole = "Admin";
                // Pre-fill from TempData if available
                if (string.IsNullOrEmpty(model.Email))
                    model.Email = TempData["AdminEmail"] as string;
                if (string.IsNullOrEmpty(model.FullName))
                    model.FullName = TempData["AdminFullName"] as string;
                if (string.IsNullOrEmpty(model.PhoneNumber))
                    model.PhoneNumber = TempData["AdminPhone"] as string;
                if (string.IsNullOrEmpty(model.Password))
                    model.Password = TempData["AdminPassword"] as string;
            }

            // Create new ApplicationUser instance using input data
            var user = new ApplicationUser
            {
                UserName = model.Email.Split('@')[0], // Use email prefix as username
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                FullName = model.FullName ?? model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure the selected role exists in the database
                if (!await _roleManager.RoleExistsAsync(requestedRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(requestedRole));
                }

                // Assign the user to the specified role
                await _userManager.AddToRoleAsync(user, requestedRole);

                // Automatically sign in the user after successful registration
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Redirect user based on their assigned role
                if (requestedRole == "Doctor")
                {
                    return RedirectToAction("Index", "Schedule"); // Doctor Dashboard
                }
                else if (requestedRole == "Admin")
                {
                    return RedirectToAction("Dashboard", "Admin"); // Admin Dashboard
                }

                return RedirectToAction("Index", "Home"); // Default to home
            }

            // Add errors to ModelState if registration fails
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        // ==================== GET: Login ====================
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // ==================== POST: Login ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Find user by email address
            var user = await _userManager.FindByEmailAsync(model.EmailOrPhone);
            if (user == null)
            {
                // Fallback to find user by username if email lookup fails
                user = await _userManager.FindByNameAsync(model.EmailOrPhone);
            }

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found. Please check your email/phone.");
                return View(model);
            }

            // Attempt to sign in using password and remember me option
            var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: false);

            if (result.Succeeded)
            {
                // Redirect user according to their role permissions
                if (await _userManager.IsInRoleAsync(user, "Doctor"))
                {
                    return RedirectToAction("Index", "Schedule");
                }
                else if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    return RedirectToAction("Dashboard", "Admin");
                }

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Account is locked out.");
            }
            else if (result.IsNotAllowed)
            {
                ModelState.AddModelError(string.Empty, "Account is not allowed to sign in. Please confirm your email.");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid email or password.");
            }

            return View(model);
        }

        // ==================== POST: Logout ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // ==================== GET: LogoutConfirmation ====================
        [HttpGet]
        public IActionResult LogoutConfirmation()
        {
            return RedirectToAction("Index", "Home");
        }
    }
}