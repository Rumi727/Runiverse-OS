using RuniOS.APIBridge;
using UnityEngine;

[assembly: GenerateAPIBridgeForAssembly("UnityEngine.CoreModule")]
[assembly: GenerateAPIBridgeForType(typeof(DrivenPropertyManager), includeMember = ["RegisterProperty", "TryRegisterProperty", "UnregisterProperty", "UnregisterProperties"], forceStatic = true)]