using App.UI.DTOS;
namespace App.UI.Services
{
    public interface IMemberService
    {
        Task<IQueryable<UserAppDtoUI>> GetAllMembersAsync();
    }

    public class MemberService(IApiService apiService) : IMemberService
    {
        public async Task<IQueryable<UserAppDtoUI>> GetAllMembersAsync()
        {
            try
            {
                var result = await apiService.GetAsync<List<UserAppDtoUI>>("api/v1/user/AllMembers");
                return result.AsQueryable();
            }
            catch (Exception ex)
            {
                return new List<UserAppDtoUI>().AsQueryable();
            }
        }
    }
}
