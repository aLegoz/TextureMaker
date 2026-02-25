using System.Reactive.Linq;
using ReactiveUI;
using TextureMaker.Graph;

namespace TextureMaker.Nodes.Sources;

public class RootFolderNodeViewModel : GraphNodeViewModel
{
    private string _folderPath = "";
    public string FolderPath
    {
        get => _folderPath;
        set => this.RaiseAndSetIfChanged(ref _folderPath, value);
    }

    public OutputPin<string> FolderOutput { get; } = new() { Name = "Path" };

    public RootFolderNodeViewModel()
    {
        Name = "Root Folder";
        FolderOutput.ViewModel.Name = "Path";
        FolderOutput.ViewModel.PinType = "folder";
        AllPins.Add(FolderOutput.ViewModel);

        FolderOutput.Value = this.WhenAnyValue(x => x.FolderPath)
            .Select(p => string.IsNullOrWhiteSpace(p) ? (string?)null : p);
    }
}
