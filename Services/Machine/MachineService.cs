using App.Repositories;
using App.Repositories.Machines;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace App.Services.Machine
{
    public class MachineService(IMachineRepository machineRepository, IUnitOfWork unitOfWork, IMapper mapper) : IMachineService
    {
    }
}
