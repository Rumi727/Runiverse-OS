using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RuniOS.Installer.GitPackages;
using System.IO;
using System.Runtime.CompilerServices;
using UnityEditor.AnimatedValues;

namespace RuniOS.Installer.Screens
{
    sealed class InstallScreen : IInstallerScreen
    {
        [InitializeOnLoadMethod]
        static void Initialize() => InstallerWindow.RegisterScreen(new InstallScreen());
        
        public InstallerWindow? mainWindow { get; set; }
        public Vector2? windowSize
        {
            get
            {
                if (ConfigScriptableObject.config.currentLanguage == "ja_jp")
                    return new Vector2(670, 298);

                return null;
            }
        }

        public string label => InstallerWindow.TryGetText("installer.install_setting.label");
        public bool headDisable => false;

        public int sort => 3;

        GUIStyle? buttonLabelStyle;
        GUIStyle? oneLineLabelStyle;
        
        int selectedIndex = -1;

        readonly ConditionalWeakTable<GitPackage, AnimBool> animBools = new();
        readonly AnimBool alphaAnimBool = new();

        Vector2 scrollPosition;

        public void DrawGUI(Rect position)
        {
            if (mainWindow == null || mainWindow.gitPackages == null)
                return;
            
            buttonLabelStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontSize = 15,
                hover = new GUIStyleState { textColor = new Color(0, 0.2352941176f, 0.5333333333f) },
                active = new GUIStyleState { textColor = new Color(0, 0.2352941176f * 2, 0.5333333333f * 2) }
            };

            oneLineLabelStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleLeft };
            
            const string manifestPath = "Packages/manifest.json";
            string? manifest = null;
            Exception? exception = null;

