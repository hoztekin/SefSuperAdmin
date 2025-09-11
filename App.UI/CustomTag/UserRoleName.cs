using App.UI.Services;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace App.UI.CustomTag
{
    public class UserRoleName(IRoleService roleService) : TagHelper
    {
        [HtmlAttributeName("user-role")]
        public string userId { get; set; }


        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    output.Content.SetHtmlContent("<span class='text-danger'>User ID bulunamadı</span>");
                    return;
                }

                // Tek bir veritabanı çağrısı yaparak kullanıcı ve rolleri alın
                var userRoles = await roleService.GetUserRolesAsync(userId);

                string html = string.Empty;
                if (userRoles != null && userRoles.Count > 0)
                {
                    bool hasRole = false;
                    foreach (var role in userRoles)
                    {
                        if (role.Exist)
                        {
                            html += $"<span class='badge bg-info'>{role.RoleName}</span> &nbsp;";
                            hasRole = true;
                        }
                    }

                    if (!hasRole)
                    {
                        html = "<span class='text-muted'>Rol atanmamış</span>";
                    }
                }
                else
                {
                    html = "<span class='text-muted'>Rol atanmamış</span>";
                }

                output.Content.SetHtmlContent(html);
            }
            catch (Exception ex)
            {
                output.Content.SetHtmlContent($"<span class='text-danger'>Hata: {ex.Message}</span>");
            }
        }
    }
}
