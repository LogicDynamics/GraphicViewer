using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using GraphicViewer.Models;

namespace GraphicViewer.Services;

/// <summary>
/// Singleton service that owns all SVG viewer state.
/// Components subscribe to OnStateChanged and call StateHasChanged() accordingly.
/// </summary>
public class SvgViewerService
{
    // ── Dependencies ─────────────────────────────────────────────────────────
    private readonly IJSRuntime _js;

    public SvgViewerService(IJSRuntime js)
    {
        _js = js;
    }

    // ── State ────────────────────────────────────────────────────────────────
    public IReadOnlyList<SvgFileModel> Files => _files;
    public SvgFileModel? CurrentFile => _files.Count > 0 ? _files[CurrentIndex] : null;
    public int CurrentIndex { get; private set; } = 0;
    public double ZoomLevel { get; private set; } = 1.0;

    public bool HasFiles => _files.Count > 0;
    public bool CanGoPrev => CurrentIndex > 0;
    public bool CanGoNext => CurrentIndex < _files.Count - 1;
    public string FileCounter => HasFiles ? $"{CurrentIndex + 1} / {_files.Count}" : "—";
    public string ZoomLabel => $"{(int)(ZoomLevel * 100)}%";

    // ── Events ───────────────────────────────────────────────────────────────
    public event Action? OnStateChanged;

    // ── Private ──────────────────────────────────────────────────────────────
    private readonly List<SvgFileModel> _files = [];
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    // ── File Loading ─────────────────────────────────────────────────────────
    public async Task LoadFilesAsync(IEnumerable<IBrowserFile> browserFiles)
    {
        _files.Clear();

        var svgFiles = browserFiles
            .Where(f => f.Name.EndsWith(".svg", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.Name)
            .ToList();

        for (int i = 0; i < svgFiles.Count; i++)
        {
            var file = svgFiles[i];
            try
            {
                using var stream = file.OpenReadStream(MaxFileSizeBytes);
                using var reader = new StreamReader(stream);
                var rawContent = await reader.ReadToEndAsync();

                // Normalize via JS — fixes missing viewBox/width/height
                var normalizedContent = await _js.InvokeAsync<string>(
                    "GraphicViewer.normalizeSvg", rawContent);

                _files.Add(new SvgFileModel
                {
                    Index = i,
                    FileName = file.Name,
                    SvgContent = normalizedContent,
                    RawContent = rawContent,
                    SizeBytes = file.Size
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[SvgViewer] Failed to load {file.Name}: {ex.Message}");
            }
        }

        CurrentIndex = 0;
        ZoomLevel = 1.0;
        NotifyStateChanged();
    }

    // ── Navigation ───────────────────────────────────────────────────────────
    public void SelectFile(int index)
    {
        if (index < 0 || index >= _files.Count) return;
        CurrentIndex = index;
        ZoomLevel = 1.0;
        NotifyStateChanged();
    }

    public void NavigatePrev() { if (CanGoPrev) SelectFile(CurrentIndex - 1); }
    public void NavigateNext() { if (CanGoNext) SelectFile(CurrentIndex + 1); }

    // ── Zoom ─────────────────────────────────────────────────────────────────
    public void ZoomIn()    => SetZoom(ZoomLevel + 0.25);
    public void ZoomOut()   => SetZoom(ZoomLevel - 0.25);
    public void ResetZoom() => SetZoom(1.0);

    private void SetZoom(double value)
    {
        ZoomLevel = Math.Clamp(value, 0.25, 4.0);
        NotifyStateChanged();
    }

    // ── Clipboard ────────────────────────────────────────────────────────────
    public async Task<bool> CopyCurrentSvgAsync()
    {
        if (CurrentFile is null) return false;
        return await _js.InvokeAsync<bool>(
            "GraphicViewer.copyToClipboard", CurrentFile.RawContent);
    }

    // ── Internal ─────────────────────────────────────────────────────────────
    private void NotifyStateChanged() => OnStateChanged?.Invoke();
}
