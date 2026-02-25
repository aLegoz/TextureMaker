using System.Reactive.Linq;
using ReactiveUI;
using TextureMaker.Core;
using TextureMaker.Graph;

namespace TextureMaker.Nodes.Output;

public class SaveNodeViewModel : GraphNodeViewModel
{
    public InputPin<TextureData> InputTexture { get; } = new();
    public InputPin<string>      RootFolder   { get; } = new() { Name = "Path" };

    private TextureData? _lastTexture;
    public TextureData? LastTexture
    {
        get => _lastTexture;
        private set => this.RaiseAndSetIfChanged(ref _lastTexture, value);
    }

    private string? _currentRoot;
    public string? CurrentRoot
    {
        get => _currentRoot;
        private set => this.RaiseAndSetIfChanged(ref _currentRoot, value);
    }

    private string _fileName = "";
    public string FileName
    {
        get => _fileName;
        set => this.RaiseAndSetIfChanged(ref _fileName, value);
    }

    public SaveNodeViewModel()
    {
        Name = "Save";
        InputTexture.Name = "Image";
        InputTexture.ViewModel.Name = "Image";
        AllPins.Add(InputTexture.ViewModel);

        RootFolder.ViewModel.Name = "Path";
        AllPins.Add(RootFolder.ViewModel);

        InputTexture.Value
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(tex => LastTexture = tex);

        RootFolder.Value
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(root => CurrentRoot = root);
    }

    /// <summary>Resolves the full save path from FileName + CurrentRoot.</summary>
    public string? ResolveFullPath()
    {
        if (string.IsNullOrWhiteSpace(FileName)) return null;
        if (!string.IsNullOrWhiteSpace(CurrentRoot) && !System.IO.Path.IsPathRooted(FileName))
            return System.IO.Path.Combine(CurrentRoot, FileName);
        if (System.IO.Path.IsPathRooted(FileName))
            return FileName;
        return null;
    }
}
