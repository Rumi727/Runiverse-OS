#nullable enable
using Newtonsoft.Json;
using RuniOS.Editor.Localizations;
using UnityEditor.AnimatedValues;

namespace RuniOS.Editor.Windows;

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
                                // 이게 대체 뭔 코드지...

                                // 1. 순서를 제어하기 위해 기존 딕셔너리를 리스트로 변환합니다.
                                var languageList = selectedLanguageDataAsset._languages.ToList();

                                // 2. 새롭게 추가/업데이트 될 항목들을 순회합니다.
                                foreach (var newItem in dictionary)
                                {
                                    string newKey = newItem.Key;

                                    // 2-1. 기존 리스트에서 키가 이미 존재하는지 확인합니다 (업데이트).
                                    int existingIndex = languageList.FindIndex(item => item.Key == newKey);
                                    if (existingIndex >= 0)
                                    {
                                        languageList[existingIndex] = newItem;
                                        continue;
                                    }

                                    // 2-2. 새 항목인 경우, 삽입할 적절한 위치를 찾습니다.
                                    int insertIndex = languageList.Count; // 기본값은 맨 뒤
                                    int maxCommonPathLength = 0; // 최대 공통 경로 길이
                                    string[] newParts = newKey.Split('.');

                                    // *******************************************************************
                                    // * 1단계: 가장 긴 공통 경로(최적 그룹)를 가진 항목을 찾습니다.
                                    // *******************************************************************
                                    for (int i = 0; i < languageList.Count; i++)
                                    {
                                        string existingKey = languageList[i].Key;
                                        string[] existingParts = existingKey.Split('.');

                                        // 현재 항목과 새 항목 간의 공통 접두사 경로 길이 계산
                                        int commonLength = 0;
                                        int minLen = newParts.Length.Min(existingParts.Length);
                                        for (int j = 0; j < minLen; j++)
                                        {
                                            if (newParts[j] == existingParts[j])
                                                commonLength++;
                                            else
                                                break;
                                        }

                                        // 새로운 더 긴 공통 경로를 가진 항목을 발견했다면 업데이트합니다.
                                        if (commonLength > maxCommonPathLength)
                                            maxCommonPathLength = commonLength;
                                    }

                                    // *******************************************************************
                                    // * 2단계: 최적 그룹의 마지막 위치(경계)를 찾아 삽입 인덱스를 확정합니다.
                                    // *******************************************************************

                                    if (maxCommonPathLength > 0)
                                    {
                                        int lastMatchingIndex = -1; // 공통 경로를 가진 마지막 항목의 인덱스

                                        // 1단계에서 찾은 maxCommonPathLength를 기준으로 그룹의 마지막 요소를 찾습니다.
                                        for (int i = 0; i < languageList.Count; i++)
                                        {
                                            string existingKey = languageList[i].Key;
                                            string[] existingParts = existingKey.Split('.');

                                            // 현재 항목이 maxCommonPathLength 길이의 공통 경로를 갖는지 확인합니다.
                                            bool isSameGroup = false;
                                            if (existingParts.Length >= maxCommonPathLength)
                                            {
                                                isSameGroup = true;
                                                for (int j = 0; j < maxCommonPathLength; j++)
                                                {
                                                    // newParts[j]가 그룹의 기준이 되는 경로입니다.
                                                    if (existingParts[j] != newParts[j])
                                                    {
                                                        isSameGroup = false;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (isSameGroup) // 그룹에 속하는 항목을 발견하면 인덱스를 계속 업데이트합니다.
                                                lastMatchingIndex = i;
                                        }

                                        // 그룹을 찾았다면, 그 다음 위치에 삽입합니다.
                                        if (lastMatchingIndex != -1)
                                            insertIndex = lastMatchingIndex + 1;
                                    }

                                    // 2-3. 최종 확정된 리스트의 위치에 새 항목을 삽입합니다.
                                    languageList.Insert(insertIndex, newItem);
                                }

                                // 3. 순서가 정렬된 리스트를 다시 새 딕셔너리 타입으로 변환하여 할당합니다.
                                selectedLanguageDataAsset._languages.Clear();
                                foreach (var item in languageList)
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

        {
            Space();
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