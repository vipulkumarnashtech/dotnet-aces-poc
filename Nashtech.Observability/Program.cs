using System.Diagnostics;
using System.Xml.Linq;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol.Core.Types;

namespace Nashtech.Observability;

internal class Program
{
    private static async Task Main(string[] args)
    {
        string featuresMenu = $"Please select required feature:" +
                                "\n1.Tracing" +
                                "\n2.Monitoring" +
                                "\n3.Logging" +
                                "\nX.Exit";

        IList<PackageItem> tracingTools = new List<PackageItem>() { new("Jaeger", "Jaeger"), new("Zipkin", "Zipkin"), new("Splunk", "Serilog.Sinks.Splunk") };
        IList<PackageItem> monitoringTools = new List<PackageItem>() { new("Prometheus", "prometheus-net.AspNetCore"), new("Datadog", "Serilog.Sinks.Datadog.Logs"), new("Dynatrace", "Dynatrace") };
        IList<PackageItem> loggingTools = new List<PackageItem>() { new("Logstash", "Serilog.Sinks.LogstashHttp"), new("Grafana Loki", "Serilog.Sinks.Grafana.Loki"), new("New Relic", "Serilog.Sinks.NewRelic.Logs") };

        string tracingMenuTitle = @"Tracing tools";
        string monitoringMenuTitle = @"Monitoring tools:";
        string loggingMenuTitle = @"Logging tools:";
        string currentDirectory = System.IO.Directory.GetCurrentDirectory();
        string projectPath = "/home/nashtech/Documents/Code/dotnet-aces-poc/MyApi/MyApi.csproj";
        string projectFolderPath = "/home/nashtech/Documents/Code/dotnet-aces-poc/MyApi";
        string jsonFilePath = "/home/nashtech/Documents/Code/dotnet-aces-poc/Nashtech.Observability/Json Files/SerilogConfig.json";
        var projects = Directory.GetFiles(currentDirectory).Where(s => s.Contains("csproj")).ToList();
        if (projects.Count == 0)
        {
            Console.WriteLine("No project(s) found in the current directory.");
        }
        else if (projects.Count > 1)
        {
            Console.WriteLine("Multiple projects found in the current directory. Please select default project!");
            for (int i = 0; i < projects.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {projects[i]}");
            }

            var inputString = Console.ReadLine();
            int selectedProjectIndex;

            var isValidInput = int.TryParse(inputString, out selectedProjectIndex);

            if (isValidInput && selectedProjectIndex > 0 && selectedProjectIndex <= projects.Count)
            {
                projectPath = projects[selectedProjectIndex - 1];
            }
            else
            {
                Console.WriteLine("Invalid input. Try again");
            }
        }
        else
        {
            projectPath = projects.FirstOrDefault();
        }

        Console.WriteLine($"Default project path: {projectPath}");

        char selectedFeature;

        bool canTerminate = false;

        while (!canTerminate)
        {
            Console.WriteLine(featuresMenu);
            var inputString = Console.ReadLine();
            var isValidFeatureSelection = char.TryParse(inputString, out selectedFeature);

            switch (selectedFeature)
            {
                case '1':
                    await InstallFeatureAsync(tracingMenuTitle, tracingTools, projectPath);

                    break;

                case '2':

                    await InstallFeatureAsync(monitoringMenuTitle, monitoringTools, projectPath);

                    break;

                case '3':
                    await InstallFeatureAsync(loggingMenuTitle, loggingTools, projectPath);

                    break;

                case 'X':
                case 'x':
                    Console.WriteLine("Exit successfully");
                    canTerminate = true;
                    break;

                default:
                    Console.WriteLine("Invalid input!");
                    break;
            }
        }
        CreateJsonFile(projectFolderPath, jsonFilePath);
    }
    private static void CreateJsonFile(string projectFolderPath, string jsonFilePath)
    {
        try
        {
            string jsonContent = File.ReadAllText(jsonFilePath);
            if (!Directory.Exists(projectFolderPath))
            {
                Directory.CreateDirectory(projectFolderPath);
            }
            string destinationFilePath = Path.Combine(projectFolderPath, Path.GetFileName(jsonFilePath));
            File.WriteAllText(destinationFilePath, jsonContent);
            Console.WriteLine("JSON file successfully written to " + destinationFilePath);
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred: " + ex.Message);
        }
    }

