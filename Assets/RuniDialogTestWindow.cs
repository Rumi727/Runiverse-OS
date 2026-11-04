#nullable enable
#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using RuniOS.UIElements;
using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEngine.UIElements;

/// <summary>
/// RuniDialog를 다양한 구성과 극단적인 경우로 직접 띄워 보는 테스트 창입니다.
/// </summary>
[NoAutoStaticsCleanup]
public sealed class RuniDialogTestWindow : EditorWindow
{
    [MenuItem("RuniOS/Runi Dialog Test")]
    static void Open() => GetWindow<RuniDialogTestWindow>("Runi Dialog Test");

    const string styleSheetPath = "Assets/RuniDialogTestWindow.uss";
    const string fadeUssClassName = "runi-dialog-test--fade";

    readonly List<string> logs = new();

    StyleSheet? styleSheet;

    Toggle dismissToggle = null!;
    Toggle fadeToggle = null!;
    Toggle defaultButtonToggle = null!;
    Label? logLabel;

    void CreateGUI()
    {
        styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(styleSheetPath);
        if (styleSheet != null)
            rootVisualElement.styleSheets.Add(styleSheet);

        ScrollView scrollView = new(ScrollViewMode.Vertical) { style = { flexGrow = 1 } };
        rootVisualElement.Add(scrollView);

        scrollView.Add(CreateSettings());
        scrollView.Add(CreateBasicSection());
        scrollView.Add(CreateInteractionSection());
        scrollView.Add(CreateLifetimeSection());

        logLabel = new Label { name = "log" };
        logLabel.AddToClassList("runi-dialog-test__log");
        rootVisualElement.Add(logLabel);
        Log("Ready.");
    }

    #region Layout

    VisualElement CreateSettings()
    {
        VisualElement section = CreateSection("Settings (applied to every dialog when it opens)");

        VisualElement row = CreateRow();
        dismissToggle = new Toggle("Dismiss on overlay click");
        fadeToggle = new Toggle("Fade (0.3s)");
        defaultButtonToggle = new Toggle("Set default button") { value = true };
        row.Add(dismissToggle);
        row.Add(fadeToggle);
        row.Add(defaultButtonToggle);
        row.Add(new Button(() => { logs.Clear(); Log("Log cleared."); }) { text = "Clear log" });
        section.Add(row);

        // 대화 상자가 뒤쪽 입력을 막는지 확인하는 용도입니다.
        row = CreateRow();
        row.Add(new TextField("Background field"));
        row.Add(new Button(() => Log("Background button clicked.")) { text = "Background button" });
        section.Add(row);

        return section;
    }

    VisualElement CreateBasicSection()
    {
        VisualElement section = CreateSection("Basic");

        VisualElement row = CreateRow();
        AddShowButton(row, "Delete confirm", BuildDeleteDialog);
        AddShowButton(row, "No title", () => Build(string.Empty, "제목 없이 메시지만 있습니다.", "확인", "취소"));
        AddShowButton(row, "No message", () => Build("메시지 없음", string.Empty, "확인", "취소"));
        AddShowButton(row, "Empty", () => Build(string.Empty, string.Empty, "확인"));
        AddShowButton(row, "No buttons (Esc only)", () => Build("버튼 없음", "Esc로만 닫을 수 있습니다. (바깥 클릭 설정을 켜면 바깥 클릭도)"));
        section.Add(row);

        row = CreateRow();
        AddShowButton(row, "Long message", BuildLongDialog);
        AddShowButton(row, "Long word", () => Build("긴 단어", new string('W', 300), "확인"));
        AddShowButton(row, "Many buttons", () => Build("버튼 여러 개", "Tab과 Shift+Tab으로 끝에서 반대쪽 끝으로 돌아가는지 확인합니다.", "하나", "둘", "셋", "넷", "다섯", "여섯", "일곱"));
        section.Add(row);

        return section;
    }

