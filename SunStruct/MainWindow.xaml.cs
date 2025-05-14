using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.IO;
using Windows.Storage;

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

            System.Diagnostics.Debug.WriteLine($"LocalFolder Path: {ApplicationData.Current.LocalFolder.Path}");
            System.Diagnostics.Debug.WriteLine($"Projects Directory: {Path.Combine(ApplicationData.Current.LocalFolder.Path, "Projects")}");

            // Initialize data before InitializeComponent
            // Note: We don't call InitializeProjects() here anymore
            // Instead we load projects asynchronously after initialization

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

            // Load projects asynchronously
            LoadProjectsAsync();
        }

        // Asynchronously load projects from XML files
        private async void LoadProjectsAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Loading projects from XML files...");

                // Clear current projects
                ViewModel.Projects.Clear();

                // Get projects from the ProjectManager
                var projects = await ProjectManager.Instance.LoadAllProjectsAsync();

                // If no projects were found, add sample projects
                if (projects.Count == 0)
                {
                    //System.Diagnostics.Debug.WriteLine("No projects found, adding sample projects");
                    //AddSampleProjects();
                }
                else
                {
                    // Add loaded projects to ViewModel
                    foreach (var project in projects)
                    {
                        ViewModel.Projects.Add(project);
                    }
                    System.Diagnostics.Debug.WriteLine($"Loaded {projects.Count} projects");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
                // If there's an error, add sample projects
                AddSampleProjects();
            }
        }

        // Add sample projects for first-time users
        private void AddSampleProjects()
        {
            ViewModel.Projects.Add(new Project
            {
                Name = "Residential Solar",
                Description = "123 Valley Rd, San Jose, CA 95123",
                Location = "San Jose, CA",
                IsStarred = true,
                CreatedDate = DateTime.Now.AddDays(-30),
                LastModifiedDate = DateTime.Now.AddDays(-5)
            });

            ViewModel.Projects.Add(new Project
            {
                Name = "Commercial Demo",
                Description = "456 Market St, San Francisco, CA 94105",
                Location = "San Francisco, CA",
                IsStarred = false,
                CreatedDate = DateTime.Now.AddDays(-15),
                LastModifiedDate = DateTime.Now.AddDays(-15)
            });

            ViewModel.Projects.Add(new Project
            {
                Name = "Solar Farm",
                Description = "789 Desert Ave, Las Vegas, NV 89123",
                Location = "Las Vegas, NV",
                IsStarred = true,
                CreatedDate = DateTime.Now.AddDays(-45),
                LastModifiedDate = DateTime.Now.AddDays(-2)
            });
        }

        // Public method that can be called from other pages to navigate back to home
        public void NavigateToHome()
        {
            System.Diagnostics.Debug.WriteLine("NavigateToHome called");

            // Reload projects when navigating back to the home page
            LoadProjectsAsync();

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
                    Description = "", // Empty description since we'll set location later in the design page
                    Location = "", // Empty location - will be set via search in the design page
                    IsStarred = false,
                    CreatedDate = DateTime.Now,
                    LastModifiedDate = DateTime.Now
                };

                System.Diagnostics.Debug.WriteLine($"Creating project with name: {projectName}");

                try
                {
                    // Save the project to XML
                    await ProjectManager.Instance.SaveProjectAsync(newProject);
                    System.Diagnostics.Debug.WriteLine("Project saved successfully to XML");

                    // Add to the ViewModel
                    ViewModel.Projects.Add(newProject);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error saving project: {ex.Message}");
                    // We could show an error dialog here
                }

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
            // Get the clicked project
            if (e.ClickedItem is Project clickedProject)
            {
                System.Diagnostics.Debug.WriteLine($"Project clicked: {clickedProject.Name}");

                // Navigate to the project design page with the selected project
                if (MainFrame != null)
                {
                    MainFrame.Navigate(typeof(ProjectDesignPage), clickedProject);

                    // Hide the home UI
                    HideHomeUI();
                }
            }
        }

        // Method to handle deleting a project (will need to connect to button in XAML)
        private async void DeleteProject_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the project from the button's data context
                if (sender is Button button && button.DataContext is Project project)
                {
                    // Ask for confirmation
                    ContentDialog deleteDialog = new ContentDialog
                    {
                        Title = "Delete Project",
                        Content = $"Are you sure you want to delete '{project.Name}'? This cannot be undone.",
                        PrimaryButtonText = "Delete",
                        CloseButtonText = "Cancel",
                        DefaultButton = ContentDialogButton.Close,
                        XamlRoot = this.Content.XamlRoot
                    };

                    ContentDialogResult result = await deleteDialog.ShowAsync();

                    if (result == ContentDialogResult.Primary)
                    {
                        // Delete the project file
                        await ProjectManager.Instance.DeleteProjectAsync(project);

                        // Remove from the ViewModel
                        ViewModel.Projects.Remove(project);

                        System.Diagnostics.Debug.WriteLine($"Project deleted: {project.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting project: {ex.Message}");
                // We could show an error dialog here
            }
        }

        // Toggle the starred status
        private async void ToggleStarred_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Get the project from the button's data context
                if (sender is Button button && button.DataContext is Project project)
                {
                    // Toggle the star
                    project.IsStarred = !project.IsStarred;

                    // Update the project file
                    await ProjectManager.Instance.UpdateProjectAsync(project);

                    System.Diagnostics.Debug.WriteLine($"Project '{project.Name}' starred status toggled to: {project.IsStarred}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error toggling starred status: {ex.Message}");
            }
        }
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