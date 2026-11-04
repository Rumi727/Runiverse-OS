using RuniOS.APIBridge;
using UnityEditor.UIElements;
using UnityEditor.UIElements.Bindings;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEditor.UIElementsModule")]
[assembly: GenerateAPIBridgeForType(typeof(UxmlAttributeConverter), includeMember = ["TryGetConverter"])]
[assembly: GenerateAPIBridgeForType(typeof(SerializedObjectBindingContext), includeMember = ["DefaultBind"], onlyByMyself = true, skipConstructors = true)]