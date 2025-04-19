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

        // Reference to the main frame for navigation
        private Frame MainFrame { get; set; }

        public MainWindow()
        {
            // Initialize data before InitializeComponent
            InitializeProjects();

            this.InitializeComponent();

            // Find and initialize the main frame
            if (Content is FrameworkElement rootElement)
            {
                rootElement.DataContext = ViewModel;

                // Find the frame by name from the root element
                MainFrame = rootElement.FindName("ContentFrame") as Frame;

                if (MainFrame == null)
                {
                    System.Diagnostics.Debug.WriteLine("ContentFrame not found, creating new Frame");

                    // If Frame isn't found, create one and add it to the visual tree
                    MainFrame = new Frame();

                    if (rootElement is Grid rootGrid)
                    {
                        // Add frame as the first child so it's behind other UI elements
                        rootGrid.Children.Insert(0, MainFrame);
                    }
                }

                // Add a handler for navigated event to detect when going back
                MainFrame.Navigated += MainFrame_Navigated;
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

        private void MainFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
        {
            // When navigation happens, check if we're back to the home page (null or empty source)
            if (e.SourcePageType == null || e.SourcePageType.FullName == string.Empty)
            {
                // We navigated back to home, show the home grid
                ShowHomeUI();
            }
            else if (e.SourcePageType == typeof(ProjectDesignPage))
            {
                // We navigated to ProjectDesignPage, hide the home grid
                HideHomeUI();
            }
        }
        private void ShowHomeUI()
        {
            if (Content is Grid rootGrid)
            {
                var homeGrid = rootGrid.FindName("HomeGrid") as Grid;
                if (homeGrid != null)
                {
                    homeGrid.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                }
            }
        }

        private void HideHomeUI()
        {
            if (Content is Grid rootGrid)
            {
                var homeGrid = rootGrid.FindName("HomeGrid") as Grid;
                if (homeGrid != null)
                {
                    homeGrid.Visibility = Microsoft.UI.Xaml.Visibility.Collapsed;
                }
            }
        }

        private void NewDesignButton_Click(object sender, RoutedEventArgs e)
        {
            // Add debug output
            System.Diagnostics.Debug.WriteLine("New Design button clicked");

            // Create a default project for new designs
            var newProject = new Project
            {
                Name = "New Project",
                Description = "San Francisco, CA 94105",
                IsStarred = false
            };

            System.Diagnostics.Debug.WriteLine("Project created");

            // Check if MainFrame is null
            if (MainFrame == null)
            {
                System.Diagnostics.Debug.WriteLine("ERROR: MainFrame is null");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Attempting to navigate to ProjectDesignPage");
                MainFrame.Navigate(typeof(ProjectDesignPage), newProject);

                // Hide the home UI - this will actually be handled by the Navigated event now
                // but we can call it directly for clarity
                HideHomeUI();
            }
        }

        private void ProjectsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            // TODO: Implement project selection functionality
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