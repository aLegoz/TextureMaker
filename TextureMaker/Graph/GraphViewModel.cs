using System.Collections.ObjectModel;
using ReactiveUI;

namespace TextureMaker.Graph;

public class GraphViewModel : ReactiveObject
{
    public ObservableCollection<GraphNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();

    private GraphNodeViewModel? _selectedNode;
    public GraphNodeViewModel? SelectedNode
    {
        get => _selectedNode;
        set
        {
            if (_selectedNode != null) _selectedNode.IsSelected = false;
            this.RaiseAndSetIfChanged(ref _selectedNode, value);
            if (_selectedNode != null) _selectedNode.IsSelected = true;
        }
    }

    public void AddNode(GraphNodeViewModel node)
    {
        Nodes.Add(node);
        SelectedNode = node;
    }

    public void Connect(PinViewModel outputPin, PinViewModel inputPin,
        Action connectData)
    {
        // Remove existing connection to this input pin
        var existing = Connections.FirstOrDefault(c => c.InputPin == inputPin);
        if (existing != null) Connections.Remove(existing);

        connectData();
        Connections.Add(new ConnectionViewModel(outputPin, inputPin));
    }

    public void RemoveNode(GraphNodeViewModel node)
    {
        if (SelectedNode == node) SelectedNode = null;
        var toRemove = Connections
            .Where(c => node.AllPins.Contains(c.OutputPin) || node.AllPins.Contains(c.InputPin))
            .ToList();
        foreach (var conn in toRemove)
            Connections.Remove(conn);
        Nodes.Remove(node);
    }
}
