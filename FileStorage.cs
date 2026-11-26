using MediCare.Services.Common;

namespace MediCare.Services.Storage;

public class FileStorageOptions
{
    /// <summary>Absolute folder where medical files are saved. Must NOT be inside wwwroot.</summary>
    public string RootPath { get; set; } = string.Empty;

    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10 MB
}

/// <summary>A file coming from the web layer (keeps the service layer free of ASP.NET types).</summary>
public class FileUpload
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long Length { get; init; }
    public Stream Content { get; init; } = Stream.Null;
}

public record StoredFile(string StoredFileName, string ContentType, long Size);

public interface IFileStorageService
{
    IReadOnlyCollection<string> AllowedExtensions { get; }
    ServiceResult Validate(FileUpload file);
    Task<ServiceResult<StoredFile>> SaveAsync(FileUpload file);
    Stream? OpenRead(string storedFileName);
    void Delete(string storedFileName);
}

public class LocalFileStorageService : IFileStorageService
{
    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".dcm"] = "application/dicom"
    };

    // First bytes of each allowed format, to reject renamed files (e.g. an .exe renamed to .pdf).
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = new[] { new byte[] { 0x25, 0x50, 0x44, 0x46 } },          // %PDF
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } }
        // .dcm: DICOM has a 128-byte preamble then "DICM"; checked separately below.
    };

    private readonly FileStorageOptions _options;

    public LocalFileStorageService(FileStorageOptions options)
    {
        _options = options;
        Directory.CreateDirectory(_options.RootPath);
    }

    public IReadOnlyCollection<string> AllowedExtensions => ContentTypes.Keys;

    public ServiceResult Validate(FileUpload file)
    {
        if (file.Length <= 0)
            return ServiceResult.Fail($"\"{file.FileName}\" is empty.");

        if (file.Length > _options.MaxFileSizeBytes)
            return ServiceResult.Fail($"\"{file.FileName}\" is larger than {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");

        var ext = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(ext) || !ContentTypes.ContainsKey(ext))
            return ServiceResult.Fail($"\"{file.FileName}\" is not allowed. Allowed types: PDF, JPG, PNG, DICOM.");

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<StoredFile>> SaveAsync(FileUpload file)
    {
        var validation = Validate(file);
        if (!validation.Succeeded)
            return ServiceResult<StoredFile>.Fail(validation.Error!);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();

        using var buffer = new MemoryStream();
        await file.Content.CopyToAsync(buffer);
        var bytes = buffer.ToArray();

        if (!HasValidSignature(ext, bytes))
            return ServiceResult<StoredFile>.Fail($"\"{file.FileName}\" does not look like a real {ext} file.");

        // Random name: never trust the user's file name on disk.
        var storedName = $"{Guid.NewGuid():N}{ext}";
        await File.WriteAllBytesAsync(GetFullPath(storedName), bytes);

        return ServiceResult<StoredFile>.Ok(new StoredFile(storedName, ContentTypes[ext], bytes.LongLength));
    }

    public Stream? OpenRead(string storedFileName)
    {
        var path = GetFullPath(storedFileName);
        return File.Exists(path) ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read) : null;
    }

    public void Delete(string storedFileName)
    {
        var path = GetFullPath(storedFileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private string GetFullPath(string storedFileName)
    {
        // Only allow our own generated names (blocks "../" path tricks).
        var safeName = Path.GetFileName(storedFileName);
        return Path.Combine(_options.RootPath, safeName);
    }

    private static bool HasValidSignature(string ext, byte[] bytes)
    {
        if (ext == ".dcm")
            return bytes.Length > 132 && bytes[128] == 'D' && bytes[129] == 'I' && bytes[130] == 'C' && bytes[131] == 'M';

        if (!Signatures.TryGetValue(ext, out var signatures))
            return false;

        return signatures.Any(sig => bytes.Length >= sig.Length && bytes.AsSpan(0, sig.Length).SequenceEqual(sig));
    }
}
