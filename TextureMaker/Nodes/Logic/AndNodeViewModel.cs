using System.Reactive.Linq;
using TextureMaker.Graph;

namespace TextureMaker.Nodes.Logic;

public class AndNodeViewModel : GraphNodeViewModel
{
    public InputPin<bool>  InputA     { get; } = new() { Name = "A" };
    public InputPin<bool>  InputB     { get; } = new() { Name = "B" };
    public OutputPin<bool> BoolOutput { get; } = new();

    public AndNodeViewModel()
    {
        Name = "AND";

        InputA.ViewModel.Name    = "A";
        InputA.ViewModel.PinType = "bool";
        InputB.ViewModel.Name    = "B";
        InputB.ViewModel.PinType = "bool";
        BoolOutput.ViewModel.Name    = "Result";
        BoolOutput.ViewModel.PinType = "bool";

        AllPins.Add(InputA.ViewModel);
        AllPins.Add(InputB.ViewModel);
        AllPins.Add(BoolOutput.ViewModel);

        BoolOutput.Value = Observable.CombineLatest(
            InputA.Value, InputB.Value, (a, b) => a == true && b == true);
    }
}
