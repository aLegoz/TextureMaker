using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using ReactiveUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;

namespace TextureMaker.Nodes.Sources;

public class ImageLoadNodeViewModel : TextureNodeViewModel
{
    private string? _filePath;
    public string? FilePath
    {
        get => _filePath;
        set => this.RaiseAndSetIfChanged(ref _filePath, value);
    }

    private string? _currentRoot;
    public string? CurrentRoot
    {
        get => _currentRoot;
        private set => this.RaiseAndSetIfChanged(ref _currentRoot, value);
    }

    public InputPin<string> RootFolder { get; } = new() { Name = "Path" };

    private FileSystemWatcher? _watcher;

    public ImageLoadNodeViewModel() : base("Image Load")
    {
        RootFolder.ViewModel.Name = "Path";
        RootFolder.ViewModel.PinType = "folder";
        AllPins.Add(RootFolder.ViewModel);

        // Track current root for the Browse dialog
        RootFolder.Value
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(r => CurrentRoot = r);

        var resolvedPath = Observable.CombineLatest(
            this.WhenAnyValue(x => x.FilePath),
            RootFolder.Value,
            (path, root) => ResolvePath(path, root));

        Output.Value = resolvedPath
            .Select(full => Observable.Start(() => Load(full), RxApp.TaskpoolScheduler))
            .Switch()
            .ObserveOn(RxApp.MainThreadScheduler);

        // Set up FileSystemWatcher whenever the resolved path changes
        resolvedPath
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(WatchFile);
    }

    private void WatchFile(string? fullPath)
    {
        _watcher?.Dispose();
        _watcher = null;

        if (string.IsNullOrWhiteSpace(fullPath))
        {
            HasError = false;
            return;
        }

        // Immediate check
        HasError = !File.Exists(fullPath);

        var dir  = Path.GetDirectoryName(fullPath);
        var file = Path.GetFileName(fullPath);
        if (string.IsNullOrEmpty(dir) || string.IsNullOrEmpty(file) || !Directory.Exists(dir))
            return;

        try
        {
            _watcher = new FileSystemWatcher(dir, file)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                EnableRaisingEvents = true
            };
            _watcher.Deleted += (_, _)    => RxApp.MainThreadScheduler.Schedule(() => HasError = true);
            _watcher.Created += (_, _)    => RxApp.MainThreadScheduler.Schedule(() => HasError = false);
            _watcher.Renamed += (_, _)    => RxApp.MainThreadScheduler.Schedule(() => HasError = !File.Exists(fullPath));
            _watcher.Changed += (_, _)    => RxApp.MainThreadScheduler.Schedule(() => HasError = !File.Exists(fullPath));
        }
        catch { /* directory may vanish between the check and watcher creation */ }
    }

    public static string? ResolvePath(string? path, string? root)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!string.IsNullOrWhiteSpace(root) && !Path.IsPathRooted(path))
            return Path.Combine(root, path);
        return path;
    }

    private static TextureData? Load(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        try { return new TextureData(Image.Load<Rgba32>(path)); }
        catch { return null; }
    }
}
