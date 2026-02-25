using ReactiveUI;

namespace TextureMaker.Graph;

/// <summary>UI-facing ViewModel for a pin socket (visual only).</summary>
public class PinViewModel : ReactiveObject
{
    public bool IsOutput { get; init; }

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set => this.RaiseAndSetIfChanged(ref _isConnected, value);
    }

    public string Name { get; set; } = string.Empty;

    /// <summary>"texture" | "color" | "folder"</summary>
    public string PinType { get; set; } = "texture";
}
