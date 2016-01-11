using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Build.Evaluation;
using Microsoft.Msagl.Drawing;
using Microsoft.Msagl.WpfGraphControl;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Color = Microsoft.Msagl.Drawing.Color;

namespace MsbuildDependencyVisualizer.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        private readonly GraphViewer _graphViewer = new GraphViewer();
        private string _fileName;
        private readonly Dictionary<string, string> _processedFiles = new Dictionary<string, string>();
        private readonly Dictionary<string, List<string>> _dependencies = new Dictionary<string, List<string>>();
        private Graph _graph;

        public MainWindow()
        {
            InitializeComponent();

            _graphViewer.MouseUp += OnMouseCursorChanged;
            _graphViewer.BindToPanel(dockPanel);
#if DEBUG
            txtPath.Text = @"C:\Users\Administrator\Downloads\DevOps\Build\Scripts\Code\PackageRelease.proj";
#endif
        }

        private void OnMouseCursorChanged(object sender, MsaglMouseEventArgs e)
        {
            foreach (var en in _graphViewer.Entities)
            {
                var node = en as IViewerNode;

                if (node != null)
                {
                    //CLEAR
                    node.Node.Attr.FillColor = Color.Gray;

                    node.OutEdges.ToList().ForEach(x =>
                    {
                        node.Node.Attr.FillColor = Color.LightGreen;
                        x.Edge.SourceNode.Attr.FillColor = Color.LightGreen;
                        x.Edge.Attr.Color = Color.Black;
                    });
                    node.InEdges.ToList().ForEach(x =>
                    {
                        node.Node.Attr.FillColor = Color.LightGreen;
                        x.Edge.TargetNode.Attr.FillColor = Color.LightGreen;
                        x.Edge.Attr.Color = Color.Black;
                    });
                }
            }
            foreach (var en in _graphViewer.Entities)
            {
                var node = en as IViewerNode;

                if (en.MarkedForDragging && node != null)
                {
                    //MARK
                    node.Node.Attr.FillColor = Color.Yellow;

                    node.OutEdges.ToList().ForEach(x =>
                    {
                        x.Edge.TargetNode.Attr.FillColor = Color.PaleVioletRed;
                        x.Edge.Attr.Color = Color.Red;
                    });

                    node.InEdges.ToList().ForEach(x =>
                    {
                        x.Edge.SourceNode.Attr.FillColor = Color.LightBlue;
                        x.Edge.Attr.Color = Color.Blue;
                    });
                }
            }
        }

        private void OnBrowseClick(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.CheckFileExists = true;
            openFileDialog.Multiselect = false;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Filter = "All Files (*.*)|*.*";
            var showDialog = openFileDialog.ShowDialog();
            if (showDialog.HasValue && showDialog.Value)
            {
                _fileName = openFileDialog.FileName;
                txtPath.Text = _fileName;
            }
        }

        private async void OnStartClick(object sender, RoutedEventArgs e)
        {
            _processedFiles.Clear();
            _dependencies.Clear();

            if (string.IsNullOrWhiteSpace(txtPath.Text))
            {
                MessageBox.Show(this, "Please select a file", "MSBuild Dependency Visualizer", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            var controller = await this.ShowProgressAsync("Please wait...", "Processing files..");

            try
            {
                await TraverseProjects(txtPath.Text, true);
                _graph = new Graph();

                double i = 1.0;
                foreach (KeyValuePair<string, List<string>> item in _dependencies)
                {
                    double percent = i / _dependencies.Count;
                    controller.SetProgress(percent);

                    controller.SetMessage($"Processing... {item.Key}");
                    await Task.Delay(50);
                    var node = _graph.AddNode(item.Key);
                    node.Attr.FillColor = Color.LightGreen;

                    foreach (var child in item.Value)
                    {
                        var edge = _graph.AddEdge(node.LabelText, child);
                    }
                    i += 1.0;
                }

                _graph.Attr.LayerDirection = LayerDirection.LR;
                _graphViewer.Graph = _graph;

                await controller.CloseAsync();
            }
            catch (Exception exception)
            {
                await controller.CloseAsync();
                await this.ShowMessageAsync("Oops! Error occurred", exception.Message);
            }
        }

        private async void OnAboutClick(object sender, RoutedEventArgs e)
        {
            await this.ShowMessageAsync("MSBuild Dependency Visualizer", "By Utkarsh Shigihalli - CM Team");
        }

        public async Task TraverseProjects(string rootPath, bool recursive = false)
        {
            ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
            if (_processedFiles.ContainsKey(rootPath))
            {
                return;
            }

            var fileName = Path.GetFileName(rootPath);
            _processedFiles.Add(rootPath, fileName);
            Project project = new Project(rootPath);
            var nonImportedProjects = project.Imports.Where(x => !x.IsImported).ToList();
            var importedProject = new List<string>();

            foreach (var import in nonImportedProjects)
            {
                var importingProject = import.ImportedProject;
                var projectPath = Path.GetFileName(importingProject.ProjectFileLocation.File);
                importedProject.Add(projectPath);
            }

            if (!_dependencies.ContainsKey(fileName))
            {
                _dependencies.Add(fileName, importedProject);
            }

            if (recursive)
            {
                foreach (var import in nonImportedProjects)
                {
                    await TraverseProjects(import.ImportedProject.ProjectFileLocation.File, recursive);
                }
            }
        }
    }
}