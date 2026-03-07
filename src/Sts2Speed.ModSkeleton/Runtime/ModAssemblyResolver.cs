using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace Sts2Speed.ModSkeleton.Runtime;

internal static class ModAssemblyResolver
{
    private static readonly object Sync = new();
    private static bool _installed;

#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        lock (Sync)
        {
            if (_installed)
            {
                return;
            }

            AppDomain.CurrentDomain.AssemblyResolve += ResolveFromModDirectory;
            AssemblyLoadContext.Default.Resolving += ResolveFromModDirectory;
            _installed = true;
        }
    }

    private static Assembly? ResolveFromModDirectory(object? sender, ResolveEventArgs args)
    {
        return ResolveFromModDirectory(new AssemblyName(args.Name));
    }

    private static Assembly? ResolveFromModDirectory(AssemblyLoadContext context, AssemblyName name)
    {
        return ResolveFromModDirectory(name);
    }

    private static Assembly? ResolveFromModDirectory(AssemblyName assemblyName)
    {
        var shortName = assemblyName.Name;
        if (string.IsNullOrWhiteSpace(shortName))
        {
            return null;
        }

        var alreadyLoaded = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, shortName, StringComparison.OrdinalIgnoreCase));
        if (alreadyLoaded is not null)
        {
            return alreadyLoaded;
        }

        var modDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrWhiteSpace(modDirectory))
        {
            return null;
        }

        var candidatePath = Path.Combine(modDirectory, shortName + ".dll");
        if (!File.Exists(candidatePath))
        {
            return null;
        }

        return AssemblyLoadContext.Default.LoadFromAssemblyPath(candidatePath);
    }
}
