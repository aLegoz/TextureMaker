using System.Collections.ObjectModel;
using ReactiveUI;

namespace TextureMaker.Graph;

public class GraphViewModel : ReactiveObject
{
    public ObservableCollection<GraphNodeViewModel> Nodes { get; } = new();
    public ObservableCollection<ConnectionViewModel> Connections { get; } = new();
    public ObservableCollection<CommentBlockViewModel> Comments { get; } = new();

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

    /// <summary>
    /// Returns true if adding a connection from <paramref name="outputPin"/> to
    /// <paramref name="inputPin"/> would create a cycle in the graph.
    /// </summary>
    public bool WouldCreateCycle(PinViewModel outputPin, PinViewModel inputPin)
    {
        var outNode = Nodes.FirstOrDefault(n => n.AllPins.Contains(outputPin));
        var inNode  = Nodes.FirstOrDefault(n => n.AllPins.Contains(inputPin));
        if (outNode == null || inNode == null) return false;
        if (outNode == inNode) return true; // self-loop

        // DFS downstream from inNode; if we reach outNode, the new edge creates a cycle.
        var visited = new HashSet<GraphNodeViewModel>();
        var stack   = new Stack<GraphNodeViewModel>();
        stack.Push(inNode);

        while (stack.Count > 0)
        {
            var node = stack.Pop();
            if (!visited.Add(node)) continue;

            foreach (var conn in Connections)
            {
                if (!node.AllPins.Contains(conn.OutputPin)) continue;
                var dst = Nodes.FirstOrDefault(n => n.AllPins.Contains(conn.InputPin));
                if (dst == null) continue;
                if (dst == outNode) return true;
                stack.Push(dst);
            }
        }

        return false;
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
