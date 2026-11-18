# 문서 형식 규칙

완성 예시는 `Packages/io.github.rumi727.runios.nbs/Documentation~/`입니다. 규칙이 애매하면 그 파일들이 기준입니다.

## 폴더 구조

```
Packages/<package>/Documentation~/
  README.md              목차, 한 장으로 보기(Mermaid), 핵심 아이디어, 읽는 순서 표, 연관 패키지 표, 용어집, 파일 지도, 그림 목록
  01-<topic>.md          장 파일. 번호-주제, 소문자, 하이픈
  images/<name>.svg      정적 그림
  interactive/<name>.html 인터랙티브 페이지 (있을 때)
```

- 작은 패키지는 `README.md` 하나와 주제별 파일 몇 개면 됩니다. uielements처럼 하위 영역이 따로 있으면 `Documentation~/<Area>/README.md`를 씁니다.
- 폴더 이름이 `~`로 끝나야 Unity가 임포트하지 않습니다. `git check-ignore`로 무시되지 않는지 확인합니다.

## 머리말

장 파일은 이렇게 시작합니다.

```markdown
# 3. 클록 앵커: NoteBlockSource의 시간축

> 기준 커밋: `a90d9814`. [← 목차](README.md) · 이전: [2. 데이터 모델](02-data-model.md) · 다음: [4. 워커와 예약](04-worker-scheduling.md)
>
> 선행: [다른 패키지 문서 제목](../../<other package>/Documentation~/<file>.md)

한두 문단의 도입: 이 장이 설명하는 것과 그것이 왜 중요한지.

- [3.1 절 제목](#31-절-제목)
- …
```

- README의 머리말에는 기준 커밋, 패키지 README 링크, 인터랙티브 링크(있으면)를 둡니다.
- 하위 패키지 문서는 상위 패키지를 언급하지 않습니다. 링크 방향은 항상 상위 → 하위입니다.

## 코드 인용

- **실제 코드만 인용합니다.** 기억으로 재구성한 발췌는 쓰지 않습니다. 필드 목록 같은 요약이 필요하면 실제 선언 줄을 그대로 옮깁니다.
- **줄 번호는 실제 파일 기준입니다.**
  - XML 주석을 빼고 읽은 결과의 줄 번호는 밀려 있습니다.
  - `grep -n '심볼' 파일`이나 `awk 'NR>=a && NR<=b {print NR": "$0}' 파일`로 실제 번호를 얻습니다.
- **발췌 형식:**
  - 맨 위 주석에 패키지 기준 상대 경로를 적습니다.
  - 각 줄 앞에 실제 줄 번호를 붙입니다. 번호가 건너뛰는 곳은 생략한 곳입니다(XML 주석, 빈 줄).
  - 같은 줄의 이어지는 부분은 번호 없이 들여 씁니다.
  - 중간을 크게 생략하면 `…` 줄을 넣습니다.
  - 설명할 지점에 `// ① …` 표시를 달고, 발췌 아래에서 ①②로 설명합니다.

  ```csharp
  // Playback/NBSClockAnchor.cs
  68  public static bool TryGetNextMixClock(SoundSystem system, out ulong clock)
      {
  70      ArgumentNullException.ThrowIfNull(system);
  72      return system.Execute(static soundSystem => …, out clock);   // ① …
      }
  ```

