using App.Repositories.Machines;
using App.Repositories.UserApps;
using App.Repositories.UserRoles;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace App.Repositories.SeedDatas
{
    public class DatabaseSeeder(UserManager<UserApp> userManager, RoleManager<UserRole> roleManager, AppDbContext context)
    {
        public async Task SeedAsync()
        {
            Serilog.Log.Information("Veritabanı seed işlemi başlatılıyor...");
            await SeedRolesAsync();
            await SeedUsersAsync();
            await SeedMachinesAsync();
            Serilog.Log.Information("Veritabanı seed işlemi tamamlandı");
        }

        private async Task SeedRolesAsync()
        {
            Serilog.Log.Information("Roller oluşturuluyor...");

            foreach (var role in CustomRoleExtensions.GetAllRoles())
            {
                var roleName = role.ToString();

                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var appRole = new UserRole
                    {
                        Name = roleName,
                        NormalizedName = roleName.ToUpperInvariant()
                    };

                    var result = await roleManager.CreateAsync(appRole);

                    if (result.Succeeded)
                    {
                        Serilog.Log.Information("Rol oluşturuldu: {RoleName}", roleName);
                    }
                    else
                    {
                        Serilog.Log.Error("Rol oluşturulamadı: {RoleName}, Errors: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    Serilog.Log.Information("Rol zaten mevcut: {RoleName}", roleName);
                }
            }
        }

        private async Task SeedUsersAsync()
        {
            Serilog.Log.Information("Test kullanıcıları oluşturuluyor...");

            // SuperAdmin User
            await CreateUserAsync("superadmin", "super@admin.com", "SuperAdmin123", CustomRole.SuperAdmin);

            // Admin User  
            await CreateUserAsync("admin", "admin@admin.com", "Admin123", CustomRole.Admin);

            // Destek User
            await CreateUserAsync("destek", "destek@admin.com", "Destek123", CustomRole.Destek);
        }

        private async Task CreateUserAsync(string userName, string email, string password, CustomRole role)
        {
            var existingUser = await userManager.FindByNameAsync(userName);

            if (existingUser == null)
            {
                var user = new UserApp
                {
                    UserName = userName,
                    Email = email,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString()
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    // Role ata
                    var roleResult = await userManager.AddToRoleAsync(user, role.ToString());

                    if (roleResult.Succeeded)
                    {
                        Serilog.Log.Information("Kullanıcı oluşturuldu: {UserName} - {Role}", userName, role);
                    }
                    else
                    {
                        Serilog.Log.Error("Kullanıcıya rol atanamadı: {UserName}, Errors: {Errors}",
                            userName, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    }
                }
                else
                {
                    Serilog.Log.Error("Kullanıcı oluşturulamadı: {UserName}, Errors: {Errors}",
                        userName, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                Serilog.Log.Information("Kullanıcı zaten mevcut: {UserName}", userName);

                // Role kontrolü yap, yoksa ekle
                if (!await userManager.IsInRoleAsync(existingUser, role.ToString()))
                {
                    await userManager.AddToRoleAsync(existingUser, role.ToString());
                    Serilog.Log.Information("Mevcut kullanıcıya rol eklendi: {UserName} - {Role}", userName, role);
                }
            }
        }

        private async Task SeedMachinesAsync()
        {
            Serilog.Log.Information("Test makineleri oluşturuluyor...");

            // Test makineleri
            var testMachines = new List<Machine>
            {
                new Machine
                {
                    BranchId = "branch-001",
                    BranchName = "Ana Merkez",
                    ApiAddress = "https://ubuntu.erhanbursoy.com/api/",
                    Code = "MAIN-001",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Machine
                {
                    BranchId = "branch-002",
                    BranchName = "Şube 1",
                    ApiAddress = "https://api.erhanbursoy.com/",
                    Code = "BRANCH-001",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                },
                new Machine
                {
                    BranchId = "branch-003",
                    BranchName = "Test Ortamı",
                    ApiAddress = "http://localhost:9012/",
                    Code = "TEST-001",
                    CreatedBy = "System",
                    CreatedDate = DateTime.UtcNow
                }
            };

            foreach (var machine in testMachines)
            {
                var existingMachine = await context.Machines
                    .FirstOrDefaultAsync(m => m.BranchId == machine.BranchId && !m.IsDeleted);

                if (existingMachine == null)
                {
                    context.Machines.Add(machine);
                    Serilog.Log.Information("Test makine eklendi: {BranchName} - {ApiAddress}",
                        machine.BranchName, machine.ApiAddress);
                }
                else
                {
                    Serilog.Log.Information("Test makine zaten mevcut: {BranchName}", machine.BranchName);
                }
            }

            await context.SaveChangesAsync();
            Serilog.Log.Information("Machine seed işlemi tamamlandı");
        }
    }
}
