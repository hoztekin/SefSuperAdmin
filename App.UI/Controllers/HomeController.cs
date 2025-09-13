using App.UI.Models;
using App.UI.Services;
using App.UI.ViewModels;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace App.UI.Controllers;

public class HomeController(ILogger<HomeController> logger, IApiService apiService, ISessionService sessionService) : Controller
{


    public IActionResult Index()
    {
        // Dashboard için statik veriler (sonra API'den çekilecek)
        var dashboardModel = new DashboardViewModel
        {
            TotalUsers = 1247,
            TotalCompanies = 342,
            ActiveBranches = 156,
            IpOperations = 89,
            PendingApprovals = 17,
            OnlineUsers = 127,
            DailyOperations = 2456,
            SecurityScore = 60
        };

        return View(dashboardModel);
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
            // Düzeltilmiş validasyon
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

            // API bağlantısını test et
            //var connectionResult = await apiService.GetAsync<bool>($"api/v1/Machine/test-connection?apiAddress={Uri.EscapeDataString(machine.ApiAddress)}");

            //if (!connectionResult)
            //{
            //    logger.LogWarning("Makine API'sine bağlanılamadı: {ApiAddress}", machine.ApiAddress);
            //    return Json(new
            //    {
            //        success = false,
            //        message = $"Makine API'sine bağlanılamadı: {machine.ApiAddress}"
            //    });
            //}

            // Session'a seçili makineyi kaydet
            sessionService.SaveSelectedMachine(
                machine.Id,
                machine.BranchId,
                machine.BranchName,
                machine.ApiAddress);

            logger.LogInformation("Makine başarıyla seçildi: {MachineName} - {ApiAddress}",
                machine.BranchName, machine.ApiAddress);

            return Json(new
            {
                success = true,
                message = $"{machine.BranchName} makinesi başarıyla seçildi",
                machine = new
                {
                    id = machine.Id,
                    branchId = machine.BranchId,
                    branchName = machine.BranchName,
                    apiAddress = machine.ApiAddress
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Makine seçilirken hata oluştu");
            return Json(new { success = false, message = "Makine seçilirken beklenmeyen bir hata oluştu" });
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
