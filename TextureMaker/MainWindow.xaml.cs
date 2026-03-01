using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using TextureMaker.Views.Dialogs;
using TextureMaker.Views.Nodes;

namespace TextureMaker;

public partial class MainWindow : Window
{
    public ObservableCollection<ProjectTab> Tabs { get; } = new();
    private ProjectTab _activeTab = null!;
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
        TabStrip.ItemsSource = Tabs;

        var first = new ProjectTab();
        Tabs.Add(first);
        SubscribeGraphDirty(first);
        SwitchToTab(first);

        NodeGraph.SelectionChanged += OnSelectionChanged;
        FloatCanvas.SizeChanged    += FloatCanvas_SizeChanged;
        Loaded  += OnLoaded;
        Closing += OnClosing;
    }

    // ── Tab management ────────────────────────────────────────────────

    private void SwitchToTab(ProjectTab newTab)
    {
        if (_activeTab != null)
        {
            _activeTab.IsActive = false;
            var (s, px, py) = NodeGraph.GetViewTransform();
            _activeTab.SavedScale = s;
            _activeTab.SavedPanX  = px;
            _activeTab.SavedPanY  = py;
        }

        _activeTab = newTab;
        newTab.IsActive = true;

        NodeGraph.SetGraph(newTab.Graph);
        NodeGraph.SetViewTransform(newTab.SavedScale, newTab.SavedPanX, newTab.SavedPanY);
        FloatPreviewPanel.ClearPreview();
        UpdateTitle();
        StatusText.Text = newTab.FilePath != null
            ? $"Opened: {Path.GetFileName(newTab.FilePath)}"
            : "New project";
    }

    private void CloseTab(ProjectTab tab)
    {
        if (tab.IsDirty)
        {
            SwitchToTab(tab);
            if (!PromptSaveIfDirty()) return;
        }

        int idx = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        if (Tabs.Count == 0)
        {
            var empty = new ProjectTab();
            Tabs.Add(empty);
            SubscribeGraphDirty(empty);
            SwitchToTab(empty);
        }
        else
        {
            SwitchToTab(Tabs[Math.Min(idx, Tabs.Count - 1)]);
        }
    }

    private void Tab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ProjectTab tab)
            SwitchToTab(tab);
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is ProjectTab tab)
            CloseTab(tab);
        e.Handled = true; // prevent Tab_Click from also firing
    }

    private void NewTab_Click(object sender, RoutedEventArgs e)
    {
        var tab = new ProjectTab();
        Tabs.Add(tab);
        SubscribeGraphDirty(tab);
        SwitchToTab(tab);
    }

    // ── Lifecycle ─────────────────────────────────────────────────────

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

        if (_snapLeft)   left = 0;
        if (_snapRight)  left = maxLeft;
        if (_snapTop)    top  = 0;
        if (_snapBottom) top  = maxTop;

        Canvas.SetLeft(FloatCard, left);
        Canvas.SetTop(FloatCard,  top);
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        foreach (var tab in Tabs.Where(t => t.IsDirty).ToList())
        {
            SwitchToTab(tab);
            if (!PromptSaveIfDirty()) { e.Cancel = true; return; }
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
        if (e.Key == Key.Delete && _activeTab.Graph.SelectedNode != null)
        {
            NodeGraph.DeleteSelectedNode();
            StatusText.Text = "Node deleted";
            e.Handled = true;
        }
        else if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            NewTab_Click(this, new RoutedEventArgs());
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
        else if (e.Key == Key.W && Keyboard.Modifiers == ModifierKeys.Control)
        {
            CloseTab(_activeTab);
            e.Handled = true;
        }
        else if (e.Key == Key.Tab && Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (Tabs.Count > 1)
            {
                int idx = (Tabs.IndexOf(_activeTab) + 1) % Tabs.Count;
                SwitchToTab(Tabs[idx]);
            }
            e.Handled = true;
        }
    }

    // ── Node creation ─────────────────────────────────────────────────

    private void AddNode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;
        string key = btn.Tag?.ToString() ?? string.Empty;
        var node = NodeFactory.Create(key);
        if (node == null) return;

        node.Position = new System.Windows.Point(
            (_activeTab.NodeCount % 4) * 230 + 30,
            (_activeTab.NodeCount / 4) * 200 + 30);
        _activeTab.NodeCount++;

        _activeTab.Graph.AddNode(node);
        StatusText.Text = $"Added: {node.Name}";
    }

    private void AddComment_Click(object sender, RoutedEventArgs e)
    {
        var comment = new CommentBlockViewModel
        {
            Position = new System.Windows.Point(
                (_activeTab.NodeCount % 4) * 230 + 30,
                (_activeTab.NodeCount / 4) * 200 + 30)
        };
        _activeTab.Graph.Comments.Add(comment);
        _activeTab.IsDirty = true;
        StatusText.Text = "Added: Comment";
    }

    // ── Dirty tracking ────────────────────────────────────────────────

    private void SubscribeGraphDirty(ProjectTab tab)
    {
        var g = tab.Graph;
        g.Nodes.CollectionChanged       += (_, _) => tab.IsDirty = true;
        g.Connections.CollectionChanged += (_, _) => tab.IsDirty = true;
        g.Nodes.CollectionChanged       += (_, e) =>
        {
            if (e.NewItems == null) return;
            foreach (GraphNodeViewModel node in e.NewItems)
                SubscribeNodePropertyChanged(node, tab);
        };
        foreach (var node in g.Nodes)
            SubscribeNodePropertyChanged(node, tab);
    }

    private static void SubscribeNodePropertyChanged(GraphNodeViewModel node, ProjectTab tab)
        => node.PropertyChanged += (_, pe) =>
        {
            if (pe.PropertyName is nameof(GraphNodeViewModel.IsSelected)
                                or nameof(GraphNodeViewModel.HasError)) return;
            tab.IsDirty = true;
        };

    /// <summary>
    /// Prompts to save if the active tab has unsaved changes.
    /// Returns true if safe to proceed (saved or discarded), false if cancelled.
    /// </summary>
    private bool PromptSaveIfDirty()
    {
        if (!_activeTab.IsDirty) return true;

        var dlg = new UnsavedChangesDialog(this);
        dlg.ShowDialog();

        if (dlg.Result == UnsavedResult.Cancel)  return false;
        if (dlg.Result == UnsavedResult.Save)
        {
            SaveProject_Click(this, new RoutedEventArgs());
            return !_activeTab.IsDirty; // false if Save As was cancelled
        }

        return true; // Discard
    }

    // ── File menu handlers ────────────────────────────────────────────

    private void OpenProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog { Filter = "TextureMaker Project|*.txmk" };
        if (dlg.ShowDialog() != true) return;
        try
        {
            var graph = ProjectSerializer.Load(dlg.FileName);

            ProjectTab tab;
            // Reuse current tab if it is empty and untouched
            if (_activeTab.FilePath == null && !_activeTab.IsDirty && _activeTab.Graph.Nodes.Count == 0)
            {
                int idx = Tabs.IndexOf(_activeTab);
                Tabs.Remove(_activeTab);
                tab = new ProjectTab(graph);
                Tabs.Insert(idx, tab);
            }
            else
            {
                tab = new ProjectTab(graph);
                Tabs.Add(tab);
            }

            tab.FilePath  = dlg.FileName;
            tab.NodeCount = graph.Nodes.Count;
            tab.IsDirty   = false;
            SubscribeGraphDirty(tab);
            SwitchToTab(tab);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to load project:\n{ex.Message}", "Load Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveProject_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab.FilePath == null) SaveAsProject_Click(sender, e);
        else DoSave();
    }

    private void SaveAsProject_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new SaveFileDialog { Filter = "TextureMaker Project|*.txmk", DefaultExt = "txmk" };
        if (dlg.ShowDialog() != true) return;
        _activeTab.FilePath = dlg.FileName;
        DoSave();
        UpdateTitle();
    }

    private void DoSave()
    {
        try
        {
            ProjectSerializer.Save(_activeTab.Graph, _activeTab.FilePath!);
            _activeTab.IsDirty = false;
            StatusText.Text = $"Saved: {Path.GetFileName(_activeTab.FilePath!)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save project:\n{ex.Message}", "Save Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void UpdateTitle()
    {
        var name = _activeTab.FilePath == null ? null : Path.GetFileName(_activeTab.FilePath);
        Title = name == null ? "TextureMaker" : $"TextureMaker — {name}";
        FileNameText.Text = name ?? "New Project";
    }

    private async void ExportAll_Click(object sender, RoutedEventArgs e)
    {
        var nodes = _activeTab.Graph.Nodes.OfType<SaveNodeViewModel>()
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
