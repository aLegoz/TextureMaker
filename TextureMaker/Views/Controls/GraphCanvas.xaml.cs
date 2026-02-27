using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using System.Windows;
using ReactiveUI;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using TextureMaker.Graph;

namespace TextureMaker.Views.Controls;

public partial class GraphCanvas : UserControl
{
    private GraphViewModel? _graph;
    private readonly Dictionary<GraphNodeViewModel, NodeCardView> _nodeViews = new();
    private readonly Dictionary<ConnectionViewModel, Path> _connectionPaths = new();
    private readonly Dictionary<CommentBlockViewModel, CommentBlockView> _commentViews = new();
    private bool _isLoading;

    // Connection drag state
    private PinViewModel? _dragSourcePin;
    private NodeCardView? _dragSourceCard;
    private bool _isDragging;
    private Point _dragStart;

    // Pan / Zoom state
    private readonly TranslateTransform _pan   = new();
    private readonly ScaleTransform     _scale = new(1, 1);
    private bool _isPanning;
    private Point _panStart;

    private const double MinScale = 0.1;
    private const double MaxScale = 4.0;

    public event Action<GraphNodeViewModel?>? SelectionChanged;

    public GraphCanvas()
    {
        InitializeComponent();
        var tg = new TransformGroup();
        tg.Children.Add(_scale);
        tg.Children.Add(_pan);
        RootGrid.RenderTransform = tg;
        MouseWheel += OnMouseWheel;
        MouseDown  += OnMouseDown;
        MouseMove  += OnMouseMove;
        MouseUp    += OnMouseUp;
    }

    // ── Zoom via Ctrl+Wheel (cursor as pivot) ─────────────────────────
    // ── Pan  via Wheel / Shift+Wheel ─────────────────────────────────
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            double factor   = e.Delta > 0 ? 1.12 : 1.0 / 1.12;
            double newScale = Math.Clamp(_scale.ScaleX * factor, MinScale, MaxScale);
            double actual   = newScale / _scale.ScaleX;

