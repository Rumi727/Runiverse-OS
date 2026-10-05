using RuniOS.APIBridge;
using UnityEngine;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEngine.IMGUIModule")]
[assembly: GenerateAPIBridgeForType(typeof(GUIUtility), includeMember = ["s_LastControlID", "contextWidth", "s_LabelWidth", "s_FieldWidth"], onlyByMyself = true, forceStatic = true)]