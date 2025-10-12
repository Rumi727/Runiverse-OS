#nullable enable
using Newtonsoft.Json;
using RuniOS.Editor.Localizations;
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Windows
{
    public sealed class DevelopmentPanel : ScriptableObject, IControlPanel
    {
        public string label => "control_panel.development";

        public int sort => 1000000;

        public bool allowUpdate => false;
        public bool allowUpdateInEditor => false;

        [SerializeField] EditorLanguageDataAsset? selectedLanguageDataAsset;
        [SerializeField] string languageTextArea = string.Empty;
        [SerializeField] Vector2 languageScrollPosition;
        public void OnGUI()
        {
            EditorGUILayout.HelpBox(GetTextOrKey("control_panel.development.warning"), MessageType.Warning);
            Space();
            
            {
                GUILayout.Label(GetTextOrKey("control_panel.development.localization"), ControlPanel.bigLabelStyle);

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
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
                                        selectedLanguageDataAsset._languages.Add(item.Key, item.Value);
                                
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
        }
    }
}