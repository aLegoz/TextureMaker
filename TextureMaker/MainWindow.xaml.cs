using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;
using TextureMaker.Nodes.Output;

namespace TextureMaker;

public partial class MainWindow : Window
{
    private GraphViewModel _graph = new();
    private int _nodeCount = 0;
    private string? _currentFilePath;

    // Float/dock state
    private IObservable<TextureData?>? _currentPreviewObs;
    private bool _isFloating;

    // Float card drag state
    private bool _isFloatDragging;
    private Point _floatDragOffset;

    // Snap state — which edges the card is currently locked to
    private bool _snapLeft, _snapRight, _snapTop, _snapBottom;

    public MainWindow()
    {
        InitializeComponent();
        NodeGraph.SetGraph(_graph);
        NodeGraph.SelectionChanged += OnSelectionChanged;
        FloatCanvas.SizeChanged    += FloatCanvas_SizeChanged;
        Loaded  += OnLoaded;
        Closing += OnClosing;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var s = AppSettings.Load();
        if (s.PreviewIsFloating)
        {
            // Float first (sets position/size from PreviewContainer), then override with saved values
            FloatPreviewButton_Click(this, new RoutedEventArgs());

            FloatCard.Width  = s.PreviewWidth;
            FloatCard.Height = s.PreviewHeight;

            double maxLeft = Math.Max(0, FloatCanvas.ActualWidth  - s.PreviewWidth);
            double maxTop  = Math.Max(0, FloatCanvas.ActualHeight - s.PreviewHeight);
            Canvas.SetLeft(FloatCard, Math.Clamp(s.PreviewLeft, 0, maxLeft));
            Canvas.SetTop(FloatCard,  Math.Clamp(s.PreviewTop,  0, maxTop));
        }
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        new AppSettings
        {
            PreviewIsFloating = _isFloating,
            PreviewLeft       = Canvas.GetLeft(FloatCard),
            PreviewTop        = Canvas.GetTop(FloatCard),
            PreviewWidth      = FloatCard.ActualWidth  > 0 ? FloatCard.ActualWidth  : FloatCard.Width,
            PreviewHeight     = FloatCard.ActualHeight > 0 ? FloatCard.ActualHeight : FloatCard.Height,
        }.Save();
    }

    private void OnSelectionChanged(GraphNodeViewModel? node)
    {
        IObservable<TextureData?>? obs = null;
        if (node is TextureNodeViewModel tn)      obs = tn.Output.Value!;
        else if (node is SaveNodeViewModel sn)    obs = sn.InputTexture.Value;
        _currentPreviewObs = obs;

        var target = _isFloating ? FloatPreviewPanel : PreviewPanel;
        if (obs != null) target.BindToOutput(obs);
        else             target.ClearPreview();
    }