- **코드 링크:** ``[`심볼`](../Runtime/Dir/File.cs#L39)``처럼 문서 파일 기준 상대 경로에 `#L번호`를 붙입니다. 링크 라벨에 그 줄 근처에 실제로 있는 심볼 이름을 넣으면 링크 검사가 맞는지 확인할 수 있습니다.
- **기준 커밋 이후 변경:** 코드가 바뀌면 줄 링크가 밀립니다. 마무리 직전에 `git diff --stat <기준 커밋> HEAD -- Packages`로 확인하고, 바뀌었으면 기준 커밋을 올리고 줄 번호와 내용을 다시 맞출지 묻습니다.
- 공개 API는 이름, 시그니처, 계약(잠금 요구, 스레드, 반환 의미)을 먼저 쓰고 구현을 뒤에 씁니다.
- 이름이 `Unsafe`로 끝나는 메서드처럼 호출 조건이 있으면 절 첫 문장에 적습니다.

## 서술

- 굵은 글씨는 그 절에서 가장 중요한 한 문장에만 씁니다.
- "왜 이렇게 하는가"를 반드시 씁니다. 예: "먼저 접는 이유", "한 블록을 더하는 이유". 코드를 문장으로 다시 읽어 주기만 하지 않습니다.
- 같은 내용을 여러 장에 반복하지 않고 링크합니다.
- 비교가 도움이 되면 표를 씁니다. (예: 두 트랜스포트의 차이, 명령별 효과)
- **관찰된 동작:** 사용자가 의도라고 확인하지 않은 특이 동작은 다음 상자로 적습니다. 버그라고 단정하거나 수정을 제안하지 않습니다.

  ```markdown
  > **관찰된 동작:** `FindStopTimeAtOrAfter`는 같은 시각의 Stopper를 포함합니다. `nextStopTime`은 같은 열을 제외합니다([`#L120`](…)).
  ```

## Mermaid

- `flowchart`, `sequenceDiagram`, `classDiagram`, `stateDiagram-v2`만 씁니다. GitHub 렌더러에서 문제가 적은 종류입니다.
- 제네릭은 `Foo~T~`로 씁니다.
- 라벨의 `<`, `>`는 `#lt;`, `#gt;`로 씁니다. `&lt;`는 깨집니다. 줄바꿈은 `<br/>`로 씁니다.
- 라벨에 괄호, 콜론, 따옴표가 있으면 `["…"]`로 감쌉니다.
- 한 다이어그램에 노드를 15개 넘게 넣지 않습니다. 넘으면 나눕니다.

## SVG

- [scripts/svg_kit.py](../scripts/svg_kit.py)의 `Svg` 클래스로 그립니다. 생성 스크립트는 스크래치패드에 두고 저장소에 넣지 않습니다.
- 흰 배경 패널(둥근 모서리, 옅은 테두리)을 깝니다. GitHub 다크 테마에서도 읽힙니다.
- `<script>`, `foreignObject`, 외부 폰트·이미지를 쓰지 않습니다. GitHub가 막습니다.
- 폰트 스택은 `"Noto Sans KR","Apple SD Gothic Neo","Malgun Gothic","Segoe UI",sans-serif`, 코드 글씨는 모노 스택입니다.
- 색 의미를 그림 사이에서 통일합니다. 예: 파랑 = 타임라인/시간, 주황 = 현재 믹스 중/주의, 초록 = 안전/확정, 보라 = 출력, 빨강 = 취소·오류.
- 그림마다 제목(굵게 16)과 한 줄 부제(회색 12)를 넣습니다. 문서의 `![대체 텍스트](images/x.svg)` 대체 텍스트는 그림 제목과 같게 합니다.
- 시간축 그림은 실제 공식으로 좌표를 계산합니다. 눈대중으로 그리지 않습니다. 예: outputClock = stamp.dspClock − bufferLength·bufferCount

## 인터랙티브 HTML

- 외부 리소스 없는 단일 파일입니다. `<!doctype html>`, charset, viewport를 넣습니다. 저장소 파일로 직접 열리기 때문입니다.
- 색은 `:root` 토큰으로 정의하고, 다크 테마는 `prefers-color-scheme`과 `[data-theme]` 둘 다로 재정의합니다(`artifact-design` 스킬 규칙).
- 시뮬레이션은 **실제 코드와 같은 식**을 씁니다. 식 옆에 주석으로 원본 파일과 메서드를 적습니다.
- 화면에 불변 조건(예: 시작 오차 0샘플)을 숫자로 보여 줍니다. 조작할 때마다 유지되는지 사용자가 볼 수 있게 합니다.
- 375px 폭에서 가로 스크롤이 없어야 합니다. canvas는 컨테이너 폭에 맞춥니다.
- 문서에는 두 링크를 둡니다.
  - 렌더링 링크: `https://raw.githack.com/<owner>/<repo>/<branch>/<path>`. push한 뒤부터 동작한다고 적습니다.
  - 저장소 상대 링크

## README 구성 (패키지 문서의 첫 페이지)

1. 머리말: 기준 커밋, 패키지 README 링크
2. 한 장으로 보기: 패키지와 연관 패키지의 주요 타입을 `subgraph`로 묶은 `flowchart`
3. 핵심 아이디어 3개 안팎: 각 한 문단 + 해당 장 링크
4. 읽는 순서 표: 장 | 내용 | 깊이
5. 연관 패키지 문서 표: 패키지 | 문서 | 이 패키지에서 쓰는 부분
6. 용어집 표
7. 파일 지도 표: 영역 | 파일
8. 그림 목록 표
