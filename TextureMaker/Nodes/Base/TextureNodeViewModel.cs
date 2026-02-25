using TextureMaker.Core;
using TextureMaker.Graph;

namespace TextureMaker.Nodes.Base;

/// <summary>
/// Abstract base for all texture-processing nodes.
/// Provides one output pin of type TextureData?.
/// </summary>
public abstract class TextureNodeViewModel : GraphNodeViewModel
{
    public OutputPin<TextureData> Output { get; } = new() { Name = "Image" };

    protected TextureNodeViewModel(string name)
    {
        Name = name;
        Output.ViewModel.Name = "Image";
        AllPins.Add(Output.ViewModel);
    }

    protected void RegisterInput(InputPin<TextureData> pin, string pinName)
    {
        pin.Name = pinName;
        pin.ViewModel.Name = pinName;
        AllPins.Add(pin.ViewModel);
    }
}
