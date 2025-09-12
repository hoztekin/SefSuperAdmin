using App.Services.Filters;
using App.Services.Machine;
using App.Services.Users.Create;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{

    public class MachineController(IMachineService machineService) : CustomBaseController
    {        
        //[HttpGet]
        //public async Task<IActionResult> GetMachine() => CreateActionResult(await machineService.GetAllListAsync());

        //[HttpGet("{id:int}")]
        //public async Task<IActionResult> GetMachine(int id) => CreateActionResult(await machineService.GetByIdAsync(id));

        //[HttpPost]
        //public async Task<IActionResult> CreateUser(CreateUserDto createUserDto) => CreateActionResult(await machineService.CreateMachineAsync(createUserDto));

        //[ServiceFilter(typeof(NotFoundFilter<Repositories.Machines.Machine, int>))]
        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> DeleteCategory(int id) => CreateActionResult(await machineService.DeleteAsync(id));

    }
}
