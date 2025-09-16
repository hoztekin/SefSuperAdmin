using App.Services.Users.Create;
using App.UI.Application.DTOS;
using App.UI.Application.Services;
using App.UI.Helper;
using App.UI.Infrastructure.Http;
using App.UI.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    [Authorize(Policy = "SuperAdminOnly")]
    public class SuperAdminController(IMemberService memberService,
                                      IRoleService roleService,
                                      IApiService apiService,
                                      IMachineAppService machineAppService,
                                      ILogger<SuperAdminController> logger) : Controller
    {
        public async Task<IActionResult> Index()
        {
            try
            {
                var members = await memberService.GetAllMembersAsync();
                var machines = await machineAppService.GetAllAsync();
                var rolesResult = await roleService.GetAllRolesAsync();

                var roles = rolesResult?.Data ?? new List<RoleDto>();

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
                var userResult = await roleService.GetUserByIdAsync(userId);
                if (userResult == null || !userResult.IsSuccess || userResult.Data == null)
                {
                    this.SetErrorMessage("Kullanıcı bulunamadı");
                    return RedirectToAction(nameof(Users));
                }

                var userRolesResult = await roleService.GetUserRolesAsync(userId);
                var allRolesResult = await roleService.GetAllRolesAsync();

                var userRoles = userRolesResult?.Data ?? new List<RoleAssignDtoUI>();
                var allRoles = allRolesResult?.Data ?? new List<RoleDto>();

                var viewModel = new UserRoleAssignViewModel
                {
                    User = userResult.Data,
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
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Geçersiz veri gönderildi" });
                }
                this.SetErrorMessage("Geçersiz veri gönderildi");
                return RedirectToAction(nameof(Users));
            }

            try
            {
                var result = await roleService.AssignRolesToUserAsync(roles, userId);

                if (result != null && result.IsSuccess)
                {
                    // AJAX isteği ise JSON response döndür
                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new
                        {
                            success = true,
                            message = "Roller başarıyla atandı",
                            redirectTo = Url.Action("Users", "SuperAdmin")
                        });
                    }

                    this.SetSuccessMessage("Roller başarıyla atandı");
                    return RedirectToAction(nameof(Users));
                }
                else
                {
                    var errorMessage = result?.Message ?? "Rol atama başarısız";

                    if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    {
                        return Json(new { success = false, message = errorMessage });
                    }

                    this.SetErrorMessage(errorMessage);
                    return RedirectToAction(nameof(AssignRoles), new { userId });
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Rol atama işlemi sırasında hata oluştu. UserId: {UserId}", userId);

                // AJAX isteği ise JSON response döndür
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new
                    {
                        success = false,
                        message = "Rol atama işlemi sırasında bir hata oluştu: " + ex.Message
                    });
                }

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

        // AJAX - Kullanıcı detaylarını getir
        [HttpGet]
        public async Task<IActionResult> GetUserById(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı ID'si" });
            }

            try
            {
                var user = await roleService.GetUserByIdAsync(userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Kullanıcı bulunamadı" });
                }

                return Json(new { success = true, data = user });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kullanıcı detayları getirilirken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "Kullanıcı detayları yüklenemedi" });
            }
        }

        // AJAX - Yeni kullanıcı oluştur
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto createUserDto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Validasyon hatası", errors = errors });
            }

            try
            {
                // API'ye kullanıcı oluşturma isteği gönder
                var response = await apiService.PostAsync<CreateUserDto>("api/v1/user", createUserDto, false);

                if (response != null)
                {
                    logger.LogInformation("Yeni kullanıcı oluşturuldu: {UserName}", createUserDto.UserName);
                    return Json(new { success = true, message = "Kullanıcı başarıyla oluşturuldu" });
                }

                return Json(new { success = false, message = "Kullanıcı oluşturulamadı" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kullanıcı oluşturulurken hata oluştu. UserName: {UserName}", createUserDto.UserName);
                return Json(new { success = false, message = "Kullanıcı oluşturulurken bir hata oluştu: " + ex.Message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser(string userId, [FromBody] UpdateUserDtoUI updateUserDto)
        {
            if (string.IsNullOrEmpty(userId) || updateUserDto == null)
            {
                return Json(new { success = false, message = "Geçersiz veri gönderildi" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
                return Json(new { success = false, message = "Validasyon hatası", errors = errors });
            }

            try
            {
                var response = await apiService.PutAsync<object>($"api/v1/user/{userId}", updateUserDto);

                if (response != null)
                {
                    logger.LogInformation("Kullanıcı güncellendi: UserId: {UserId}", userId);
                    return Json(new { success = true, message = "Kullanıcı başarıyla güncellendi" });
                }

                return Json(new { success = false, message = "Kullanıcı güncellenemedi" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kullanıcı güncellenirken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "Kullanıcı güncellenirken bir hata oluştu: " + ex.Message });
            }
        }

        // AJAX - Kullanıcı silme
        [HttpDelete]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Geçersiz kullanıcı ID'si" });
            }

            try
            {
                var response = await apiService.DeleteAsync<object>($"api/v1/user/{userId}", null);

                logger.LogInformation("Kullanıcı silindi: UserId: {UserId}", userId);
                return Json(new { success = true, message = "Kullanıcı başarıyla silindi" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kullanıcı silinirken hata oluştu. UserId: {UserId}", userId);
                return Json(new { success = false, message = "Kullanıcı silinirken bir hata oluştu: " + ex.Message });
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
                var rolesResult = await roleService.GetAllRolesAsync();

                var roles = rolesResult?.Data ?? new List<RoleDto>();

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
