using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace App.Services.File
{
    public class FileService(IWebHostEnvironment environment) : IFileService
    {
        public async Task<string> SaveFile(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0)
                return null;

            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var path = Path.Combine(environment.WebRootPath, folderName, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        public void DeleteFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return;

            var fullPath = Path.Combine(environment.WebRootPath, filePath);
            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }
        }
    }

}
