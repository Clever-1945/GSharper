using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GSharper.Extensions
{
    public static class AppDomainExtensions
    {
        private class AppDomainDefinition
        {
            private Lazy<Type[]> _types;
            private static ConcurrentDictionary<string, Type> dictionaryType = new ConcurrentDictionary<string, Type>();

            public AppDomain Domain;

            public AppDomainDefinition(AppDomain domain)
            {
                Domain = domain;
                _types = new Lazy<Type[]>(() => GetTypesInternal().ToArray());
            }

            private IEnumerable<Type> GetTypesInternal()
            {
                foreach (var assembly in Domain.GetAssemblies())
                {
                    var types = Array.Empty<Type>();

                    assembly.GetTypes();


                    try
                    {
                        types = assembly.GetTypes();
                    }
                    catch
                    {
                    }

                    foreach (var type in types)
                    {
                        yield return type;
                    }
                }
            }

            public Type[] GetTypes() => _types.Value;

            public Type GetTypeByName(string namespaceAndName)
            {
                return dictionaryType.GetOrAdd(namespaceAndName, (key) =>
                {
                    var types = GetTypes();
                    return types.FirstOrDefault(x =>
                    {
                        if (!String.IsNullOrWhiteSpace(x.Namespace))
                            return key == $"{x.Namespace}.{x.Name}";

                        return key == x.Name;
                    });
                });
            }
        }

        private static ConcurrentDictionary<AppDomain, AppDomainDefinition> dictionaryAppDomain = new ConcurrentDictionary<AppDomain, AppDomainDefinition>();

        private static AppDomainDefinition GetDefinition(this AppDomain appDomain)
        {
            return dictionaryAppDomain.GetOrAdd(appDomain, (d) => new AppDomainDefinition(d));
        }

        public static Type GetTypeByName(this AppDomain appDomain, string namespaceAndName)
        {
            return appDomain.GetDefinition().GetTypeByName(namespaceAndName);
        }
        // Microsoft.CodeAnalysis.Navigation.ISymbolNavigationService
    }

}