    VisualElement CreateInteractionSection()
    {
        VisualElement section = CreateSection("Interaction");

        VisualElement row = CreateRow();
        AddShowButton(row, "With content", BuildContentDialog);
        AddShowButton(row, "Nested", BuildNestedDialog);
        AddShowButton(row, "Reopen while open", BuildReopenDialog);

        Button fromMenu = new() { text = "From RuniMenu item" };
        fromMenu.clicked += () =>
        {
            RuniMenu menu = new();
            menu.Add(new RuniMenuItem("Delete…", () => Show(BuildDeleteDialog(), "From RuniMenu item", fromMenu)));
            menu.Add(new RuniMenuItem("Other item", () => Log("Other item clicked.")));
            menu.DropDown(fromMenu.worldBound, fromMenu);
        };
        row.Add(fromMenu);

        section.Add(row);

        return section;
    }

    VisualElement CreateLifetimeSection()
    {
        VisualElement section = CreateSection("Lifetime");

        VisualElement row = CreateRow();

        Button autoClose = new() { text = "Close() after 3s" };
        autoClose.clicked += () =>
        {
            RuniDialog dialog = Build("자동 닫기", "3초 뒤에 Close()로 닫힙니다.", "확인");
            Show(dialog, autoClose.text, autoClose);
            rootVisualElement.schedule.Execute(() => dialog.Close()).StartingIn(3000);
        };
        row.Add(autoClose);

        Button detach = new() { text = "Target detaches after 2s" };
        detach.clicked += () =>
        {
            VisualElement target = new();
            rootVisualElement.Add(target);

            Show(Build("대상 분리", "2초 뒤에 대상 요소가 패널에서 떨어집니다.", "확인"), detach.text, target);
            rootVisualElement.schedule.Execute(target.RemoveFromHierarchy).StartingIn(2000);
        };
        row.Add(detach);

        Button detached = new() { text = "Target not in panel" };
        detached.clicked += () => Show(Build("표시 안 됨", "보이면 안 됩니다.", "확인"), detached.text, new VisualElement());
        row.Add(detached);

        Button twice = new() { text = "Same dialog twice (sync)" };
        twice.clicked += () =>
        {
            RuniDialog dialog = Build("두 번 열기", "같은 대화 상자를 연달아 두 번 띄웠습니다. 첫 번째 결과는 null이어야 합니다.", "확인");
            Show(dialog, "twice #1", twice);
            Show(dialog, "twice #2", twice);
        };
        row.Add(twice);

        Button loop = new() { text = "Same dialog x3 (await loop)" };
        loop.clicked += () => ShowLoopAsync(loop).Forget();
        row.Add(loop);

        section.Add(row);

        return section;
    }

    static VisualElement CreateSection(string title)
    {
        VisualElement section = new();
        section.AddToClassList("runi-dialog-test__section");

        Label label = new(title);
        label.AddToClassList("runi-dialog-test__section-title");
        section.Add(label);

        return section;
    }

    static VisualElement CreateRow()
    {
        VisualElement row = new();
        row.AddToClassList("runi-dialog-test__row");
        return row;
    }

    void AddShowButton(VisualElement row, string text, Func<RuniDialog> build)
    {
        Button button = new() { text = text };
        button.clicked += () => Show(build(), text, button);
        row.Add(button);
    }

    #endregion

    #region Dialogs

    RuniDialog Build(string title, string message, params string[] buttons)
    {
        RuniDialog dialog = new(title, message);
        foreach (string text in buttons)
            dialog.AddButton(text);

        return dialog;
    }

    RuniDialog BuildDeleteDialog()
    {
        RuniDialog dialog = new("삭제", "'새 폴더'을(를) 삭제할까요?");
        dialog.defaultButton = dialog.AddButton("휴지통으로 이동");
        dialog.AddButton("영구 삭제");
        dialog.AddButton("취소");

        return dialog;
    }

    RuniDialog BuildLongDialog()
    {
        string message = string.Join(" ", Enumerable("긴 메시지가 대화 상자의 최대 너비 안에서 줄바꿈되는지 확인합니다.", 20));
        return Build("긴 메시지", message, "확인", "취소");

        static IEnumerable<string> Enumerable(string text, int count)
        {
            for (int i = 0; i < count; i++)
                yield return text;
        }
    }

