using System;
using System.Xml.Serialization;

namespace SunStruct
{
    // XML serializable model for Projects
    [XmlRoot("Project")]
    public class ProjectXmlModel
    {
        // Basic project information
        [XmlElement("Name")]
        public string Name { get; set; } = string.Empty;

        [XmlElement("Description")]
        public string Description { get; set; } = string.Empty;

        [XmlElement("Location")]
        public string Location { get; set; } = string.Empty;

        [XmlElement("IsStarred")]
        public bool IsStarred { get; set; }

        [XmlElement("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [XmlElement("LastModifiedDate")]
        public DateTime LastModifiedDate { get; set; }

        // Location coordinates
        [XmlElement("Latitude")]
        public double Latitude { get; set; } = 0.0;

        [XmlElement("Longitude")]
        public double Longitude { get; set; } = 0.0;

        // Add additional properties as you develop new features
        // This serves as a skeleton that you can extend

        // Convert to regular Project class
        public Project ToProject()
        {
            return new Project
            {
                Name = this.Name,
                Description = this.Description,
                Location = this.Location,
                IsStarred = this.IsStarred,
                CreatedDate = this.CreatedDate,
                LastModifiedDate = this.LastModifiedDate,
                Latitude = this.Latitude,
                Longitude = this.Longitude
            };
        }

        // Create a ProjectXmlModel from a Project
        public static ProjectXmlModel FromProject(Project project)
        {
            return new ProjectXmlModel
            {
                Name = project.Name,
                Description = project.Description,
                Location = project.Location,
                IsStarred = project.IsStarred,
                CreatedDate = project.CreatedDate,
                LastModifiedDate = project.LastModifiedDate,
                Latitude = project.Latitude,
                Longitude = project.Longitude
            };
        }
    }

    // You can add more classes here as you develop features
    // For example:
    // - SolarPanelData for panel specifications
    // - ComponentData for electrical components
    // - RoofData for roof measurements and properties
    // etc.
}