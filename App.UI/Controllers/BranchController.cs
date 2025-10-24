using App.UI.Application.DTOS;
using App.UI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    public class BranchController(IBranchService branchService, ILogger<BranchController> logger) : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var result = await branchService.GetListAsync();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await branchService.GetByIdAsync(id);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BranchCreateDto createDto)
        {
            var result = await branchService.CreateAsync(createDto);
            return Json(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] BranchUpdateDto updateDto)
        {
            var result = await branchService.UpdateAsync(updateDto);
            return Json(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteDto deleteDto)
        {
            var result = await branchService.DeleteAsync(deleteDto);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts()
        {
            var result = await branchService.GetDistrictsAsync();
            return Json(result);
        }
    }
}
