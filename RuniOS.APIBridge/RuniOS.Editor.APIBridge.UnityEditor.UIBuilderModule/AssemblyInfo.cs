using RuniOS.APIBridge;
using UnityEditor.UIElements;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEditor.UIBuilderModule")]
[assembly: GenerateAPIBridgeForType(typeof(TypeSearchProvider))]