    RuniDialog BuildContentDialog()
    {
        RuniDialog dialog = Build("내용 포함", "내용 안의 필드, 드롭다운, RuniMenu 팝업이 대화 상자 위에 뜨고 포커스를 받는지 확인합니다.", "확인", "취소");

        dialog.content.Add(new Toggle("모두 적용"));
        dialog.content.Add(new TextField("Name") { value = "새 폴더" });
        dialog.content.Add(new DropdownField("Dropdown", new List<string> { "A", "B", "C" }, 0));

        RuniMenu menu = new("RuniMenu button");
        menu.Add(new RuniMenuItem("Item A", () => Log("Content menu: Item A")));
        menu.Add(new RuniMenuItem("Item B", () => Log("Content menu: Item B")));
        dialog.content.Add(menu);

        return dialog;
    }

    RuniDialog BuildNestedDialog()
    {
        RuniDialog dialog = Build("바깥 대화 상자", "안쪽 대화 상자를 열고 닫은 뒤 포커스가 이 대화 상자로 돌아오는지 확인합니다.", "닫기");

        Button openInner = new() { text = "Open inner dialog" };
        openInner.clicked += () => Show(BuildDeleteDialog(), "Inner", openInner);
        dialog.content.Add(openInner);

        return dialog;
    }

    RuniDialog BuildReopenDialog()
    {
        RuniDialog dialog = Build("다시 열기", "아래 버튼은 열려 있는 이 대화 상자를 다시 띄웁니다. 이전 결과는 null이어야 합니다.", "확인");

        Button reopen = new() { text = "ShowAsync again" };
        reopen.clicked += () => Show(dialog, "Reopened", reopen);
        dialog.content.Add(reopen);

        return dialog;
    }

    // 결과를 받은 바로 그 자리에서 같은 대화 상자를 다시 띄웁니다. 덮어쓰기 확인처럼 파일마다 묻는 경우입니다.
    async UniTaskVoid ShowLoopAsync(VisualElement target)
    {
        RuniDialog dialog = Build("반복", string.Empty, "덮어쓰기", "건너뛰기", "취소");
        for (int i = 1; i <= 3; i++)
        {
            dialog.message = $"파일 {i}/3을(를) 덮어쓸까요?";
            Button? result = await ShowAsync(dialog, $"Loop {i}", target);
            if (result == null || result.text == "취소")
                break;
        }
    }

    void Show(RuniDialog dialog, string name, VisualElement target) => ShowAsync(dialog, name, target).Forget();

    async UniTask<Button?> ShowAsync(RuniDialog dialog, string name, VisualElement target)
    {
        dialog.dismissOnOverlayClick = dismissToggle.value;
        dialog.overlay.EnableInClassList(fadeUssClassName, fadeToggle.value);

        if (!defaultButtonToggle.value)
            dialog.defaultButton = null;
        else if (dialog.defaultButton == null && dialog.buttonContainer.childCount > 0)
            dialog.defaultButton = dialog.buttonContainer[0] as Button;

        if (styleSheet != null && !dialog.overlay.styleSheets.Contains(styleSheet))
            dialog.overlay.styleSheets.Add(styleSheet);

        dialog.opened += OnOpened;
        dialog.closed += OnClosed;

        Log($"{name}: show");
        Button? result = await dialog.ShowAsync(target);
        Log($"{name}: result = {(result != null ? $"'{result.text}'" : "null")}, focused = {Describe(target.focusController?.focusedElement ?? rootVisualElement.focusController?.focusedElement)}");

        dialog.opened -= OnOpened;
        dialog.closed -= OnClosed;

        return result;

        void OnOpened() => Log($"{name}: opened");
        void OnClosed() => Log($"{name}: closed");
    }

    static string Describe(Focusable? focusable) => focusable switch
    {
        null => "none",
        Button button => $"Button '{button.text}'",
        VisualElement element => $"{element.GetType().Name} '{element.name}'",
        _ => focusable.GetType().Name
    };

    #endregion

    void Log(string message)
    {
        logs.Insert(0, $"[{DateTime.Now:HH:mm:ss.fff}] {message}");
        if (logs.Count > 12)
            logs.RemoveAt(logs.Count - 1);

        if (logLabel != null)
            logLabel.text = string.Join("\n", logs);
    }
}
#endif
