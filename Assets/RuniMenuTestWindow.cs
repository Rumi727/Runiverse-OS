#nullable enable
#if UNITY_EDITOR
using RuniOS.Resource;
using RuniOS.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Toolbar = RuniOS.UIElements.Toolbar;

/// <summary>
/// RuniMenu를 다양한 구성과 극단적인 경우로 직접 열어 보는 테스트 창입니다.
/// </summary>
public sealed class RuniMenuTestWindow : EditorWindow
{
    [MenuItem("RuniOS/Runi Menu Test")]
    static void Open() => GetWindow<RuniMenuTestWindow>("Runi Menu Test");

    const string styleSheetPath = "Assets/RuniMenuTestWindow.uss";
    const string uxmlPath = "Assets/RuniMenuTest.uxml";

    const string fadeUssClassName = "runi-menu-test--fade";
    const string slowFadeUssClassName = "runi-menu-test--slow-fade";
    const string slideUssClassName = "runi-menu-test--slide";
    const string minWidthUssClassName = "runi-menu-test--min-width";

    static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) folderIcon = ("runios-ui:ui/io_browser/places/16/folder", "runios-ui:ui_dark/io_browser/places/16/folder");
    static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) computerIcon = ("runios-ui:ui/io_browser/devices/16/computer", "runios-ui:ui_dark/io_browser/devices/16/computer");
    static readonly (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon) networkIcon = ("runios-ui:ui/io_browser/places/16/network-workgroup", "runios-ui:ui_dark/io_browser/places/16/network-workgroup");

    static readonly string[] transitionChoices = { "None", "Fade", "Slow fade (1.5s)", "Slide (opacity 0.2s + translate 0.5s)" };

    readonly Dictionary<string, RuniMenu> menus = new();
    readonly List<string> logs = new();

    StyleSheet? styleSheet;

    IntegerField openDelayField = null!;
    IntegerField closeDelayField = null!;
    Toggle openOnHoverToggle = null!;
    Toggle openOnFocusToggle = null!;
    EnumField sizeModeField = null!;
    DropdownField transitionField = null!;
    Toggle minWidthToggle = null!;
    Label logLabel = null!;

    void CreateGUI()
    {
        styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
        if (styleSheet != null)
            rootVisualElement.styleSheets.Add(styleSheet);

        rootVisualElement.AddToClassList("runi-menu-test");

        ScrollView scrollView = new(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
        rootVisualElement.Add(scrollView);

        scrollView.Add(CreateSettings());
        scrollView.Add(CreateDropDownSection());
        scrollView.Add(CreateAttachedSection());
        scrollView.Add(CreateContextSection());
        scrollView.Add(CreateCornerSection());

        logLabel = new Label { name = "log" };
        logLabel.AddToClassList("runi-menu-test__log");
        rootVisualElement.Add(logLabel);
        Log("Ready.");
    }

    #region Layout

    VisualElement CreateSettings()
    {
        VisualElement section = CreateSection("Settings (applied to every menu when it opens)");

        VisualElement row = CreateRow();
        openDelayField = new IntegerField("Open delay (ms)") { value = 500 };
        closeDelayField = new IntegerField("Close delay (ms)") { value = 0 };
        openOnHoverToggle = new Toggle("Open on hover") { value = true };
        openOnFocusToggle = new Toggle("Open on focus") { value = true };
        row.Add(openDelayField);
        row.Add(closeDelayField);
        row.Add(openOnHoverToggle);
        row.Add(openOnFocusToggle);
        section.Add(row);

        row = CreateRow();
        sizeModeField = new EnumField("Size mode", DropdownMenuSizeMode.Auto);
        transitionField = new DropdownField("Transition", transitionChoices.ToList(), 0);
        minWidthToggle = new Toggle("Popup min-width 150px (USS)");
        row.Add(sizeModeField);
        row.Add(transitionField);
        row.Add(minWidthToggle);
        row.Add(new Button(() => { logs.Clear(); Log("Log cleared."); }) { text = "Clear log" });
        section.Add(row);

        return section;
    }

    VisualElement CreateDropDownSection()
    {
        VisualElement section = CreateSection("DropDown (not attached, opened below the button)");

        VisualElement row = CreateRow();
        AddDropDownButton(row, "Basic", BuildBasicMenu);
        AddDropDownButton(row, "Icons & shortcuts", BuildIconAndShortcutMenu);
        AddDropDownButton(row, "Deep (15 levels)", () => BuildDeepMenu(15));
        AddDropDownButton(row, "Wide (40 x 40)", BuildWideMenu);
        AddDropDownButton(row, "Huge (1000 items)", () => BuildHugeMenu(1000));
        section.Add(row);

        row = CreateRow();
        AddDropDownButton(row, "Long text", BuildLongTextMenu);
        AddDropDownButton(row, "Mixed elements", BuildMixedMenu);
        AddDropDownButton(row, "Degenerate", BuildDegenerateMenu);
        AddDropDownButton(row, "Dynamic", BuildDynamicMenu);
        AddDropDownButton(row, "Reentrant", BuildReentrantMenu);
        AddDropDownButton(row, "Per-menu delays", BuildPerMenuDelayMenu);
        section.Add(row);

        row = CreateRow();
        Button wide = new() { text = "Very wide anchor (sizeMode test)", style = { width = 400 } };
        wide.clicked += () => DropDown(GetMenu("Basic", BuildBasicMenu), wide);
        row.Add(wide);

        Button rebuild = new(() =>
        {
            foreach (RuniMenu menu in menus.Values)
                menu.CloseAll(false);

            menus.Clear();
            Log("All cached menus discarded. They will be rebuilt on next open.");
        }) { text = "Discard cached menus" };
        row.Add(rebuild);
        section.Add(row);

        return section;
    }

    VisualElement CreateAttachedSection()
    {
        VisualElement section = CreateSection("Attached (menu buttons, expanded preview, UXML)");

        Toolbar toolbar = new();
        RuniMenu file = BuildBasicMenu();
        file.text = "File";
        toolbar.Add(file);

        RuniMenu edit = BuildDeepMenu(5);
        edit.text = "Edit";
        toolbar.Add(edit);

        RuniMenu view = BuildMixedMenu();
        view.text = "View";
        toolbar.Add(view);

        HookAll(file);
        HookAll(edit);
        HookAll(view);
        section.Add(toolbar);

        section.Add(new Label("expanded = true (inline preview, nested menu also expanded):"));
        RuniMenu expanded = BuildIconAndShortcutMenu();
        expanded.text = "Expanded menu";
        expanded.expanded = true;
        foreach (RuniMenu nested in expanded.Query<RuniMenu>().ToList())
            nested.expanded = true;

        HookAll(expanded);
        section.Add(expanded);

        Toggle expandedToggle = new("Toggle expanded") { value = true };
        expandedToggle.RegisterValueChangedCallback(evt => expanded.expanded = evt.newValue);
        section.Add(expandedToggle);

        section.Add(new Label($"UXML ({uxmlPath}):"));
        VisualTreeAsset? uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
        if (uxml != null)
        {
            VisualElement container = uxml.Instantiate();
            // 닫혀 있는 동안 팝업은 계층 안에 있으므로, 다른 메뉴 안에 있지 않은 메뉴만 골라 하위 메뉴까지 한 번씩 연결합니다.
            foreach (RuniMenu menu in container.Query<RuniMenu>().ToList().Where(static x => x.GetFirstAncestorOfType<RuniMenu>() == null))
                HookAll(menu);

            section.Add(container);
        }
        else
            section.Add(new Label("UXML not found."));

        return section;
    }

    VisualElement CreateContextSection()
    {
        VisualElement section = CreateSection("Context menu (right click anywhere in the box)");

        VisualElement area = new() { name = "context-area", focusable = true };
        area.AddToClassList("runi-menu-test__context-area");
        area.Add(new Label("Right click here. The menu opens at the cursor."));
        area.RegisterCallback<PointerDownEvent>(evt =>
        {
            if (evt.button != 1)
                return;

            RuniMenu menu = GetMenu("Context", BuildContextMenu);
            ApplySettings(menu);
            menu.DropDown(new Rect(evt.position, Vector2.zero), area);
            evt.StopPropagation();
        });

        section.Add(area);
        return section;
    }

    VisualElement CreateCornerSection()
    {
        VisualElement section = CreateSection("Edge placement (menus near every edge of the box)");

        VisualElement area = new();
        area.AddToClassList("runi-menu-test__corner-area");

        AddCornerButton(area, "↖ Huge", 0, 0, () => BuildHugeMenu(300));
        AddCornerButton(area, "↗ Deep", 0, null, () => BuildDeepMenu(15));
        AddCornerButton(area, "↙ Wide", null, 0, BuildWideMenu);
        AddCornerButton(area, "↘ Long", null, null, BuildLongTextMenu);

        section.Add(area);
        return section;
    }

    void AddCornerButton(VisualElement area, string text, float? top, float? left, Func<RuniMenu> build)
    {
        Button button = new() { text = text, style = { position = Position.Absolute } };
        if (top.HasValue)
            button.style.top = top.Value;
        else
            button.style.bottom = 0;

        if (left.HasValue)
            button.style.left = left.Value;
        else
            button.style.right = 0;

        button.clicked += () => DropDown(GetMenu("Corner " + text, build), button);
        area.Add(button);
    }

    static VisualElement CreateSection(string title)
    {
        VisualElement section = new();
        section.AddToClassList("runi-menu-test__section");

        Label label = new(title);
        label.AddToClassList("runi-menu-test__section-title");
        section.Add(label);

        return section;
    }

    static VisualElement CreateRow()
    {
        VisualElement row = new();
        row.AddToClassList("runi-menu-test__row");
        return row;
    }

    void AddDropDownButton(VisualElement row, string text, Func<RuniMenu> build)
    {
        Button button = new() { text = text };
        button.clicked += () => DropDown(GetMenu(text, build), button);
        row.Add(button);
    }

    #endregion

    #region Menus

    RuniMenu BuildBasicMenu()
    {
        RuniMenu menu = new();
        menu.Add(Item("New Tab", new KeyShortcut(KeyCode.T, true)));
        menu.Add(Item("Close Tab", new KeyShortcut(KeyCode.W, true)));
        menu.Add(new Separator());

        RuniMenuItem disabled = Item("Disabled item");
        disabled.SetEnabled(false);
        menu.Add(disabled);

        RuniMenuItem checkedItem = Item("Checked (closes)");
        checkedItem.isChecked = true;
        checkedItem.clicked += () => checkedItem.isChecked = !checkedItem.isChecked;
        menu.Add(checkedItem);

        RuniMenuItem keepOpen = Item("Checked (stays open)");
        keepOpen.closeOnClick = false;
        keepOpen.clicked += () => keepOpen.isChecked = !keepOpen.isChecked;
        menu.Add(keepOpen);

        menu.Add(new Separator());

        RuniMenu sub = new("Submenu");
        sub.Add(Item("Child A"));
        sub.Add(Item("Child B"));

        RuniMenu subSub = new("Sub-submenu");
        subSub.Add(Item("Grandchild"));
        sub.Add(subSub);

        menu.Add(sub);
        menu.Add(Item("After submenu"));
        return menu;
    }

    RuniMenu BuildIconAndShortcutMenu()
    {
        RuniMenu menu = new();
        menu.Add(Item("Folder icon", default, folderIcon));
        menu.Add(Item("Computer icon", new KeyShortcut(KeyCode.Alpha1, true), computerIcon));
        menu.Add(Item("No icon", new KeyShortcut(KeyCode.F5)));
        menu.Add(Item("Network icon + checked", new KeyShortcut(KeyCode.N, true, EventModifiers.Shift), networkIcon, true));
        menu.Add(new Separator());
        menu.Add(Item("Action+Shift+Alt+Delete", new KeyShortcut(KeyCode.Delete, true, EventModifiers.Shift | EventModifiers.Alt)));
        menu.Add(Item("Ctrl only (no action key)", new KeyShortcut(KeyCode.K, false, EventModifiers.Control)));
        menu.Add(Item("Cmd only", new KeyShortcut(KeyCode.K, false, EventModifiers.Command)));
        menu.Add(Item("Alpha0", new KeyShortcut(KeyCode.Alpha0, true)));
        menu.Add(Item("Keypad Enter", new KeyShortcut(KeyCode.KeypadEnter)));
        menu.Add(Item("F12 + all modifiers", new KeyShortcut(KeyCode.F12, true, EventModifiers.Shift | EventModifiers.Alt | EventModifiers.Control | EventModifiers.Command)));
        menu.Add(new Separator());

        RuniMenuItem redo = Item("Two shortcuts (Redo)");
        redo.shortcuts = new[] { new KeyShortcut(KeyCode.Y, true), new KeyShortcut(KeyCode.Z, true, EventModifiers.Shift) };
        menu.Add(redo);

        RuniMenuItem many = Item("Five shortcuts", default, computerIcon);
        many.shortcuts = new[] { new KeyShortcut(KeyCode.F1), new KeyShortcut(KeyCode.F2, true), new KeyShortcut(KeyCode.Alpha3, true, EventModifiers.Shift), new KeyShortcut(KeyCode.Delete), new KeyShortcut(KeyCode.Escape, false, EventModifiers.Alt) };
        menu.Add(many);

        RuniMenuItem withNone = Item("None entries are skipped");
        withNone.shortcuts = new[] { default(KeyShortcut), new KeyShortcut(KeyCode.Q, true), default(KeyShortcut) };
        menu.Add(withNone);

        RuniMenuItem swap = Item("Click to cycle 0 → 1 → 3 shortcuts (stays open)");
        swap.closeOnClick = false;
        swap.clicked += () => swap.shortcuts = swap.shortcuts.Length switch
        {
            0 => new[] { new KeyShortcut(KeyCode.S, true) },
            1 => new[] { new KeyShortcut(KeyCode.S, true), new KeyShortcut(KeyCode.S, true, EventModifiers.Shift), new KeyShortcut(KeyCode.F12) },
            _ => Array.Empty<KeyShortcut>()
        };
        menu.Add(swap);

        RuniMenu sub = new("Submenu with icon");
        sub.iconManipulator.assetRef = folderIcon.icon;
        sub.iconManipulator.editorDarkAssetRef = folderIcon.editorDarkIcon;
        sub.Add(Item("Inside", new KeyShortcut(KeyCode.I, true), computerIcon));
        menu.Add(sub);

        return menu;
    }

    RuniMenu BuildDeepMenu(int depth)
    {
        RuniMenu root = new();
        RuniMenu current = root;

        for (int level = 1; level <= depth; level++)
        {
            current.Add(Item($"Level {level} - A"));
            current.Add(Item($"Level {level} - B"));

            RuniMenu next = new($"Go deeper ({level + 1})");
            current.Add(next);
            current.Add(Item($"Level {level} - after"));
            current = next;
        }

        current.Add(Item("Bottom!"));
        return root;
    }

    RuniMenu BuildWideMenu()
    {
        RuniMenu menu = new();
        for (int i = 0; i < 40; i++)
        {
            RuniMenu sub = new($"Group {i}");
            for (int j = 0; j < 40; j++)
                sub.Add(Item($"Group {i} / Item {j}"));

            menu.Add(sub);
        }

        return menu;
    }

    RuniMenu BuildHugeMenu(int count)
    {
        RuniMenu menu = new();
        for (int i = 0; i < count; i++)
        {
            if (i % 50 == 49)
                menu.Add(new Separator());

            RuniMenuItem item = Item($"Item {i}", i % 10 == 0 ? new KeyShortcut(KeyCode.Alpha0 + i / 10 % 10, true) : default);
            if (i % 7 == 0)
                item.SetEnabled(false);

            menu.Add(item);
        }

        return menu;
    }

    RuniMenu BuildLongTextMenu()
    {
        RuniMenu menu = new();
        menu.Add(Item(string.Concat(Enumerable.Repeat("Very long label ", 25))));
        menu.Add(Item("Short", new KeyShortcut(KeyCode.F12, true, EventModifiers.Shift | EventModifiers.Alt | EventModifiers.Control | EventModifiers.Command)));
        menu.Add(Item("Multi\nline\nlabel"));
        menu.Add(Item(string.Empty));
        menu.Add(Item("    leading and trailing spaces    "));
        menu.Add(Item("Unicode: 한글 日本語 العربية 🙂"));

        RuniMenu sub = new(string.Concat(Enumerable.Repeat("Long submenu header ", 10)));
        sub.Add(Item(string.Concat(Enumerable.Repeat("Child ", 60))));
        menu.Add(sub);

        return menu;
    }

    RuniMenu BuildMixedMenu()
    {
        RuniMenu menu = new();
        menu.Add(Item("RuniMenuItem"));

        Button button = new() { text = "Plain Button (does not close)" };
        button.clicked += () => Log("Plain button clicked.");
        menu.Add(button);

        Button closingButton = new() { text = "Button that closes via GetMenu" };
        closingButton.clicked += () =>
        {
            Log("Closing button clicked.");
            RuniMenu.GetMenu(closingButton)?.CloseAll();
        };
        menu.Add(closingButton);

        Toggle toggle = new("Toggle");
        toggle.RegisterValueChangedCallback(evt => Log($"Toggle: {evt.newValue}"));
        menu.Add(toggle);

        TextField textField = new("TextField (↑↓ belong to it)") { value = "type here" };
        textField.RegisterValueChangedCallback(evt => Log($"TextField: {evt.newValue}"));
        menu.Add(textField);

        Slider slider = new("Slider", 0, 100) { value = 50 };
        menu.Add(slider);

        menu.Add(new IntegerField("IntegerField") { value = 42 });
        menu.Add(new Label("Plain Label (not focusable)"));
        menu.Add(new UnityEngine.UIElements.ProgressBar { title = "ProgressBar", value = 70 });

        Foldout foldout = new() { text = "Foldout with items (not direct children)" };
        foldout.Add(Item("Item inside foldout"));
        foldout.Add(Item("Another one"));
        menu.Add(foldout);

        VisualElement box = new() { style = { height = 30, backgroundColor = new Color(0.4f, 0.2f, 0.6f, 0.5f) } };
        box.Add(new Label("Non-focusable colored box"));
        menu.Add(box);

        RuniMenu sub = new("Submenu after widgets");
        sub.Add(new TextField("Text in submenu"));
        sub.Add(Item("Item in submenu"));
        menu.Add(sub);

        return menu;
    }

    RuniMenu BuildDegenerateMenu()
    {
        RuniMenu menu = new();
        menu.Add(new RuniMenu("Empty submenu"));

        RuniMenu onlyDisabled = new("Only disabled items");
        for (int i = 0; i < 3; i++)
        {
            RuniMenuItem item = Item($"Disabled {i}");
            item.SetEnabled(false);
            onlyDisabled.Add(item);
        }

        menu.Add(onlyDisabled);

        RuniMenu onlySeparators = new("Only separators");
        for (int i = 0; i < 5; i++)
            onlySeparators.Add(new Separator());

        menu.Add(onlySeparators);

        RuniMenu onlyLabels = new("Only non-focusable labels");
        onlyLabels.Add(new Label("Label 1"));
        onlyLabels.Add(new Label("Label 2"));
        menu.Add(onlyLabels);

        RuniMenu disabledSub = new("Disabled submenu");
        disabledSub.Add(Item("You should not see this"));
        disabledSub.SetEnabled(false);
        menu.Add(disabledSub);

        RuniMenu hidden = new("Hidden by display: none");
        hidden.Add(Item("Hidden"));
        menu.Add(hidden);
        menu.Add(Item("Item after hidden submenu (hidden one should be skipped)"));
        hidden.style.display = DisplayStyle.None;

        RuniMenuItem invisible = Item("visibility: hidden item");
        invisible.style.visibility = Visibility.Hidden;
        menu.Add(invisible);

        menu.Add(new Separator());
        menu.Add(Item("Last real item"));
        return menu;
    }

    int dynamicOpenCount;

    RuniMenu BuildDynamicMenu()
    {
        RuniMenu menu = new();

        RuniMenuItem counter = Item(string.Empty);
        counter.closeOnClick = false;
        menu.Add(counter);

        RuniMenuItem clock = Item(string.Empty);
        clock.closeOnClick = false;
        menu.Add(clock);

        // 팝업이 열려 있는 동안만 스케줄러가 돕니다.
        menu.popup.schedule.Execute(() => clock.text = $"Live clock: {DateTime.Now:HH:mm:ss.fff}").Every(50);

        RuniMenu random = new("Random items (rebuilt when this submenu opens)");
        menu.Add(random);

        menu.opened += () =>
        {
            dynamicOpenCount++;
            counter.text = $"Opened {dynamicOpenCount} times";
        };

        // 하위 메뉴의 opened는 배치 전에 불리므로, 열면서 바뀐 내용으로 크기가 잡혀야 합니다.
        random.opened += () =>
        {
            random.Clear();
            int count = UnityEngine.Random.Range(0, 20);
            for (int i = 0; i < count; i++)
                random.Add(Item($"Random {i} / {count}"));
        };

        menu.Add(new Separator());

        int added = 0;
        RuniMenuItem add = Item("Add item at end (stays open)");
        add.closeOnClick = false;
        add.clicked += () => menu.Add(Item($"Added {++added}"));
        menu.Add(add);

        RuniMenuItem insert = Item("Insert item at top (stays open)");
        insert.closeOnClick = false;
        insert.clicked += () => menu.Insert(0, Item($"Inserted {++added}"));
        menu.Add(insert);

        RuniMenuItem remove = Item("Remove last child (stays open)");
        remove.closeOnClick = false;
        remove.clicked += () =>
        {
            if (menu.childCount > 0)
                menu.RemoveAt(menu.childCount - 1);
        };
        menu.Add(remove);

        RuniMenuItem rename = Item("Rename me (stays open)");
        rename.closeOnClick = false;
        rename.clicked += () => rename.text = $"Renamed at {DateTime.Now:HH:mm:ss.fff}";
        menu.Add(rename);

        RuniMenuItem target = Item("Target of enable toggle");
        RuniMenuItem toggleEnabled = Item("Toggle enabled of next item (stays open)");
        toggleEnabled.closeOnClick = false;
        toggleEnabled.clicked += () => target.SetEnabled(!target.enabledSelf);
        menu.Add(toggleEnabled);
        menu.Add(target);

        RuniMenuItem removeSelf = Item("Remove myself (stays open)");
        removeSelf.closeOnClick = false;
        removeSelf.clicked += () => removeSelf.RemoveFromHierarchy();
        menu.Add(removeSelf);

        menu.Add(new Separator());

        RuniMenuItem[] radios = new RuniMenuItem[3];
        for (int i = 0; i < radios.Length; i++)
        {
            int index = i;
            radios[i] = Item($"Radio {i} (stays open)");
            radios[i].closeOnClick = false;
            radios[i].clicked += () =>
            {
                for (int j = 0; j < radios.Length; j++)
                    radios[j].isChecked = j == index;
            };

            menu.Add(radios[i]);
        }

        radios[0].isChecked = true;
        return menu;
    }

    RuniMenu BuildReentrantMenu()
    {
        RuniMenu menu = new();

        RuniMenuItem reopen = Item("Reopen at random position (stays open)");
        reopen.closeOnClick = false;
        reopen.clicked += () =>
        {
            Rect bounds = rootVisualElement.worldBound;
            Vector2 position = new(UnityEngine.Random.Range(bounds.xMin, bounds.xMax), UnityEngine.Random.Range(bounds.yMin, bounds.yMax));
            menu.DropDown(new Rect(position, Vector2.zero), rootVisualElement);
        };
        menu.Add(reopen);

        RuniMenuItem openOther = Item("Open another menu from here");
        openOther.clicked += () =>
        {
            RuniMenu other = GetMenu("Basic", BuildBasicMenu);
            ApplySettings(other);
            other.DropDown(openOther.worldBound, rootVisualElement);
        };
        menu.Add(openOther);

        RuniMenuItem closeThenOpen = Item("Close, then reopen next frame");
        closeThenOpen.clicked += () => rootVisualElement.schedule.Execute(() => menu.DropDown(closeThenOpen.worldBound, rootVisualElement));
        menu.Add(closeThenOpen);

        RuniMenuItem rapid = Item("Rapid open/close x20 (stays open)");
        rapid.closeOnClick = false;
        rapid.clicked += () =>
        {
            Rect anchor = rapid.worldBound;
            for (int i = 0; i < 20; i++)
            {
                menu.Close(false);
                menu.DropDown(anchor, rootVisualElement);
            }
        };
        menu.Add(rapid);

        RuniMenuItem throws = Item("Throw exception in clicked");
        throws.clicked += () => throw new InvalidOperationException("Test exception from RuniMenuItem.clicked");
        menu.Add(throws);

        RuniMenuItem dialog = Item("Show modal dialog");
        dialog.clicked += () => EditorUtility.DisplayDialog("Runi Menu Test", "Modal dialog opened from a menu item.", "OK");
        menu.Add(dialog);

        RuniMenuItem closeWindow = Item("Close this window");
        closeWindow.clicked += Close;
        menu.Add(closeWindow);

        RuniMenu sub = new("Submenu that closes everything");
        RuniMenuItem closeAll = Item("CloseAll from here");
        sub.Add(closeAll);
        RuniMenuItem closeParentOnly = Item("Close() only this submenu (stays open flag)");
        closeParentOnly.closeOnClick = false;
        closeParentOnly.clicked += () => sub.Close();
        sub.Add(closeParentOnly);
        menu.Add(sub);

        return menu;
    }

    RuniMenu BuildPerMenuDelayMenu()
    {
        RuniMenu menu = new();

        RuniMenu instant = new("openDelay = 0") { openDelay = 0 };
        instant.Add(Item("Opened instantly"));
        menu.Add(instant);

        RuniMenu slow = new("openDelay = 3000") { openDelay = 3000 };
        slow.Add(Item("Took 3 seconds"));
        menu.Add(slow);

        RuniMenu noHover = new("openOnHover = false") { openOnHover = false };
        noHover.Add(Item("Click, Submit or → only"));
        menu.Add(noHover);

        RuniMenu noFocus = new("openOnFocus = false") { openOnFocus = false };
        noFocus.Add(Item("Hover, click, Submit or → only"));
        menu.Add(noFocus);

        RuniMenu neither = new("openOnHover = false, openOnFocus = false") { openOnHover = false, openOnFocus = false };
        neither.Add(Item("Click, Submit or → only"));
        menu.Add(neither);

        RuniMenu lingering = new("closeDelay = 1500 on children") { closeDelay = 1500 };
        for (int i = 0; i < 4; i++)
        {
            RuniMenu child = new($"Child {i} (lingers after leaving)");
            child.Add(Item($"Inside child {i}"));
            lingering.Add(child);
        }

        menu.Add(lingering);

        RuniMenu sizeModes = new("Submenus ignore sizeMode");
        sizeModes.Add(Item("x"));
        menu.Add(sizeModes);

        // 이 메뉴의 설정은 창의 설정으로 덮어쓰지 않습니다.
        menu.userData = keepOwnSettings;
        return menu;
    }

    static readonly object keepOwnSettings = new();

    RuniMenu BuildContextMenu()
    {
        RuniMenu menu = new();
        menu.Add(Item("Cut", new KeyShortcut(KeyCode.X, true)));
        menu.Add(Item("Copy", new KeyShortcut(KeyCode.C, true)));
        menu.Add(Item("Paste", new KeyShortcut(KeyCode.V, true)));
        menu.Add(new Separator());

        RuniMenu more = BuildDeepMenu(4);
        more.text = "More (4 levels)";
        menu.Add(more);

        RuniMenu huge = BuildHugeMenu(200);
        huge.text = "Huge submenu (200)";
        menu.Add(huge);

        return menu;
    }

    #endregion

    #region Helpers

    RuniMenuItem Item(string text, KeyShortcut shortcut = default, (AssetRef<VectorImage> icon, AssetRef<VectorImage> editorDarkIcon)? icon = null, bool isChecked = false)
    {
        RuniMenuItem item = new(text, null) { shortcuts = new[] { shortcut }, isChecked = isChecked };
        item.clicked += () => Log($"Clicked: {item.text}");

        if (icon.HasValue)
        {
            item.iconManipulator.assetRef = icon.Value.icon;
            item.iconManipulator.editorDarkAssetRef = icon.Value.editorDarkIcon;
        }

        return item;
    }

    RuniMenu GetMenu(string key, Func<RuniMenu> build)
    {
        if (!menus.TryGetValue(key, out RuniMenu? menu))
        {
            menu = build();
            HookAll(menu);
            menus.Add(key, menu);
        }

        return menu;
    }

    void DropDown(RuniMenu menu, VisualElement anchor)
    {
        ApplySettings(menu);
        menu.DropDown(anchor.worldBound, anchor);
    }

    IEnumerable<RuniMenu> AllMenus(RuniMenu root) => new[] { root }.Concat(root.Query<RuniMenu>().ToList()).Distinct();

    void HookAll(RuniMenu root)
    {
        foreach (RuniMenu menu in AllMenus(root))
        {
            RuniMenu captured = menu;
            menu.opened += () => Log($"Opened: {Describe(captured)}");
            menu.closed += () => Log($"Closed: {Describe(captured)}");
        }
    }

    void ApplySettings(RuniMenu root)
    {
        bool keep = ReferenceEquals(root.userData, keepOwnSettings);
        int transition = transitionField.index;

        foreach (RuniMenu menu in AllMenus(root))
        {
            if (!keep)
            {
                menu.openDelay = openDelayField.value;
                menu.closeDelay = closeDelayField.value;
                menu.openOnHover = openOnHoverToggle.value;
                menu.openOnFocus = openOnFocusToggle.value;
            }

            menu.sizeMode = (DropdownMenuSizeMode)sizeModeField.value;

            if (styleSheet != null && !menu.popup.styleSheets.Contains(styleSheet))
                menu.popup.styleSheets.Add(styleSheet);

            menu.popup.EnableInClassList(fadeUssClassName, transition == 1);
            menu.popup.EnableInClassList(slowFadeUssClassName, transition == 2);
            menu.popup.EnableInClassList(slideUssClassName, transition == 3);
            menu.popup.EnableInClassList(minWidthUssClassName, minWidthToggle.value);
        }
    }

    static string Describe(RuniMenu menu) => string.IsNullOrEmpty(menu.text) ? "(root)" : menu.text;

    void Log(string message)
    {
        logs.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        if (logs.Count > 12)
            logs.RemoveAt(logs.Count - 1);

        if (logLabel != null)
            logLabel.text = string.Join("\n", logs);
    }

    #endregion
}
#endif
