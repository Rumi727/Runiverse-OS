#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Installer
{
    sealed class PackageSettingScreen : IInstallerScreen
    {
        [InitializeOnLoadMethod]
        static void Initialize() => InstallerWindow.RegisterScreen(new PackageSettingScreen());



        public InstallerWindow? mainWindow { get; set; }
        public Vector2? windowSize => null;

        public string label => InstallerWindow.TryGetText("installer.package_setting.label");
        public bool headDisable { get; } = false;

        public int sort { get; } = 1;




        GUIStyle? largeBoldLabel;
        public void DrawGUI()
        {
            largeBoldLabel ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };

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

                if (!manifestObject.TryGetValue("scopedRegistries", out JToken? scopedRegistries))
                {
                    scopedRegistries = new JArray();
                    manifestObject.Add("scopedRegistries", scopedRegistries);
                }

                const string openupmName = "package.openupm.com";

                Dictionary<string, JObject> parsedScopes = new Dictionary<string, JObject>();
                if (scopedRegistries.Type == JTokenType.Array)
                {
                    JArray array = (JArray)scopedRegistries;
                    if (CheckNameAndUrl(openupmName, "https://package.openupm.com"))
                        return;
                    //true면 return
                    bool CheckNameAndUrl(string name, string url)
                    {
                        bool exists = false;
                        foreach (var itemToken in array)
                        {
                            if (itemToken.Type != JTokenType.Object)
                                continue;

                            JObject itemObject = (JObject)itemToken;
                            if (!itemObject.TryGetValue("name", out JToken? nameValue) || !itemObject.TryGetValue("url", out JToken? urlValue))
                                continue;
                            
                            if (nameValue.Type != JTokenType.String || nameValue.ToObject<string>() != name)
                                continue;
                            
                            if (urlValue.Type != JTokenType.String || urlValue.ToObject<string>() != url)
                            {
                                Error(InstallerWindow.TryGetText("installer.package_setting.url_warning").Replace("{name}", name).Replace("{url}", url));
                                return true;
                            }

                            parsedScopes[name] = itemObject;
                            exists = true;
                        }

                        if (exists)
                            return false;
                        
                        JObject jObject = JObject.FromObject(new Dictionary<string, object>()
                        {
                            { "name", name },
                            { "url", url },
                            { "scopes", Array.Empty<string>() }
                        });

                        parsedScopes[name] = jObject;
                        array.Add(jObject);

                        return false;
                    }
                }

                GUILayout.Label(InstallerWindow.TryGetText("installer.package_setting.setting_info"));
                AddScopedRegistry(openupmName, "https://package.openupm.com", "com.cysharp.unitask", "com.quickeye.ui-toolkit-plus", "com.coffee.csharp-compiler-settings");
                //Debug.Log(UnityEditor.PackageManager.PackageInfo.IsPackageRegistered("com.cysharp.unitask"));
                bool AddScopedRegistry(string name, string url, params string?[] scopes)
                {
                    bool noChanges = true;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                    GUILayout.Label($"# {name}", largeBoldLabel);
                    GUILayout.Label($"URL: {url}");
                    GUILayout.Label("Scope:");
                    
                    if (parsedScopes[name].TryGetValue("scopes", out JToken? token) && token.Type == JTokenType.Array)
                    {
                        JArray? array = (JArray)token;

                        for (int i = 0; i < scopes.Length; i++)
                        {
                            bool exists = false;
                            if (array.Any(item => item.Type == JTokenType.String && item.ToObject<string>() == scopes[i]))
                            {
                                scopes[i] = null;
                                exists = true;
                            }

                            if (exists)
                                continue;
                            
                            GUILayout.Label($"+ {scopes[i]}");
                            noChanges = false;
                        }

                        if (noChanges)
                            GUILayout.Label(InstallerWindow.TryGetText("installer.package_setting.no_changes"));
                        else
                        {
                            if (GUILayout.Button(InstallerWindow.TryGetText("installer.package_setting.apply"), GUILayout.ExpandWidth(false)))
                            {
                                foreach (var scope in scopes)
                                {
                                    if (scope != null)
                                        array?.Add(JToken.FromObject(scope));
                                }

                                File.WriteAllText(manifestPath, manifestObject.ToString());
                                AssetDatabase.Refresh();
                            }
                        }
                    }

                    EditorGUILayout.EndVertical();

                    return noChanges;
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
