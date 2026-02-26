using System.Reactive.Linq;
using ReactiveUI;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Logic;

public class GateNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new() { Name = "Image" };
    public InputPin<bool>        ConditionPin { get; } = new() { Name = "Cond" };

    private bool _isActive;
    public bool IsActive
    {
        get => _isActive;
        set => this.RaiseAndSetIfChanged(ref _isActive, value);
    }

    public GateNodeViewModel() : base("Gate")
    {
        RegisterInput(InputTexture, "Image");

        ConditionPin.ViewModel.Name    = "Cond";
        ConditionPin.ViewModel.PinType = "bool";
        AllPins.Add(ConditionPin.ViewModel);

        var effectiveCondition = Observable.CombineLatest(
            ConditionPin.Value,
            this.WhenAnyValue(x => x.IsActive),
            (pinVal, manual) => ConditionPin.ViewModel.IsConnected ? pinVal : manual);

        Output.Value = Observable
            .CombineLatest(InputTexture.Value, effectiveCondition,
                (tex, allow) => allow == true ? tex : null)
            .Throttle(TimeSpan.FromMilliseconds(50), RxApp.TaskpoolScheduler)
            .Select(Observable.Return)
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }
}
