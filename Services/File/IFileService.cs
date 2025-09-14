using Microsoft.AspNetCore.Http;

namespace App.Services.File
{
    public interface IFileService
    {
        Task<string> SaveFile(IFormFile file, string folderName);
        void DeleteFile(string filePath);
    }
}
