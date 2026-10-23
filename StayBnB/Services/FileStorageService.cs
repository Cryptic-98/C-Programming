using Microsoft.AspNetCore.Http;
using StayBnB.Models.Entities;

namespace StayBnB.Services;

/// <summary>
/// Stores uploaded files. Property photos live in <c>wwwroot/images/properties</c>
/// so they can be served statically, while guest identity documents are kept in a
/// private folder outside <c>wwwroot</c> and are only streamed through an
/// authorised controller action.
/// </summary>
public interface IFileStorageService
{
    Task<string> SaveImageAsync(IFormFile file);
    void DeleteImage(string imageUrl);
    Task<string> SavePrivateDocumentAsync(IFormFile file);
    Task<Stream> OpenPrivateAsync(string fileName);
    void DeletePrivate(string fileName);
    bool PrivateFileExists(string fileName);
}

public class FileStorageService : IFileStorageService
{
    private static readonly string[] AllowedImageExtensions = { ".jpg", ".jpeg", ".png", ".webp" };
    private static readonly string[] AllowedDocumentExtensions = { ".jpg", ".jpeg", ".png", ".pdf" };
    private const long MaxFileSize = 5 * 1024 * 1024;

    private readonly IWebHostEnvironment _environment;

    public FileStorageService(IWebHostEnvironment environment) => _environment = environment;

    private string PropertyImageFolder => Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), "images", "properties");
    private string PrivateFolder => Path.Combine(_environment.ContentRootPath, "App_Data", "documents");

    public async Task<string> SaveImageAsync(IFormFile file)
    {
        var extension = ValidateFile(file, AllowedImageExtensions);
        await ValidateSignatureAsync(file, extension);

        Directory.CreateDirectory(PropertyImageFolder);

        var name = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(PropertyImageFolder, name);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return $"/images/properties/{name}";
    }

    public void DeleteImage(string imageUrl)
    {
        if (!TryResolveImagePath(imageUrl, out var path))
            return;

        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    public async Task<string> SavePrivateDocumentAsync(IFormFile file)
    {
        var extension = ValidateFile(file, AllowedDocumentExtensions);
        await ValidateSignatureAsync(file, extension);

        Directory.CreateDirectory(PrivateFolder);

        var name = $"{Guid.NewGuid():N}{extension}";
        var path = Path.Combine(PrivateFolder, name);

        await using var stream = System.IO.File.Create(path);
        await file.CopyToAsync(stream);

        return name;
    }

    public Task<Stream> OpenPrivateAsync(string fileName)
    {
        var path = ResolvePrivatePath(fileName);
        Stream stream = System.IO.File.OpenRead(path);
        return Task.FromResult(stream);
    }

    public void DeletePrivate(string fileName)
    {
        var path = ResolvePrivatePath(fileName);
        if (System.IO.File.Exists(path))
            System.IO.File.Delete(path);
    }

    public bool PrivateFileExists(string fileName) => System.IO.File.Exists(ResolvePrivatePath(fileName));

    /// <summary>Guards against path traversal when a file name comes from the database or a query string.</summary>
    private string ResolvePrivatePath(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName != Path.GetFileName(fileName) ||
            fileName.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Invalid document file name.");

        return Path.Combine(PrivateFolder, fileName);
    }

    private bool TryResolveImagePath(string imageUrl, out string path)
    {
        path = string.Empty;
        const string prefix = "/images/properties/";

        if (string.IsNullOrWhiteSpace(imageUrl) || !imageUrl.StartsWith(prefix, StringComparison.Ordinal))
            return false;

        var fileName = imageUrl[prefix.Length..];
        if (fileName != Path.GetFileName(fileName) || fileName.Contains("..", StringComparison.Ordinal))
            return false;

        path = Path.Combine(PropertyImageFolder, fileName);
        return true;
    }

    private static string ValidateFile(IFormFile file, IReadOnlyCollection<string> allowedExtensions)
    {
        if (file is null || file.Length is <= 0 or > MaxFileSize)
            throw new InvalidDataException("The uploaded file must be between 1 byte and 5 MB.");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new InvalidDataException("The uploaded file type is not allowed.");

        return extension;
    }

    private static async Task ValidateSignatureAsync(IFormFile file, string extension)
    {
        var header = new byte[12];
        await using var stream = file.OpenReadStream();
        var bytesRead = await stream.ReadAsync(header);

        var valid = extension switch
        {
            ".jpg" or ".jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            ".png" => bytesRead >= 8 && header.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            ".webp" => bytesRead >= 12 &&
                       header.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                       header.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            ".pdf" => bytesRead >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8),
            _ => false
        };

        if (!valid)
            throw new InvalidDataException("The uploaded file content does not match its file type.");
    }
}
