using App.Services.Filters;
using App.Services.Machine;
using App.Services.Machine.Dtos;
using App.Services.Machine.Validation;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{

    public class MachineController(IMachineService machineService) : CustomBaseController
    {
        [HttpGet]
        public async Task<IActionResult> GetMachines() => CreateActionResult(await machineService.GetAllListAsync());

        [HttpGet("active")]
        public async Task<IActionResult> GetActiveMachines() => CreateActionResult(await machineService.GetActiveListAsync());

        [HttpGet("{id:int}")]
        [ServiceFilter(typeof(NotFoundFilter<Repositories.Machines.Machine, int>))]
        public async Task<IActionResult> GetMachine(int id) => CreateActionResult(await machineService.GetByIdAsync(id));

        /// <summary>
        /// Koda göre makine getirir
        /// </summary>
        /// <param name="code">Makine kodu</param>
        /// <returns>Makine detayı</returns>
        [HttpGet("code/{code}")]
        public async Task<IActionResult> GetMachineByCode(string code) => CreateActionResult(await machineService.GetByCodeAsync(code));

        /// <summary>
        /// Şube ID'sine göre makineleri getirir
        /// </summary>
        /// <param name="branchId">Şube ID'si</param>
        /// <returns>Şubeye ait makine listesi</returns>
        [HttpGet("branch/{branchId}")]
        public async Task<IActionResult> GetMachinesByBranch(string branchId) => CreateActionResult(await machineService.GetByBranchIdAsync(branchId));

        [HttpPost]
        public async Task<IActionResult> CreateMachine(CreateMachineDto createMachineDto) => CreateActionResult(await machineService.CreateAsync(createMachineDto));


        [HttpPut("{id:int}")]
        [ServiceFilter(typeof(NotFoundFilter<Repositories.Machines.Machine, int>))]
        public async Task<IActionResult> UpdateMachine(int id, UpdateMachineDto updateMachineDto)
        {
            updateMachineDto.Id = id;
            return CreateActionResult(await machineService.UpdateAsync(updateMachineDto));
        }

        [HttpDelete("{id:int}")]
        [ServiceFilter(typeof(NotFoundFilter<Repositories.Machines.Machine, int>))]
        public async Task<IActionResult> DeleteMachine(int id)
            => CreateActionResult(await machineService.DeleteAsync(id));


        [HttpPatch("{id:int}/status")]
        [ServiceFilter(typeof(NotFoundFilter<Repositories.Machines.Machine, int>))]
        public async Task<IActionResult> SetMachineStatus(int id, [FromQuery] bool isActive) => CreateActionResult(await machineService.SetActiveStatusAsync(id, isActive));


        /// <summary>
        /// API bağlantısını test eder
        /// </summary>
        /// <param name="apiAddress">Test edilecek API adresi</param>
        /// <returns>Bağlantı test sonucu</returns>
        [HttpGet("test-connection")]
        public async Task<IActionResult> TestApiConnection([FromQuery] string apiAddress) => CreateActionResult(await machineService.CheckApiConnectionAsync(apiAddress));

    }
}
