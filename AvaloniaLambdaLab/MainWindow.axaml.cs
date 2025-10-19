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
        
        public ISeries[] Series { get; set; } = Array.Empty<ISeries>();
        
        private Dictionary<string, List<float>> _chart_data = new();

        
        // bound with the back ground ...
        /*
       
        */
       // public string PathToKey { get; set; } = "";
        
        /* Log information to screen */
        
        
        

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
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            //GuiBackend.OnLogMessage += MonitorLog;
            //GuiBackend.OnInstanceLaunched += LaunchNotice;
            
            //GuiBackend.PropertyChanged += Backend;
            
            //_backend.LoadAllData();
            //LoadData();
            GuiBackend.Startup();
        }

        
        
        /*
        public void LaunchNotice(string msg)
        {
            IsRunning = GuiBackend.IsRunning;
            LogViewMessage += msg + "\n";
            this.CallChangeOnGui(nameof(IsRunning));
        }
        */
        
        
        
        

        private void CallChangeOnGui(string nm)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(0);
                
                
               
            });
        }

        
        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Update a UI element, for example, a TextBlock
            // MyTextBlock.Text = DateTime.Now.ToLongTimeString(); 
            List<ISeries> series = new List<ISeries>();

            if (GuiBackend.RunningInstances.Count == 0) return;
            
            
            
            var mss = _chart_data.Keys.Where(x => !GuiBackend.RunningInstances.Any(i => i.Id == x)).ToList();

            foreach (var ms in mss)
            {
                _chart_data.Remove(ms);
                //_chart_data.Remove(ms.Key);
            }

            var to_remove = new List<Instance>();
            foreach (var i in GuiBackend.RunningInstances)
            {
                
                    
                    Task.Run(async () =>
                    {
                        var ins = await GuiBackend.GetInstance(i.Id);

                        if (ins is null)
                        {
                            to_remove.Add(i);
                        }
                        else
                        {
                            i.Status = ins.Status;

                            if (i.Status != "active")
                                return;
                            
                            var sts =await GuiBackend.GetInstanceData(i);

                            if (!_chart_data.ContainsKey(i.Id))
                            {
                                _chart_data.Add(i.Id,new  List<float>());
                            }

                            if (sts is null)
                                return;
                            var lst = _chart_data[i.Id];

                            var ttl =(float)sts.GpuStats.Sum(g => g.UtilizationPercentage) /
                                      (sts.GpuStats.Count == 0 ? 1 : sts.GpuStats.Count);
                            //var tst = (float)(new Random()).NextDouble() * 100;
                            lst.Add(ttl);
                            if (lst.Count > 40)
                            {
                                lst.RemoveAt(0);
                            }
                            series.Add(new LineSeries<float>
                            {
                                Name = i.Name,
                                Values = lst,
                                Fill = null
                            });
                        }
                        
                    }).Wait();
                    
                
            }
            foreach (var r in to_remove)
            {
               GuiBackend.RunningInstances.Remove(r);
            }
            this.Series = series.ToArray();
            
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
                await GuiBackend.StartInstance();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(exception);
                GuiBackend.LogViewMessage += "ERROR STARTING INSTANCE\n" + ex.Message + "\n";
                //this.CallChangeOnGui(nameof(LogViewM
                //LogViewMessage += "ERROR STARTING: " + exception.Message + "\n";
                //this.CallChangeOnGui(nameof(LogViewMessage));
                return;
                // throw;
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
            // Logic to launch an instance using selected parameters
            //_backend.LaunchBrowser();
            
            var ins = sender as Button;
            
            if (ins != null && ins.DataContext is Instance)
            {
                var instance = ins.DataContext as Instance;
                if (instance != null)
                {
                    var fldr = await FindDirectory();
                    if (!string.IsNullOrEmpty(fldr))
                    {
                        GuiBackend.ZipAndUpload(fldr, instance);
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
                // Process selection
                var selectedItem = combo.SelectedItem.ToString();
                // Add your logic here
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
                // Process selection
                var selectedItem = combo.SelectedItem.ToString();
                // Add your logic here
            }
        }
        
        private void ListImages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 3 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                // Process selection
                var selectedItem = combo.SelectedItem.ToString();
                // Add your logic here
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
            // copy back to backend
            //Utils.CopyProperties(this, GuiBackend);
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