    private static async Task InstallFeatureAsync(string menuTitle, IList<PackageItem> packages, string projectPath)
    {
        var flag = true;

        while (flag)
        {
            Console.WriteLine(menuTitle);
            for (int i = 0; i < packages.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {packages[i].DisplayName}");
            }
            Console.WriteLine($"{packages.Count + 1}. Back to Main menu");

            var inputString = Console.ReadLine();
            int selectedTool;

            var isValidInput = int.TryParse(inputString, out selectedTool);

            if (isValidInput && selectedTool > 0 && selectedTool <= packages.Count)
            {
                try
                {
                    await InstallPackageAsync(projectPath, packages[selectedTool - 1]);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }
            else if (selectedTool == packages.Count + 1)
            {
                return;
            }
        }
    }

    private static async Task InstallPackageAsync(string projectPath, PackageItem package)
    {
        Console.WriteLine("Installing packages...");
        var settings = Settings.LoadDefaultSettings(null);
        var sourceProvider = new PackageSourceProvider(settings);
        var sources = sourceProvider.LoadPackageSources();
        var sourceRepository = new SourceRepository(new PackageSource("https://api.nuget.org/v3/index.json"), Repository.Provider.GetCoreV3());
        var packageMetadataResource = await sourceRepository.GetResourceAsync<PackageMetadataResource>();

        var packageVersion = package.Version ?? await GetLatestVersionAsync(packageMetadataResource, package.Name);

        if (packageVersion == null)
        {
            throw new Exception("Package version not found.");
        }
        AddPackageReference(projectPath, package.Name, packageVersion);
        Console.WriteLine($"Package {package.DisplayName} {(packageVersion != null ? $"version {packageVersion}" : "latest")} installed successfully.");

        //Todo: copy json file
        // Restore packages to apply changes
        Console.WriteLine("Restoring packages...");
        RestorePackages(projectPath);
    }

    public static void RestorePackages(string projectPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"restore \"{projectPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using (var process = new Process { StartInfo = startInfo })
        {
            process.OutputDataReceived += (sender, args) => Console.WriteLine(args.Data);
            process.ErrorDataReceived += (sender, args) => Console.WriteLine(args.Data);

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
        }
    }

    private static void AddPackageReference(string projectPath, string packageName, string version)
    {
        var doc = XDocument.Load(projectPath);
        var ns = doc.Root.Name.Namespace;
        var itemGroup = doc.Root.Element(ns + "ItemGroup") ?? new XElement(ns + "ItemGroup");
        //doc.Root.Add(itemGroup);

        var res = itemGroup.Descendants().FirstOrDefault(s => s.Attributes().Any(b => b.Value.Equals(packageName)));
        //update existing
        if (res != null)
        {
            var isUpdated = res.Attribute("Version")?.Value != version;

            if (!isUpdated)
            {
                res.SetAttributeValue("Version", version);
            }
            else
            {
                Console.WriteLine("Package already exist with the latest version!");
            }
        }
        //add new
        else
        {
            var packageReference = new XElement(ns + "PackageReference",
                        new XAttribute("Include", packageName),
                        new XAttribute(ns + "Version", version)
                    );
            itemGroup.Add(packageReference);
        }

        doc.Save(projectPath);
    }

    private static async Task<string> GetLatestVersionAsync(PackageMetadataResource metadataResource, string packageName)
    {
        var packageMetadata = await metadataResource.GetMetadataAsync(packageName, includePrerelease: false, includeUnlisted: false, sourceCacheContext: new SourceCacheContext(), log: NullLogger.Instance, CancellationToken.None);

        return packageMetadata?.LastOrDefault()?.Identity.Version.ToString() ?? "";
    }
}