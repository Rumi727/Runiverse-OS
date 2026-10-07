#nullable enable
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace RuniOS.Editor.Installer
{
    /// <summary>
    /// Owns Setup screens and animates their navigation within the Setup shell.<br/>
    /// Setup 화면들을 소유하고 Setup shell 안에서 화면 이동을 애니메이션합니다.
    /// </summary>
    public sealed class SetupWindow : EditorWindow
    {
        public sealed class SetupView : VisualElement
        {
            public float headerExpansion
            {
                get;
                set
                {
                    if (Mathf.Approximately(field, value))
                        return;

                    field = value;
                    UpdateHeaderHeight();
                }
            }

            public float trackIndex
            {
                get;
                set
                {
                    if (Mathf.Approximately(field, value))
                        return;

                    field = value;
                    UpdateTrackPosition();
                }
            }

            public IReadOnlyList<SetupScreen> screens => _screens;
            readonly List<SetupScreen> _screens = [];

            static readonly CustomStyleProperty<Length> headerHeightProperty = new("--header-height");
            float headerHeight = 0;

            static readonly CustomStyleProperty<Length> separatorThicknessProperty = new("--separator-thickness");
            float separatorThickness = 0;

            public VisualElement header { get; }
            public VisualElement headerLogo { get; }
            public Label title { get; }

            public VisualElement viewport { get; }
            public VisualElement track { get; }

            public VisualElement footer { get; }

            public Button left { get; }
            public Label page { get; }
            public Button right { get; }

            public DropdownField dropdown { get; }

            public VisualElement logo { get; }

            public SetupView()
            {
                AddToClassList("runios-setup");

                const string templatePath = "Packages/io.github.rumi727.runios/Editor/Setup.uxml";

                VisualTreeAsset? template = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(templatePath);
                if (template == null)
                    throw new InvalidOperationException("Setup template was not found: " + templatePath);

                template.CloneTree(this);

                header = Q<VisualElement>(this, "Header");
                headerLogo = Q<VisualElement>(this, "Header-Logo");
                title = Q<Label>(header, "Title");

                viewport = Q<VisualElement>(this, "Viewport");
                track = Q<VisualElement>(viewport, "Track");

                footer = Q<VisualElement>(this, "Footer");

                left = Q<Button>(this, "Left");
                page = Q<Label>(this, "Page");
                right = Q<Button>(this, "Right");

                dropdown = Q<DropdownField>(this, "Dropdown");

                logo = Q<VisualElement>(this, "Logo");

                header.RegisterCallback<GeometryChangedEvent>(_ => UpdateTrackHeight());
                viewport.RegisterCallback<GeometryChangedEvent>(_ =>
                {
                    UpdateTrackHeight();
                    UpdateTrackPosition();
                });

                RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
            }

            static T Q<T>(VisualElement root, string name) where T : VisualElement
                => root.Q<T>(name) ?? throw new InvalidOperationException($"Required Setup element '{name}' was not found.");

            public void ClearScreen()
            {
                track.Clear();
                _screens.Clear();
            }

            public void AddScreen(SetupScreen screen)
            {
                if (track.childCount != 0)
                {
                    VisualElement separator = new();
                    separator.AddToClassList("runios-setup__separator");
                    separator.AddToClassList("runios-setup__separator--vertical");

                    track.Add(separator);
                }

                track.Add(screen);
                _screens.Add(screen);
            }

            void UpdateHeaderHeight() => header.style.height = Mathf.Lerp(0, headerHeight, headerExpansion);

            // Header occupies flex space; their sum keeps screen height stable while the viewport clips it.
            void UpdateTrackHeight()
            {
                track.style.height = header.layout.height + viewport.layout.height;
                for (int i = 0; i < screens.Count; i++)
                {
                    SetupScreen screen = screens[i];
                    if (!screen.usesHeader)
                        continue;

                    screen.scrollView?.verticalScroller.style.height = viewport.layout.height;
                }
            }

            void UpdateTrackPosition()
            {
                float step = viewport.layout.width + separatorThickness;
                track.style.translate = new Translate(-trackIndex * step, 0);
            }

            void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
            {
                if (evt.customStyle.TryGetValue(headerHeightProperty, out Length headerHeight))
                    this.headerHeight = headerHeight.value;

                if (evt.customStyle.TryGetValue(separatorThicknessProperty, out Length separatorThickness))
                    this.separatorThickness = separatorThickness.value;

                UpdateHeaderHeight();
                UpdateTrackHeight();
            }
        }

        public IReadOnlyList<SetupScreen> screens => _screens;
        readonly List<SetupScreen> _screens = [];

        public const float followRate = 12;

        public SetupView setupView => field ??= new SetupView();

        public SetupScreen? activeScreen => targetIndex >= 0 && targetIndex < screens.Count ? screens[targetIndex] : null;

        // ReSharper disable once Unity.RedundantSerializeFieldAttribute
        [field: SerializeField] public int targetIndex
        {
            get;
            set
            {
                if (field == value)
                    return;

                activeScreen?.OnDeactivated();

                field = value;
                UpdateTitleAndNavigation();

                activeScreen?.OnActivated();
            }
        }

        // ReSharper disable once Unity.RedundantSerializeFieldAttribute
        [field: SerializeField] public float animatedIndex
        {
            get;
            private set
            {
                field = value;
                setupView.trackIndex = value;
            }
        }

        // ReSharper disable once Unity.RedundantSerializeFieldAttribute
        [field: SerializeField] public float animatedHeaderExpansion
        {
            get;
            private set
            {
                field = value;
                setupView.headerExpansion = value;
            }
        }

        /// <summary>
        /// Opens the Setup window.<br/>
        /// Setup 창을 엽니다.
        /// </summary>
        [MenuItem("RuniOS/Setup")]
        public static void Open() => GetWindow<SetupWindow>(true, "Runiverse OS Setup");

        void OnEnable()
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
            foreach (Type type in TypeCache.GetTypesDerivedFrom<SetupScreen>())
            {
                if (type.IsAbstract)
                    continue;

                try
                {
                    if (Activator.CreateInstance(type) is not SetupScreen screen)
                        throw new InvalidOperationException("The constructor did not return a SetupScreen.");

                    _screens.Add(screen);
                }
                catch (Exception exception)
                {
                    Debug.LogException(new InvalidOperationException(
                        "Could not construct Setup screen: " + type.AssemblyQualifiedName, exception));
                }
            }

            _screens.Sort(static (left, right) => left.order.CompareTo(right.order));

            foreach (var screen in screens)
                setupView.AddScreen(screen);

            minSize = maxSize = new Vector2(584, 348);

            List<string> languageChoices = [];
            foreach (string language in InstallerLocalization.languages)
                languageChoices.Add($"{InstallerLocalization.GetText("language.name", language)} ({InstallerLocalization.GetText("language.region", language)})");

            setupView.dropdown.choices = languageChoices;
            setupView.dropdown.RegisterValueChangedCallback(OnLanguageSelected);
            InstallerLocalization.languageChanged += OnLanguageChanged;
            OnLanguageChanged();
            activeScreen?.OnActivated();

            previousTime = EditorApplication.timeSinceStartup;
        }

        void OnDisable()
        {
            InstallerLocalization.languageChanged -= OnLanguageChanged;
            setupView.dropdown.UnregisterValueChangedCallback(OnLanguageSelected);

            activeScreen?.OnDeactivated();
            setupView.ClearScreen();

            _screens.Clear();
        }

        void CreateGUI()
        {
            rootVisualElement.Add(setupView);

            setupView.left.clicked += Previous;
            setupView.right.clicked += Next;

            rootVisualElement.focusable = true;
            rootVisualElement.RegisterCallback<NavigationMoveEvent>(OnNavigationMove);
            rootVisualElement.Focus();
        }

        void OnLanguageSelected(ChangeEvent<string> evt)
            => InstallerLocalization.currentLanguage = InstallerLocalization.languages[setupView.dropdown.index];

        void OnLanguageChanged()
        {
            foreach (SetupScreen screen in screens)
                screen.OnLanguageChanged();

            int languageIndex = 0;
            string currentLanguage = InstallerLocalization.currentLanguage;
            for (int i = 0; i < InstallerLocalization.languages.Count; i++)
            {
                if (InstallerLocalization.languages[i] != currentLanguage)
                    continue;

                languageIndex = i;
                break;
            }

            setupView.dropdown.SetValueWithoutNotify(setupView.dropdown.choices[languageIndex]);
            UpdateTitleAndNavigation();
        }

        void OnNavigationMove(NavigationMoveEvent evt)
        {
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Left:
                    Previous();
                    break;
                case NavigationMoveEvent.Direction.Right:
                    Next();
                    break;
                default:
                    return;
            }

            evt.StopPropagation();
        }

        double previousTime;
        void Update()
        {
            double now = EditorApplication.timeSinceStartup;
            float deltaTime = (float)Math.Max(0, now - previousTime);
            previousTime = now;

            animatedHeaderExpansion = SetupAnimationUtility.Follow(animatedHeaderExpansion, activeScreen?.usesHeader ?? false ? 1 : 0, followRate, deltaTime);
            animatedIndex = SetupAnimationUtility.Follow(animatedIndex, targetIndex, followRate, deltaTime);

            foreach (SetupScreen screen in screens)
                screen.OnUpdate(now, deltaTime);

            UpdateLogo(deltaTime);
        }

        VisualElement? previousLogoTarget;
        Rect logoOffset;
        Vector3 logoOriginOffset;
        Vector3 logoScaleOffset;
        float logoRotationOffset;
        void UpdateLogo(float deltaTime)
        {
            VisualElement target = activeScreen?.logoTarget ?? setupView.headerLogo;
            VisualElement logo = setupView.logo;
            if (target.panel == null || logo.panel == null)
                return;

            IResolvedStyle targetStyle = target.resolvedStyle;
            Vector3 origin = targetStyle.transformOrigin;
            // Convert the pivot so the target's rotation and scale are applied only once.
            Vector2 pivot = new(origin.x, origin.y);
            Rect bounds = new(target.ChangeCoordinatesTo(logo.parent, pivot) - pivot, target.layout.size);
            Vector3 scale = targetStyle.scale.value;
            Rotate rotate = targetStyle.rotate;
            if (previousLogoTarget != target)
            {
                if (previousLogoTarget != null)
                {
                    Translate translate = logo.style.translate.value;
                    logoOffset = new Rect
                    {
                        position = (logo.layout.position + new Vector2(translate.x.value, translate.y.value)) - bounds.position,
                        size = new Vector2(logo.style.width.value.value, logo.style.height.value.value) - bounds.size
                    };
                    logoOriginOffset = logo.resolvedStyle.transformOrigin - origin;
                    logoScaleOffset = logo.resolvedStyle.scale.value - scale;
                    logoRotationOffset = logo.resolvedStyle.rotate.angle.ToDegrees() - rotate.angle.ToDegrees();
                }

                previousLogoTarget = target;
            }

            // Animate only target-switch offsets; apply live target changes directly.
            float remaining = SetupAnimationUtility.Follow(1, 0, followRate, deltaTime);
            logoOffset.position *= remaining;
            logoOffset.size *= remaining;
            logoOriginOffset *= remaining;
            logoScaleOffset *= remaining;
            logoRotationOffset *= remaining;

            bounds.position += logoOffset.position;
            bounds.size += logoOffset.size;

            Vector2 position = bounds.position - logo.layout.position;
            origin += logoOriginOffset;
            rotate.angle = Angle.Degrees(rotate.angle.ToDegrees() + logoRotationOffset);
            logo.style.transformOrigin = new TransformOrigin(origin.x, origin.y, origin.z);
            logo.style.translate = new Translate(position.x, position.y);
            logo.style.scale = new Scale(scale + logoScaleOffset);
            logo.style.rotate = rotate;
            logo.style.width = bounds.width;
            logo.style.height = bounds.height;
        }

        void UpdateTitleAndNavigation()
        {
            if (activeScreen?.usesHeader ?? false)
                setupView.title.text = InstallerLocalization.GetText(activeScreen.title);

            setupView.left.SetEnabled(targetIndex > 0);
            setupView.right.SetEnabled(targetIndex + 1 < screens.Count);

            setupView.page.text = (screens.Count == 0 ? 0 : targetIndex + 1) + "/" + screens.Count;
        }

        public void Previous()
        {
            if (targetIndex > 0)
                targetIndex--;
        }

        public void Next()
        {
            if (targetIndex < screens.Count - 1)
                targetIndex++;
        }
    }
}
