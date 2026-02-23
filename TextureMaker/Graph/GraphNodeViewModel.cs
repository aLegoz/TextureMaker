using System.Collections.ObjectModel;
using System.Windows;
using ReactiveUI;

namespace TextureMaker.Graph;

/// <summary>Base node ViewModel for the custom graph canvas.</summary>
public abstract class GraphNodeViewModel : ReactiveObject
{
    public string Name { get; protected set; } = "Node";

    private Point _position;
    public Point Position
    {
        get => _position;
        set => this.RaiseAndSetIfChanged(ref _position, value);
    }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set => this.RaiseAndSetIfChanged(ref _isSelected, value);
    }

    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set => this.RaiseAndSetIfChanged(ref _hasError, value);
    }

    // All pins (for hit-testing and connection rendering)
    public ObservableCollection<PinViewModel> AllPins { get; } = new();
}