    private void MainWindow_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _graph.SelectedNode != null)
        {
            NodeGraph.DeleteSelectedNode();
            StatusText.Text = "Node deleted";
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            NewProject_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenProject_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
        else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SaveProject_Click(this, new RoutedEventArgs());
            e.Handled = true;
        }
    }

    private void AddNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        string key = btn.Tag?.ToString() ?? string.Empty;
        var node = NodeFactory.Create(key);
        if (node == null) return;

        node.Position = new System.Windows.Point(
            (_nodeCount % 4) * 230 + 30,
            (_nodeCount / 4) * 200 + 30);
        _nodeCount++;

        _graph.AddNode(node);
        StatusText.Text = $"Added: {node.Name}";
    }

    // ── File menu handlers ────────────────────────────────────────────

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        _graph.Connections.Clear();
        _graph.Nodes.Clear();
        PreviewPanel.ClearPreview();
        FloatPreviewPanel.ClearPreview();
        _currentFilePath = null;
        _nodeCount = 0;
        UpdateTitle();
        StatusText.Text = "New project";
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "TextureMaker Project|*.txmk" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _graph = ProjectSerializer.Load(dlg.FileName);
            NodeGraph.SetGraph(_graph);
            NodeGraph.SelectionChanged += OnSelectionChanged;
            PreviewPanel.ClearPreview();
            FloatPreviewPanel.ClearPreview();
            _currentFilePath = dlg.FileName;
            _nodeCount = _graph.Nodes.Count;
            UpdateTitle();
            StatusText.Text = $"Loaded: {Path.GetFileName(dlg.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load project:\n{ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        if (_currentFilePath == null) SaveAsProject_Click(sender, e);
        else DoSave();
    }

    private void SaveAsProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "TextureMaker Project|*.txmk", DefaultExt = "txmk" };
        if (dlg.ShowDialog() != true) return;
        _currentFilePath = dlg.FileName;
        DoSave();
        UpdateTitle();
    }

    private void DoSave()
    {
        try
        {
            ProjectSerializer.Save(_graph, _currentFilePath!);
            StatusText.Text = $"Saved: {Path.GetFileName(_currentFilePath!)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save project:\n{ex.Message}", "Save Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateTitle() =>
        Title = _currentFilePath == null ? "TextureMaker" : $"TextureMaker — {Path.GetFileName(_currentFilePath)}";

    // ── Float / Dock preview ──────────────────────────────────────────

    private void FloatPreviewButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isFloating) { DockPreview(); return; }

        // Position float card where the docked panel currently sits
        var pt = PreviewContainer.TranslatePoint(new Point(0, 0), FloatCanvas);
        Canvas.SetLeft(FloatCard, pt.X);
        Canvas.SetTop(FloatCard, pt.Y);
        FloatCard.Width  = Math.Max(200, PreviewContainer.ActualWidth);
        FloatCard.Height = Math.Max(150, PreviewContainer.ActualHeight);

        // Collapse docked panel
        PreviewPanel.ClearPreview();
        PreviewContainer.Visibility = Visibility.Collapsed;
        PreviewSplitter.Visibility  = Visibility.Collapsed;
        PreviewColumn.MinWidth      = 0;
        PreviewColumn.Width         = new GridLength(0);
        SplitterColumn.Width        = new GridLength(0);

        // Show float card
        FloatCard.Visibility       = Visibility.Visible;
        FloatPreviewButton.Content = "Dock";
        _isFloating = true;

        if (_currentPreviewObs != null)
            FloatPreviewPanel.BindToOutput(_currentPreviewObs);
    }

    private void DockPreview()
    {
        FloatPreviewPanel.ClearPreview();
        FloatCard.Visibility = Visibility.Collapsed;

        SplitterColumn.Width        = new GridLength(5);
        PreviewColumn.MinWidth      = 250;
        PreviewColumn.Width         = new GridLength(1, GridUnitType.Star);
        PreviewSplitter.Visibility  = Visibility.Visible;
        PreviewContainer.Visibility = Visibility.Visible;
        FloatPreviewButton.Content  = "Float";
        _isFloating = false;

        if (_currentPreviewObs != null) PreviewPanel.BindToOutput(_currentPreviewObs);
        else                            PreviewPanel.ClearPreview();
    }

    // ── Float card drag ───────────────────────────────────────────────

    private void FloatHeader_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        _isFloatDragging = true;
        var pos = e.GetPosition(FloatCanvas);
        _floatDragOffset = new Point(
            pos.X - Canvas.GetLeft(FloatCard),
            pos.Y - Canvas.GetTop(FloatCard));
        ((UIElement)sender).CaptureMouse();
        e.Handled = true;
    }

    private void FloatHeader_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isFloatDragging) return;

        const double snap = 16;
        var pos = e.GetPosition(FloatCanvas);

        double maxLeft = Math.Max(0, FloatCanvas.ActualWidth  - FloatCard.ActualWidth);
        double maxTop  = Math.Max(0, FloatCanvas.ActualHeight - FloatCard.ActualHeight);

        double left = Math.Clamp(pos.X - _floatDragOffset.X, 0, maxLeft);
        double top  = Math.Clamp(pos.Y - _floatDragOffset.Y, 0, maxTop);

        // Snap to edges and record which edges are active
        _snapLeft   = left <= snap;             if (_snapLeft)   left = 0;
        _snapRight  = maxLeft - left <= snap;   if (_snapRight)  left = maxLeft;
        _snapTop    = top <= snap;              if (_snapTop)    top  = 0;
        _snapBottom = maxTop - top <= snap;     if (_snapBottom) top  = maxTop;

        Canvas.SetLeft(FloatCard, left);
        Canvas.SetTop(FloatCard,  top);
    }

    // Reposition card on canvas resize so snapped edges stay snapped
    private void FloatCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (FloatCard.Visibility != Visibility.Visible) return;

        double maxLeft = Math.Max(0, FloatCanvas.ActualWidth  - FloatCard.ActualWidth);
        double maxTop  = Math.Max(0, FloatCanvas.ActualHeight - FloatCard.ActualHeight);

        double left = Canvas.GetLeft(FloatCard);
        double top  = Canvas.GetTop(FloatCard);

        if (_snapRight)  left = maxLeft;
        if (_snapBottom) top  = maxTop;
        if (_snapLeft)   left = 0;
        if (_snapTop)    top  = 0;

        Canvas.SetLeft(FloatCard, Math.Clamp(left, 0, maxLeft));
        Canvas.SetTop(FloatCard,  Math.Clamp(top,  0, maxTop));
    }

    private void FloatHeader_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        _isFloatDragging = false;
        ((UIElement)sender).ReleaseMouseCapture();
    }

    // ── Float card resize ─────────────────────────────────────────────

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        FloatCard.Width  = Math.Max(200, FloatCard.Width  + e.HorizontalChange);
        FloatCard.Height = Math.Max(150, FloatCard.Height + e.VerticalChange);
    }
}
