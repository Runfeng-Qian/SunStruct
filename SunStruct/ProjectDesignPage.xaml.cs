using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SunStruct
{
    public sealed partial class ProjectDesignPage : Page
    {
        // Property to hold the project that was passed during navigation
        public Project CurrentProject { get; private set; }

        // Flag to track if changes have been made
        private bool _hasUnsavedChanges = false;

        // Store current location data
        private LocationData _currentLocationData;

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
                ProjectLocationTextBlock.Text = string.IsNullOrEmpty(project.Location) ? "No location set" : project.Location;
                System.Diagnostics.Debug.WriteLine($"Project details set: {project.Name}, {project.Description}");

                // Update location button text based on whether location exists
                UpdateLocationButtonText();

                // If the project was just created (has an empty file path), save it
                if (string.IsNullOrEmpty(project.FilePath))
                {
                    try
                    {
                        await ProjectManager.Instance.SaveProjectAsync(project);
                        System.Diagnostics.Debug.WriteLine("New project saved to XML file");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error saving new project: {ex.Message}");
                    }
                }
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

                        // Set up message handler for location updates
                        MapWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

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

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string message = e.TryGetWebMessageAsString();
                var locationData = JsonConvert.DeserializeObject<LocationData>(message);
                _currentLocationData = locationData;
                System.Diagnostics.Debug.WriteLine($"Received location data: {locationData.Address}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error processing web message: {ex.Message}");
            }
        }

        private void UpdateLocationButtonText()
        {
            string buttonText = string.IsNullOrEmpty(CurrentProject?.Location) ? "Select Location" : "Update Location";
            LocationButton.Content = buttonText;
        }

        private void LoadGoogleMapsSatelliteView()
        {
            System.Diagnostics.Debug.WriteLine("LoadGoogleMapsSatelliteView called");

            // Get initial coordinates if location exists
            string initialCoords = "{ lat: 37.7749, lng: -122.4194 }"; // Default: San Francisco
            string initialAddress = "";

            if (!string.IsNullOrEmpty(CurrentProject?.Location))
            {
                initialAddress = Uri.EscapeDataString(CurrentProject.Location);
            }

            System.Diagnostics.Debug.WriteLine($"Using initial address: {initialAddress}");

            // HTML content for Google Maps with satellite view and search
            string htmlContent = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <title>Google Maps with Search</title>
                <meta name='viewport' content='initial-scale=1.0'>
                <style>
                    html, body, #map {{ height: 100%; margin: 0; padding: 0; }}
                    .search-container {{
                        position: absolute;
                        top: 10px;
                        left: 50%;
                        transform: translateX(-50%);
                        z-index: 1000;
                        background: white;
                        border-radius: 8px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.3);
                        padding: 8px;
                        width: 400px;
                    }}
                    #searchInput {{
                        width: 100%;
                        padding: 10px;
                        border: none;
                        border-radius: 4px;
                        font-size: 16px;
                        outline: none;
                    }}
                    .search-results {{
                        position: absolute;
                        top: 100%;
                        left: 0;
                        right: 0;
                        background: white;
                        border-radius: 0 0 8px 8px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.3);
                        max-height: 300px;
                        overflow-y: auto;
                        display: none;
                    }}
                    .search-result {{
                        padding: 10px;
                        cursor: pointer;
                        border-bottom: 1px solid #eee;
                    }}
                    .search-result:hover {{
                        background-color: #f5f5f5;
                    }}
                    .current-location {{
                        position: absolute;
                        bottom: 20px;
                        left: 50%;
                        transform: translateX(-50%);
                        z-index: 1000;
                        background: white;
                        padding: 10px 15px;
                        border-radius: 20px;
                        box-shadow: 0 2px 10px rgba(0,0,0,0.3);
                        font-size: 14px;
                        color: #333;
                    }}
                </style>
                <script src='https://maps.googleapis.com/maps/api/js?key=AIzaSyDB1By6BfZo6ck95FbYO3FhGRJ3TApJn_4&libraries=places&callback=initMap&v=weekly' defer></script>
                <script>
                    let map;
                    let marker;
                    let geocoder;
                    let placesService;
                    let searchBox;
                    let currentLocationData = null;

                    function initMap() {{
                        // Default coordinates
                        let coords = {initialCoords};
                        
                        // Create a map centered at the coordinates
                        map = new google.maps.Map(document.getElementById('map'), {{
                            zoom: 18,
                            center: coords,
                            mapTypeId: 'satellite'
                        }});
                        
                        // Initialize services
                        geocoder = new google.maps.Geocoder();
                        placesService = new google.maps.places.PlacesService(map);
                        
                        // Create marker
                        marker = new google.maps.Marker({{
                            map: map,
                            position: coords,
                            draggable: true
                        }});
                        
                        // Set up search box
                        const input = document.getElementById('searchInput');
                        searchBox = new google.maps.places.SearchBox(input);
                        
                        // Bias the SearchBox results towards current map's viewport
                        map.addListener('bounds_changed', () => {{
                            searchBox.setBounds(map.getBounds());
                        }});
                        
                        // Listen for the event fired when the user selects a prediction
                        searchBox.addListener('places_changed', () => {{
                            const places = searchBox.getPlaces();
                            if (places.length == 0) return;
                            
                            const place = places[0];
                            if (!place.geometry || !place.geometry.location) return;
                            
                            // Update map and marker
                            map.setCenter(place.geometry.location);
                            marker.setPosition(place.geometry.location);
                            
                            // Update current location data
                            updateLocationData(place.geometry.location, place.formatted_address);
                        }});
                        
                        // Handle marker drag events
                        marker.addListener('dragend', () => {{
                            const position = marker.getPosition();
                            reverseGeocode(position);
                        }});
                        
                        // Handle map clicks
                        map.addListener('click', (e) => {{
                            marker.setPosition(e.latLng);
                            reverseGeocode(e.latLng);
                        }});
                        
                        // Load initial location if available
                        if ('{initialAddress}') {{
                            geocoder.geocode({{ 'address': '{CurrentProject?.Location}' }}, (results, status) => {{
                                if (status === 'OK') {{
                                    map.setCenter(results[0].geometry.location);
                                    marker.setPosition(results[0].geometry.location);
                                    updateLocationData(results[0].geometry.location, results[0].formatted_address);
                                }}
                            }});
                        }} else {{
                            // Update location data for default coordinates
                            reverseGeocode(new google.maps.LatLng(coords.lat, coords.lng));
                        }}
                    }}
                    
                    function reverseGeocode(latlng) {{
                        geocoder.geocode({{ 'location': latlng }}, (results, status) => {{
                            if (status === 'OK') {{
                                if (results[0]) {{
                                    updateLocationData(latlng, results[0].formatted_address);
                                }}
                            }}
                        }});
                    }}
                    
                    function updateLocationData(position, address) {{
                        currentLocationData = {{
                            latitude: position.lat(),
                            longitude: position.lng(),
                            address: address
                        }};
                        
                        // Update the current location display
                        document.getElementById('currentLocation').textContent = address;
                        
                        // Send location data to C# code
                        window.chrome.webview.postMessage(JSON.stringify(currentLocationData));
                    }}
                    
                    // Function to get current location (called from C#)
                    function getCurrentLocationData() {{
                        return currentLocationData;
                    }}
                </script>
            </head>
            <body>
                <div class='search-container'>
                    <input id='searchInput' type='text' placeholder='Search for a location...' />
                </div>
                <div id='map'></div>
                <div class='current-location' id='currentLocation'>Loading...</div>
            </body>
            </html>";

            System.Diagnostics.Debug.WriteLine("Navigating to HTML string");
            // Load the HTML content
            MapWebView.NavigateToString(htmlContent);
        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Back button clicked");

            // If there are unsaved changes, prompt the user
            if (_hasUnsavedChanges)
            {
                ContentDialog saveDialog = new ContentDialog
                {
                    Title = "Save Changes",
                    Content = "Would you like to save your changes before leaving?",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Don't Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                ContentDialogResult result = await saveDialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    // Save the project
                    await SaveProjectChanges();
                }
                else if (result == ContentDialogResult.None)
                {
                    // User cancelled, don't navigate away
                    return;
                }
                // If Secondary (Don't Save), just navigate away without saving
            }

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

        // Method to save project changes
        private async Task SaveProjectChanges()
        {
            try
            {
                if (CurrentProject != null)
                {
                    // Update the last modified date
                    CurrentProject.LastModifiedDate = DateTime.Now;

                    // Save to XML file
                    await ProjectManager.Instance.UpdateProjectAsync(CurrentProject);

                    _hasUnsavedChanges = false;
                    System.Diagnostics.Debug.WriteLine("Project changes saved");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving project changes: {ex.Message}");
                // Show error message to user
                ErrorInfoBar.Message = $"Error saving changes: {ex.Message}";
                ErrorInfoBar.IsOpen = true;
            }
        }

        // When design changes are made, mark project as having unsaved changes
        private void MarkProjectAsChanged()
        {
            _hasUnsavedChanges = true;
        }

        // Event handlers for the design tools - just stubs for now
        // In a real implementation, these would actually manipulate the solar design

        private void SolarPanels_Click(object sender, RoutedEventArgs e)
        {
            MarkProjectAsChanged();
            // Implementation for adding/configuring solar panels
        }

        private void Measure_Click(object sender, RoutedEventArgs e)
        {
            // Measurement tool doesn't change the project
            // Implementation for measuring distances on the map
        }

        private void Components_Click(object sender, RoutedEventArgs e)
        {
            MarkProjectAsChanged();
            // Implementation for adding/configuring components
        }

        private void Layout_Click(object sender, RoutedEventArgs e)
        {
            MarkProjectAsChanged();
            // Implementation for changing the layout
        }

        private async void Calculate_Click(object sender, RoutedEventArgs e)
        {
            // This doesn't change the project directly, but we might want to save calculation results
            MarkProjectAsChanged();
            // Implementation for calculating energy production, costs, etc.

            // Save calculations automatically
            await SaveProjectChanges();
        }

        // Handle location button click
        private async void LocationButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentLocationData != null && CurrentProject != null)
                {
                    // Update project with new location data
                    CurrentProject.Location = _currentLocationData.Address;
                    CurrentProject.Description = _currentLocationData.Address; // Also update description
                    CurrentProject.LastModifiedDate = DateTime.Now;

                    // Update the UI
                    ProjectLocationTextBlock.Text = _currentLocationData.Address;
                    UpdateLocationButtonText();

                    // Save the project
                    await ProjectManager.Instance.UpdateProjectAsync(CurrentProject);

                    // Show success message
                    LocationInfoBar.Message = "Location saved successfully!";
                    LocationInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Success;
                    LocationInfoBar.IsOpen = true;

                    System.Diagnostics.Debug.WriteLine($"Location saved: {_currentLocationData.Address}");
                }
                else
                {
                    // Show error if no location data
                    LocationInfoBar.Message = "Please search for or select a location on the map first.";
                    LocationInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Warning;
                    LocationInfoBar.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving location: {ex.Message}");
                LocationInfoBar.Message = $"Error saving location: {ex.Message}";
                LocationInfoBar.Severity = Microsoft.UI.Xaml.Controls.InfoBarSeverity.Error;
                LocationInfoBar.IsOpen = true;
            }
        }
    }

    // Data class for location information
    public class LocationData
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string Address { get; set; }
    }
}