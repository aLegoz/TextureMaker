using System.IO;
using System.Windows;
using System.Windows.Controls;
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

    public MainWindow()
    {
        InitializeComponent();
        NodeGraph.SetGraph(_graph);
        NodeGraph.SelectionChanged += OnSelectionChanged;
    }

    private void OnSelectionChanged(GraphNodeViewModel? node)
    {
        if (node is TextureNodeViewModel tn)
            PreviewPanel.BindToOutput(tn.Output.Value!);
        else if (node is SaveNodeViewModel sn)
            PreviewPanel.BindToOutput(sn.InputTexture.Value);
        else
            PreviewPanel.ClearPreview();
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
        if (_currentFilePath == null)
            SaveAsProject_Click(sender, e);
        else
            DoSave();
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
}
