using RuniOS.APIBridge;
using UnityEngine;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEngine.CoreModule")]
[assembly: GenerateAPIBridgeForType(typeof(NumericFieldDraggerUtility), forceStatic = true)]
[assembly: GenerateAPIBridgeForType(typeof(UINumericFieldsUtils))]