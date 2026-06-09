namespace GraphicViewer.Models;

/// <summary>
/// Represents a loaded audio file (MP3, WAV, OGG, FLAC, AAC).
/// ObjectUrl = blob URL created via JS URL.createObjectURL().
/// Must be revoked when done to free browser memory.
/// </summary>
public class AudioFileModel
{
    public int Index { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>Blob URL — e.g. "blob:https://localhost:7221/abc123"</summary>
    public string ObjectUrl { get; set; } = string.Empty;

    /// <summary>MIME type — e.g. "audio/mpeg"</summary>
    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    /// <summary>Track title — parsed from filename (without extension).</summary>
    public string Title => Path.GetFileNameWithoutExtension(FileName);

    public string DisplaySize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024):F1} MB"
    };

    public string Extension =>
        Path.GetExtension(FileName).ToUpperInvariant().TrimStart('.');

    public static string GetMimeType(string fileName) =>
        Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".mp3"  => "audio/mpeg",
            ".wav"  => "audio/wav",
            ".ogg"  => "audio/ogg",
            ".flac" => "audio/flac",
            ".aac"  => "audio/aac",
            ".m4a"  => "audio/mp4",
            ".opus" => "audio/opus",
            _       => "audio/mpeg"
        };
}
