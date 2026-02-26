using System.Reactive.Linq;
using ReactiveUI;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Special;

public class SwitchNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> IfTrue      { get; } = new() { Name = "If True" };
    public InputPin<TextureData> IfFalse     { get; } = new() { Name = "If False" };
    public InputPin<bool>        ConditionPin { get; } = new() { Name = "Cond" };

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    public SwitchNodeViewModel() : base("Switch")
    {
        // Register texture inputs
        RegisterInput(IfTrue,  "If True");
        RegisterInput(IfFalse, "If False");

        // Register bool input manually
        ConditionPin.ViewModel.Name    = "Cond";
        ConditionPin.ViewModel.PinType = "bool";
        AllPins.Add(ConditionPin.ViewModel);

        // Effective condition: use connected pin value when connected, otherwise fallback to IsActive
        var effectiveCondition = Observable.CombineLatest(
            ConditionPin.Value,
            this.WhenAnyValue(x => x.IsActive),
            (pinVal, manual) => ConditionPin.ViewModel.IsConnected ? pinVal : manual);

        Output.Value = Observable
            .CombineLatest(IfTrue.Value, IfFalse.Value, effectiveCondition,
                (t, f, c) => c == true ? t : f)
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(Observable.Return)
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }
}