            var mouse = e.GetPosition(this);   // screen-space pivot
            _pan.X = mouse.X + (_pan.X - mouse.X) * actual;
            _pan.Y = mouse.Y + (_pan.Y - mouse.Y) * actual;
            _scale.ScaleX = newScale;
            _scale.ScaleY = newScale;
        }
        else
        {
            double delta = e.Delta / 120.0 * 40;
            if (Keyboard.Modifiers == ModifierKeys.Shift)
                _pan.X += delta;
            else
                _pan.Y += delta;
        }
        e.Handled = true;
    }

    // ── Pan via middle-button drag ────────────────────────────────────
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        _isPanning = true;
        _panStart  = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning) return;
        var pos = e.GetPosition(this);
        _pan.X += pos.X - _panStart.X;
        _pan.Y += pos.Y - _panStart.Y;
        _panStart = pos;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle || !_isPanning) return;
        _isPanning = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    public void SetGraph(GraphViewModel graph)
    {
        if (_graph != null)
        {
            _graph.Nodes.CollectionChanged -= Nodes_Changed;
            _graph.Connections.CollectionChanged -= Connections_Changed;
            _graph.Comments.CollectionChanged -= Comments_Changed;
        }

        _graph = graph;
        NodeCanvas.Children.Clear();
        ConnectionCanvas.Children.Clear();
        _nodeViews.Clear();
        _connectionPaths.Clear();

        // Clear and rebuild comment views
        foreach (var cv in _commentViews.Values) CommentCanvas.Children.Remove(cv);
        _commentViews.Clear();

        foreach (var node in graph.Nodes)
            AddNodeView(node);

        foreach (var conn in graph.Connections)
            AddConnectionPath(conn);

        _isLoading = true;
        foreach (var comment in graph.Comments)
            AddCommentView(comment);
        _isLoading = false;

        graph.Nodes.CollectionChanged += Nodes_Changed;
        graph.Connections.CollectionChanged += Connections_Changed;
        graph.Comments.CollectionChanged += Comments_Changed;

        // Defer refresh until after layout pass so pin sockets are loaded
        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, RefreshConnections);
    }

    private void Nodes_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var n in _nodeViews.Keys.ToList()) RemoveNodeView(n);
            return;
        }
        if (e.NewItems != null)
            foreach (GraphNodeViewModel n in e.NewItems) AddNodeView(n);
        if (e.OldItems != null)
            foreach (GraphNodeViewModel n in e.OldItems) RemoveNodeView(n);
    }

    private void Connections_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var c in _connectionPaths.Keys.ToList()) RemoveConnectionPath(c);
            return;
        }
        if (e.NewItems != null)
            foreach (ConnectionViewModel c in e.NewItems) AddConnectionPath(c);
        if (e.OldItems != null)
            foreach (ConnectionViewModel c in e.OldItems) RemoveConnectionPath(c);
    }

    private void AddNodeView(GraphNodeViewModel node)
    {
        var card = new NodeCardView(node);
        card.PinDragStarted += OnPinDragStarted;
        card.PinDropped += OnPinDropped;
        card.NodeSelected += OnNodeSelected;
        card.NodeDeleteRequested += n => DeleteNode(n);
        Canvas.SetLeft(card, node.Position.X);
        Canvas.SetTop(card, node.Position.Y);
        NodeCanvas.Children.Add(card);
        _nodeViews[node] = card;

        // Keep canvas position in sync with ViewModel
        node.WhenAnyValue(n => n.Position)
            .ObserveOn(ReactiveUI.RxApp.MainThreadScheduler)
            .Subscribe(pos =>
            {
                Canvas.SetLeft(card, pos.X);
                Canvas.SetTop(card, pos.Y);
                UpdateConnectionsForNode(node);
            });
    }

    private void RemoveNodeView(GraphNodeViewModel node)
    {
        if (_nodeViews.TryGetValue(node, out var card))
        {
            NodeCanvas.Children.Remove(card);
            _nodeViews.Remove(node);
        }
    }

    private void AddConnectionPath(ConnectionViewModel conn)
    {
        var path = new Path
        {
            Stroke = PinStroke(conn.OutputPin.PinType),
            StrokeThickness = 2,
            IsHitTestVisible = false
        };
        ConnectionCanvas.Children.Add(path);
        _connectionPaths[conn] = path;
        UpdateConnectionPath(conn, path);
    }

    private static Brush PinStroke(string pinType) => pinType switch
    {
        "color"  => new SolidColorBrush(Color.FromRgb(80, 200, 100)),
        "folder" => new SolidColorBrush(Colors.LightSteelBlue),
        "bool"   => new SolidColorBrush(Color.FromRgb(180, 100, 210)),
        _        => new SolidColorBrush(Color.FromRgb(200, 160, 50)),
    };

    private void RemoveConnectionPath(ConnectionViewModel conn)
    {
        if (_connectionPaths.TryGetValue(conn, out var path))
        {
            ConnectionCanvas.Children.Remove(path);
            _connectionPaths.Remove(conn);
        }
    }

    private void UpdateConnectionPath(ConnectionViewModel conn, Path path)
    {
        // Find pin screen positions
        var startPt = GetPinCenter(conn.OutputPin);
        var endPt   = GetPinCenter(conn.InputPin);
        if (startPt == null || endPt == null) return;

        var geo = BuildBezier(startPt.Value, endPt.Value);
        path.Data = geo;
    }

    private void UpdateConnectionsForNode(GraphNodeViewModel node)
    {
        if (_graph == null) return;
        foreach (var conn in _graph.Connections)
        {
            if (_nodeViews.TryGetValue(node, out var card)
                && (card.ContainsPin(conn.OutputPin) || card.ContainsPin(conn.InputPin)))
            {
                if (_connectionPaths.TryGetValue(conn, out var path))
                    UpdateConnectionPath(conn, path);
            }
        }
    }

    private Point? GetPinCenter(PinViewModel pin)
    {
        foreach (var kv in _nodeViews)
        {
            var pt = kv.Value.GetPinCenter(pin, ConnectionCanvas);
            if (pt != null) return pt;
        }
        return null;
    }

    // ── Connection drag ────────────────────────────────────────────────
    private void OnPinDragStarted(NodeCardView card, PinViewModel pin, Point screenPt)
    {
        _dragSourcePin  = pin;
        _dragSourceCard = card;
        _isDragging     = true;
        _dragStart      = ConnectionCanvas.PointFromScreen(screenPt);
        DragPath.Data       = new PathGeometry(); // clear stale data from previous drag
        DragPath.Stroke     = PinStroke(pin.PinType);
        DragPath.Visibility = Visibility.Visible;
        // Enable hit-testing so CaptureMouse works, then capture
        DragCanvas.IsHitTestVisible = true;
        DragCanvas.CaptureMouse();
        DragCanvas.MouseMove  += DragCanvas_MouseMove;
        DragCanvas.MouseUp    += DragCanvas_MouseUp;
    }

    private void DragCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || _dragSourcePin == null) return;
        var cur = e.GetPosition(ConnectionCanvas);
        DragPath.Data = BuildBezier(_dragStart, cur);
    }

    private void DragCanvas_MouseUp(object sender, MouseButtonEventArgs e)
    {
        DragPath.Visibility = Visibility.Collapsed;
        DragCanvas.ReleaseMouseCapture();
        DragCanvas.IsHitTestVisible = false;
        DragCanvas.MouseMove -= DragCanvas_MouseMove;
        DragCanvas.MouseUp   -= DragCanvas_MouseUp;

        // Hit-test NodeCanvas to find the target pin socket under the cursor
        if (_isDragging && _dragSourcePin != null && _dragSourceCard != null && _graph != null)
        {
            var mousePos  = e.GetPosition(NodeCanvas);
            var targetPin = FindPinAtPoint(mousePos);
            if (targetPin != null)
            {
                var targetCard = _nodeViews.Values.FirstOrDefault(c => c.ContainsPin(targetPin));
                if (targetCard != null)
                    TryFinishConnection(targetCard, targetPin);
            }
        }

        _isDragging     = false;
        _dragSourcePin  = null;
        _dragSourceCard = null;
    }

    // Called from NodeCardView.PinDropped (non-drag click path — kept for symmetry)
    private void OnPinDropped(NodeCardView targetCard, PinViewModel targetPin, Point screenPt)
    {
        if (!_isDragging || _dragSourcePin == null || _graph == null) return;
        TryFinishConnection(targetCard, targetPin);
    }

    private void TryFinishConnection(NodeCardView targetCard, PinViewModel targetPin)
    {
        var src = _dragSourcePin!;
        PinViewModel? outPin = src.IsOutput ? src : (targetPin.IsOutput ? targetPin : null);
        PinViewModel? inPin  = !src.IsOutput ? src : (!targetPin.IsOutput ? targetPin : null);
        if (outPin == null || inPin == null || outPin == inPin) return;

        if (_dragSourceCard!.TryConnectDataPin(outPin, targetCard, inPin))
        {
            _graph!.Connect(outPin, inPin, () => { });
            // Defer refresh so layout is complete before measuring pin positions
            Dispatcher.BeginInvoke(() =>
            {
                foreach (var kv in _connectionPaths)
                    UpdateConnectionPath(kv.Key, kv.Value);
            });
        }
    }

    private PinViewModel? FindPinAtPoint(Point posOnNodeCanvas)
    {
        PinViewModel? found = null;
        VisualTreeHelper.HitTest(
            NodeCanvas,
            null,
            result =>
            {
                if (result.VisualHit is System.Windows.Shapes.Ellipse el && el.Tag is PinViewModel pin)
                {
                    found = pin;
                    return HitTestResultBehavior.Stop;
                }
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(posOnNodeCanvas));
        return found;
    }

    private void OnNodeSelected(GraphNodeViewModel node)
    {
        if (_graph != null) _graph.SelectedNode = node;
        SelectionChanged?.Invoke(node);
    }

    public void DeleteSelectedNode()
    {
        if (_graph?.SelectedNode != null)
            DeleteNode(_graph.SelectedNode);
    }

    private void DeleteNode(GraphNodeViewModel node)
    {
        if (_graph == null) return;

        // Disconnect data pins on nodes whose input was fed by this node's output
        foreach (var conn in _graph.Connections.Where(c => node.AllPins.Contains(c.OutputPin)).ToList())
        {
            var inputNode = _graph.Nodes.FirstOrDefault(n => n != node && n.AllPins.Contains(conn.InputPin));
            if (inputNode != null)
                NodeCardView.DisconnectDataPin(inputNode, conn.InputPin);
        }

        _graph.RemoveNode(node);
        SelectionChanged?.Invoke(null);
    }

    // ── Comment block management ──────────────────────────────────────

    private void Comments_Changed(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var c in _commentViews.Keys.ToList()) RemoveCommentView(c);
            return;
        }
        if (e.NewItems != null)
            foreach (CommentBlockViewModel c in e.NewItems) AddCommentView(c);
        if (e.OldItems != null)
            foreach (CommentBlockViewModel c in e.OldItems) RemoveCommentView(c);
    }

    private void AddCommentView(CommentBlockViewModel vm)
    {
        var view = new CommentBlockView(vm);
        view.DeleteRequested += c => _graph?.Comments.Remove(c);
        view.Width  = vm.Width;
        view.Height = vm.Height;
        Canvas.SetLeft(view, vm.Position.X);
        Canvas.SetTop(view,  vm.Position.Y);
        CommentCanvas.Children.Add(view);
        _commentViews[vm] = view;

        // Loaded comments with content start in preview (read) mode
        if (_isLoading && !string.IsNullOrWhiteSpace(vm.Body))
            view.SetPreviewMode(true);
    }

    private void RemoveCommentView(CommentBlockViewModel vm)
    {
        if (_commentViews.TryGetValue(vm, out var view))
        {
            CommentCanvas.Children.Remove(view);
            _commentViews.Remove(vm);
        }
    }

    // ── Bezier helper ─────────────────────────────────────────────────
    private static PathGeometry BuildBezier(Point start, Point end)
    {
        double dx = Math.Abs(end.X - start.X) * 0.5;
        var p1 = new Point(start.X + dx, start.Y);
        var p2 = new Point(end.X - dx, end.Y);
        var seg = new BezierSegment(p1, p2, end, true);
        var figure = new PathFigure { StartPoint = start };
        figure.Segments.Add(seg);
        return new PathGeometry(new[] { figure });
    }

    // ── Public: refresh all connections (call after layout pass) ──────
    public void RefreshConnections()
    {
        if (_graph == null) return;
        foreach (var kv in _connectionPaths)
            UpdateConnectionPath(kv.Key, kv.Value);
    }
}
