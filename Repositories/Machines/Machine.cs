namespace App.Repositories.Machines
{
    public class Machine : BaseEntity
    {
        public string BranchId { get; set; }
        public string BranchName { get; set; }
        public string ApiAddress { get; set; }
    }
}
