#nullable enable
using Newtonsoft.Json;
using RuniOS.Editor.Localizations;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Windows
{
    public sealed class DevelopmentPanel : ScriptableObject, IControlPanel
    {
        public string label => "control_panel.development";

        public int sort => 1000000;

        public bool allowUpdate => true;
        public bool allowUpdateInEditor => true;

        [SerializeField] EditorLanguageDataAsset? selectedLanguageDataAsset;
        [SerializeField] string languageTextArea = string.Empty;
        [SerializeField] Vector2 languageScrollPosition;

        readonly Dictionary<DrivenPropertyManager.DrivenPropertyData, DrivenPropertyDataExtension> drivenPropertyDatas = new();
        [SerializeField] Vector2 drivenPropertyScrollPosition;
        
        public void OnGUI()
        {
            EditorGUILayout.HelpBox(GetTextOrKey("control_panel.development.warning"), MessageType.Warning);
            Space();
            
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.Label(GetTextOrKey("control_panel.development.localization"), ControlPanel.bigLabelStyle);
                DrawHLine();

                {
                    EditorGUILayout.BeginHorizontal();
                    selectedLanguageDataAsset = (EditorLanguageDataAsset)EditorGUILayout.ObjectField(GetTextOrKey("control_panel.development.localization.asset"), selectedLanguageDataAsset, typeof(EditorLanguageDataAsset), false);

                    EditorGUI.BeginDisabledGroup(selectedLanguageDataAsset == null);
                    if (GUILayout.Button(GetTextOrKey("control_panel.development.localization.json_import"), GUILayout.ExpandWidth(false), GUILayout.Height(EditorGUIUtility.singleLineHeight)) && selectedLanguageDataAsset != null)
                    {
                        int result = EditorUtility.DisplayDialogComplex(GetTextOrKey("control_panel.development.localization.json_import"), GetTextOrKey("control_panel.development.localization.overwrite.message"), GetTextOrKey("control_panel.development.localization.overwrite.ok"), GetTextOrKey("control_panel.development.localization.overwrite.cancel"), GetTextOrKey("control_panel.development.localization.overwrite.alt"));

                        EditorLanguageDictionary? dictionary = null;
                        try
                        {
                            dictionary = JsonConvert.DeserializeObject<EditorLanguageDictionary>(languageTextArea);
                        }
                        catch (Exception e)
                        {
                            Debug.LogException(e);
                        }
                        
                        if (dictionary != null)
                        {
                            switch (result)
                            {
                                case 0:
                                {
                                    selectedLanguageDataAsset._languages = dictionary;
                                    EditorUtility.SetDirty(selectedLanguageDataAsset);
                                    break;
                                }
                                case 2:
                                {
                                    foreach (var item in dictionary.Where(item => !selectedLanguageDataAsset._languages.ContainsKey(item.Key)))
                                        selectedLanguageDataAsset._languages[item.Key] = item.Value;
                                
                                    EditorUtility.SetDirty(selectedLanguageDataAsset);
                                    break;
                                }
                            }
                        }
                    }
                    if (GUILayout.Button(GetTextOrKey("control_panel.development.localization.json_export"), GUILayout.ExpandWidth(false), GUILayout.Height(EditorGUIUtility.singleLineHeight)) && selectedLanguageDataAsset != null)
                    {
                        GUIUtility.keyboardControl = 0;
                        languageTextArea = JsonConvert.SerializeObject(selectedLanguageDataAsset.languages, Formatting.Indented);
                    }
                    EditorGUI.EndDisabledGroup();

                    EditorGUILayout.EndHorizontal();
                }

                {
                    languageScrollPosition = EditorGUILayout.BeginScrollView(languageScrollPosition, GUILayout.Height(GetYSize(new GUIContent(languageTextArea), EditorStyles.textArea).Clamp(0, 500) + 5));
                    languageTextArea = EditorGUILayout.TextArea(languageTextArea);
                    EditorGUILayout.EndScrollView();
                }

                EditorGUILayout.EndVertical();
            }

            {
                Space();
                GUILayout.Label(GetTextOrKey("control_panel.development.driven_property"), ControlPanel.bigLabelStyle);
                drivenPropertyDatas.SyncKeysWithList(DrivenPropertyManager.drivenProperties, x => new DrivenPropertyDataExtension(x));
                if (DrivenPropertyManager.drivenProperties.Count <= 0)
                    GUILayout.Label(GetTextOrKey("control_panel.development.driven_property.zero_count"));

                drivenPropertyScrollPosition = EditorGUILayout.BeginScrollView(drivenPropertyScrollPosition, GUILayout.ExpandHeight(false), GUILayout.Height(400));
                
                for (int i = 0; i < DrivenPropertyManager.drivenProperties.Count; i++)
                {
                    DrivenPropertyManager.DrivenPropertyData data = DrivenPropertyManager.drivenProperties[i];
                    DrivenPropertyDataExtension extData = drivenPropertyDatas[data];

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    extData.animBool.target = EditorGUILayout.Foldout(extData.animBool.target, $"{data.target} : {extData.property?.propertyPath ?? string.Empty}", true);

                    FadeGroup(ref extData.animBool, () =>
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

                            static void DrawObject(string label, UnityEngine.Object obj, UnityEditor.Editor? editor)
                            {
                                BeginIndentLevel(0);
                                BeginLabelWidth(label);
                                
                                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                                ObjectPingFieldLayout(label, obj);

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

        struct DrivenPropertyDataExtension : IDisposable
        {
            public AnimBool animBool;
            
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
            
            public readonly void Dispose()
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