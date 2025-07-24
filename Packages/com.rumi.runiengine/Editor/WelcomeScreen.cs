#nullable enable
using UnityEditor;
using UnityEngine;

namespace RuniEngine.Installer
{
    sealed class WelcomeScreen : IInstallerScreen
    {
        [InitializeOnLoadMethod]
        static void Initialize() => InstallerWindow.RegisterScreen(new WelcomeScreen());



        public InstallerWindow? mainWindow { get; set; }
        public Vector2? windowSize => new Vector2(584, 298);

        public string label => InstallerWindow.TryGetText("installer.welcome");
        public bool headDisable => true;

        public int sort => 0;



        static GUIStyle? headStyle;
        public static Rect logoRect;



        public void DrawGUI()
        {
            float timer = (float)InstallerWindow.stopwatch.Elapsed.TotalSeconds;

            GUILayout.FlexibleSpace();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            GUILayout.Space(20);

            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(100), GUILayout.Height(100));
            if (rect.position != Vector2.zero)
                logoRect = rect;

            GUILayout.Space(20);

            GUILayout.BeginVertical();
            headStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 30 };

            DrawAnimatedLabels(timer);

            GUILayout.Space(20);

            GUILayout.Label(InstallerWindow.TryGetText("installer.welcome.text"));
            GUILayout.Label(InstallerWindow.TryGetText("installer.welcome.text2"));

            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        void DrawAnimatedLabels(float timer)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 40);
            Color baseColor = GUI.color;

            int interval = 45;
            int count = 3;

            DrawFloatingLabel("en_us", 0);
            DrawFloatingLabel("ko_kr", 1);
            DrawFloatingLabel("ja_jp", 2);

            GUI.color = baseColor;

            void DrawFloatingLabel(string language, int index)
            {
                float animYOffset = -interval + Mathf.Repeat((timer * 20) + (index * interval), interval * count);
                Rect animRect = new Rect(new Vector2(rect.x, rect.y + animYOffset), rect.size);
                float distance = animYOffset;
                float alpha = distance < 0 ? (1 - (Mathf.Abs(distance) / interval)) * 2 : 1 - (Mathf.Abs(distance) / (interval * 0.5f));
                alpha = Mathf.Clamp01(alpha);

                GUI.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);
                GUI.Label(animRect, InstallerWindow.TryGetText("installer.welcome", language), headStyle);
            }
        }
    }
}
