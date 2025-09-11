using App.UI.DTOS;
using App.UI.Helper;
using App.UI.Models;
using App.UI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Areas.Admin.Controllers
{
    [Authorize]
    [Area("Admin")]

    public class RoleController(IRoleService roleService) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var result = await roleService.GetAllRolesAsync();
            return View(result);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(RoleDto roleRequest)
        {
            if (!ModelState.IsValid)
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    this.SetErrorMessage(error.ErrorMessage);
                }
                return View(roleRequest);
            }

            try
            {
                await roleService.CreateRoleAsync(roleRequest);
                this.SetSuccessMessage("Rol başarıyla oluşturuldu!");
            }
            catch (Exception ex)
            {
                this.SetErrorMessage($"Rol oluşturulurken hata: {ex.Message}");
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await roleService.DeleteRoleAsync(id);
                this.SetSuccessMessage("Rol başarıyla silindi!");
            }
            catch (Exception ex)
            {
                this.SetErrorMessage($"Rol silinirken hata: {ex.Message}");
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UserRoles(string userId)
        {
            try
            {
                var roles = await roleService.GetUserRolesAsync(userId);
                ViewBag.UserId = userId;
                return View(roles);
            }
            catch (Exception ex)
            {
                var errorViewModel = new ErrorViewModel
                {
                    Message = ex.Message,
                    RequestId = HttpContext.TraceIdentifier
                };
                return View("Error", errorViewModel);
            }
        }





        public async Task<IActionResult> RoleAssign(string userId)
        {
            TempData["userId"] = userId;
            ViewBag.UserId = userId;
            var user = await roleService.GetUserByIdAsync(userId);
            ViewBag.Username = user?.UserName ?? "Kullanıcı bulunamadı";
            var roles = await roleService.GetAllRolesAsync();
            var userRoles = await roleService.GetUserRolesAsync(userId);
            List<RoleAssignDtoUI> model = new List<RoleAssignDtoUI>();

            foreach (var role in roles)
            {
                bool exists = userRoles.Any(r => r.RoleId == role.RoleId && r.Exist);
                RoleAssignDtoUI roleAssign = new RoleAssignDtoUI
                {
                    RoleId = role.RoleId,
                    RoleName = role.RoleName,
                    Exist = exists,
                    UserId = userId
                };

                model.Add(roleAssign);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> RoleAssign(List<RoleAssignDtoUI> roles, string userId)
        {

            if (string.IsNullOrEmpty(userId) && TempData["userId"] != null)
            {
                userId = TempData["userId"].ToString();
            }

            try
            {
                var roleAssignments = roles.Select(r => new RoleAssignDtoUI
                {
                    RoleId = r.RoleId,
                    RoleName = r.RoleName,
                    Exist = r.Exist,
                    UserId = userId
                }).ToList();


                await roleService.AssignRolesToUserAsync(roleAssignments, userId);
                this.SetSuccessMessage("Kullanıcı rolleri başarıyla güncellendi!");
                return RedirectToAction("Index", "Member", new { area = "Admin" });
            }
            catch (Exception ex)
            {
                this.SetErrorMessage($"Roller güncellenirken hata: {ex.Message}");
                var errorViewModel = new ErrorViewModel
                {
                    Message = ex.Message,
                    RequestId = HttpContext.TraceIdentifier
                };
                return View("Error", errorViewModel);
            }
        }
    }
}
