using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace SunStruct
{
    public class ProjectManager
    {
        private static string ProjectsDirectory => Path.Combine(
            ApplicationData.Current.LocalFolder.Path,
            "Projects");

        // Singleton instance
        private static ProjectManager _instance;
        public static ProjectManager Instance => _instance ??= new ProjectManager();

        // Private constructor for singleton pattern
        private ProjectManager()
        {
            // Ensure projects directory exists
            EnsureProjectsDirectoryExists();
        }

        // Ensure the projects directory exists
        private void EnsureProjectsDirectoryExists()
        {
            if (!Directory.Exists(ProjectsDirectory))
            {
                Directory.CreateDirectory(ProjectsDirectory);
                System.Diagnostics.Debug.WriteLine($"Created projects directory: {ProjectsDirectory}");
            }
        }

        // Load all projects from XML files
        public async Task<List<Project>> LoadAllProjectsAsync()
        {
            EnsureProjectsDirectoryExists();
            List<Project> projects = new List<Project>();

            try
            {
                // Get all XML files in the projects directory
                string[] xmlFiles = Directory.GetFiles(ProjectsDirectory, "*.xml");
                System.Diagnostics.Debug.WriteLine($"Found {xmlFiles.Length} project files");

                foreach (string filePath in xmlFiles)
                {
                    try
                    {
                        Project project = await LoadProjectFromFileAsync(filePath);
                        if (project != null)
                        {
                            projects.Add(project);
                            System.Diagnostics.Debug.WriteLine($"Loaded project: {project.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading project from {filePath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading projects: {ex.Message}");
            }

            return projects;
        }

        // Load a project from a specific file
        private async Task<Project> LoadProjectFromFileAsync(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectXmlModel));

                using (FileStream fs = new FileStream(filePath, FileMode.Open))
                {
                    ProjectXmlModel xmlModel = (ProjectXmlModel)serializer.Deserialize(fs);
                    Project project = xmlModel.ToProject();
                    project.FilePath = filePath;
                    return project;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deserializing project: {ex.Message}");
                return null;
            }
        }

        // Save a project to an XML file
        public async Task SaveProjectAsync(Project project)
        {
            EnsureProjectsDirectoryExists();

            try
            {
                // Generate a valid filename
                string sanitizedName = SanitizeFileName(project.Name);
                string fileName = $"{sanitizedName}_{DateTime.Now:yyyyMMdd_HHmmss}.xml";
                string filePath = Path.Combine(ProjectsDirectory, fileName);

                // Set the file path on the project
                project.FilePath = filePath;

                // Update the last modified date
                project.LastModifiedDate = DateTime.Now;

                // Convert to XML model
                ProjectXmlModel xmlModel = ProjectXmlModel.FromProject(project);

                // Serialize to XML
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectXmlModel));

                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    serializer.Serialize(fs, xmlModel);
                }

                System.Diagnostics.Debug.WriteLine($"Project saved to: {filePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving project: {ex.Message}");
                throw;
            }
        }

        // Update an existing project
        public async Task UpdateProjectAsync(Project project)
        {
            try
            {
                if (string.IsNullOrEmpty(project.FilePath) || !File.Exists(project.FilePath))
                {
                    // If no file path or the file doesn't exist, create a new file
                    await SaveProjectAsync(project);
                    return;
                }

                // Update the last modified date
                project.LastModifiedDate = DateTime.Now;

                // Convert to XML model
                ProjectXmlModel xmlModel = ProjectXmlModel.FromProject(project);

                // Serialize to XML
                XmlSerializer serializer = new XmlSerializer(typeof(ProjectXmlModel));

                using (FileStream fs = new FileStream(project.FilePath, FileMode.Create))
                {
                    serializer.Serialize(fs, xmlModel);
                }

                System.Diagnostics.Debug.WriteLine($"Project updated at: {project.FilePath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating project: {ex.Message}");
                throw;
            }
        }

        // Delete a project file
        public async Task DeleteProjectAsync(Project project)
        {
            try
            {
                if (!string.IsNullOrEmpty(project.FilePath) && File.Exists(project.FilePath))
                {
                    File.Delete(project.FilePath);
                    System.Diagnostics.Debug.WriteLine($"Project deleted: {project.FilePath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error deleting project: {ex.Message}");
                throw;
            }
        }

        // Helper function to sanitize file names
        private string SanitizeFileName(string fileName)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = new string(fileName.Where(c => !invalidChars.Contains(c)).ToArray());

            // Remove any additional characters that might cause issues
            sanitized = sanitized.Replace(' ', '_');

            // If the sanitized name is empty, use a default name
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                sanitized = "Project";
            }

            return sanitized;
        }

        // Add your own methods as you develop features
        // For example:
        // - SaveSolarPanelLayout
        // - UpdateRoofMeasurements
        // - SaveCalculationResults
        // etc.
    }
}