using System.Reflection;
using SJAConnect.Shared.Abstractions;

namespace SJAConnect.Host;

public static class ModuleRegistry
{
    public static IReadOnlyList<IModule> Discover(IEnumerable<Assembly> assemblies)
    {
        var results = new List<IModule>();
        foreach (var asm in assemblies)
        {
            var types = asm.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && typeof(IModule).IsAssignableFrom(t));
            foreach (var t in types)
            {
                var instance = (IModule?)Activator.CreateInstance(t);
                if (instance is not null)
                {
                    results.Add(instance);
                }
            }
        }

        return results;
    }
}
