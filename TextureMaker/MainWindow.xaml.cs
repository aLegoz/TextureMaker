using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Win32;
using ModernWpf;
using TextureMaker.Core;
using TextureMaker.Graph;
using TextureMaker.Nodes.Base;
using TextureMaker.Nodes.Output;
using TextureMaker.Views.Nodes;

namespace TextureMaker;

public partial class MainWindow : Window
{
    private GraphViewModel _graph = new();
    private int _nodeCount = 0;
    private string? _currentFilePath;
    private bool _isDirty;
    private IObservable<TextureData?>? _currentPreviewObs;

    // Float card drag state
    private bool _isFloatDragging;
    private Point _floatDragOffset;

    // Snap state
    private bool _snapLeft, _snapRight, _snapTop, _snapBottom;

    public MainWindow()
    {
        ThemeManager.Current.ApplicationTheme = ApplicationTheme.Dark;
        InitializeComponent();
        NodeGraph.SetGraph(_graph);
        NodeGraph.SelectionChanged += OnSelectionChanged;
        FloatCanvas.SizeChanged    += FloatCanvas_SizeChanged;
        Loaded  += OnLoaded;
        Closing += OnClosing;
        SubscribeGraphDirty();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var s = AppSettings.Load();
        FloatCard.Width  = s.PreviewWidth;
        FloatCard.Height = s.PreviewHeight;

        _snapLeft   = s.SnapLeft;
        _snapRight  = s.SnapRight;
        _snapTop    = s.SnapTop;
        _snapBottom = s.SnapBottom;

        double maxLeft = Math.Max(0, FloatCanvas.ActualWidth  - FloatCard.Width);
        double maxTop  = Math.Max(0, FloatCanvas.ActualHeight - FloatCard.Height);

        double left = Math.Clamp(s.PreviewLeft, 0, maxLeft);
        double top  = Math.Clamp(s.PreviewTop,  0, maxTop);

        // Re-apply snap so card sits exactly at the edge
        if (_snapLeft)   left = 0;
        if (_snapRight)  left = maxLeft;
        if (_snapTop)    top  = 0;
        if (_snapBottom) top  = maxTop;

        Canvas.SetLeft(FloatCard, left);
        Canvas.SetTop(FloatCard,  top);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!PromptSaveIfDirty())
        {
            e.Cancel = true;
            return;
        }

