using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MediCare.Web.Models;
using System.Threading.Tasks;

namespace MediCare.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
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

            // Create new IdentityUser instance using input data
            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                // Ensure the selected role (Patient or Doctor) exists in the database
                if (!await _roleManager.RoleExistsAsync(model.UserRole))
                {
                    await _roleManager.CreateAsync(new IdentityRole(model.UserRole));
                }

                // Assign the user to the specified role
                await _userManager.AddToRoleAsync(user, model.UserRole);

                // Automatically sign in the user after successful registration
                await _signInManager.SignInAsync(user, isPersistent: false);

                // Redirect user based on their assigned role
                if (model.UserRole == "Doctor")
                {
                    return RedirectToAction("Index", "Schedule"); // Doctor Dashboard
                }

                return RedirectToAction("Index", "Dashboard"); // Patient Dashboard
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

            if (user != null)
            {
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
                        return RedirectToAction("Index", "Admin");
                    }

                    return RedirectToAction("Index", "Dashboard");
                }
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
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
    }
}