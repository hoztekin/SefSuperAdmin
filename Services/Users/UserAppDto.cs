namespace App.Services.Users
{
    public class UserAppDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public string EMail { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
