#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;

namespace RuniEngine.Installer
{
    sealed class InstallerWindow : EditorWindow
    {
        public static InstallerWindow? instance;
        public Texture2D? logoTexture;

        public LanguageScriptableObject? ko_kr;
        public LanguageScriptableObject? en_us;
        public LanguageScriptableObject? ja_jp;

        public static System.Diagnostics.Stopwatch stopwatch = new();
        public static System.Diagnostics.Stopwatch deltaTimeStopwatch = new();

        static readonly List<IInstallerScreen> installerScreens = new();

        static GUIStyle? headStyle;
        static Vector2 scrollPosition;

        static Vector2 logoPos = new(114, 77);
        static float logoRotation;
        static float logoSize = 100;

        static readonly AnimFloat headAnim = new AnimFloat(0);
        static readonly AnimFloat indexAnim = new AnimFloat(0);
        static readonly AnimVector3 sizeAnim = new AnimVector3(new Vector3(584, 298));
        static Vector2? lastAnimSize = null;

        [MenuItem("Runi Engine/Show Installer")]
        public static void ShowInstallerWindow()
        {
            if (!HasOpenInstances<InstallerWindow>())
                GetWindow<InstallerWindow>(true, "Runiverse OS Installer");
            else
                FocusWindowIfItsOpen<InstallerWindow>();
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            string path = Path.Combine("Assets", "~RumiEngineInstalled~");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, string.Empty);
                EditorApplication.update += ShowOnce;
            }

