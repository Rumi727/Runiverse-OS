#nullable enable
#if UNITY_EDITOR
using Cysharp.Threading.Tasks;
using RuniOS.IO;
using RuniOS.IO.Virtual;
using RuniOS.UIElements;
using RuniOS.UIElements.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

/// <summary>
/// UI Builder에서는 우클릭 등의 입력을 캔버스가 가져가므로, 에디터 창에서 IO 브라우저를 직접 테스트합니다.
/// 왼쪽과 오른쪽에 서로 다른 경우의 브라우저를 띄워, 그 사이의 드래그 앤 드롭과 클립보드를 시험합니다.
/// </summary>
public sealed class IOBrowserTestWindow : EditorWindow
{
    [MenuItem("RuniOS/IO Browser Test")]
    static void Open() => GetWindow<IOBrowserTestWindow>("IO Browser Test");

    // 사용자 폴더를 건드리지 않도록 임시 폴더에 만듭니다.
    static readonly string testRoot = Path.Combine(Path.GetTempPath(), "RuniOS IO Browser Test");

    // 같은 인스턴스를 고른 두 브라우저 사이에서는 이동할 수 있고, 다른 인스턴스 사이에서는 복사만 됩니다.
    PhysicalIOProvider sharedProvider = null!;
    VirtualIOProvider virtualProvider = null!;

    readonly List<(string name, Func<IOBrowser> create)> cases = new();

    Pane leftPane = null!;
    Pane rightPane = null!;

    void CreateGUI()
    {
        CreateProviders();

        cases.Clear();
        cases.Add(("File system", () => new FileSystemBrowser()));
        cases.Add(("Test folder (shared instance)", () => new IONodeBrowser(sharedProvider)));
        cases.Add(("Test folder (separate instance, same disk)", () => new IONodeBrowser(new PhysicalIOProvider(PhysicalPath.From(testRoot)))));
        cases.Add(("Test folder/A (separate instance, nested root)", () => new IONodeBrowser(new PhysicalIOProvider(PhysicalPath.From(Path.Combine(testRoot, "A"))))));
        cases.Add(("Test folder (read-only provider)", () => new IONodeBrowser(new GroupIOProvider(new IIOProvider[] { sharedProvider }, true))));
        cases.Add(("Virtual (in-memory, no links)", () => new IONodeBrowser(virtualProvider)));

        Toolbar toolbar = new();
        rootVisualElement.Add(toolbar);

        ToolbarGroup group = new();
        group.Add(new Button(() => ResetTestFolder().Forget()) { text = "Reset test folder" });
        group.Add(new Button(CreateLargeFile) { text = "Create 1 GB file" });
        group.Add(new Button(() => EditorUtility.RevealInFinder(testRoot)) { text = "Reveal" });
        toolbar.Add(group);

        TwoPaneSplitView splitView = new(0, 600, TwoPaneSplitViewOrientation.Horizontal) { style = { flexGrow = 1 } };
        leftPane = new Pane(cases, 0);
        rightPane = new Pane(cases, 1);
        splitView.Add(leftPane);
        splitView.Add(rightPane);
        rootVisualElement.Add(splitView);

        SeedAndShow().Forget();
    }

    void CreateProviders()
    {
        Directory.CreateDirectory(testRoot);
        sharedProvider = new PhysicalIOProvider(PhysicalPath.From(testRoot));
        virtualProvider = new VirtualIOProvider(new VirtualDirectory());
    }

    async UniTaskVoid SeedAndShow()
    {
        if (!File.Exists(Path.Combine(testRoot, "A/같은 이름.txt")))
            await SeedPhysical();

        await SeedVirtual();

        leftPane.Rebuild();
        rightPane.Rebuild();
    }

    async UniTaskVoid ResetTestFolder()
    {
        if (Directory.Exists(testRoot))
            Directory.Delete(testRoot, true);

        CreateProviders();
        await SeedPhysical();
        await SeedVirtual();

        leftPane.Rebuild();
        rightPane.Rebuild();
    }

    // 다른 프로바이더로 복사할 때의 진행 대화 상자와 취소를 시험합니다. 희소 파일이라 바로 만들어지지만, 복사할 때는 1 GB를 모두 읽고 씁니다.
    static void CreateLargeFile()
    {
        Directory.CreateDirectory(Path.Combine(testRoot, "Large"));
        using FileStream stream = File.Create(Path.Combine(testRoot, "Large", "large.bin"));
        stream.SetLength(1L << 30);
    }

    async UniTask SeedPhysical()
    {
        await SeedCommon(sharedProvider, "physical");

        // 리눅스 등은 이름 하나가 255바이트까지라, UTF-8로 3바이트인 한글은 80자 안쪽이어야 합니다.
        await Write(sharedProvider, $"A/{new string('긴', 80)}.txt", "긴 이름");
    }

    // 실제 파일 시스템과 같은 이름을 넣어, 가상과 실제 사이에 복사할 때도 이름이 겹치게 합니다.
    async UniTask SeedVirtual()
    {
        await SeedCommon(virtualProvider, "virtual");

        // 가상 디렉터리는 이름 길이 제한이 없으므로, 실제 파일 시스템으로 복사하면 실패해야 합니다.
        await Write(virtualProvider, $"가상 전용/{new string('긴', 200)}.txt", "실제로 복사할 수 없는 긴 이름");

        // Windows에서 쓸 수 없는 문자와, 끝이 공백이나 점인 이름입니다.
        await Write(virtualProvider, "가상 전용/콜론:별표*물음표?.txt", "Windows에서 쓸 수 없는 이름");
        await Write(virtualProvider, "가상 전용/끝이 공백 ", "trailing space");
        await Write(virtualProvider, "가상 전용/끝이 점.", "trailing dot");
        await Write(virtualProvider, "가상 전용/역슬래시\\이름.txt", "backslash");

        // 가상 디렉터리는 항상 대소문자를 구분하므로, 대소문자를 구분하지 않는 파일 시스템으로 복사하면 겹칩니다.
        await Write(virtualProvider, "가상 전용/대소문자/a.txt", "a");
        await Write(virtualProvider, "가상 전용/대소문자/A.txt", "A");
        await virtualProvider.CreateDirectory((RuniPath)"가상 전용/대소문자/폴더");
        await virtualProvider.CreateDirectory((RuniPath)"가상 전용/대소문자/FOLDER");

        await Write(virtualProvider, "가상 전용/빈 파일.txt", string.Empty);
        await Write(virtualProvider, "가상 전용/이모지 😀.txt", "emoji");
    }

