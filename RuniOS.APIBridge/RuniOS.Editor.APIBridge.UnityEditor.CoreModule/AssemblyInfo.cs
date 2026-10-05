using RuniOS.APIBridge;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;

[assembly: APIBridgeNamespace("RuniOS.Editor.APIBridge")]

[assembly: GenerateAPIBridgeForAssembly("UnityEditor.CoreModule")]

[assembly: GenerateAPIBridgeForType(typeof(AdvancedDropdown), includeMember = ["m_State", "SetFilter", "minimumSize", "maximumSize"])]

//[assembly: GenerateAPIBridgeForType(typeof(AudioFilterGUI))]
//[assembly: GenerateAPIBridgeForType(typeof(AudioUtil), forceStatic = true)]

[assembly: GenerateAPIBridgeForType(typeof(EditorGUI), onlyByMyself = true, excludeMember = ["s_PropertyStack", "AdvancedLazyPopup"], forceStatic = true)]
//[assembly: GenerateAPIBridgeForType(typeof(EditorGUI.VUMeter), forceStatic = true)]
[assembly: GenerateAPIBridgeForType(typeof(EditorGUIUtility), onlyByMyself = true, forceStatic = true)]
[assembly: GenerateAPIBridgeForType(typeof(GUIView), includeMember = ["current", "Repaint"], onlyByMyself = true)]
[assembly: GenerateAPIBridgeForType(typeof(InspectorWindow), onlyByMyself = true, includeMember = ["RepaintAllInspectors"])]
[assembly: GenerateAPIBridgeForType(typeof(ScriptAttributeUtility), onlyByMyself = true, includeMember = ["GetFieldInfoFromProperty"])]
//[assembly: GenerateAPIBridgeForType(typeof(SerializedObject), onlyByMyself = true)]
[assembly: GenerateAPIBridgeForType(typeof(EditorStyles), onlyByMyself = true, includeMember = ["textFieldDropDown", "textFieldDropDownText"])]
[assembly: GenerateAPIBridgeForType(typeof(EditorWindow), onlyByMyself = true, includeMember = ["IsSelectedTab"])]
[assembly: GenerateAPIBridgeForType(typeof(PlayModeView), onlyByMyself = true, includeMember = ["s_PlayModeViews"])]
//[assembly: GenerateAPIBridgeForType(typeof(GameView), onlyByMyself = true, excludeMember = ["m_DisplaySubsystems"])]
[assembly: GenerateAPIBridgeForType(typeof(PropertyEditor), onlyByMyself = true, includeMember = ["inspectorMode", "CreateIMGUIContainer", "RebuildContentsContainers", "editorsElement"])]
[assembly: GenerateAPIBridgeForType(typeof(Editor), onlyByMyself = true, includeMember = ["inspectorMode"])]
[assembly: GenerateAPIBridgeForType(typeof(ProjectWindowUtil), onlyByMyself = true, includeMember = ["GetActiveFolderPath"], forceStatic = true)]

[assembly: GenerateAPIBridgeForType(typeof(ReorderableListWrapper), onlyByMyself = true)]