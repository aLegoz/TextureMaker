using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace TextureMaker.Graph;

/// <summary>Output pin that holds a reactive value observable.</summary>
public class OutputPin<T>
{
    public string Name { get; set; } = "Output";
    public IObservable<T?>? Value { get; set; }
    public PinViewModel ViewModel { get; } = new() { IsOutput = true };
}

/// <summary>Input pin whose Value tracks a connected OutputPin, or returns default when disconnected.</summary>
public class InputPin<T>
{
    private readonly BehaviorSubject<IObservable<T?>> _source =
        new(System.Reactive.Linq.Observable.Return<T?>(default));

    public string Name { get; set; } = "Input";
    public IObservable<T?> Value => _source.Switch();
    public PinViewModel ViewModel { get; } = new() { IsOutput = false };

    public OutputPin<T>? ConnectedOutput { get; private set; }

    public void Connect(OutputPin<T> output)
    {
        ConnectedOutput = output;
        _source.OnNext(output.Value ?? System.Reactive.Linq.Observable.Return<T?>(default));
        ViewModel.IsConnected = true;
        output.ViewModel.IsConnected = true;
    }

    public void Disconnect()
    {
        ConnectedOutput = null;
        _source.OnNext(System.Reactive.Linq.Observable.Return<T?>(default));
        ViewModel.IsConnected = false;
    }
}
