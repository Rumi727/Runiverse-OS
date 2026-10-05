using RuniOS.APIBridge;
using UnityEngine.UIElements;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]
[assembly: GenerateAPIBridgeForAssembly("UnityEngine.UIElementsModule")]
//[assembly: GenerateAPIBridgeForType(typeof(VisualElement), excludeMember = ["m_RunningAnimations", "s_TypeData"], onlyByMyself = true)]
//[assembly: GenerateAPIBridgeForType(typeof(PseudoStates))]
[assembly: GenerateAPIBridgeForType(typeof(BaseField<>), includeMember = [""], onlyByMyself = true)]
[assembly: GenerateAPIBridgeForType(typeof(IPrefixLabel))]
[assembly: GenerateAPIBridgeForType(typeof(TextInputBaseField<>), includeMember = ["m_TextInputBase"])]
//[assembly: GenerateAPIBridgeForType(typeof(Panel), skipConstructors = true, onlyByMyself = true)]
[assembly: GenerateAPIBridgeForType(typeof(IMGUIContainer), onlyByMyself = true, includeMember = ["GetCurrentIMGUIContainer", "MakeCurrentIMGUIContainerDirty"])]