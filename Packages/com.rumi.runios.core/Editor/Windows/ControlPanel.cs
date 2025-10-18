#nullable enable
using RuniOS.Editor.Localizations;
using System;
using System.Collections.Immutable;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static RuniOS.Editor.EditorTool;

namespace RuniOS.Editor.Windows
{
    public sealed class ControlPanel : EditorWindow
    {
        static ControlPanel() => panelTypes = ReflectionUtility.types
            .Where(x => typeof(IControlPanel).IsAssignableFrom(x) && typeof(ScriptableObject).IsAssignableFrom(x) && x.HasDefaultConstructor())
            .ToImmutableArray();

        public static ImmutableArray<Type> panelTypes { get; }

        public static GUIStyle bigLabelStyle => _bigLabelStyle ??= new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
        static GUIStyle? _bigLabelStyle;



        [MenuItem("RuniOS/Control Panel")]
        public static ControlPanel GetWindow() => GetWindow<ControlPanel>();



        public ImmutableArray<IControlPanel> panels { get; private set; }
        [SerializeField] ScriptableObject[]? _panels; // 도메인 재로드시에도 에디터 창의 데이터가 유지될 수 있게하기 위한 똥꼬쇼

        public IControlPanel? selectedPanel
        {
            get
            {
                if (!panels.IsDefault && panelIndex >= 0 && panelIndex < panels.Length)
                    return panels[panelIndex];

                return null;
            }
        }

        public int panelIndex
        {
            get => _panelIndex;
            set => _panelIndex = value;
        }
        [SerializeField] int _panelIndex = 0;

        public bool isDelayedRepaint
        {
            get => _isDelayedRepaint;
            set => _isDelayedRepaint = value;
        }
        [SerializeField] bool _isDelayedRepaint = true;

        public Texture2D? icon => _icon;
        [SerializeField] Texture2D? _icon;
        
        public Texture2D? iconDark => _iconDark;
        [SerializeField] Texture2D? _iconDark;

        void OnEnable()
        {
            _ = SystemInfo.deviceModel; //이거 없으면 유니티 킬때 딱 한번 에러남 deviceModel 프로퍼티를 GUI 단계에서 처음 호출할 때 생기는 유니티 버그인듯
            
            // 도메인 재로드시에도 에디터 창의 데이터가 유지될 수 있게하기 위한 똥꼬쇼
            if (_panels == null || _panels.WhereNotFakeNull().Count() != panelTypes.Length)
                _panels = panelTypes.Select(CreateInstance).ToArray();
            panels = _panels.OfType<IControlPanel>().OrderBy(x => x.sort).ToImmutableArray();
            
            EditorLocalization.onLanguageUpdate += TitleUpdate;
            TitleUpdate();
        }
        
        void OnDisable() => EditorLocalization.onLanguageUpdate -= TitleUpdate;

        void TitleUpdate() => titleContent = new GUIContent(GetTextOrKey("control_panel.title"), EditorGUIUtility.isProSkin ? _iconDark : _icon);
        
        void Update()
        {
            if (selectedPanel == null)
                return;

            if ((Kernel.isPlaying || selectedPanel.allowUpdateInEditor) && !isDelayedRepaint && selectedPanel.allowUpdate)
                Repaint();
        }

        void OnInspectorUpdate()
        {
            if (selectedPanel == null)
                return;

            if ((Kernel.isPlaying || selectedPanel.allowUpdateInEditor) && isDelayedRepaint && selectedPanel.allowUpdate)
                Repaint();
        }

        string[] toolbarTexts = Array.Empty<string>();
        [SerializeField] Vector2 scrollPosition;
        void OnGUI()
        {
            if (panels.IsDefault)
                return;
            
            if (toolbarTexts.Length != panels.Length)
                toolbarTexts = new string[panels.Length];

            for (int i = 0; i < toolbarTexts.Length; i++)
                toolbarTexts[i] = GetTextOrKey(panels[i].label);
            
            Space();
            
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            panelIndex = GUILayout.Toolbar(panelIndex, toolbarTexts);
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            if (selectedPanel == null)
                return;
            
            Space(5);

            if ((Kernel.isPlaying || selectedPanel.allowUpdateInEditor) && selectedPanel.allowUpdate)
            {
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                string toggleLabelText = GetTextOrKey("control_panel.refresh_delay");
                
                BeginLabelWidth(toggleLabelText);
                isDelayedRepaint = EditorGUILayout.Toggle(toggleLabelText, isDelayedRepaint, GUILayout.ExpandWidth(false));
                EndLabelWidth();

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            DrawHLine(2);
            
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            selectedPanel.OnGUI();
            GUILayout.EndScrollView();
        }

        void OnDestroy()
        {
            if (_panels == null)
                return;
            
            for (int i = 0; i < _panels.Length; i++)
            {
                ScriptableObject? panel = _panels[i];
                if (panel != null)
                    DestroyImmediate(_panels[i]);
            }
        }
    }
}