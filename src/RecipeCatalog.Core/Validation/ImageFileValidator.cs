namespace RecipeCatalog.Core.Validation;

/// <summary>
/// Проверява качено изображение: разрешено разширение, размер и „магически байтове“ в началото на файла,
/// за да не може под името photo.jpg да се качи друг тип файл.
/// </summary>
public static class ImageFileValidator
{
    public const long MaxBytes = 2 * 1024 * 1024;

    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
        [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
        [".webp"] = new[] { "RIFF"u8.ToArray() }
    };

    public static IReadOnlyCollection<string> AllowedExtensions => Signatures.Keys;

    public static string? Validate(string fileName, long length, ReadOnlySpan<byte> header)
    {
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !Signatures.TryGetValue(extension, out var signatures))
        {
            return "Разрешени са само изображения .jpg, .png и .webp.";
        }

        if (length <= 0 || length > MaxBytes)
        {
            return "Изображението трябва да е до 2 MB.";
        }

        foreach (var signature in signatures)
        {
            if (header.Length >= signature.Length && header[..signature.Length].SequenceEqual(signature))
            {
                return null;
            }
        }

        return "Съдържанието на файла не отговаря на разширението му.";
    }
}
