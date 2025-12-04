#nullable enable
using RuniOS.Editor.APIBridge.UnityEditor.Search;
using RuniOS.Reflection;
using System.Collections;
using System.Reflection;
using UnityEditor.Search;

namespace RuniOS.Editor
{
    public class TypeSearchProvider : SearchProvider
    {
        public const string assemblyToken = "asm";
        public const string nameToken = "name";
        public const string namespaceToken = "ns";

        readonly Type baseType;
        readonly HashSet<Assembly> assemblies = new();
        readonly QueryEngine<Type> queryEngine = new();

        public TypeSearchProvider(Type baseType) : base("type", GetTextOrKey("gui.type"))
        {
            this.baseType = baseType;

            fetchPropositions = FetchPropositions;
            fetchItems = FetchItems;
            fetchColumns = FetchColumns;
            
            SearchProviderBridge.__GetInstanceFrom(this).tableConfig = GetDefaultTableConfig;
            
            queryEngine.SetSearchDataCallback(GetSearchableData, StringComparison.OrdinalIgnoreCase);
            queryEngine.AddFilter(assemblyToken, o => o.Assembly.GetName().Name);
            queryEngine.AddFilter(nameToken, o => o.Name);
            queryEngine.AddFilter(namespaceToken, o => o.Namespace);
        }

        IEnumerable<SearchProposition> FetchPropositions(SearchContext context, SearchPropositionOptions options)
        {
            yield return new SearchProposition(null, GetTextOrKey("type_search_provider.name"), $"{nameToken}:", GetTextOrKey("type_search_provider.name.help"));
            yield return new SearchProposition(null, GetTextOrKey("type_search_provider.namespace"), $"{namespaceToken}:", GetTextOrKey("type_search_provider.namespace.help"));

            // We want to provide a list of all the assemblies that contain types derived from the base type.
            foreach (string? assemblyName in assemblies.Select(x => x.GetName().Name))
                yield return new SearchProposition(GetTextOrKey("type_search_provider.assembly"), assemblyName, $"{assemblyToken}={assemblyName}", GetTextOrKey("type_search_provider.assembly.help"));
        }

        IEnumerator FetchItems(SearchContext context, List<SearchItem> items, SearchProvider provider)
        {
            if (context.empty)
                yield break;

            var query = queryEngine.ParseQuery(context.searchQuery);
            if (!query.valid)
                yield break;

            var filteredObjects = query.Apply(GetSearchData());
            foreach (var t in filteredObjects)
                yield return provider.CreateItem(context, t.AssemblyQualifiedName, t.Name, t.FullName, null, t);
        }

        IEnumerable<Type> GetSearchData()
        {
            foreach (var t in ReflectionUtility.types.Where(x => !x.IsSpecialName && !x.IsCompilerGenerated() && x.IsAssignableToAny(baseType)))
            {
                assemblies.Add(t.Assembly);
                yield return t;
            }
        }

        static IEnumerable<string> GetSearchableData(Type t) => Enumerable.Repeat(t.AssemblyQualifiedName ?? string.Empty, 1);

        static SearchTable GetDefaultTableConfig(SearchContext context)
        {
            List<SearchColumn> defaultColumns = new List<SearchColumn> { new SearchColumn(GetTextOrKey("gui.name"), "label") { width = 400 } };
            defaultColumns.AddRange(FetchColumns(context, null));
            
            return new SearchTable("type", defaultColumns);
        }

        static IEnumerable<SearchColumn> FetchColumns(SearchContext context, IEnumerable<SearchItem>? searchDatas)
        {
            yield return new SearchColumn(GetTextOrKey("type_search_provider.namespace")) { getter = GetNamespace, width = 250 };
            yield return new SearchColumn(GetTextOrKey("type_search_provider.assembly")) { getter = GetAssemblyName, width = 250 };
        }

        static object? GetNamespace(SearchColumnEventArgs args)
        {
            if (args.item.data is not Type t)
                return null;
            
            return t.Namespace;
        }

        static object? GetAssemblyName(SearchColumnEventArgs args)
        {
            if (args.item.data is not Type t)
                return null;
            
            return t.Assembly.GetName().Name;
        }
    }
}