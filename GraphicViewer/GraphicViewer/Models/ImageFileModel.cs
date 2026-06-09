namespace GraphicViewer.Models;

/// <summary>
/// Represents a loaded image file (PNG, JPEG, WEBP, GIF, BMP).
/// DataUrl = base64 data URL ready for <img src="@DataUrl" />.
/// </summary>
public class ImageFileModel
{
    public int Index { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>Base64 data URL — e.g. "data:image/png;base64,..."</summary>
    public string DataUrl { get; set; } = string.Empty;

    /// <summary>MIME type — e.g. "image/png"</summary>
    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string DisplaySize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024):F1} MB"
    };

    public string Extension => Path.GetExtension(FileName).ToUpperInvariant().TrimStart('.');

    /// <summary>Resolve MIME type from file extension.</summary>
    public static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".ico" => "image/x-icon",
            _ => "image/png"
        };
}
