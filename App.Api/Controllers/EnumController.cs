using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace App.Api.Controllers
{
    [AllowAnonymous]
    public class EnumController : CustomBaseController
    {
        [HttpGet("{typeName}")]
        public IActionResult GetEnumValues(string typeName)
        {
            Type foundType = null;

            // Enum tipini bul
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foundType = assembly.GetTypes().FirstOrDefault(t => t.IsEnum && (t.Name.Equals($"{typeName}Enum", StringComparison.OrdinalIgnoreCase) ||
                                                                                 t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase)));

                if (foundType != null)
                    break;
            }

            if (foundType == null)
                return NotFound($"Enum type '{typeName}' not found");

            var values = Enum.GetValues(foundType).Cast<object>().Select(e => new
            {
                Value = e.ToString(),
                DisplayName = e.ToString(),
                NumericValue = Convert.ToInt32(e)
            });

            return Ok(values);
        }
    }
}