        new AppSettings
        {
            PreviewLeft   = Canvas.GetLeft(FloatCard),
            PreviewTop    = Canvas.GetTop(FloatCard),
            PreviewWidth  = FloatCard.ActualWidth  > 0 ? FloatCard.ActualWidth  : FloatCard.Width,
            PreviewHeight = FloatCard.ActualHeight > 0 ? FloatCard.ActualHeight : FloatCard.Height,
            SnapLeft      = _snapLeft,
            SnapRight     = _snapRight,
            SnapTop       = _snapTop,
            SnapBottom    = _snapBottom,
        }.Save();
    }

    private void OnSelectionChanged(GraphNodeViewModel? node)
    {
        IObservable<TextureData?>? obs = null;
        if (node is TextureNodeViewModel tn)      obs = tn.Output.Value!;
        else if (node is SaveNodeViewModel sn)    obs = sn.InputTexture.Value;
        _currentPreviewObs = obs;

        if (obs != null) FloatPreviewPanel.BindToOutput(obs);
        else             FloatPreviewPanel.ClearPreview();
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

    // ── Dirty tracking ────────────────────────────────────────────────

    private void SubscribeGraphDirty()
    {
        _graph.Nodes.CollectionChanged       += OnGraphChanged;
        _graph.Connections.CollectionChanged += OnGraphChanged;
    }

    private void OnGraphChanged(object? sender,
        System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => _isDirty = true;

    /// <summary>
    /// Prompts to save if there are unsaved changes.
    /// Returns true if it is safe to proceed (saved or discarded), false if the user cancelled.
    /// </summary>
    private bool PromptSaveIfDirty()
    {
        if (!_isDirty) return true;

        var result = MessageBox.Show(
            "The project has unsaved changes.\nSave before continuing?",
            "Unsaved Changes",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Cancel) return false;

        if (result == MessageBoxResult.Yes)
        {
            SaveProject_Click(this, new RoutedEventArgs());
            return !_isDirty; // false if Save As was cancelled
        }

        return true; // No — discard changes
    }

    // ── File menu handlers ────────────────────────────────────────────

    private void NewProject_Click(object sender, RoutedEventArgs e)
    {
        if (!PromptSaveIfDirty()) return;

        _graph.Connections.Clear();
        _graph.Nodes.Clear();
        FloatPreviewPanel.ClearPreview();
        _currentFilePath = null;
        _nodeCount = 0;
        _isDirty = false;
        UpdateTitle();
        StatusText.Text = "New project";
        FileNameText.Text = "New Project";
    }

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        if (!PromptSaveIfDirty()) return;

        var dlg = new OpenFileDialog { Filter = "TextureMaker Project|*.txmk" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            _graph = ProjectSerializer.Load(dlg.FileName);
            NodeGraph.SetGraph(_graph);
            NodeGraph.SelectionChanged += OnSelectionChanged;
            SubscribeGraphDirty();
            FloatPreviewPanel.ClearPreview();
            _currentFilePath = dlg.FileName;
            _nodeCount = _graph.Nodes.Count;
            _isDirty = false;
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
            _isDirty = false;
            StatusText.Text = $"Saved: {Path.GetFileName(_currentFilePath!)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save project:\n{ex.Message}", "Save Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateTitle()
    {
        var name = _currentFilePath == null ? null : Path.GetFileName(_currentFilePath);
        Title = name == null ? "TextureMaker" : $"TextureMaker — {name}";
        FileNameText.Text = name ?? "New Project";
    }

    private async void ExportAll_Click(object sender, RoutedEventArgs e)
    {
        var nodes = _graph.Nodes.OfType<SaveNodeViewModel>()
            .Where(n => n.LastTexture != null && n.ResolveFullPath() != null)
            .ToList();

        if (nodes.Count == 0)
        {
            StatusText.Text = "No Save nodes ready to export.";
            return;
        }

        ExportProgress.Maximum = nodes.Count;
        ExportProgress.Value = 0;
        ExportProgress.Visibility = Visibility.Visible;

        int ok = 0, fail = 0;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            var path = n.ResolveFullPath()!;
            StatusText.Text = $"Exporting {i + 1}/{nodes.Count}: {Path.GetFileName(path)}…";
            try
            {
                await Task.Run(() => SaveNodeView.SaveTexture(n, path));
                ok++;
            }
            catch { fail++; }
            ExportProgress.Value = i + 1;
        }

        ExportProgress.Visibility = Visibility.Collapsed;
        StatusText.Text = fail == 0
            ? $"Exported {ok} texture{(ok != 1 ? "s" : "")}."
            : $"Exported {ok}, failed {fail}.";
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

        _snapLeft   = left <= snap;              if (_snapLeft)   left = 0;
        _snapRight  = maxLeft - left <= snap;    if (_snapRight)  left = maxLeft;
        _snapTop    = top  <= snap;              if (_snapTop)    top  = 0;
        _snapBottom = maxTop  - top  <= snap;    if (_snapBottom) top  = maxTop;

        Canvas.SetLeft(FloatCard, left);
        Canvas.SetTop(FloatCard,  top);
    }

    private void FloatHeader_MouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left) return;
        _isFloatDragging = false;
        ((UIElement)sender).ReleaseMouseCapture();
    }

    // Reposition card on canvas resize so snapped edges stay snapped
    private void FloatCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
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

    // ── Float card resize ─────────────────────────────────────────────

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        FloatCard.Width  = Math.Max(200, FloatCard.Width  + e.HorizontalChange);
        FloatCard.Height = Math.Max(150, FloatCard.Height + e.VerticalChange);
    }
}
