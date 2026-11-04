using System.Reflection;
using System.Text.Json;

namespace PepperDash.Essentials.Plugins.Tests;

/// <summary>
/// Loads the built plugin DLL with MetadataLoadContext (reflection only, no Crestron runtime), the same way
/// Essentials inspects plugins at startup.
/// </summary>
public static class AssemblyFixture
{
    public const string PluginAssemblyName = "PepperDash.Essentials.Plugins.Panasonic.Camera.Http";

    private static readonly Lazy<MetadataLoadContext> LazyContext = new(CreateContext);
    private static readonly Lazy<Assembly> LazyAssembly = new(LoadPluginAssembly);

    // tests/bin/{Configuration}/net8.0/ -> Configuration is second to last
    private static string Configuration
    {
        get
        {
            var baseDir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            var parts = baseDir.Split(Path.DirectorySeparatorChar);
            return parts[^2];
        }
    }

    public static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    public static string SourceDirectory => Path.Combine(RepoRoot, "src");

    // The plugin csproj has no <OutputPath>, so output is src/bin/{Configuration}/net8.0/
    private static string PluginDllPath =>
        Path.Combine(SourceDirectory, "bin", Configuration, "net8.0", PluginAssemblyName + ".dll");

    private static string PluginOutputDir => Path.GetDirectoryName(PluginDllPath)!;

    public static MetadataLoadContext Context => LazyContext.Value;
    public static Assembly PluginAssembly => LazyAssembly.Value;

    private static MetadataLoadContext CreateContext()
    {
        if (!File.Exists(PluginDllPath))
            throw new FileNotFoundException($"Plugin DLL not found at '{PluginDllPath}'. Build the plugin first.", PluginDllPath);

        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var dllByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Priority 1: plugin output dir. Priority 2: .NET runtime. Priority 3: packages from project.assets.json.
        foreach (var dll in Directory.GetFiles(PluginOutputDir, "*.dll"))
            dllByName[Path.GetFileName(dll)] = dll;

        foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            dllByName.TryAdd(Path.GetFileName(dll), dll);

        // NOT deps.json: the plugin excludes the PepperDashEssentials runtime assets, so its deps.json omits every dependency.
        var assetsJsonPath = Path.Combine(SourceDirectory, "obj", "project.assets.json");
        if (File.Exists(assetsJsonPath))
        {
            foreach (var path in ResolveProjectAssetsAssemblies(assetsJsonPath))
                dllByName.TryAdd(Path.GetFileName(path), path);
        }

        return new MetadataLoadContext(new PathAssemblyResolver(dllByName.Values));
    }

    private static IEnumerable<string> ResolveProjectAssetsAssemblies(string assetsJsonPath)
    {
        var nugetDir = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
        if (string.IsNullOrWhiteSpace(nugetDir))
            nugetDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nuget", "packages");

        using var stream = File.OpenRead(assetsJsonPath);
        using var doc = JsonDocument.Parse(stream);

        if (!doc.RootElement.TryGetProperty("targets", out var targets))
            yield break;

        // Prefer the RID-less target; a RID-specific one carries runtime assets only
        var allTargets = targets.EnumerateObject().ToList();
        var target = allTargets.FirstOrDefault(t => !t.Name.Contains('/'));
        if (target.Value.ValueKind != JsonValueKind.Object)
            target = allTargets.FirstOrDefault();
        if (target.Value.ValueKind != JsonValueKind.Object)
            yield break;

        foreach (var lib in target.Value.EnumerateObject())
        {
            var slash = lib.Name.LastIndexOf('/');
            if (slash < 0) continue;
            var packageId = lib.Name[..slash].ToLowerInvariant();
            var version = lib.Name[(slash + 1)..];

            if (!lib.Value.TryGetProperty("compile", out var assets) &&
                !lib.Value.TryGetProperty("runtime", out assets))
                continue;

            foreach (var asset in assets.EnumerateObject())
            {
                if (!asset.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                    continue;

                var dllPath = Path.Combine(nugetDir, packageId, version,
                    asset.Name.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(dllPath))
                    yield return dllPath;
            }
        }
    }

    private static Assembly LoadPluginAssembly()
    {
        if (!File.Exists(PluginDllPath))
            throw new FileNotFoundException($"Plugin DLL not found at '{PluginDllPath}'. Build the plugin first.", PluginDllPath);
        return Context.LoadFromAssemblyPath(PluginDllPath);
    }

    // Walks the whole base-type chain so factories deriving through an intermediate base are found too
    public static List<Type> FindFactoryTypes(string baseTypePrefix = "EssentialsPluginDeviceFactory") =>
        PluginAssembly.GetTypes()
            .Where(t => !t.IsAbstract && InheritsFromFactory(t, baseTypePrefix))
            .ToList();

    private static bool InheritsFromFactory(Type type, string prefix)
    {
        for (var b = type.BaseType; b != null; b = b.BaseType)
            if (b.IsGenericType && b.GetGenericTypeDefinition().Name.StartsWith(prefix))
                return true;
        return false;
    }

    // Source is read once; constructors can't be executed under MetadataLoadContext, so values assigned there
    // (TypeNames, MinimumEssentialsFrameworkVersion) are checked from source.
    private static readonly Lazy<string[]> AllSourceContents = new(() =>
        Directory.GetFiles(SourceDirectory, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")
                     && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .Select(File.ReadAllText)
            .ToArray());

    public static string? FindSourceForClass(string className) =>
        AllSourceContents.Value.FirstOrDefault(content => DeclaresClass(content, className));

    private static bool DeclaresClass(string content, string className)
    {
        var needle = "class " + className;
        for (var i = content.IndexOf(needle, StringComparison.Ordinal); i >= 0;
             i = content.IndexOf(needle, i + 1, StringComparison.Ordinal))
        {
            var after = i + needle.Length;
            var next = after < content.Length ? content[after] : ' ';
            if (!char.IsLetterOrDigit(next) && next != '_')
                return true;
        }
        return false;
    }
}
