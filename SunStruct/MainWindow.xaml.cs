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

        // Static instance for access from other pages
        public static MainWindow Current { get; private set; }

        public MainWindow()
        {
            // Set the static reference
            Current = this;

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

        // Public method that can be called from other pages to navigate back to home
        public void NavigateToHome()
        {
            System.Diagnostics.Debug.WriteLine("NavigateToHome called");

            // Clear the frame content
            if (MainFrame != null)
            {
                MainFrame.Content = null;
            }

            // Show the home UI
            ShowHomeUI();

            System.Diagnostics.Debug.WriteLine("Home UI should now be visible");
        }

        private void ShowHomeUI()
        {
            if (Content is Grid rootGrid)
            {
                var homeGrid = rootGrid.FindName("HomeGrid") as Grid;
                if (homeGrid != null)
                {
                    homeGrid.Visibility = Microsoft.UI.Xaml.Visibility.Visible;
                    System.Diagnostics.Debug.WriteLine("HomeGrid visibility set to Visible");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: HomeGrid not found");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ERROR: Root content is not a Grid");
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

        private async void NewDesignButton_Click(object sender, RoutedEventArgs e)
        {
            // Create a dialog to ask for project name
            ContentDialog projectNameDialog = new ContentDialog
            {
                Title = "New Project",
                PrimaryButtonText = "Create",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            // Create content for the dialog
            StackPanel dialogContent = new StackPanel();
            TextBlock textBlock = new TextBlock
            {
                Text = "Enter project name:",
                Margin = new Thickness(0, 0, 0, 10)
            };
            TextBox projectNameTextBox = new TextBox
            {
                PlaceholderText = "Project Name",
                MinWidth = 300
            };
            dialogContent.Children.Add(textBlock);
            dialogContent.Children.Add(projectNameTextBox);
            projectNameDialog.Content = dialogContent;

            // Show the dialog and wait for user input
            ContentDialogResult result = await projectNameDialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Get the project name from the text box
                string projectName = projectNameTextBox.Text.Trim();

                // If the name is empty, use a default name
                if (string.IsNullOrEmpty(projectName))
                {
                    projectName = "New Project";
                }

                // Create a new project with the entered name
                var newProject = new Project
                {
                    Name = projectName,
                    Description = "San Francisco, CA 94105",
                    IsStarred = false
                };

                System.Diagnostics.Debug.WriteLine($"Creating project with name: {projectName}");

                // Check if MainFrame is null
                if (MainFrame == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: MainFrame is null");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Attempting to navigate to ProjectDesignPage");
                    MainFrame.Navigate(typeof(ProjectDesignPage), newProject);

                    // Hide the home UI
                    HideHomeUI();
                }
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