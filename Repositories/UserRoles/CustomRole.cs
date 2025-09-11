namespace App.Repositories.UserRoles
{
    public enum CustomRole
    {
        Admin,
        SuperAdmin,
        Destek
    }
    public static class CustomRoleExtensions
    {
        public static string GetDisplayName(this CustomRole role)
        {
            return role switch
            {
                CustomRole.Admin => "Admin",
                CustomRole.SuperAdmin => "Süper Admin",
                CustomRole.Destek => "Destek",
                _ => role.ToString()
            };
        }

        public static string GetDescription(this CustomRole role)
        {
            return role switch
            {
                CustomRole.Admin => "Sistem yöneticisi - tam yetkili",
                CustomRole.SuperAdmin => "Süper yönetici - tüm sistemlere erişim",
                CustomRole.Destek => "Destek personeli - sınırlı erişim",
                _ => role.ToString()
            };
        }

        public static List<CustomRole> GetAllRoles()
        {
            return Enum.GetValues<CustomRole>().ToList();
        }
    }
}
