using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SeniorProject.Data;
using SeniorProject.Models;
using System.Security.Claims;

namespace SeniorProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _db;

        public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager,
            ApplicationDbContext db)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            var userRolesList = new List<UserRolesViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userRolesList.Add(new UserRolesViewModel
                {
                    UserId = user.Id,
                    Email = user.Email,
                    Roles = roles
                });
            }

            return View(userRolesList);
        }

        [HttpGet]
        public async Task<IActionResult> ManageRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var allRoles = await _roleManager.Roles.ToListAsync();
            var userRoles = await _userManager.GetRolesAsync(user);

            var model = allRoles.Select(role => new ManageUserRolesViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name,
                Selected = userRoles.Contains(role.Name)
            }).ToList();

            ViewBag.UserId = userId;
            ViewBag.UserEmail = user.Email;
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ManageRoles(string userId, List<ManageUserRolesViewModel> model)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            var selectedRoles = model.Where(r => r.Selected).Select(r => r.RoleName);
            await _userManager.AddToRolesAsync(user, selectedRoles);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Index");
            }
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            await _userManager.DeleteAsync(user);
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Dashboard()
        {
            var totalUsers = await _userManager.Users.CountAsync();
            var totalBaskets = await _db.SavedBaskets.CountAsync();
            var totalProducts = await _db.ImportedProducts.CountAsync();
            var lastUpload = await _db.ImportedProducts.MaxAsync(p => (DateTime?)p.ImportDate);

            var mostPopularTown = await _db.SavedBaskets
                .GroupBy(b => b.WinningSupermarket)
                .OrderByDescending(g => g.Count())
                .Select(g => new { Store = g.Key, Count = g.Count() })
                .FirstOrDefaultAsync();

            ViewBag.TotalUsers = totalUsers;
            ViewBag.TotalBaskets = totalBaskets;
            ViewBag.TotalProducts = totalProducts;
            ViewBag.LastUpload = lastUpload?.ToString("MMM dd, yyyy HH:mm") ?? "No data yet";
            ViewBag.MostPopularStore = mostPopularTown?.Store ?? "None yet";
            ViewBag.MostPopularStoreCount = mostPopularTown?.Count ?? 0;

            return View();
        }
    }
}