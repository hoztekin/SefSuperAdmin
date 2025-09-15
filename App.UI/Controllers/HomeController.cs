using App.UI.Infrastructure.ExternalApi;
using App.UI.Infrastructure.Http;
using App.UI.Infrastructure.Storage;
using App.UI.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers;

[Authorize]
public class HomeController(ILogger<HomeController> logger, IApiService apiService, ISessionService sessionService, IExternalApiService externalApiService) : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Normal kullanıcılar için dashboard
    private IActionResult UserDashboard()
    {
        try
        {
            var selectedMachine = sessionService.GetSelectedMachine();

            var dashboardModel = new UserDashboardViewModel
            {
                SelectedMachine = selectedMachine,
                HasSelectedMachine = selectedMachine != null,
                UserName = User.Identity.Name,
                LastHealthCheck = selectedMachine?.LastHealthCheck
            };

            return View("UserDashboard", dashboardModel);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "User dashboard yüklenirken hata oluştu");
            return View("UserDashboard", new UserDashboardViewModel { UserName = User.Identity.Name });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ApiOperations()
    {
        var selectedMachine = sessionService.GetSelectedMachine();

        if (selectedMachine == null)
        {
            TempData["WarningMessage"] = "Önce bir makine seçmeniz gerekiyor.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            // Seçili makinenin health check'ini yap
            var healthResponse = await externalApiService.CheckHealthAsync(selectedMachine.ApiAddress);

            var model = new ApiOperationsViewModel
            {
                SelectedMachine = selectedMachine,
                HealthStatus = healthResponse,
                AvailableEndpoints = GetAvailableEndpoints() // API'nin sunduğu endpoint'ler
            };

            return View(model);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API operations sayfası yüklenirken hata oluştu");
            TempData["ErrorMessage"] = "API bilgileri yüklenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }


    }

    // External API'nin sunduğu endpoint'leri döndür
    private List<ApiEndpoint> GetAvailableEndpoints()
    {
        return new List<ApiEndpoint>
            {
                new ApiEndpoint { Name = "Kullanıcılar", Path = "/api/v1/users", Method = "GET", Description = "Tüm kullanıcıları listele" },
                new ApiEndpoint { Name = "Yeni Kullanıcı", Path = "/api/v1/users", Method = "POST", Description = "Yeni kullanıcı oluştur" },
                new ApiEndpoint { Name = "Sistem Bilgisi", Path = "/api/v1/system/info", Method = "GET", Description = "Sistem bilgilerini getir" },
                new ApiEndpoint { Name = "Health Check", Path = "/health", Method = "GET", Description = "Sistem sağlık durumu" }
            };
    }

    // Aktif makineleri getir (Modal için)
    [HttpGet]
    public async Task<IActionResult> GetMachines()
    {
        try
        {
            var machines = await apiService.GetAsync<List<MachineListViewModel>>("api/v1/Machine/active");

            if (machines != null && machines.Any())
            {
                return Json(new { success = true, data = machines });
            }

            return Json(new { success = false, message = "Aktif makine bulunamadı" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Makineler yüklenirken hata oluştu");
            return Json(new { success = false, message = "Makineler yüklenemedi" });
        }
    }

    // Seçili makineyi getir
    [HttpGet]
    public IActionResult GetSelectedMachine()
    {
        try
        {
            var selectedMachine = sessionService.GetSelectedMachine();

            if (selectedMachine != null)
            {
                return Json(new
                {
                    success = true,
                    data = new
                    {
                        id = selectedMachine.MachineId,
                        branchId = selectedMachine.BranchId,
                        branchName = selectedMachine.BranchName,
                        apiAddress = selectedMachine.ApiAddress
                    }
                });
            }

            return Json(new { success = false, message = "Seçili makine bulunamadı" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seçili makine bilgisi alınırken hata oluştu");
            return Json(new { success = false, message = "Seçili makine bilgisi alınamadı" });
        }
    }

    // Makine seç
    [HttpPost]
    public async Task<IActionResult> SelectMachine([FromBody] SelectMachineRequest request)
    {
        try
        {
            // Validasyon
            if (request == null || string.IsNullOrEmpty(request.MachineId))
            {
                return Json(new { success = false, message = "Geçersiz makine ID'si" });
            }

            // String'den int'e çevir
            if (!int.TryParse(request.MachineId, out int machineId))
            {
                return Json(new { success = false, message = "Geçersiz makine ID formatı" });
            }

            // API'den makine bilgisini al
            var machine = await apiService.GetAsync<MachineViewModel>($"api/v1/Machine/{machineId}");

            if (machine == null)
            {
                return Json(new { success = false, message = "Makine bulunamadı" });
            }

            logger.LogInformation("Makine seçildi: {MachineName} - API: {ApiAddress}",
                machine.BranchName, machine.ApiAddress);

            // 1. Uzaktaki API'nin health check'ini yap
            var healthResponse = await externalApiService.CheckHealthAsync(machine.ApiAddress);

            if (!healthResponse.IsHealthy)
            {
                logger.LogWarning("Makine API'si sağlıksız: {ApiAddress} - {Message}",
                    machine.ApiAddress, healthResponse.Message);

                return Json(new
                {
                    success = false,
                    message = $"Makine API'sine bağlanılamıyor: {healthResponse.Message}",
                    healthCheck = new
                    {
                        healthy = false,
                        message = healthResponse.Message,
                        responseTime = healthResponse.ResponseTime
                    }
                });
            }

            logger.LogInformation("Health check başarılı: {ApiAddress} - {ResponseTime}ms",
                machine.ApiAddress, healthResponse.ResponseTime);

            // 2. Uzaktaki API'ye login ol ve token al
            var loginResponse = await externalApiService.LoginAsync(machine.ApiAddress, "SystemAdmin", "1234");

            if (!loginResponse.Success || string.IsNullOrEmpty(loginResponse.AccessToken))
            {
                logger.LogError("Uzaktaki API'ye login başarısız: {ApiAddress} - {Message}",
                    machine.ApiAddress, loginResponse.Message);

                return Json(new
                {
                    success = false,
                    message = $"Makine API'sine giriş yapılamadı: {loginResponse.Message}",
                    healthCheck = new
                    {
                        healthy = true,
                        responseTime = healthResponse.ResponseTime
                    },
                    login = new
                    {
                        success = false,
                        message = loginResponse.Message
                    }
                });
            }

            logger.LogInformation("Login başarılı: {ApiAddress} - Token alındı", machine.ApiAddress);

            // 3. Token'ı session'da sakla (YENİ METHOD)
            var expiresAt = loginResponse.ExpiresAt != default
                ? loginResponse.ExpiresAt
                : DateTime.UtcNow.AddHours(1);

            sessionService.SaveMachineApiToken(
                machine.ApiAddress,
                loginResponse.AccessToken,
                expiresAt,
                loginResponse.RefreshToken);

            // 4. Session'a seçili makineyi kaydet
            sessionService.SaveSelectedMachine(
                machine.Id,
                machine.BranchId,
                machine.BranchName,
                machine.ApiAddress);

            logger.LogInformation("Makine başarıyla seçildi ve API'ye bağlanıldı: {MachineName} - {ApiAddress}",
                machine.BranchName, machine.ApiAddress);

            return Json(new
            {
                success = true,
                message = $"{machine.BranchName} makinesi başarıyla seçildi ve API'ye bağlanıldı",
                machine = new
                {
                    id = machine.Id,
                    branchId = machine.BranchId,
                    branchName = machine.BranchName,
                    apiAddress = machine.ApiAddress
                },
                healthCheck = new
                {
                    healthy = true,
                    message = healthResponse.Message,
                    responseTime = healthResponse.ResponseTime
                },
                login = new
                {
                    success = true,
                    message = "Başarıyla giriş yapıldı",
                    tokenExpires = expiresAt
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Makine seçilirken hata oluştu: {MachineId}", request?.MachineId);
            return Json(new
            {
                success = false,
                message = "Makine seçilirken beklenmeyen bir hata oluştu"
            });
        }
    }

    // Seçili makinenin durumunu kontrol et - Yeni metod
    [HttpGet]
    public IActionResult CheckSelectedMachineStatus()
    {
        try
        {
            var selectedMachine = sessionService.GetSelectedMachine();

            if (selectedMachine == null)
            {
                return Json(new { success = false, message = "Seçili makine bulunamadı" });
            }

            var hasValidToken = sessionService.HasValidMachineToken();
            var machineToken = sessionService.GetMachineApiToken();

            return Json(new
            {
                success = true,
                data = new
                {
                    machine = new
                    {
                        id = selectedMachine.MachineId,
                        branchId = selectedMachine.BranchId,
                        branchName = selectedMachine.BranchName,
                        apiAddress = selectedMachine.ApiAddress
                    },
                    token = new
                    {
                        hasValidToken = hasValidToken,
                        hasToken = !string.IsNullOrEmpty(machineToken),
                        // Token expiry bilgisini ayrıca almak istersen GetMachineApiTokenInfo gibi bir method ekleyebiliriz
                    }
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Seçili makine durumu kontrol edilirken hata oluştu");
            return Json(new { success = false, message = "Makine durumu kontrol edilemedi" });
        }
    }

    // Token yenileme - Yeni metod
    [HttpPost]
    public async Task<IActionResult> RefreshExternalApiToken()
    {
        try
        {
            var selectedMachine = sessionService.GetSelectedMachine();

            if (selectedMachine == null)
            {
                return Json(new { success = false, message = "Seçili makine bulunamadı" });
            }

            // Yeniden login ol
            var loginResponse = await externalApiService.LoginAsync(selectedMachine.ApiAddress, "SystemAdmin", "1234");

            if (!loginResponse.Success || string.IsNullOrEmpty(loginResponse.AccessToken))
            {
                return Json(new
                {
                    success = false,
                    message = $"Token yenilenemedi: {loginResponse.Message}"
                });
            }

            var expiresAt = loginResponse.ExpiresAt != default
                ? loginResponse.ExpiresAt
                : DateTime.UtcNow.AddHours(1);

            sessionService.SaveMachineApiToken(
                selectedMachine.ApiAddress,
                loginResponse.AccessToken,
                expiresAt,
                loginResponse.RefreshToken);

            logger.LogInformation("Token yenilendi: {ApiAddress}", selectedMachine.ApiAddress);

            return Json(new
            {
                success = true,
                message = "Token başarıyla yenilendi",
                tokenExpires = expiresAt
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token yenilenirken hata oluştu");
            return Json(new { success = false, message = "Token yenilenemedi" });
        }
    }


    // Ajax endpoint'leri (gelecekte API'den veri çekmek için)
    [HttpGet]
    public IActionResult GetDashboardStats()
    {
        var stats = new
        {
            totalUsers = 1247,
            totalCompanies = 342,
            activeBranches = 156,
            ipOperations = 89
        };

        return Json(stats);
    }

    [HttpGet]
    public IActionResult GetRecentActivities()
    {
        var activities = new[]
        {
            new {
                userName = "Ahmet Yılmaz",
                action = "Giriş Yaptı",
                timestamp = DateTime.Now.AddMinutes(-5),
                status = "Success",
                ipAddress = "192.168.1.100"
            },
            new {
                userName = "Mehmet Kaya",
                action = "Profil Güncelleme",
                timestamp = DateTime.Now.AddMinutes(-15),
                status = "Success",
                ipAddress = "192.168.1.101"
            },
            new {
                userName = "Ayşe Demir",
                action = "Şifre Değiştirme",
                timestamp = DateTime.Now.AddMinutes(-30),
                status = "Pending",
                ipAddress = "192.168.1.102"
            },
            new {
                userName = "Fatma Özkan",
                action = "Hesap Oluşturma",
                timestamp = DateTime.Now.AddHours(-1),
                status = "Success",
                ipAddress = "192.168.1.103"
            }
        };

        return Json(activities);
    }

    public IActionResult Privacy()
    {
        return View();
    }

}
