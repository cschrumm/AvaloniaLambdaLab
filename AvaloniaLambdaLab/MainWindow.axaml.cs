using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
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
        
        public ObservableCollection<InstanceNameDesc> Instances { get; set; }
        public ObservableCollection<Filesystem> Filesystems { get; set; }
        public ObservableCollection<SSHKey> SshKeys { get; set; }
        
        public ObservableCollection<Image> Images { get; set; }
        public InstanceNameDesc SelectedInstance { get; set; }
        public Filesystem SelectedFilesystem { get; set; }
        public SSHKey SelectedSshKey { get; set; }
        
        public Image SelectedImage { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializeData();
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(3); 
        }
        
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

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
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

        private void DropDown1_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 1 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                // Process selection
                var selectedItem = combo.SelectedItem.ToString();
                // Add your logic here
            }
        }

        private void DropDown2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 2 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                // Process selection
                var selectedItem = combo.SelectedItem.ToString();
                // Add your logic here
            }
        }

        private void DropDown3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle dropdown 3 selection change
            if (sender is ComboBox combo && combo.SelectedItem != null)
            {
                // Process selection
                var selectedItem = combo.SelectedItem.ToString();
                // Add your logic here
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }