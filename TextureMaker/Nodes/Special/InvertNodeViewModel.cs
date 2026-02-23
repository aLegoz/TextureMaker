using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp.Processing;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Special;

public class InvertNodeViewModel : TextureNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();

    public InvertNodeViewModel() : base("Invert")
    {
        RegisterInput(InputTexture, "Input");
        Output.Value = InputTexture.Value
            .Select(tex => Observable.Start(() => Apply(tex), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);
    }

    private static TextureData? Apply(TextureData? input)
    {
        if (input == null) return null;
        var clone = input.Clone();
        clone.Image.Mutate(ctx => ctx.Invert());
        return clone;
    }
}
