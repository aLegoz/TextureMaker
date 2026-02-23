using ReactiveUI;

namespace TextureMaker.Graph;

/// <summary>Represents a drawn connection between two pins.</summary>
public class ConnectionViewModel : ReactiveObject
{
    public PinViewModel OutputPin { get; set; }
    public PinViewModel InputPin { get; set; }

    // Screen coordinates (updated by the graph canvas after layout)
    private System.Windows.Point _start;
    public System.Windows.Point Start
    {
        get => _start;
        set => this.RaiseAndSetIfChanged(ref _start, value);
    }

    private System.Windows.Point _end;
    public System.Windows.Point End
    {
        get => _end;
        set => this.RaiseAndSetIfChanged(ref _end, value);
    }

    public ConnectionViewModel(PinViewModel output, PinViewModel input)
    {
        OutputPin = output;
        InputPin = input;
    }
}
