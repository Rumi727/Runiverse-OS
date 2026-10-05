using RuniOS.APIBridge;
using UnityEditor.UIElements;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEditor.UIElementsModule")]
[assembly: GenerateAPIBridgeForType(typeof(UxmlAttributeConverter), includeMember = ["TryGetConverter"])]