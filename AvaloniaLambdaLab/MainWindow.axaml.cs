using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
   
        private bool _isRunning = false;
        private ObservableCollection<DataPoint> _graphData;
        private DispatcherTimer _timer;
        private MainGuiBackend _backend = new MainGuiBackend();

        
        // bound with the back ground ...
        public ObservableCollection<InstanceNameDesc> Instances { get; set; } = new();
        public ObservableCollection<Filesystem> Filesystems { get; set; } = new();
        public ObservableCollection<SSHKey> SshKeys { get; set; } = new();
        public ObservableCollection<Service.Library.Image> Images { get; set; } = new();
        public InstanceNameDesc SelectedInstance { get; set; }
        public Filesystem SelectedFilesystem { get; set; }
        public SSHKey SelectedSshKey { get; set; }
        public Service.Library.Image SelectedImage { get; set; }
        
        public ObservableCollection<Instance> RunningInstances { get; set; } = new();
        
        public string PathToKey { get; set; } = "";
        
        /* Log information to screen */
        public string LogViewMessage { get; set; } = "";
        
        

        public MainWindow()
        {
            /*
             *  
             */
            InitializeComponent();
            DataContext = this;
            InitializeData();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3);
            _backend.OnLogMessage += MonitorLog;
            _backend.OnInstanceLaunched += LaunchNotice;
            
            _backend.PropertyChanged += Backend;
            
            //_backend.LoadAllData();
            //LoadData();
            _backend.Startup();
        }

        private void Backend(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                var th = this.Instances;
                if (Debugger.IsAttached)
                {
                    Console.WriteLine($"Backend Property Changed: {e.PropertyName}");
                }
                // backend copy to me...
                this.SetPropertyValue(e.PropertyName, _backend);
                
                this.OnPropertyChanged(e.PropertyName);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                //throw;
            }
            
        }
        
        public void LaunchNotice(string msg)
        {
            IsRunning = _backend.IsRunning;
            LogViewMessage += msg + "\n";
            this.CallChangeOnGui(nameof(IsRunning));
        }
        
        
        
        public void MonitorLog(string msg)
        {
            LogViewMessage += msg + "\n";
            this.CallChangeOnGui(nameof(LogViewMessage));
        }
        
        private void ClearLog()
        {
            LogViewMessage = "";
            this.CallChangeOnGui(nameof(LogViewMessage));
        }

        private void CallChangeOnGui(string nm)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                OnPropertyChanged(nm);
            });
        }

        /*
        private void LoadData()
        {
            Task.Run(async () =>
            {
                var intypes = _backend.InStanceTypes();
                var filesys = _backend.ListFileSystems();
                var keys = _backend.ListSshKeys();
                var images = _backend.ListImages();
                await Task.WhenAll(intypes, filesys, keys, images);
                
                Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    // UI operations here, e.g.,
                    //myTextBlock.Text = "Updated from background thread.";
                    foreach (var x in intypes.Result)
                    {
                        Instances.Add(x);
                    }
                    foreach (var x in filesys.Result)
                    {
                        Filesystems.Add(x);
                    }
                    
                    foreach (var x in keys.Result)
                    {
                        SshKeys.Add(x);
                    }
                    foreach (var x in images.Result)
                    {
                        Images.Add(x);
                    }
                    
                    OnPropertyChanged(nameof(Instances));
                    OnPropertyChanged(nameof(Filesystems));
                    OnPropertyChanged(nameof(Images));
                });
                

            });
        }
        */
        
        private void Timer_Tick(object sender, EventArgs e)
        {
            // Update a UI element, for example, a TextBlock
            // MyTextBlock.Text = DateTime.Now.ToLongTimeString(); 
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
                    OnPropertyChanged(nameof(IsRunning));
                    OnPropertyChanged(nameof(IsNotRunning));
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
                OnPropertyChanged(nameof(GraphData));
            }
        }

        public async void SelectFile()
        {
            var topLevel = TopLevel.GetTopLevel(this);

            // Start async operation to open the dialog.
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open Text File",
                AllowMultiple = false
            });
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
                await _backend.StartInstance();
            }
            catch (Exception exception)
            {
                //Console.WriteLine(exception);
                LogViewMessage += "ERROR STARTING: " + exception.Message + "\n";
                this.CallChangeOnGui(nameof(LogViewMessage));
                return;
                // throw;
            }
            IsRunning = true;
            _timer.Start();
            
            
            // Add logic for what happens when starting
            // For example, you might start a timer or begin data collection
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
                    _backend.LaunchBrowser(instance);
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
                    var imgs = _backend.CompantibleImages(selected);
                    this.Images = new ObservableCollection<Service.Library.Image>(imgs);

                    
                    OnPropertyChanged(nameof(Images));

                    if (this.Images.Count > 0)
                    {
                        this.SelectedImage = this.Images[^1];
                        OnPropertyChanged(nameof(SelectedImage));
                    }


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
            
            _backend.PathToKey = path;
            this.PathToKey = path;
            OnPropertyChanged(nameof(PathToKey));
        }
        
        private async void OnUnload_Window(object? sender, RoutedEventArgs e)
        {
             _backend.Shutdown();
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

            if (ins != null && ins.DataContext is Instance)
            {
                _backend.DeleteServer(ins.DataContext as Instance);
            }
            
        }
        
        private async Task<bool> AskDelete(string name)
        {
            var msg = $"Are you sure you want to delete instance: {name}?";
            
            var rslt =MessageBoxManager.GetMessageBoxStandard("Caption", "Are you sure you would like to delete appender_replace_page_1?",
                    ButtonEnum.YesNo);
            //rslt.
            return false;
        }
        
        private void Unload_Window(object? sender, RoutedEventArgs e)
        {
            // copy back to backend
            Utils.CopyProperties(this, _backend);
            _backend.Shutdown();
        }
        
        
        public event PropertyChangedEventHandler PropertyChanged;

        private void CallOnGui(Action action)
        {
            Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                //LogViewMessage += s + "\n";
                action();
                
            });
        }
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }