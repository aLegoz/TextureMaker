using System.Reactive.Linq;
using ReactiveUI;
using TextureMaker.Graph;
using WpfColor  = System.Windows.Media.Color;
using WpfColors = System.Windows.Media.Colors;

namespace TextureMaker.Nodes.Sources;

/// <summary>A node that outputs a single WpfColor value.
/// Connect its output to any node that has a Color input pin.</summary>
public class ColorNodeViewModel : GraphNodeViewModel
{
    public OutputPin<WpfColor> ColorOutput { get; } = new();

    private WpfColor _selectedColor = WpfColors.White;
    public WpfColor SelectedColor
    {
        get => _selectedColor;
        set => this.RaiseAndSetIfChanged(ref _selectedColor, value);
    }

    public ColorNodeViewModel()
    {
        Name = "Color";
        ColorOutput.Name = "Color";
        ColorOutput.ViewModel.Name    = "Color";
        ColorOutput.ViewModel.PinType = "color";
        AllPins.Add(ColorOutput.ViewModel);

        ColorOutput.Value = this.WhenAnyValue(x => x.SelectedColor);
    }
}
