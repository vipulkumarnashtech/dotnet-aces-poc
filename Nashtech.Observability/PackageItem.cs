using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nashtech.Observability
{
    internal class PackageItem
    {
        public PackageItem(string displayName, string name)
        {
            DisplayName = displayName;
            Name = name;
        }

        public PackageItem(string displayName, string name, string? description, string? version)
        {
            DisplayName = displayName;
            Name = name;
            Description = description;
            Version = version;
        }

        public string DisplayName { get; set; }
        public string Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? ConfigFile { get; set; }

    }
}
