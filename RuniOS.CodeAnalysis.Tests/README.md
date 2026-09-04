# TypeSyntaxSerializer 테스트

실제 `RuniOS.CodeAnalysis` 프로젝트를 참조하는 xUnit 테스트입니다. 테스트 실행 대상은 .NET 8입니다.
테스트 코드 문법은 C# 12, 생성하는 C# fixture는 생산 코드와 같은 Roslyn 4.3.0의 C# 10으로 고정했습니다.

## 실행

저장소 루트에서 테스트 기반 검증부터 실행합니다.

```sh
dotnet test RuniOS.CodeAnalysis.Tests/RuniOS.CodeAnalysis.Tests.csproj --filter 'Category=Infrastructure'
```

이 그룹은 직렬화기를 호출하지 않습니다. 입력과 수작업 기대값의 바인딩/emit,
metadata fixture의 실제 모양, nullable 설정, 비교 헬퍼의 변형 탐지를 검사합니다.
여기서 실패하면 테스트 fixture나 헬퍼부터 고쳐야 합니다.

그다음 전체 테스트를 실행합니다.

```sh
dotnet test RuniOS.CodeAnalysis.Tests/RuniOS.CodeAnalysis.Tests.csproj --logger 'trx;LogFileName=serializer.trx'
```

`Category=Contract`는 공개 `ITypeSymbol.TrySerialize` API를 검증합니다.
실패 메시지에는 원문, 기대값, 출력, 타입 심볼, nullable 문맥, 컴파일 진단이 포함됩니다.
성공 경로는 실제 타입 바인딩, 의미 비교, PE emit, 재직렬화 안정성까지 검사합니다.
테스트 실행 결과는 `TestResults/`에 저장됩니다.

프로젝트 참조 빌드는 기존 analyzer 프로젝트의 `CopyAnalyzerToUnityPackage` 대상도 실행합니다.

## 범위와 기대값

- 기본 타입, native integer, dynamic, nullable, 제네릭 및 중첩 타입, unbound generic.
- 배열 rank/순서/SZ 여부/nullable 위치, tuple 이름과 8·15·20-element rest 구조.
- 클래스/메서드 타입 매개변수와 소유자, escaped identifier, alias, 이름 충돌.
- 포인터, 함수 포인터의 ref/out/in/반환 ref, 호출 규약, 중첩 시그니처.
- 288개의 결정론적 조합, 20단계 제네릭, 32단계 배열, 병렬 반복 호출.
- 기본 오류 결과, generic/non-generic 열거, 오류 결합 순서/결합법칙/불변성.
- `MetadataBuilder`로 메모리에서 생성한 non-SZ 배열, 잘못된 식별자,
  vararg 함수 포인터, 단일 legacy modopt의 unmanaged 함수 포인터, 복합 오류.

기대값은 현재 직렬화기 출력을 복사하지 않습니다. namespace 구분점, 전역 정규화,
서로 다른 배열 rank 순서 등의 계약 위반은 실패로 남겨야 합니다.
익명 타입은 적절한 표현 오류를 요구하되 명시되지 않은 오류 enum 하나로 한정하지 않습니다.
알 수 없는 타입은 원래 컴파일 오류와 직렬화 오류를 구분합니다.
nullable key 제약 경고처럼 유효한 fixture의 경고는 허용하되 컴파일 오류/emit 실패는 허용하지 않습니다.

## 경계

C# 12의 `ref readonly` 매개변수와 file-local 타입은 고정한 C# 10 fixture 문법 범위 밖입니다.
`ref readonly` 반환은 테스트합니다. 함수 포인터 자체를 tuple의 generic type argument로 넣는 등
C#에서 허용하지 않는 조합은 성공 fixture로 사용하지 않습니다.
컴파일 결과를 실행해 포인터를 역참조하거나 unmanaged 함수를 호출하지 않습니다.

## 검증 상태

2026-09-05 사용자의 명시적인 실행 요청으로 .NET 8에서 전체 테스트를 실행했습니다.
초기 결과는 1,079개 중 성공 754개, 실패 325개였습니다.
현재 참조 환경의 `IntPtr`/`UIntPtr` native integer 심볼 특성에 맞춰 기대값 2개를 수정하고,
직렬화기를 호출하지 않는 심볼 특성 검증 2개를 추가했습니다.
잘못된 직렬화 출력은 필드 검색 예외 대신 C# 컴파일 진단으로 보고하도록 헬퍼도 수정했습니다.

직렬화기의 namespace 구분점 누락, 배열 rank 출력 순서, 키워드 escaping,
global namespace 정규화 문제를 수정했습니다. 배열은 nullable 경계에서 rank 그룹을
분리하도록 수정하고, 혼합 rank/nullable 회귀 사례 3개를 추가했습니다.
기존 기대값을 완화하지 않았습니다.

최종 결과: **1,090개 실행, 성공 1,090개, 실패 0개, 건너뜀 0개** (약 15초).
Infrastructure **479개 모두 통과**.
빌드 경고 RS2008 4개와 xUnit2013 1개는 남아 있습니다.
Unity 재임포트 및 런타임 통합 검증은 수행하지 않았습니다.

최종 원본 결과: `TestResults/serializer-fixed.trx`.
짧은 원인 보고서: `TestResults/serializer-summary.md`.
