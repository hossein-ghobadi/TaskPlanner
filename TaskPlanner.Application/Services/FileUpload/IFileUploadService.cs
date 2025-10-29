using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TaskPlanner.Application.Services.FileUpload
{
    public interface IFileUploadService
    {
        Task<(bool Success, string FilePath, string Error)> UploadFileAsync(IFormFile file, string folder);
        bool DeleteFile(string filePath);
        string GetFileType(string fileName);
    }
}

