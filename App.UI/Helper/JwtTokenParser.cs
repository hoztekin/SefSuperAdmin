using System.Text;
using System.Text.Json;

namespace App.UI.Helper
{
    public static class JwtTokenParser
    {
        public static (string userId, List<string> roles, string username) ParseToken(string token)
        {
            try
            {

                var parts = token.Split('.');
                if (parts.Length != 3)
                    return ("", new List<string>(), "");

                // Decode the payload
                var payloadBase64 = parts[1];

                // Make sure the base64 string is properly padded
                while (payloadBase64.Length % 4 != 0)
                {
                    payloadBase64 += "=";
                }

                // Replace characters that are different in base64url and base64
                payloadBase64 = payloadBase64.Replace('-', '+').Replace('_', '/');

                var payloadBytes = Convert.FromBase64String(payloadBase64);
                var payloadJson = Encoding.UTF8.GetString(payloadBytes);

                Console.WriteLine($"JWT Payload: {payloadJson}");





                // Parse JSON
                using var document = JsonDocument.Parse(payloadJson);
                var root = document.RootElement;

                // Extract user ID
                string userId = "";
                if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", out var nameIdProperty))
                {
                    userId = nameIdProperty.GetString();
                    Console.WriteLine($"Found userId in 'nameidentifier': {userId}");
                }
                else if (root.TryGetProperty("nameid", out var nameidProperty))
                {
                    userId = nameidProperty.GetString();
                }
                else if (root.TryGetProperty("sub", out var subProperty))
                {
                    userId = subProperty.GetString();
                }

                //  Extract username
                string username = "";
                if (root.TryGetProperty("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", out var nameProperty))
                {
                    username = nameProperty.GetString();
                    Console.WriteLine($"Found username in 'name': {username}");
                }
                else if (root.TryGetProperty("name", out var nameCompactProperty))
                {
                    username = nameCompactProperty.GetString();
                }
                else if (root.TryGetProperty("unique_name", out var uniqueNameProperty))
                {
                    username = uniqueNameProperty.GetString();
                }

                // Extract roles
                var roles = new List<string>();
                if (root.TryGetProperty("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", out var roleClaimProperty))
                {
                    if (roleClaimProperty.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roleClaimProperty.EnumerateArray())
                        {
                            roles.Add(role.GetString());
                        }
                    }
                    else if (roleClaimProperty.ValueKind == JsonValueKind.String)
                    {
                        roles.Add(roleClaimProperty.GetString());
                    }
                }
                // Compact "role" property as fallback
                else if (root.TryGetProperty("role", out var roleProperty))
                {
                    if (roleProperty.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var role in roleProperty.EnumerateArray())
                        {
                            roles.Add(role.GetString());
                        }
                    }
                    else if (roleProperty.ValueKind == JsonValueKind.String)
                    {
                        roles.Add(roleProperty.GetString());
                    }
                }

                return (userId, roles, username);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JWT token: {ex.Message}");
                return ("", new List<string>(), "");
            }
        }

        public static string CleanToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return token;

            // Baştaki ve sondaki boşlukları kaldır
            token = token.Trim();

            // TÜM tırnak işaretlerini kaldır (başta, sonda ve ortada)
            token = token.Replace("\"", "");

            // Varsa "Bearer " önekini kaldır
            if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring(7);
            }

            // Boşlukları temizle (tokenın ortasında boşluk olmamalı)
            token = token.Trim();

            // Detaylı loglama
            Console.WriteLine($"Orijinal token uzunluğu: {token.Length}");
            Console.WriteLine($"Temizlenmiş token: {token.Substring(0, Math.Min(20, token.Length))}...");

            return token;
        }
    }
}
