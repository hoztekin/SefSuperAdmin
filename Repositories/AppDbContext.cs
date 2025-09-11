using App.Repositories.Machines;
using App.Repositories.UserApps;
using App.Repositories.UserRefreshTokens;
using App.Repositories.UserRoles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace App.Repositories
{
    public class AppDbContext(DbContextOptions<AppDbContext> options, IHttpContextAccessor httpContextAccessor) : IdentityDbContext<UserApp, UserRole, string>(options)
    {
        public DbSet<Machine> Machines { get; set; } = default!;
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(modelBuilder);
        }

        public override int SaveChanges()
        {
            UpdateAuditFields();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateAuditFields();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateAuditFields()
        {
            var currentUserId = GetCurrentUserId();

            foreach (var item in ChangeTracker.Entries())
            {
                if (item.Entity is BaseEntity entityReference)
                {
                    switch (item.State)
                    {
                        case EntityState.Added:
                            entityReference.CreatedBy = currentUserId.ToString()!;
                            Entry(entityReference).Property(x => x.UpdatedDate).IsModified = false;
                            entityReference.CreatedDate = DateTime.Now;
                            break;
                        case EntityState.Modified:
                            entityReference.UpdatedBy = currentUserId.ToString()!;
                            Entry(entityReference).Property(x => x.CreatedDate).IsModified = false;
                            entityReference.UpdatedDate = DateTime.Now;
                            break;
                        default:
                            break;
                    }
                }

            }
        }



        private Guid? GetCurrentUserId()
        {
            try
            {
                var user = httpContextAccessor?.HttpContext?.User;
                if (user?.Identity?.IsAuthenticated == true)
                {
                    var userIdClaim = user.FindFirst("UserId")?.Value;
                    if (Guid.TryParse(userIdClaim, out var userId))
                    {
                        return userId;
                    }
                }
            }
            catch (Exception ex)
            {

                Serilog.Log.Error($"Error getting current user ID: {ex.Message}");
            }

            return null;
        }
    }
}
