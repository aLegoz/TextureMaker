using ReactiveUI;
using TextureMaker.Graph;

namespace TextureMaker.Nodes.Logic;

public class ToggleNodeViewModel : GraphNodeViewModel
{
    public OutputPin<bool> BoolOutput { get; } = new();

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    public ToggleNodeViewModel()
    {
        Name = "Toggle";
        BoolOutput.Name = "Value";
        BoolOutput.ViewModel.Name    = "Value";
        BoolOutput.ViewModel.PinType = "bool";
        AllPins.Add(BoolOutput.ViewModel);

        BoolOutput.Value = this.WhenAnyValue(x => x.IsActive);
    }
}
