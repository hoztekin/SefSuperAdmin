namespace App.Services.Users.Create
{
    public class CreateUserDto
    {
        public string UserName { get; set; }

        public string EMail { get; set; }

        public string Password { get; set; }

        public string ConfirmPassword { get; set; }
    }
}
