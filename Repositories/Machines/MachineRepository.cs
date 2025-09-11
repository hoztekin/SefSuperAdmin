namespace App.Repositories.Machines
{
    public class MachineRepository(AppDbContext context) : GenericRepository<Machine, int>(context), IMachineRepository
    {

    }
}
