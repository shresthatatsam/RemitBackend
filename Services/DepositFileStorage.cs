
using Microsoft.AspNetCore.Http;

namespace EverestRemit.Api.Services;

public sealed class DepositFileStorage
{
    private readonly string _root;

    public DepositFileStorage(IWebHostEnvironment environment)
    {
        _root = Path.Combine(
            environment.ContentRootPath,
            "PrivateDepositFiles");

        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException(
                "Please select a deposit proof image.");

        if (file.Length > 5 * 1024 * 1024)
            throw new ArgumentException(
                "Maximum file size is 5 MB.");

        var extension = Path.GetExtension(file.FileName)
            .ToLowerInvariant();

        var allowedExtensions = new[]
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException(
                "Only JPG, JPEG, PNG and WebP images are allowed.");

        // Do not trust the client filename or ContentType.
        // Validate image signatures and decode/re-encode images
        // before production use.

        var key = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_root, key);

        await using var stream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        try
        {
            await file.CopyToAsync(stream, ct);
        }
        catch
        {
            stream.Close();

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            throw;
        }

        // Store only the generated key in the database.
        return key;
    }

    public string GetPath(string key)
    {
        if (string.IsNullOrWhiteSpace(key) ||
            key != Path.GetFileName(key))
        {
            throw new ArgumentException(
                "Invalid file reference.");
        }

        return Path.Combine(_root, key);
    }

    public bool Delete(string key)
    {
        var path = GetPath(key);

        if (!File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }
}
