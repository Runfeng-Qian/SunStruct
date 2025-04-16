using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;

namespace SunStruct
{
    // ViewModel class to hold our data
    public class MainViewModel
    {
        public ObservableCollection<Project> Projects { get; } = new();
    }

    public sealed partial class MainWindow : Window
    {
        // Create a ViewModel instance instead of putting data directly on the Window
        private MainViewModel ViewModel { get; } = new MainViewModel();

        public MainWindow()
        {
            // Initialize data before InitializeComponent
            InitializeProjects();

            this.InitializeComponent();

            // Set DataContext on the root Grid (not on Window directly)
            // This is the key change - setting DataContext on a FrameworkElement, not the Window
            if (Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;
            }

            // Setting title explicitly since WinUI 3 doesn't inherit from window title
            this.Title = "SunStruct";

            // You would handle sizing here - WinUI 3 doesn't auto-apply Height/Width from XAML
            var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(windowHandle);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            appWindow.Resize(new Windows.Graphics.SizeInt32(900, 700));
        }

        private void InitializeProjects()
        {
            // Add projects to the ViewModel instead of directly to the Window
            ViewModel.Projects.Add(new Project { Name = "Residential Solar", Description = "123 Valley Rd, San Jose, CA 95123", IsStarred = true });
            ViewModel.Projects.Add(new Project { Name = "Commercial Demo", Description = "456 Market St, San Francisco, CA 94105", IsStarred = false });
            ViewModel.Projects.Add(new Project { Name = "Solar Farm", Description = "789 Desert Ave, Las Vegas, NV 89123", IsStarred = true });
        }
    }

    // Note: In WinUI 3, best practice is to create a separate file for each class
    public class Project
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsStarred { get; set; }
    }

    public class BoolToStarBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isStarred && isStarred)
            {
                return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 241, 196, 15)); // #F1C40F - Yellow
            }
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, 220, 220, 220)); // #DCDCDC - Gray
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}