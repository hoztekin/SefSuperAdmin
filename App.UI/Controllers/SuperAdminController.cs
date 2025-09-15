using App.UI.Application.DTOS;
using App.UI.Application.Services;
using App.UI.Helper;
using App.UI.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminController(IMemberService memberService, 
                                      IRoleService roleService, 
                                      IMachineAppService machineAppService, 
                                      ILogger<SuperAdminController> logger) : Controller
    {
        public async Task<IActionResult> Index()
        {
            try
            {
                var members = await memberService.GetAllMembersAsync();
                var machines = await machineAppService.GetAllAsync();
                var roles = await roleService.GetAllRolesAsync();

                var dashboardModel = new SuperAdminDashboardViewModel
                {
                    TotalUsers = members.Count(),
                    TotalMachines = machines.Count(),
                    TotalRoles = roles.Count,
                    ActiveMachines = machines.Count(m => m.IsActive),
                    RecentUsers = members.Take(5).ToList(),
                    RecentMachines = machines.Take(5).ToList()
                };

                return View(dashboardModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sistem admin dashboard yüklenirken hata oluştu");
                this.SetErrorMessage("Dashboard bilgileri yüklenirken bir hata oluştu.");
                return View(new SuperAdminDashboardViewModel());
            }
        }

        // Kullanıcı listesi
        [HttpGet]
        public async Task<IActionResult> Users()
        {
            try
            {
                var users = await memberService.GetAllMembersAsync();
                return View(users.ToList());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kullanıcı listesi yüklenirken hata oluştu");
                this.SetErrorMessage("Kullanıcı listesi yüklenirken bir hata oluştu.");
                return View(new List<UserAppDtoUI>());
            }
        }

        // Kullanıcıya rol atama sayfası
        [HttpGet]
        public async Task<IActionResult> AssignRoles(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                this.SetErrorMessage("Geçersiz kullanıcı ID'si");
                return RedirectToAction(nameof(Users));
            }

            try
            {
                var user = await roleService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    this.SetErrorMessage("Kullanıcı bulunamadı");
                    return RedirectToAction(nameof(Users));
                }

                var userRoles = await roleService.GetUserRolesAsync(userId);
                var allRoles = await roleService.GetAllRolesAsync();

                var viewModel = new UserRoleAssignViewModel
                {
                    User = user,
                    UserRoles = userRoles,
                    AllRoles = allRoles
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rol atama sayfası yüklenirken hata oluştu. UserId: {UserId}", userId);
                this.SetErrorMessage("Rol bilgileri yüklenirken bir hata oluştu.");
                return RedirectToAction(nameof(Users));
            }
        }

        // Kullanıcıya rol atama işlemi
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignRoles(string userId, List<RoleAssignDtoUI> roles)
        {
            if (string.IsNullOrEmpty(userId) || roles == null)
            {
                this.SetErrorMessage("Geçersiz veri gönderildi");
                return RedirectToAction(nameof(Users));
            }

            try
            {
                await roleService.AssignRolesToUserAsync(roles, userId);
                this.SetSuccessMessage("Roller başarıyla atandı");
                return RedirectToAction(nameof(Users));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rol atama işlemi sırasında hata oluştu. UserId: {UserId}", userId);
                this.SetErrorMessage("Rol atama işlemi sırasında bir hata oluştu.");
                return RedirectToAction(nameof(AssignRoles), new { userId });
            }
        }

        // AJAX - Kullanıcı rollerini getir
        [HttpGet]
        public async Task<IActionResult> GetUserRoles(string userId)
        {
            try
            {
                var userRoles = await roleService.GetUserRolesAsync(userId);
                return Json(new { success = true, data = userRoles });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kullanıcı rolleri getirilirken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "Kullanıcı rolleri yüklenemedi" });
            }
        }



        #region Makine Yönetimi Linkleri

        // Makine yönetimi ana sayfasına yönlendir
        public IActionResult Machines()
        {
            return RedirectToAction("Index", "Machine");
        }

        // Yeni makine ekleme sayfasına yönlendir
        public IActionResult CreateMachine()
        {
            return RedirectToAction("Create", "Machine");
        }

        // Makine düzenleme sayfasına yönlendir
        public IActionResult EditMachine(int id)
        {
            return RedirectToAction("Edit", "Machine", new { id });
        }

        #endregion

        #region Sistem Bilgileri

        // Sistem istatistikleri (AJAX)
        [HttpGet]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                var users = await memberService.GetAllMembersAsync();
                var machines = await machineAppService.GetAllAsync();
                var roles = await roleService.GetAllRolesAsync();

                var stats = new
                {
                    totalUsers = users.Count(),
                    totalMachines = machines.Count(),
                    activeMachines = machines.Count(m => m.IsActive),
                    inactiveMachines = machines.Count(m => !m.IsActive),
                    totalRoles = roles.Count,
                    adminUsers = users.Count(u => u.Roles != null && (u.Roles.Contains("Admin") || u.Roles.Contains("SuperAdmin")))
                };

                return Json(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sistem istatistikleri getirilirken hata oluştu");
                return Json(new { success = false, message = "İstatistikler yüklenemedi" });
            }
        }

        #endregion
    }
}
