namespace App.UI.Application.DTOS
{
    public class AuthResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; } = new();

        public static AuthResult Success(string message = "İşlem başarılı")
        {
            return new AuthResult { IsSuccess = true, Message = message };
        }

        public static AuthResult Failed(string message)
        {
            return new AuthResult { IsSuccess = false, Message = message };
        }

        public AuthResult WithData(string key, object value)
        {
            Data[key] = value;
            return this;
        }
    }
}
