#nullable enable
using RuniOS.Editor.IMGUI;
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.Windows
{
    public sealed class DevelopmentPanel : ScriptableObject, IControlPanel
    {
        public string label => "control_panel.development";

        public int sort => 1000000;

        public bool allowUpdate => true;
        public bool allowUpdateInEditor => true;

        readonly Dictionary<DrivenPropertyManager.DrivenPropertyData, DrivenPropertyDataExtension> drivenPropertyDatas = new();
        [SerializeField] Vector2 drivenPropertyScrollPosition;
        
        public void OnGUI()
        {
            EditorGUILayout.HelpBox(GetTextOrKey("control_panel.development.warning"), MessageType.Warning);
            Space();

            {
                GUILayout.Label(GetTextOrKey("control_panel.development.driven_property"), ControlPanel.bigLabelStyle);
                drivenPropertyDatas.SyncKeysWithEnumerable(DrivenPropertyManager.drivenProperties, x => new DrivenPropertyDataExtension(x));
                if (DrivenPropertyManager.drivenProperties.Count <= 0)
                    GUILayout.Label(GetTextOrKey("control_panel.development.driven_property.zero_count"));

                drivenPropertyScrollPosition = EditorGUILayout.BeginScrollView(drivenPropertyScrollPosition, GUILayout.ExpandHeight(false), GUILayout.Height(400));
                
                for (int i = 0; i < DrivenPropertyManager.drivenProperties.Count; i++)
                {
                    DrivenPropertyManager.DrivenPropertyData data = DrivenPropertyManager.drivenProperties[i];
                    DrivenPropertyDataExtension extData = drivenPropertyDatas[data];

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    extData.animBool.target = EditorGUILayout.Foldout(extData.animBool.target, $"{data.target} : {extData.property?.propertyPath ?? string.Empty}", true);

                    FadeGroup(extData.animBool, () =>
                    {
                        BeginIndentLevel();

                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.PropertyField(extData.property);

                            string buttonText = GetTextOrKey("control_panel.development.driven_property.unregister");
                            if (GUILayout.Button(buttonText, GUILayout.Width(GetXSize(buttonText, GUI.skin.button)), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
                                DrivenPropertyManager.UnregisterProperty(data.driver, data.target, data.propertyPath);

                            EditorGUILayout.EndHorizontal();
                        }

                        Space();

                        {
                            EditorGUILayout.BeginHorizontal();
                            Space(EditorGUI.indentLevel * 15);

                            DrawObject(GetTextOrKey("control_panel.development.driven_property.driver"), data.driver, extData.driverEditor);
                            GUILayout.Label("→", GUILayout.Width(GetLabelXSize("→")));
                            DrawObject(GetTextOrKey("control_panel.development.driven_property.target"), data.driver, extData.driverEditor);

                            static void DrawObject(string label, Object obj, UnityEditor.Editor? editor)
                            {
                                BeginIndentLevel(0);
                                BeginLabelWidth(label);
                                
                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                RuniLayoutFields.ObjectPingField(label, obj);

                                if (editor != null)
                                {
                                    // ReSharper disable once Unity.NoNullPatternMatching
                                    editor.ReloadPreviewInstances();
                                    editor.DrawPreview(EditorGUILayout.GetControlRect(false, 256));
                                }
                                EditorGUILayout.EndVertical();
                                
                                EndLabelWidth();
                                EndIndentLevel();
                            }
                            
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EndIndentLevel();
                    });

                    drivenPropertyDatas[data] = extData;
                    EditorGUILayout.EndVertical();
                }
                
                EditorGUILayout.EndScrollView();
            }
        }

        void OnDestroy()
        {
            foreach (var item in drivenPropertyDatas)
                item.Value.Dispose();
            
            drivenPropertyDatas.Clear();
        }

        readonly struct DrivenPropertyDataExtension : IDisposable
        {
            public readonly AnimBool animBool;
            
            public readonly UnityEditor.Editor? driverEditor;
            public readonly UnityEditor.Editor? targetEditor;
            
            public readonly SerializedProperty? property;

            public DrivenPropertyDataExtension(DrivenPropertyManager.DrivenPropertyData data)
            {
                animBool = new AnimBool(false);
                
                driverEditor = null;
                targetEditor = null;
                
                // ReSharper disable Unity.NoNullPatternMatching
                if (data.driver != null)
                {
                    if (data.driver is Component component)
                        UnityEditor.Editor.CreateCachedEditor(component.gameObject, null, ref driverEditor);
                    else
                        UnityEditor.Editor.CreateCachedEditor(data.driver, null, ref driverEditor);
                }
                if (data.target != null)
                {
                    if (data.target is Component component)
                        UnityEditor.Editor.CreateCachedEditor(component.gameObject, null, ref targetEditor);
                    else
                        UnityEditor.Editor.CreateCachedEditor(data.target, null, ref targetEditor);
                }
                // ReSharper restore Unity.NoNullPatternMatching

                property = new SerializedObject(data.target).FindProperty(data.propertyPath);
            }
            
            public void Dispose()
            {
                DestroyImmediate(driverEditor);
                DestroyImmediate(targetEditor);

                if (property != null)
                {
                    property.serializedObject.Dispose();
                    property.Dispose();
                }
            }
        }
    }
}
