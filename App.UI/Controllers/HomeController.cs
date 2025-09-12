//using App.Repositories;
//using App.UI.Models;
//using App.UI.Services;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using Nest;
//using System.Diagnostics;

//namespace App.UI.Controllers;

//public class HomeController : Controller
//{
//    private readonly ILogger<HomeController> _logger;
//    private readonly AppDbContext _context;
//    private readonly ISessionService _sessionService;
//    private readonly IApiAuthService _apiAuthService;

//    public HomeController(ILogger<HomeController> logger, AppDbContext context,
//            ISessionService sessionService,
//            IApiAuthService apiAuthService)
//    {
//        _logger = logger;
//        _context = context;
//        _sessionService = sessionService;
//        _apiAuthService = apiAuthService;
//    }

//    public IActionResult Index()
//    {
//        // Dashboard için statik veriler (sonra API'den çekilecek)
//        var dashboardModel = new DashboardViewModel
//        {
//            TotalUsers = 1247,
//            TotalCompanies = 342,
//            ActiveBranches = 156,
//            IpOperations = 89,
//            PendingApprovals = 17,
//            OnlineUsers = 127,
//            DailyOperations = 2456,
//            SecurityScore = 60
//        };

//        return View(dashboardModel);
//    }

//    // Makineleri getir
//    [HttpGet]
//    public async Task<IActionResult> GetMachines()
//    {
//        //try
//        //{
//        //    var machines = await _context.Machines
//        //        .Where(m => !m.IsDeleted && m.IsActive)
//        //        .Select(m => new
//        //        {
//        //            id = m.Id.ToString(),
//        //            branchId = m.BranchId,
//        //            branchName = m.BranchName,
//        //            apiAddress = m.ApiAddress,
//        //            code = m.Code
//        //        })
//        //        .ToListAsync();

//        //    return Json(new { success = true, data = machines });
//        //}
//        //catch (Exception ex)
//        //{
//        //    _logger.LogError(ex, "Makineler yüklenirken hata oluştu");
//        //    return Json(new { success = false, message = "Makineler yüklenemedi" });
//        //}
//    }

//    // Seçili makineyi getir
//    [HttpGet]
//    public IActionResult GetSelectedMachine()
//    {
//        try
//        {
//            var selectedMachine = _sessionService.GetSelectedMachine();

//            if (selectedMachine != null)
//            {
//                return Json(new
//                {
//                    success = true,
//                    data = new
//                    {
//                        branchId = selectedMachine.BranchId,
//                        branchName = selectedMachine.BranchName,
//                        apiAddress = selectedMachine.ApiAddress
//                    }
//                });
//            }

//            return Json(new { success = false, message = "Seçili makine bulunamadı" });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Seçili makine bilgisi alınırken hata oluştu");
//            return Json(new { success = false, message = "Seçili makine bilgisi alınamadı" });
//        }
//    }

//    // Makine seç
//    [HttpPost]
//    public async Task<IActionResult> SelectMachine([FromBody] SelectMachineRequest request)
//    {
//        try
//        {
//            if (request == null || string.IsNullOrEmpty(request.MachineId))
//            {
//                return Json(new { success = false, message = "Geçersiz makine ID'si" });
//            }

//            // GUID parse et
//            if (!Guid.TryParse(request.MachineId, out var machineGuid))
//            {
//                return Json(new { success = false, message = "Geçersiz makine ID formatı" });
//            }

//            // Makineyi database'den bul
//            var machine = await _context.Machines
//                .FirstOrDefaultAsync(m => m.Id == machineGuid && !m.IsDeleted && m.IsActive);

//            if (machine == null)
//            {
//                return Json(new { success = false, message = "Makine bulunamadı" });
//            }

//            // Makine API'sine bağlanmayı dene
//            var loginResult = await _apiAuthService.LoginToMachineAsync(machine.ApiAddress);

//            if (!loginResult.IsSuccess)
//            {
//                _logger.LogWarning("Makine API'sine bağlanılamadı: {ApiAddress}, Hata: {Error}",
//                    machine.ApiAddress, loginResult.Message);

//                return Json(new
//                {
//                    success = false,
//                    message = $"Makine API'sine bağlanılamadı: {loginResult.Message}"
//                });
//            }

//            // Session'a seçili makineyi kaydet
//            _sessionService.SaveSelectedMachine(
//                machine.BranchId,
//                machine.BranchName,
//                machine.ApiAddress);

//            _logger.LogInformation("Makine başarıyla seçildi: {MachineName} - {ApiAddress}",
//                machine.BranchName, machine.ApiAddress);

//            return Json(new
//            {
//                success = true,
//                message = $"{machine.BranchName} makinesi başarıyla seçildi",
//                machine = new
//                {
//                    branchId = machine.BranchId,
//                    branchName = machine.BranchName,
//                    apiAddress = machine.ApiAddress
//                }
//            });
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "Makine seçilirken hata oluştu");
//            return Json(new { success = false, message = "Makine seçilirken beklenmeyen bir hata oluştu" });
//        }
//    }

//    // Ajax endpoint'leri (gelecekte API'den veri çekmek için)
//    [HttpGet]
//    public IActionResult GetDashboardStats()
//    {
//        var stats = new
//        {
//            totalUsers = 1247,
//            totalCompanies = 342,
//            activeBranches = 156,
//            ipOperations = 89
//        };

//        return Json(stats);
//    }

//    [HttpGet]
//    public IActionResult GetRecentActivities()
//    {
//        var activities = new[]
//        {
//                new {
//                    userName = "Ahmet Yılmaz",
//                    action = "Giriş Yaptı",
//                    timestamp = DateTime.Now.AddMinutes(-5),
//                    status = "Success",
//                    ipAddress = "192.168.1.100"
//                },
//                new {
//                    userName = "Mehmet Kaya",
//                    action = "Profil Güncelleme",
//                    timestamp = DateTime.Now.AddMinutes(-15),
//                    status = "Success",
//                    ipAddress = "192.168.1.101"
//                },
//                new {
//                    userName = "Ayşe Demir",
//                    action = "Şifre Değiştirme",
//                    timestamp = DateTime.Now.AddMinutes(-30),
//                    status = "Pending",
//                    ipAddress = "192.168.1.102"
//                },
//                new {
//                    userName = "Fatma Özkan",
//                    action = "Hesap Oluşturma",
//                    timestamp = DateTime.Now.AddHours(-1),
//                    status = "Success",
//                    ipAddress = "192.168.1.103"
//                }
//            };

//        return Json(activities);
//    }

//    public IActionResult Privacy()
//    {
//        return View();
//    }

//    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//    public IActionResult Error()
//    {
//        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//    }

//    public class DashboardViewModel
//    {
//        public int TotalUsers { get; set; }
//        public int TotalCompanies { get; set; }
//        public int ActiveBranches { get; set; }
//        public int IpOperations { get; set; }
//        public int PendingApprovals { get; set; }
//        public int OnlineUsers { get; set; }
//        public int DailyOperations { get; set; }
//        public int SecurityScore { get; set; }
//    }

//    // Request model for SelectMachine action
//    public class SelectMachineRequest
//    {
//        public string MachineId { get; set; }
//    }
//}
