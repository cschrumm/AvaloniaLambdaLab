using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using Service.Library;
using Image = Avalonia.Controls.Image;

namespace AvaloniaLambdaLab;

public class DataPoint
{
    public double X { get; set; }
    public double Y { get; set; }
}
    public partial class MainWindow : Window
    {
   
        private bool _isRunning = false;
        private ObservableCollection<DataPoint> _graphData = new();
        private DispatcherTimer _timer = null!;
        public MainGuiBackend GuiBackend { get; set; } = new MainGuiBackend();
        
        public Dictionary<Instance, Repository> RepositoriesPerInstance = new();
        // public ISeries[] Series { get; set; } = Array.Empty<ISeries>();
        
        //private Dictionary<string, List<float>> _chart_data = new();

        public MainWindow()
        {
            /*
             *  
             */
            DataContext = this;
            InitializeComponent();
            InitializeData();
            

        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            GuiBackend.Startup();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(4);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            
        }
       
        
        private void CallChangeOnGui(string nm)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(0);
               
            });
        }

        int lastErrorCount = 0;
        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Update a UI element, for example, a TextBlock
            // MyTextBlock.Text = DateTime.Now.ToLongTimeString(); 
            
            if(lastErrorCount > 0)
            {
                Console.WriteLine($"Last error: {lastErrorCount}");
                lastErrorCount--;
                return;
            }
            
            Task.Run(async () =>
            {
               //this._timer.Stop(); 
               try
               {
                   await GuiBackend.UpdateSeries();
               }
               catch (Exception exception)
               {
                   // if there is an error, wait a bit longer before trying again
                   lastErrorCount += 30;
                   Console.WriteLine(exception);
                   //throw;
               }
               
               //this._timer.Start();
            });
            

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning != value)
                {
                    _isRunning = value;
                    
                }
            }
        }

        public bool IsNotRunning => !IsRunning;

        public ObservableCollection<DataPoint> GraphData
        {
            get => _graphData;
            set
            {
                _graphData = value;
               
            }
        }

        public async void SelectFile()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            
            if(topLevel is null) return;

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false
            });
        }

        private async Task<string> FindDirectory()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            
            if(topLevel is null) return "";
            
            var drs = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select Folder",
                AllowMultiple = false
            });
            
            var drlist = drs.ToList();
            if (drlist.Count > 0)
            {
                return drlist[0].Path.LocalPath;
            }
            return String.Empty;
        }

        private void InitializeData()
        {
            // Initialize sample data for the graph
            GraphData = new ObservableCollection<DataPoint>();
            
            // Add some sample data points
            var random = new Random();
            for (int i = 0; i < 10; i++)
            {
                GraphData.Add(new DataPoint { X = i, Y = random.Next(10, 100) });
            }
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                await Task.Run(async () =>
                {
                   await GuiBackend.StartInstance();
                });
                
                
                

            }
            catch (Exception ex)
            {
                //Console.WriteLine(exception);
                GuiBackend.LogViewMessage += "ERROR STARTING INSTANCE\n" + ex.Message + "\n";
                
            }
           
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            IsRunning = false;
            _timer.Stop();
            // Add logic for what happens when stopping
        }
        
        private void LaunchButton_Click(object sender, RoutedEventArgs e)
        {
            // Logic to launch an instance using selected parameters
            //_backend.LaunchBrowser();
            
            var ins = sender as Button;
            
            if (ins != null && ins.DataContext is Instance)
            {
                var instance = ins.DataContext as Instance;
                if (instance != null)
                {
                    GuiBackend.LaunchBrowser(instance);
                }
            }
        }
        
        private async void CopyToServerButton_Click(object sender, RoutedEventArgs e)
        {
            var ins = sender as Button;
            
            if (ins != null && ins.DataContext is Instance)
            {
                var instance = ins.DataContext as Instance;
                if (instance != null)
                {
                    var fldr = await FindDirectory();
                    if (!string.IsNullOrEmpty(fldr))
                    {
                        var t= Task.Run(async () =>
                        {
                            await GuiBackend.ZipAndUpload(fldr, instance);
                           
                        });
                    }
                    //GuiBackend.CopyToServer(instance);
                }
            }
        }

        private void Machines_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 1 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                // Process selection
                var selected = combo.SelectedItem as InstanceNameDesc;
                if (selected != null)
                {
                    
                    GuiBackend.MakeImageSelection(selected);

                }
                // Add your logic here
            }
        }

        private void FileSystem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 2 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                
                var selectedItem = combo.SelectedItem.ToString();
                
            }
        }
        
        private async Task<string> PickFile()
        {
            var topLevel = TopLevel.GetTopLevel(this);
            
            if(topLevel is null) return "";
            
            string path = "";
            // Start async operation to open the dialog.
            var filesTask = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false
            });
           
            var files = filesTask.ToList();
            if (files.Count > 0)
            {
                var file = files[0];
                path = file.Path.LocalPath;
            }

            return path;
        }
        
        private async void SelectKey_Click(object sender, RoutedEventArgs e)
        {
            // Logic to launch an instance using selected parameters

            var path = await PickFile();
            if (!string.IsNullOrEmpty(path))
            {
               Console.WriteLine("Selected Path: " + path);
            }

            GuiBackend.setKeyPath(path);
            
            
        }
        
        private async void OnUnload_Window(object? sender, RoutedEventArgs e)
        {
             await Task.Delay(0);
             GuiBackend.Shutdown();
        }

        private void SshKey_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 3 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                
                var selectedItem = combo.SelectedItem.ToString();
              
            }
        }
        
        private void ListImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 3 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                
                var selectedItem = combo.SelectedItem.ToString();
                
            }
        }

        private async void DeleteInstance_Click(object sender, RoutedEventArgs e)
        {
            // Logic to launch an instance using selected parameters
            var ins = sender as Button;
            await Task.Delay(0);
            if (ins != null && ins.DataContext is not null && ins.DataContext is Instance)
            {
                await GuiBackend.DeleteServer((ins.DataContext as Instance)!);
            }
            
        }

        private async void Repo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox combo 
                && combo.SelectedItem != null 
                && combo.DataContext is Instance instance && combo.SelectedItem is Repository repo)
            {
                    if (this.RepositoriesPerInstance.ContainsKey(instance) == false)
                    {
                        this.RepositoriesPerInstance.Add(instance, repo);
                    }
                    else
                    {
                        this.RepositoriesPerInstance[instance] = repo;
                    }
            }
            
            await Task.CompletedTask;
            
        }
        
        private async void Deploy_Repository_Click(object sender, RoutedEventArgs e)
        {
            // Logic to launch an instance using selected parameters
            var ins = sender as Button;
            await Task.Delay(0);
            if (ins != null && ins.DataContext is not null 
                            && ins.DataContext is Instance instance
                            && this.RepositoriesPerInstance.ContainsKey((ins.DataContext as Instance)!))
            {
                   var repo = this.RepositoriesPerInstance[instance];
                   await GuiBackend.InstallRepo(repo, instance,GuiBackend.PathToKey);
            }
            
        }
        
        private async Task<bool> AskDelete(string name)
        {
            await Task.Delay(0);
            var msg = $"Are you sure you want to delete instance: {name}?";
            
            var rslt =MessageBoxManager.GetMessageBoxStandard("Caption", "Are you sure you would like to delete appender_replace_page_1?",
                    ButtonEnum.YesNo);
            //rslt.
            return false;
        }
        
        private void Unload_Window(object? sender, RoutedEventArgs e)
        {
            GuiBackend.Shutdown();
        }
       

        private void CallOnGui(Action action)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                //LogViewMessage += s + "\n";
                await Task.Delay(0);
                action();
            });
        }
       
    }