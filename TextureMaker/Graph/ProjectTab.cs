using System.IO;
using ReactiveUI;

namespace TextureMaker.Graph;

public class ProjectTab : ReactiveObject
{
    public GraphViewModel Graph { get; }

    string? _filePath;
    public string? FilePath
    {
        get => _filePath;
        set { this.RaiseAndSetIfChanged(ref _filePath, value); UpdateTitle(); }
    }

    bool _isDirty;
    public bool IsDirty
    {
        get => _isDirty;
        set { this.RaiseAndSetIfChanged(ref _isDirty, value); UpdateTitle(); }
    }

    bool _isActive;
    public bool IsActive { get => _isActive; set => this.RaiseAndSetIfChanged(ref _isActive, value); }

    public int NodeCount { get; set; }

    // Canvas pan/zoom state saved on tab switch
    public double SavedScale { get; set; } = 1.0;
    public double SavedPanX  { get; set; } = 0.0;
    public double SavedPanY  { get; set; } = 0.0;

    string _tabTitle = "Untitled";
    public string TabTitle { get => _tabTitle; private set => this.RaiseAndSetIfChanged(ref _tabTitle, value); }

    void UpdateTitle()
    {
        var name = FilePath is null ? "Untitled"
                   : Path.GetFileNameWithoutExtension(FilePath);
        TabTitle = IsDirty ? "● " + name : name;
    }

    public ProjectTab(GraphViewModel? graph = null)
        => Graph = graph ?? new GraphViewModel();
}
