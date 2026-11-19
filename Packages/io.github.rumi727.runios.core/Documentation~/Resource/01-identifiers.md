# 1. 식별자: `Identifier`, `ResourceKey`, `PackIdentifier`

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 다음: [2. 리소스 팩](02-resource-packs.md)
>
> 선행: [IO 1. 경로](../IO/01-paths.md)

리소스 시스템에서 에셋은 파일 경로가 아니라 **식별자**로 찾습니다. `runios:ui/close`처럼 네임스페이스와 경로를 붙인 이름이고, 어느 팩의 어느 파일에서 왔는지는 드러나지 않습니다. 이 장은 식별자 세 종류의 규칙을 다룹니다.

| 타입 | 무엇을 가리키나 | 예 |
| --- | --- | --- |
| `Identifier` | 에셋 하나, 레지스트리 하나, 팩 하나 | `runios:ui/close`, `runios:textures` |
| `ResourceKey` | 특정 레지스트리 안의 에셋 | `(runios:textures, runios:ui/close)` |
| `PackIdentifier` | 팩 하나 (ID 또는 디스크 경로) | `runios:vanilla`, `/home/user/packs/my_pack` |

- [1.1 `Identifier`](#11-identifier)
- [1.2 문자열 변환과 파싱](#12-문자열-변환과-파싱)
- [1.3 파일 경로에서 식별자로](#13-파일-경로에서-식별자로)
- [1.4 `ResourceKey`](#14-resourcekey)
- [1.5 `PackIdentifier`](#15-packidentifier)

## 1.1 `Identifier`

[`Identifier`](../../Runtime/Resource/Identifier.cs#L15)는 `(nameSpace, path)` 두 값의 구조체입니다. 경로는 [`RuniPath`](../IO/01-paths.md#11-runipath의-정규화)라서 앞뒤 `/`와 연속 `/`가 정규화됩니다.

```csharp
// Runtime/Resource/Identifier.cs
30  public const string defaultNamespace = "runios";
35  public const char separator = ':';
…
60  public Identifier(string nameSpace, string path)
    {
62      if (!IsNamespaceValid(nameSpace))
63          throw new InvalidIdentifierException(GetInvalidNamespaceMessage(nameSpace));
65      RuniPath runiPath = (RuniPath)path;                        // ① RuniPath로 정규화
66      if (!IsPathValid(runiPath))
67          throw new InvalidIdentifierException(GetInvalidPathMessage(runiPath));
        …
    }
…
116 readonly get => string.IsNullOrEmpty(_nameSpace) ? defaultNamespace : _nameSpace;   // ② 빈 네임스페이스 = runios
```

허용 문자는 [`IsNamespaceValid`](../../Runtime/Resource/Identifier.cs#L283)와 [`IsPathValid`](../../Runtime/Resource/Identifier.cs#L296)가 정합니다.

| 부분 | 허용 | 빈 값 |
| --- | --- | --- |
| 네임스페이스 | `a-z 0-9 . - _` | 허용, `runios`로 읽힘 |
| 경로 | `a-z 0-9 . - _ /` | 허용 |

대문자와 공백이 없으므로 식별자는 대소문자를 구분하지 않는 파일 시스템에서도 한 가지 철자만 가집니다. 경로는 `RuniPath` 정규화만 거치므로 `..` 같은 조각은 그대로 허용됩니다.

비교는 정규화된 네임스페이스와 경로의 비교입니다([`==`](../../Runtime/Resource/Identifier.cs#L178)). 그래서 `default(Identifier)`(`Identifier.empty`)와 `new Identifier("runios", "")`는 같습니다. 문자열로는 `runios:`입니다.

> **관찰된 동작:** Unity 직렬화 콜백([`#L147`](../../Runtime/Resource/Identifier.cs#L147)~165행)은 기본 네임스페이스면 `nameSpace = null`을 대입하지만, setter가 빈 값을 `"runios"`로 바꿔 저장하므로(119~120행) 직렬화된 필드에는 항상 `"runios"`가 들어갑니다.

> **관찰된 동작:** `GetHashCode`는 두 부분의 해시를 곱합니다([`#L207`](../../Runtime/Resource/Identifier.cs#L207)). 한쪽 해시가 0이면 전체가 0이고, 두 부분을 바꿔도 같은 값입니다.

## 1.2 문자열 변환과 파싱

`Identifier`는 `string`과 양방향으로 **암시적** 변환됩니다([`#L219`](../../Runtime/Resource/Identifier.cs#L219), [`#L236`](../../Runtime/Resource/Identifier.cs#L236)). 그래서 코드에서 `"runios:textures"`를 그대로 넘길 수 있고, 잘못된 문자열은 변환 순간 `InvalidIdentifierException`을 던집니다.

```csharp
// Runtime/Resource/Identifier.cs
265 public static Identifier Parse(string identifier, string? defaultNamespace = null)
    {
267     int separatorIndex = identifier.AsSpan().IndexOf(separator);
270     if (separatorIndex < 0)                                        // ① ':' 없음 = 경로만
271         return new Identifier(string.IsNullOrEmpty(defaultNamespace) ? Identifier.defaultNamespace : defaultNamespace, identifier);
273     if (identifier.AsSpan(separatorIndex + 1).IndexOf(separator) >= 0)
274         throw new InvalidIdentifierException(…);                    // ② ':'는 하나만
276     return new Identifier(identifier.Substring(0, separatorIndex), identifier.Substring(separatorIndex + 1));
    }
```

| 입력 | 결과 |
| --- | --- |
| `"ui/close"` | `runios:ui/close` (또는 `defaultNamespace` 인자) |
| `"mymod:ui/close"` | `mymod:ui/close` |
| `":ui/close"` | `runios:ui/close` (빈 네임스페이스) |
| `"a:b:c"` | 예외 |
| `"UI/Close"` | 예외 (대문자) |

JSON은 [`IdentifierConverter`](../../Runtime/Json/Converters/Resource/IdentifierConverter.cs#L13)가 문자열 하나로 읽고 씁니다. `null`은 `Identifier.empty`입니다. 읽을 때 네임스페이스가 없으면 `JsonSerializer.Context`에 [`IdentifierJsonContext`](../../Runtime/Resource/IdentifierJsonContext.cs#L20)가 있을 때 그 네임스페이스를, 없으면 `runios`를 씁니다.

> **관찰된 동작:** `IdentifierJsonContext`를 만드는 코드가 없습니다. 그래서 팩 안의 JSON 파일에 쓴 네임스페이스 없는 식별자는 그 파일이 속한 네임스페이스가 아니라 항상 `runios`로 읽힙니다.

## 1.3 파일 경로에서 식별자로

팩의 파일에서 에셋 식별자를 만드는 규칙은 레지스트리마다 정하지만, core의 레지스트리들은 같은 규칙을 씁니다([6장](06-simple-asset-registry.md)).

```text
assets/{네임스페이스}/{레지스트리 폴더}/{경로}.{확장자}
→ Identifier({네임스페이스}, {경로})
```

예: `assets/runios/textures/ui/close.png`는 `runios:textures` 레지스트리에서 `runios:ui/close`가 됩니다. 확장자는 [마지막 하나만](../IO/01-paths.md#14-fileextension과-확장자-규칙) 떼므로 `a.tar.gz`는 `a.tar`입니다.

파일 이름이 허용 문자를 벗어나면(대문자, 공백) 식별자 생성이 예외를 던지고, 레지스트리는 그 파일만 로그를 남기고 건너뜁니다. Windows에서는 [물리 경로가 소문자가 되므로](../IO/01-paths.md#15-physicalpath-디스크에-닿을-때) 같은 파일이 통과할 수 있습니다.

## 1.4 `ResourceKey`

[`ResourceKey`](../../Runtime/Resource/ResourceKey.cs#L7)는 `(registryId, assetId)` 쌍입니다. 같은 식별자의 에셋이 여러 레지스트리에 있을 수 있으므로(예: 같은 `png`가 텍스처 레지스트리와 스프라이트 레지스트리에 모두 있음), **어느 레지스트리에서** 찾을지까지 정할 때 씁니다. 식별자만으로 찾는 방법과의 차이는 [5.3절](05-registries.md#53-조회-세-가지)에 있습니다.

## 1.5 `PackIdentifier`

[`PackIdentifier`](../../Runtime/Resource/PackIdentifier.cs#L17)는 팩을 구별하는 키입니다. 팩은 코드가 이름을 붙여 만드는 것(`runios:vanilla`)도 있고 사용자가 디스크 폴더로 추가하는 것도 있어서, **`Identifier`와 `PhysicalPath` 중 정확히 하나**를 가집니다.

```csharp
// Runtime/Resource/PackIdentifier.cs
55  public readonly bool isValid => (identifier != null && path == null) || (identifier == null && path != null);
…
76  public static PackIdentifier CreateByID(Identifier identifier) => new PackIdentifier(identifier, null);
83  public static PackIdentifier CreateByPath(PhysicalPath path) => new PackIdentifier(null, path);
…
103 if (!lhs.isValid || !rhs.isValid)
107     return !lhs.isValid && !rhs.isValid;          // 잘못된 값끼리는 모두 같음
```

setter는 한쪽을 넣으면 다른 쪽을 지웁니다(28~32행, 43~47행). 잘못된 값(둘 다 없음)은 서로 같고 해시가 0입니다. 디스크 경로 팩끼리는 [`PhysicalPath`](../IO/01-paths.md#15-physicalpath-디스크에-닿을-때) 비교라서, Windows에서는 대소문자만 다른 경로가 같은 팩입니다.