    // 두 프로바이더에 같은 구조를 만듭니다. 내용에는 어느 쪽에서 만든 파일인지 적어, 복사된 뒤에도 출처를 알 수 있게 합니다.
    static async UniTask SeedCommon(IWritableIOProvider provider, string tag)
    {
        await Write(provider, "A/같은 이름.txt", $"{tag} A");
        await Write(provider, "B/같은 이름.txt", $"{tag} B");
        await Write(provider, "B/같은 이름 (2).txt", $"{tag} B2");

        // 폴더 합치기: 같은 이름의 폴더에 서로 다른 내용과 겹치는 파일이 있습니다.
        await Write(provider, "A/폴더/A만 있음.txt", $"{tag} A");
        await Write(provider, "A/폴더/겹침.txt", $"{tag} A");
        await Write(provider, "A/폴더/하위/A 하위.txt", $"{tag} A");
        await Write(provider, "B/폴더/B만 있음.txt", $"{tag} B");
        await Write(provider, "B/폴더/겹침.txt", $"{tag} B");
        await Write(provider, "B/폴더/하위/B 하위.txt", $"{tag} B");

        // 종류가 다른 충돌: 한쪽은 파일, 한쪽은 폴더입니다.
        await Write(provider, "A/파일과 폴더", $"{tag} file");
        await Write(provider, "B/파일과 폴더/안.txt", $"{tag} directory");

        await Write(provider, "A/.숨김.txt", "hidden");
        await Write(provider, "A/.bashrc", "확장자 없는 점 이름");
        await Write(provider, "A/확장자 여러 개.tar.gz", "multiple extensions");
        await Write(provider, "A/이름 #%&[]{}() 공백.txt", "특수 문자");
        await Write(provider, "A/  앞에 공백.txt", "leading space");
        await provider.CreateDirectory((RuniPath)"A/빈 폴더");

        // 대소문자만 다른 이름입니다. 대소문자를 구분하지 않는 파일 시스템에서는 하나로 합쳐집니다.
        await Write(provider, "Case/Readme.txt", "Readme");
        await Write(provider, "Case/README.txt", "README");

        await Write(provider, string.Join("/", Enumerable.Range(1, 12).Select(x => $"깊이 {x}")) + "/맨 아래.txt", "deep");

        // 진행 대화 상자가 뜰 만큼 항목이 많은 폴더입니다.
        for (int i = 0; i < 1000; i++)
            await Write(provider, $"Many/{i:D4}.txt", i.ToString());
    }

    static async UniTask Write(IWritableIOProvider provider, string path, string text)
    {
        RuniPath runiPath = (RuniPath)path;
        await provider.CreateDirectory(runiPath.GetParentPath());
        await provider.WriteAllText(runiPath, text);
    }

    // 경우를 고르는 드롭다운과 읽기 전용 토글이 위에 있고, 그 아래에 고른 경우의 브라우저가 있는 한쪽 창입니다.
    sealed class Pane : VisualElement
    {
        readonly List<(string name, Func<IOBrowser> create)> cases;
        readonly DropdownField caseField;
        readonly Toggle browserReadOnly = new("IOBrowser.readOnly");

        // 브라우저를 거치지 않고 목록의 readOnly를 직접 바꿉니다. 브라우저가 관리하는 값을 바깥에서 바꾸는 경우이며, 나중에 연 탭의 목록에는 걸리지 않습니다.
        readonly Toggle treeReadOnly = new("tree.readOnly directly (open tabs)");

        IOBrowser? browser;

        public Pane(List<(string name, Func<IOBrowser> create)> cases, int index)
        {
            this.cases = cases;
            style.flexGrow = 1;

            VisualElement header = new() { style = { flexDirection = FlexDirection.Row, flexWrap = Wrap.Wrap } };
            Add(header);

            caseField = new DropdownField(cases.Select(x => x.name).ToList(), index) { style = { width = 300 } };
            caseField.RegisterValueChangedCallback(_ => Rebuild());
            header.Add(caseField);

            browserReadOnly.RegisterValueChangedCallback(_ => ApplyReadOnly());
            header.Add(browserReadOnly);

            treeReadOnly.RegisterValueChangedCallback(_ => ApplyReadOnly());
            header.Add(treeReadOnly);
        }

        public void Rebuild()
        {
            browser?.RemoveFromHierarchy();

            browser = cases[caseField.index].create();
            browser.style.flexGrow = 1;
            Add(browser);

            ApplyReadOnly();
        }

        void ApplyReadOnly()
        {
            if (browser == null)
                return;

            // 브라우저의 readOnly가 모든 탭의 목록에 먼저 적용되고, 직접 바꾸는 토글이 켜져 있으면 그 위에 덮어씁니다.
            browser.readOnly = browserReadOnly.value;
            if (treeReadOnly.value)
                browser.Query<IONodeMultiColumnTreeView>().ForEach(static x => x.readOnly = true);
        }
    }
}
#endif