            try
            {
                if (!File.Exists(manifestPath))
                {
                    Error(InstallerWindow.TryGetText("installer.package_setting.file_not_found")
                        .Replace("{path}", manifestPath));

                    return;
                }

                manifest = File.ReadAllText(manifestPath);

                JObject? manifestObject = JsonConvert.DeserializeObject<JObject>(manifest);
                if (manifestObject == null)
                {
                    Error(InstallerWindow.TryGetText("installer.package_setting.json_deserialization_fail"));
                    return;
                }

                if (!manifestObject.TryGetValue("dependencies", out JToken? dependenciesToken))
                {
                    dependenciesToken = new JObject();
                    manifestObject.Add("dependencies", dependenciesToken);
                }

                JObject dependencies = (JObject)dependenciesToken;
                for (int i = 0; i < mainWindow.gitPackages.packages.Length; i++)
                {
                    if (selectedIndex != i)
                        Render(i);
                }

                if (selectedIndex >= 0)
                    Render(selectedIndex);

                alphaAnimBool.target = selectedIndex >= 0;

                void Render(int index)
                {
                    GitPackage? gitPackage = mainWindow.gitPackages.packages[index];
                    if (gitPackage == null)
                        return;
                    
                    AnimBool animBool = animBools.GetOrCreateValue(gitPackage);
                    bool installed = dependencies.ContainsKey(gitPackage.id) || Directory.Exists("Packages/" + gitPackage.id);

                    string label = InstallerWindow.TryGetText(gitPackage.label);
                    Vector2 labelSize = buttonLabelStyle.CalcSize(new GUIContent(label));

                    float closeBackgroundHeight = labelSize.y + RuniStyles.contentBox.padding.vertical;
                    
                    float oneLineLabelYSize = oneLineLabelStyle.CalcSize(GUIContent.none).y;
                    
                    string installText = InstallerWindow.TryGetText("installer.install_setting.install");
                    string installedText = InstallerWindow.TryGetText("installer.install_setting.installed");
                    float installButtonXSize;
                    if (installed)
                        installButtonXSize = GUI.skin.button.CalcSize(new GUIContent(installedText)).x;
                    else
                        installButtonXSize = GUI.skin.button.CalcSize(new GUIContent(installText)).x;

                    Rect area = position;
                    area.y = Mathf.Lerp(position.y + (index * (closeBackgroundHeight + 3)), position.y, animBool.faded);
                    area.height = Mathf.Lerp(closeBackgroundHeight, position.height, animBool.faded);

                    if (selectedIndex >= 0 && selectedIndex != index && !alphaAnimBool.isAnimating)
                        return;

                    if (alphaAnimBool.isAnimating && Event.current.isMouse)
                        Event.current.Use();

                    Color orgColor = GUI.color;
                    if (selectedIndex != index && !animBool.isAnimating)
                        GUI.color = new Color(orgColor.r, orgColor.g, orgColor.b, 1 - alphaAnimBool.faded);

                    GUILayout.BeginArea(area, RuniStyles.contentBox);
                    
                    if (GUILayout.Button(label, buttonLabelStyle, GUILayout.Width(area.width - installButtonXSize - 10)))
                    {
                        if (selectedIndex == index)
                            selectedIndex = -1;
                        else
                            selectedIndex = index;
                    }

                    animBool.target = selectedIndex == index;

                    {
                        Rect rect = area;
                        rect.y = 3;
                        rect.x = area.width - installButtonXSize - 4;
                        rect.width = installButtonXSize;
                        rect.height = Mathf.Lerp(labelSize.y + 2, labelSize.y + oneLineLabelYSize + 2, animBool.faded);

                        string buttonText;
                        if (installed)
                            buttonText = installedText;
                        else
                            buttonText = installText;
                        
                        if (GUI.Button(rect, buttonText))
                        {
                            if (!dependencies.ContainsKey(gitPackage.id))
                                dependencies.Add(gitPackage.id, gitPackage.gitUrl);

                            for (int i = 0; i < gitPackage.packages.Length; i++)
                            {
                                GitPackage? dependency = gitPackage.packages[i];
                                if (dependency == null)
                                    return;

                                if (!dependencies.ContainsKey(dependency.id))
                                    dependencies.Add(dependency.id, dependency.gitUrl);
                            }

                            File.WriteAllText(manifestPath, manifestObject.ToString());
                            AssetDatabase.Refresh();

                            Debug.LogWarning(InstallerWindow.TryGetText("installer.package_setting.warning"));
                        }
                    }

                    {
                        string oneLineDescription = InstallerWindow.TryGetText(gitPackage.oneLineDescription);
                        GUILayoutUtility.GetRect(0, oneLineLabelYSize + 2);

                        Rect rect = area;
                        rect.x = Mathf.Lerp(area.x + labelSize.x + 17, area.x + 1, animBool.faded);
                        rect.y = Mathf.Lerp(3, labelSize.y + 3, animBool.faded);
                        rect.height = labelSize.y;

                        GUI.Label(rect, oneLineDescription, oneLineLabelStyle);

                        rect.x = area.x + labelSize.x + 5;
                        rect.y = 3;

                        GUI.color = new Color(orgColor.r, orgColor.g, orgColor.b, 1 - alphaAnimBool.faded);

                        GUI.Label(rect, "-", oneLineLabelStyle);
                    }
                    
                    if (selectedIndex != index && !animBool.isAnimating)
                    {
                        GUI.color = orgColor;
                        GUILayout.EndArea();
                        
                        return;
                    }
                    
                    GUI.color = new Color(orgColor.r, orgColor.g, orgColor.b, animBool.faded);
                    
                    {
                        Rect rect = area;
                        rect.y = labelSize.y + oneLineLabelYSize + 4;
                        InstallerWindow.DrawLine(rect, 1);
                    }

                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(-3);
                        
                        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(area.width - 2), GUILayout.Height(area.height - labelSize.y - oneLineLabelYSize - 9));
                        GUILayout.Label(InstallerWindow.TryGetText(gitPackage.description));

                        if (gitPackage.packages.Length > 0 || gitPackage.dependencies.Length > 0)
                        {
                            InstallerWindow.DrawLine(1, 5);
                            GUILayout.Label(InstallerWindow.TryGetText("installer.install_setting.dependency"));

                            for (int i = 0; i < gitPackage.dependencies.Length; i++)
                                GUILayout.Label($"- {gitPackage.dependencies[i]}");

                            for (int i = 0; i < gitPackage.packages.Length; i++)
                            {
                                GitPackage? dependency = gitPackage.packages[i];
                                if (dependency == null)
                                    continue;
                                
                                GUILayout.Label($"- {dependency.id} ({dependency.gitUrl})");
                            }
                        }

                        GUI.color = orgColor;

                        if (animBool.isAnimating)
                            mainWindow.Repaint();

                        GUILayout.EndScrollView();
                        EditorGUILayout.EndHorizontal();
                    }
                    
                    GUILayout.EndArea();
                }
            }
            catch (Exception e)
            {
                exception = e;
                Debug.LogException(e);

                Error(null, e);
            }
            finally
            {
                string text = string.Empty;
                if (manifest == null)
                {
                    text = InstallerWindow.TryGetText("installer.package_setting.file_not_found")
                        .Replace("{path}", manifestPath);
                }

                if (exception != null)
                {
                    if (text != string.Empty)
                        text += "\n\n";

                    text += exception;
                }

                if (text != string.Empty || exception != null)
                    EditorGUILayout.HelpBox(text, MessageType.Error);
            }

            static void Error(string? text, Exception? exception = null)
            {
                if (exception != null)
                {
                    if (text != null)
                        text += "\n\n";

                    text += exception;
                }

                EditorGUILayout.HelpBox(text, MessageType.Error);
            }
        }
    }
}