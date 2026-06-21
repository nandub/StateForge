using System.IO.Compression;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

const string RepositoryUrl = "https://github.com/nandub/StateForge";
Guid sourceLinkKind = new("CC110556-A091-4D38-9FEC-25AB9A351A6A");

if (args.Length != 3)
{
    throw new ArgumentException(
        "Usage: StateForge.PackageValidationTests <package-directory> <version> <repository-commit>");
}

string packageDirectory = Path.GetFullPath(args[0]);
string version = args[1];
string repositoryCommit = args[2];

if (!Directory.Exists(packageDirectory))
{
    throw new DirectoryNotFoundException(packageDirectory);
}

if (repositoryCommit.Length != 40 || repositoryCommit.Any(value => !Uri.IsHexDigit(value)))
{
    throw new ArgumentException("Repository commit must be a 40-character Git commit.", nameof(args));
}

string[] packageIds =
{
    "StateForge.Core",
    "StateForge.FileStore",
    "StateForge.AspNet",
    "StateForge.AspNetCore",
    "StateForge.Security",
    "StateForge.Telemetry",
    "StateForge.CloudNative",
    "StateForge.Format",
    "StateForge.Prometheus",
    "StateForge.Performance",
    "StateForge.Replication",
    "StateForge.Snapshots",
    "StateForge.Remote"
};

foreach (string packageId in packageIds)
{
    string packagePath = Path.Combine(packageDirectory, $"{packageId}.{version}.nupkg");
    string symbolsPath = Path.Combine(packageDirectory, $"{packageId}.{version}.snupkg");

    ValidatePackage(packagePath, packageId, version, repositoryCommit);
    ValidateSymbols(symbolsPath, packageId, repositoryCommit);
}

string[] packages = Directory.GetFiles(packageDirectory, "*.nupkg");
string[] symbolPackages = Directory.GetFiles(packageDirectory, "*.snupkg");

if (packages.Length != packageIds.Length || symbolPackages.Length != packageIds.Length)
{
    throw new InvalidDataException(
        $"Expected {packageIds.Length} packages and symbol packages, found {packages.Length} and {symbolPackages.Length}.");
}

Console.WriteLine(
    $"Validated {packageIds.Length} NuGet packages and portable SourceLink symbol packages for commit {repositoryCommit}.");

void ValidatePackage(string path, string packageId, string expectedVersion, string expectedCommit)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException("Package is missing.", path);
    }

    using ZipArchive archive = ZipFile.OpenRead(path);
    ZipArchiveEntry nuspecEntry = archive.Entries.Single(
        entry => entry.FullName.Equals($"{packageId}.nuspec", StringComparison.OrdinalIgnoreCase));

    using Stream nuspecStream = nuspecEntry.Open();
    XDocument document = XDocument.Load(nuspecStream);
    XElement metadata = document.Root?.Elements().Single(element => element.Name.LocalName == "metadata")
        ?? throw new InvalidDataException($"{path} has no NuGet metadata.");

    AssertElement(metadata, "id", packageId, path);
    AssertElement(metadata, "version", expectedVersion, path);
    AssertElement(metadata, "license", "MIT", path);
    AssertElement(metadata, "projectUrl", RepositoryUrl, path);

    XElement repository = metadata.Elements().Single(element => element.Name.LocalName == "repository");
    AssertAttribute(repository, "type", "git", path);
    AssertAttribute(repository, "url", RepositoryUrl, path);
    AssertAttribute(repository, "commit", expectedCommit, path);

    RequireEntry(archive, "README-NUGET.md", path);
    if (!archive.Entries.Any(entry => entry.FullName.StartsWith("lib/", StringComparison.OrdinalIgnoreCase)
        && entry.FullName.EndsWith($"/{packageId}.dll", StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidDataException($"{path} does not contain its package assembly.");
    }
}

void ValidateSymbols(string path, string packageId, string expectedCommit)
{
    if (!File.Exists(path))
    {
        throw new FileNotFoundException("Symbol package is missing.", path);
    }

    using ZipArchive archive = ZipFile.OpenRead(path);
    ZipArchiveEntry pdbEntry = archive.Entries.Single(
        entry => entry.FullName.EndsWith($"/{packageId}.pdb", StringComparison.OrdinalIgnoreCase));

    using Stream pdbStream = pdbEntry.Open();
    using MemoryStream buffer = new();
    pdbStream.CopyTo(buffer);
    buffer.Position = 0;

    using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(buffer);
    MetadataReader reader = provider.GetMetadataReader();
    EntityHandle module = MetadataTokens.EntityHandle(TableIndex.Module, 1);

    CustomDebugInformation sourceLink = reader.GetCustomDebugInformation(module)
        .Select(handle => reader.GetCustomDebugInformation(handle))
        .Single(info => reader.GetGuid(info.Kind) == sourceLinkKind);

    string json = Encoding.UTF8.GetString(reader.GetBlobBytes(sourceLink.Value));
    using JsonDocument sourceLinkDocument = JsonDocument.Parse(json);
    JsonElement documents = sourceLinkDocument.RootElement.GetProperty("documents");

    string expectedUrlFragment = $"raw.githubusercontent.com/nandub/StateForge/{expectedCommit}";
    if (!documents.EnumerateObject().Any(mapping =>
        mapping.Value.GetString()?.Contains(expectedUrlFragment, StringComparison.OrdinalIgnoreCase) == true))
    {
        throw new InvalidDataException($"{path} SourceLink mappings do not target {expectedUrlFragment}.");
    }
}

static void AssertElement(XElement parent, string name, string expected, string path)
{
    string actual = parent.Elements().Single(element => element.Name.LocalName == name).Value;
    if (!string.Equals(actual, expected, StringComparison.Ordinal))
    {
        throw new InvalidDataException($"{path} has {name} '{actual}', expected '{expected}'.");
    }
}

static void AssertAttribute(XElement element, string name, string expected, string path)
{
    string actual = element.Attribute(name)?.Value
        ?? throw new InvalidDataException($"{path} repository metadata is missing {name}.");
    if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidDataException($"{path} has repository {name} '{actual}', expected '{expected}'.");
    }
}

static void RequireEntry(ZipArchive archive, string name, string path)
{
    if (!archive.Entries.Any(entry => entry.FullName.Equals(name, StringComparison.OrdinalIgnoreCase)))
    {
        throw new InvalidDataException($"{path} is missing {name}.");
    }
}
