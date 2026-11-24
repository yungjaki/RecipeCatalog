using RecipeCatalog.Core.Validation;

namespace RecipeCatalog.Web.Services;

/// <summary>Записва качените изображения в wwwroot/uploads под случайно име (GUID).</summary>
public sealed class ImageStorage
{
    private const int HeaderLength = 16;
    private readonly string _uploadFolder;

    public ImageStorage(IWebHostEnvironment environment)
    {
        _uploadFolder = Path.Combine(environment.WebRootPath, "uploads");
        Directory.CreateDirectory(_uploadFolder);
    }

    /// <summary>Проверява и записва файла. Връща (публичен път, null) или (null, грешка).</summary>
    public async Task<(string? Path, string? Error)> SaveAsync(IFormFile file)
    {
        var header = new byte[HeaderLength];
        await using (var stream = file.OpenReadStream())
        {
            var read = await stream.ReadAsync(header);
            var error = ImageFileValidator.Validate(file.FileName, file.Length, header.AsSpan(0, read));
            if (error is not null)
            {
                return (null, error);
            }
        }

        var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await using (var target = File.Create(System.IO.Path.Combine(_uploadFolder, fileName)))
        {
            await file.CopyToAsync(target);
        }

        return ($"/uploads/{fileName}", null);
    }
}
