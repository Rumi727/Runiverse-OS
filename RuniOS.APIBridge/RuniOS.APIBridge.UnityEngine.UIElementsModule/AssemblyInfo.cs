using RuniOS.APIBridge;
using UnityEngine.UIElements;

[assembly: APIBridgeNamespace("RuniOS.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEngine.UIElementsModule")]

[assembly: GenerateAPIBridgeForType(typeof(BaseVisualElementPanel), includeMember = [""], onlyByMyself = true, skipConstructors = true)]
[assembly: GenerateAPIBridgeForType(typeof(HierarchyChangeType), onlyByMyself = true)]
[assembly: GenerateAPIBridgeForType(typeof(VisualElement), includeMember = ["GetProperty", "HasProperty"], onlyByMyself = true, skipConstructors = true)]