namespace GraphicViewer.Models;

/// <summary>
/// Represents a loaded SVG file in the viewer.
/// SvgContent = normalized (safe to render).
/// RawContent  = original markup (used for clipboard copy).
/// </summary>
public class SvgFileModel
{
    public int Index { get; set; }
    public string FileName { get; set; } = string.Empty;

    /// <summary>Normalized SVG — viewBox/width/height guaranteed present.</summary>
    public string SvgContent { get; set; } = string.Empty;

    /// <summary>Original unmodified SVG markup — used for clipboard copy.</summary>
    public string RawContent { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string DisplaySize => SizeBytes switch
    {
        < 1024 => $"{SizeBytes} B",
        < 1024 * 1024 => $"{SizeBytes / 1024.0:F1} KB",
        _ => $"{SizeBytes / (1024.0 * 1024):F1} MB"
    };
}
