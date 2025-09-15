using App.UI.Application.Services;
using App.UI.Helper;
using App.UI.Presentation.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    [Authorize]
    public class MachineController(IMachineAppService machineAppService) : Controller
    {
        // Makine listesi
        public async Task<IActionResult> Index()
        {
            var machines = await machineAppService.GetAllAsync();
            return View(machines);
        }

        // Yeni makine oluşturma formu
        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateMachineViewModel());
        }

        // Yeni makine oluşturma
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateMachineViewModel model)
        {
            var machines = await machineAppService.GetAllAsync();
            return View(machines);
        }

        // Makine düzenleme formu
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var machine = await machineAppService.GetByIdAsync(id);

            if (machine == null)
            {
                this.SetErrorMessage("Makine bulunamadı");
                return RedirectToAction(nameof(Index));
            }

            var model = new UpdateMachineViewModel
            {
                Id = machine.Id,
                BranchId = machine.BranchId,
                BranchName = machine.BranchName,
                ApiAddress = machine.ApiAddress,
                Code = machine.Code
            };

            return View(model);
        }

        // Makine düzenleme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(UpdateMachineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var success = await machineAppService.UpdateAsync(model);

            if (success)
            {
                this.SetSuccessMessage("Makine başarıyla güncellendi");
                return RedirectToAction(nameof(Index));
            }

            this.SetErrorMessage("Makine güncellenemedi");
            return View(model);
        }

        // Makine detayı
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var machine = await machineAppService.GetByIdAsync(id);

            if (machine == null)
            {
                this.SetErrorMessage("Makine bulunamadı");
                return RedirectToAction(nameof(Index));
            }

            return View(machine);
        }

        // Makine silme
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await machineAppService.DeleteAsync(id);

            if (success)
            {
                this.SetSuccessMessage("Makine başarıyla silindi");
            }
            else
            {
                this.SetErrorMessage("Makine silinemedi");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}