            static void ShowOnce()
            {
                ShowInstallerWindow();
                EditorApplication.update -= ShowOnce;
            }
        }

        static readonly Dictionary<GUIStyle, Stack<TextAnchor>> alignmentQueue = new Dictionary<GUIStyle, Stack<TextAnchor>>();
        public static void BeginAlignment(TextAnchor alignment, GUIStyle style)
        {
            if (!alignmentQueue.ContainsKey(style))
                alignmentQueue.Add(style, new Stack<TextAnchor>());

            alignmentQueue[style].Push(style.alignment);
            style.alignment = alignment;
        }

        public static void EndAlignment(GUIStyle style)
        {
            if (alignmentQueue.ContainsKey(style))
            {
                Stack<TextAnchor> stack = alignmentQueue[style];
                if (stack.TryPop(out TextAnchor result))
                    style.alignment = result;
                else
                    style.alignment = 0;

                if (stack.Count <= 0)
                    alignmentQueue.Remove(style);

                return;
            }
            else
                style.alignment = 0;
        }

        public static void RegisterScreen(IInstallerScreen screen) => installerScreens.Insert(GetInsertIndex(screen.sort), screen);

        static int GetInsertIndex(int sort)
        {
            if (installerScreens.Count <= 0)
                return 0;

            int low = 0, high = installerScreens.Count;
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (installerScreens[mid].sort <= sort)
                    low = mid + 1;
                else
                    high = mid;
            }
            return low;
        }

        public static void DrawLine(int thickness = 2, int padding = 10)
        {
            Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding - 2));
            r.height = thickness;
            r.y += (padding / 2) - 2;
            r.x -= 18;
            r.width += 22;
            EditorGUI.DrawRect(r, new Color(0.498f, 0.498f, 0.498f));
        }

        public static void DrawLine(Rect position, int thickness = 2, int padding = 10)
        {
            Rect r = new Rect(position.x, position.y, position.width, thickness);
            r.y += (padding / 2) - 2;
            r.x -= 18;
            r.width += 22;
            EditorGUI.DrawRect(r, new Color(0.498f, 0.498f, 0.498f));
        }

        public static void DrawLineV(Rect position, int thickness = 2, int padding = 10)
        {
            Rect r = new Rect(position.x, position.y, thickness, position.height);
            r.x += (padding / 2) - 2;
            r.y -= 18;
            r.height += 22;
            EditorGUI.DrawRect(r, new Color(0.498f, 0.498f, 0.498f));
        }

        public static string TryGetText(string key) => TryGetText(key, instance != null && ConfigScriptableObject.config ? ConfigScriptableObject.config.currentLanguage : "en_us");
        public static string TryGetText(string key, string language)
        {
            if (instance == null)
                return key;

            LanguageScriptableObject? languageObject = language switch
            {
                "en_us" => instance.en_us,
                "ko_kr" => instance.ko_kr,
                "ja_jp" => instance.ja_jp,
                _ => null
            };

            if (languageObject != null && languageObject.texts.TryGetValue(key, out string? value))
                return value ?? string.Empty;

            return key;
        }

        void OnEnable()
        {
            stopwatch.Restart();

            minSize = maxSize = new Vector2(584, 298);

            scrollPosition = Vector2.zero;
            instance = this;

            foreach (var screen in installerScreens)
                screen.mainWindow = this;
        }

        void OnGUI() => DrawGUI();
        void OnDestroy()
        {
            headAnim.value = 0;
            indexAnim.value = 0;
            sizeAnim.value = new Vector3(584, 298);

            ConfigScriptableObject.config.screenIndex = 0;
            ConfigScriptableObject.config.SetDirty();
        }

        public static void DrawGUI()
        {
            if (instance == null || ConfigScriptableObject.config == null)
                return;

            headStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };

            indexAnim.target = ConfigScriptableObject.config.screenIndex;

            float headHeight = 50;
            float headHeightOffset = headAnim.value;

            if (ConfigScriptableObject.config.screenIndex >= 0 && ConfigScriptableObject.config.screenIndex < installerScreens.Count)
            {
                var screen = installerScreens[ConfigScriptableObject.config.screenIndex];

                headAnim.target = screen.headDisable ? 0 : headHeight;
                sizeAnim.target = screen.windowSize ?? new Vector3(584, 298);

                Vector2 sizeAnimValue = sizeAnim.value;
                if (lastAnimSize != null)
                {
                    if (sizeAnim.isAnimating)
                    {
                        instance.minSize = new Vector2(50, 50);
                        instance.maxSize = new Vector2(4000, 4000);

                        instance.position = new Rect(instance.position.position - ((sizeAnimValue - lastAnimSize.Value) * 0.5f), sizeAnimValue);
                    }
                    else
                        instance.minSize = instance.maxSize = sizeAnim.target;
                }

                lastAnimSize = sizeAnimValue;

                if (headAnim.isAnimating || !screen.headDisable)
                {
                    BeginAlignment(TextAnchor.MiddleLeft, GUI.skin.label);
                    GUI.Label(new Rect(55, headHeightOffset - headHeight, Screen.width - 55, headHeight - 2), screen.label, headStyle);
                    EndAlignment(GUI.skin.label);

                    DrawLine(new Rect(0, headHeightOffset - 5, Screen.width, headHeight));
                }
            }

            GUILayout.FlexibleSpace();

            for (int i = 0; i < installerScreens.Count; i++)
            {
                if (i != ConfigScriptableObject.config.screenIndex && !indexAnim.isAnimating)
                    continue;

                var screen = installerScreens[i];
                Vector2 size = screen.windowSize ?? new Vector2(584, 298);

                float x = (Screen.width + 2) * i;
                float offsetX = (Screen.width + 2) * indexAnim.value;

                Rect area = new Rect(x - offsetX - 2, 0, Screen.width + 2, Screen.height - 33);
                GUILayout.BeginArea(area);
                
                if (area.x + area.width >= 0 && area.x <= area.width)
                {
                    /*Matrix4x4 matrix = GUI.matrix;
                    Color color = GUI.color;

                    GUIUtility.RotateAroundPivot(45, area.size / 2f);
                    GUIUtility.ScaleAroundPivot(new Vector2(0.75f, 0.75f), area.size / 2f);
                    GUI.color = new Color(1, 1, 1, 0.5f);*/

                    DrawLineV(new Rect(-3, headHeightOffset + 18, area.width, area.height - headHeightOffset));
                    GUILayout.BeginArea(new Rect(2, screen.headDisable ? 0 : headHeight, area.width - 2, area.height - (screen.headDisable ? 0 : headHeight)));

                    scrollPosition = GUILayout.BeginScrollView(scrollPosition);
                    screen.DrawGUI();
                    GUILayout.EndScrollView();

                    GUILayout.EndArea();

                    /*GUI.color = color;
                    GUI.matrix = matrix;*/
                }

                GUILayout.EndArea();
            }

            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.LeftArrow && ConfigScriptableObject.config.screenIndex > 0)
                {
                    ConfigScriptableObject.config.screenIndex--;
                    scrollPosition = Vector2.zero;

                    ConfigScriptableObject.config.SetDirty();
                }
                else if (Event.current.keyCode == KeyCode.RightArrow && ConfigScriptableObject.config.screenIndex < installerScreens.Count - 1)
                {
                    ConfigScriptableObject.config.screenIndex++;
                    scrollPosition = Vector2.zero;

                    ConfigScriptableObject.config.SetDirty();
                }
            }

            DrawLogo();
            DrawNavigation();

            if (headAnim.isAnimating || indexAnim.isAnimating || sizeAnim.isAnimating)
                instance.Repaint();
        }

        static void DrawNavigation()
        {
            if (instance == null || ConfigScriptableObject.config == null)
                return;

            DrawLine(2, 0);
            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            GUILayout.Space(6);

            EditorGUI.BeginDisabledGroup(ConfigScriptableObject.config.screenIndex <= 0);
            if (GUILayout.Button("<"))
            {
                ConfigScriptableObject.config.screenIndex--;
                scrollPosition = Vector2.zero;

                ConfigScriptableObject.config.SetDirty();
            }
            EditorGUI.EndDisabledGroup();

            BeginAlignment(TextAnchor.MiddleCenter, GUI.skin.label);
            GUILayout.Label($"{ConfigScriptableObject.config.screenIndex + 1}/{installerScreens.Count}", GUILayout.Width(24));
            EndAlignment(GUI.skin.label);

            EditorGUI.BeginDisabledGroup(ConfigScriptableObject.config.screenIndex >= installerScreens.Count - 1);
            if (GUILayout.Button(">"))
            {
                ConfigScriptableObject.config.screenIndex++;
                scrollPosition = Vector2.zero;

                ConfigScriptableObject.config.SetDirty();
            }
            EditorGUI.EndDisabledGroup();

            GUILayout.FlexibleSpace();

            var languageIndex = ConfigScriptableObject.config.currentLanguage switch
            {
                "en_us" => 0,
                "ko_kr" => 1,
                "ja_jp" => 2,
                _ => 0,
            };

            int selectedLanguageIndex = EditorGUILayout.Popup(languageIndex, new string[] {
                                $"{TryGetText("language.name", "en_us")} ({TryGetText("language.region", "en_us")})",
                                $"{TryGetText("language.name", "ko_kr")} ({TryGetText("language.region", "ko_kr")})",
                                $"{TryGetText("language.name", "ja_jp")} ({TryGetText("language.region", "ja_jp")})"
                            }, GUILayout.Width(120));

            if (selectedLanguageIndex != languageIndex)
            {
                ConfigScriptableObject.config.currentLanguage = selectedLanguageIndex switch
                {
                    0 => "en_us",
                    1 => "ko_kr",
                    2 => "ja_jp",
                    _ => "en_us",
                };

                ConfigScriptableObject.config.SetDirty();
            }

            GUILayout.Space(6);
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }

        public static void DrawLogo()
        {
            if (instance == null || ConfigScriptableObject.config == null)
                return;

            float prevRotation = logoRotation;

            float deltaTime = (float)deltaTimeStopwatch.Elapsed.TotalSeconds;
            deltaTimeStopwatch.Restart();
            
            if (ConfigScriptableObject.config.screenIndex == 0)
            {
                logoPos = Vector2.Lerp(logoPos, WelcomeScreen.logoRect.position, 10f * deltaTime);
                logoSize = Mathf.Lerp(logoSize, 100, 10f * deltaTime);
                logoRotation = Mathf.Repeat(logoRotation + (deltaTime * 64), 360);
            }
            else if (ConfigScriptableObject.config.screenIndex >= 0 && ConfigScriptableObject.config.screenIndex < installerScreens.Count && installerScreens[ConfigScriptableObject.config.screenIndex].headDisable)
            {
                logoPos = Vector2.Lerp(logoPos, new Vector2(7, -42), 15f * deltaTime);
                logoSize = Mathf.Lerp(logoSize, 38, 15f * deltaTime);
                logoRotation = Mathf.LerpAngle(logoRotation, 0, 15f * deltaTime);
            }
            else
            {
                logoPos = Vector2.Lerp(logoPos, new Vector2(7, 5), 15f * deltaTime);
                logoSize = Mathf.Lerp(logoSize, 38, 15f * deltaTime);
                logoRotation = Mathf.LerpAngle(logoRotation, 0, 15f * deltaTime);
            }
            
            GUI.BeginGroup(new Rect(0, 0, Screen.width, Screen.height));

            Rect rect = new(logoPos, new Vector2(logoSize, logoSize));
            Matrix4x4 matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(logoRotation, rect.center);
            GUI.DrawTexture(rect, instance != null ? instance.logoTexture : null);
            GUI.matrix = matrix;

            GUI.EndGroup();

            if (instance != null && prevRotation != logoRotation)
                instance.Repaint();
        }
    }
}
