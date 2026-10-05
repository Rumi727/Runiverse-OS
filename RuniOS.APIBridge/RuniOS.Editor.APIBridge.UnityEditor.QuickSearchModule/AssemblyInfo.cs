using RuniOS.APIBridge;
using UnityEditor.Search;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEditor.QuickSearchModule")]
[assembly: GenerateAPIBridgeForType(typeof(SearchProvider), includeMember = ["tableConfig"], skipConstructors = true, onlyByMyself = true)]