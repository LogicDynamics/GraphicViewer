namespace GraphicViewer.Models;

/// <summary>
/// Represents a loaded video file (MP4, WEBM, OGV).
/// ObjectUrl = blob URL created via JS URL.createObjectURL().
/// IMPORTANT: Must be revoked via URL.revokeObjectURL() when done
/// to free browser memory.
/// </summary>
public class VideoFileModel
{
    public int Index { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>Blob URL — e.g. "blob:https://localhost:7221/abc123"</summary>
    public string ObjectUrl { get; set; } = string.Empty;

    /// <summary>MIME type — e.g. "video/mp4"</summary>
    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string DisplaySize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        < 1024 * 1024 * 1024 => $"{SizeBytes / (1024.0 * 1024):F1} MB",
        _ => $"{SizeBytes / (1024.0 * 1024 * 1024):F2} GB"
    };

    public string Extension =>
        Path.GetExtension(FileName).ToUpperInvariant().TrimStart('.');

    public static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp4"  => "video/mp4",
            ".webm" => "video/webm",
            ".ogv"  => "video/ogg",
            ".mov"  => "video/quicktime",
            ".avi"  => "video/x-msvideo",
            _       => "video/mp4"
        };
}
