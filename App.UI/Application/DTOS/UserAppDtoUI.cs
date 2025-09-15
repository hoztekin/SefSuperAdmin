namespace App.UI.Application.DTOS;

public record UserAppDtoUI(string Id, DateTime? CreatedDate, bool EmailConfirmed,DateTime? UpdatedDate, string UserName, string EMail, List<string> Roles);





