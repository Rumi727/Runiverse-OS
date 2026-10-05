using RuniOS.APIBridge;
using UnityEditor.UI;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEditor.UI")]
[assembly: GenerateAPIBridgeForType(typeof(SpriteDrawUtility))]