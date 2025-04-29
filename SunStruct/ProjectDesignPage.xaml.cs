using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;

namespace SunStruct
{
    public sealed partial class ProjectDesignPage : Page
    {
        // Property to hold the project that was passed during navigation
        public Project CurrentProject { get; private set; }

        public ProjectDesignPage()
        {
            System.Diagnostics.Debug.WriteLine("ProjectDesignPage constructor called");
            this.InitializeComponent();
            System.Diagnostics.Debug.WriteLine("ProjectDesignPage InitializeComponent completed");
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("ProjectDesignPage.OnNavigatedTo called");
            base.OnNavigatedTo(e);

            // Get the passed project if available
            if (e.Parameter is Project project)
            {
                CurrentProject = project;
                ProjectNameTextBlock.Text = project.Name;
                ProjectLocationTextBlock.Text = project.Description;
                System.Diagnostics.Debug.WriteLine($"Project details set: {project.Name}, {project.Description}");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("WARNING: No project parameter received");
            }

            // Initialize WebView2 if it's not initialized yet
            if (MapWebView != null)
            {
                System.Diagnostics.Debug.WriteLine("MapWebView control found");
                if (MapWebView.CoreWebView2 == null)
                {
                    System.Diagnostics.Debug.WriteLine("Initializing CoreWebView2...");
                    try
                    {
                        // Initialize the WebView2 environment
                        await MapWebView.EnsureCoreWebView2Async();
                        System.Diagnostics.Debug.WriteLine("CoreWebView2 initialized successfully");

                        // Enable JavaScript
                        MapWebView.CoreWebView2.Settings.IsScriptEnabled = true;
                        System.Diagnostics.Debug.WriteLine("JavaScript enabled");

                        // Load the Google Maps satellite view
                        LoadGoogleMapsSatelliteView();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"ERROR initializing WebView2: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Exception details: {ex}");
                        // Handle initialization errors
                        ErrorInfoBar.Message = $"Error initializing map: {ex.Message}";
                        ErrorInfoBar.IsOpen = true;
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("CoreWebView2 already initialized");
                    LoadGoogleMapsSatelliteView();
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ERROR: MapWebView is null");
            }
        }

        private void LoadGoogleMapsSatelliteView()
        {
            System.Diagnostics.Debug.WriteLine("LoadGoogleMapsSatelliteView called");
            // Get address from the project description (assuming it contains an address)
            string address = Uri.EscapeDataString(CurrentProject?.Description ?? "San Francisco, CA");
            System.Diagnostics.Debug.WriteLine($"Using address: {address}");

            // HTML content for Google Maps with satellite view
            string htmlContent = @"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Google Maps Satellite View</title>
                <meta name='viewport' content='initial-scale=1.0'>
                <style>
                    html, body, #map { height: 100%; margin: 0; padding: 0; }
                </style>
                <script src='https://maps.googleapis.com/maps/api/js?key=AIzaSyDB1By6BfZo6ck95FbYO3FhGRJ3TApJn_4&callback=initMap&v=weekly' defer></script>
                <script>
                    function initMap() {
                        // Initialize the geocoder
                        const geocoder = new google.maps.Geocoder();
                        
                        // Default coordinates (San Francisco)
                        let coords = { lat: 37.7749, lng: -122.4194 };
                        
                        // Create a map centered at the coordinates
                        const map = new google.maps.Map(document.getElementById('map'), {
                            zoom: 18,
                            center: coords,
                            mapTypeId: 'satellite'
                        });
                        
                        // Try to geocode the address
                        geocoder.geocode({ 'address': '" + address + @"' }, function(results, status) {
                            if (status === 'OK') {
                                // Center the map on the geocoded location
                                map.setCenter(results[0].geometry.location);
                                
                                // Add a marker at the location
                                new google.maps.Marker({
                                    map: map,
                                    position: results[0].geometry.location
                                });
                            }
                        });
                    }
                </script>
            </head>
            <body>
                <div id='map'></div>
            </body>
            </html>";

            System.Diagnostics.Debug.WriteLine("Navigating to HTML string");
            // Load the HTML content
            MapWebView.NavigateToString(htmlContent);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Back button clicked");

            // Use the static MainWindow.Current to navigate back to home
            if (MainWindow.Current != null)
            {
                System.Diagnostics.Debug.WriteLine("Using MainWindow.Current to navigate to home");
                MainWindow.Current.NavigateToHome();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MainWindow.Current is null, trying fallback");
                // Fallback method - just clear the frame content
                Frame.Content = null;
            }
        }
    }
}