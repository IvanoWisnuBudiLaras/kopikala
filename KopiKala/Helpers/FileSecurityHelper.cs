namespace KopiKala.Helpers;

public static class FileSecurityHelper
{
    public const long MaxFileSizeBytes = 2 * 1024 * 1024; // 2 MB

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png"
    };

    private static readonly byte[] JpegHeader = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static bool IsValidImageExtension(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return false;

        var ext = Path.GetExtension(filename);
        return AllowedExtensions.Contains(ext);
    }

    public static bool ValidateMagicBytes(byte[] fileBytes, string filename)
    {
        if (fileBytes == null || fileBytes.Length < 8)
            return false;

        var ext = Path.GetExtension(filename).ToLowerInvariant();
        if (ext is ".jpg" or ".jpeg")
        {
            return fileBytes.Length >= 3 &&
                   fileBytes[0] == JpegHeader[0] &&
                   fileBytes[1] == JpegHeader[1] &&
                   fileBytes[2] == JpegHeader[2];
        }
        if (ext == ".png")
        {
            return fileBytes.Length >= 8 &&
                   fileBytes[0] == PngHeader[0] &&
                   fileBytes[1] == PngHeader[1] &&
                   fileBytes[2] == PngHeader[2] &&
                   fileBytes[3] == PngHeader[3] &&
                   fileBytes[4] == PngHeader[4] &&
                   fileBytes[5] == PngHeader[5] &&
                   fileBytes[6] == PngHeader[6] &&
                   fileBytes[7] == PngHeader[7];
        }

        return false;
    }

    public static string GenerateSafeFileName(string originalFilename)
    {
        var ext = Path.GetExtension(originalFilename).ToLowerInvariant();
        return $"{Guid.NewGuid():N}{ext}";
    }
}
