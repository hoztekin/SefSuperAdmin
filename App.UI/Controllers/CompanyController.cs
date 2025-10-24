using App.UI.Application.DTOS;
using App.UI.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace App.UI.Controllers
{
    public class CompanyController(ICompanyService companyService, ILogger<CompanyController> logger) : Controller

    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var result = await companyService.GetListAsync();
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> Get(Guid id)
        {
            var result = await companyService.GetByIdAsync(id);
            return Json(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CompanyCreateDto createDto)
        {
            var result = await companyService.CreateAsync(createDto);
            return Json(result);
        }

        [HttpPut]
        public async Task<IActionResult> Update([FromBody] CompanyUpdateDto updateDto)
        {
            var result = await companyService.UpdateAsync(updateDto);
            return Json(result);
        }

        [HttpDelete]
        public async Task<IActionResult> Delete([FromBody] DeleteDto deleteDto)
        {
            var result = await companyService.DeleteAsync(deleteDto);
            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetDistricts()
        {
            var result = await companyService.GetDistrictsAsync();
            return Json(result);
        }
    }
}
