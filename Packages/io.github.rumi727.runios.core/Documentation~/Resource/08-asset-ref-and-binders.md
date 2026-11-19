# 8. `AssetRef`와 바인더

> 기준 커밋: `7e5d710f`. [← 목차](README.md) · 이전: [7. 핸들과 스코프](07-handles-and-scopes.md) · 다음: [9. 사이드카](09-sidecars.md)

컴포넌트가 "이 텍스처를 쓴다"고 저장하려면 식별자를 들고 있다가, 리로드 때마다 새 핸들로 갈아타야 합니다. 이 장은 그 두 가지를 맡는 [`AssetRef<TAsset>`](../../Runtime/Resource/AssetRef%601.cs#L17)(무엇을 쓸지 저장)과 [`AssetScopeBinder<TAsset>`](../../Runtime/Resource/AssetScopeBinder%601.cs#L9)(리로드 때 갈아타기)를 다룹니다.

- [8.1 `AssetRef`의 세 모드](#81-assetref의-세-모드)
- [8.2 핸들과 스코프 얻기, 같은 대상 판정](#82-핸들과-스코프-얻기-같은-대상-판정)
- [8.3 `AssetScopeBinder`](#83-assetscopebinder)

## 8.1 `AssetRef`의 세 모드

`AssetRef`는 직렬화되는 `record struct`이고, [`AssetRefMode`](../../Runtime/Resource/AssetRefMode.cs#L8)에 따라 세 필드 중 하나를 씁니다.

| 모드 | 쓰는 필드 | 찾는 방법 | 만드는 법 |
| --- | --- | --- | --- |
| `automatic` | `assetId` | `TAsset`을 주는 레지스트리를 `priority` 높은 순으로 ([5.3절](05-registries.md#53-조회-세-가지)) | `new AssetRef<T>(id)`, `"runios:ui/close"` |
| `registry` | `resourceKey` | 정해진 레지스트리 하나 | `new AssetRef<T>(resourceKey)` |
| `direct` | `directAsset` | 인스펙터에서 넣은 객체를 [`InstanceAssetHandle`](07-handles-and-scopes.md#77-instanceassethandle)로 감쌈 | `new AssetRef<T>(asset)` |

```csharp
// Runtime/Resource/AssetRef`1.cs
27  public AssetRef(Identifier assetId)
    {
29      this.assetId = assetId;
30      mode = AssetRefMode.automatic;
    }
…
81  public AssetRefMode mode = AssetRefMode.registry;
88  public Identifier assetId = Identifier.empty;
95  public ResourceKey resourceKey = new ResourceKey();
102 public TAsset? directAsset = default;
…
172 public static implicit operator AssetRef<TAsset>(string assetId) => new AssetRef<TAsset>(assetId);   // 문자열 → automatic
```

세 필드는 모드와 관계없이 모두 직렬화됩니다. 인스펙터에서 모드를 바꿔도 다른 모드의 값이 남아 있어 되돌리면 그대로 돌아옵니다.

> **관찰된 동작 (실행으로 확인하지 않음):** `mode`의 필드 초기값은 `registry`(81행)지만, 생성자를 거치지 않은 값(`default(AssetRef<T>)`)은 열거형 0인 `automatic`입니다.

## 8.2 핸들과 스코프 얻기, 같은 대상 판정

```csharp
// Runtime/Resource/AssetRef`1.cs
138 public IAssetHandle<TAsset>? GetHandle() => mode switch
    {
140     AssetRefMode.automatic => ResourceManager.GetHandle<TAsset>(assetId),
141     AssetRefMode.registry => ResourceManager.GetHandle<TAsset>(resourceKey),
142     AssetRefMode.direct => directAsset != null ? new InstanceAssetHandle<TAsset>(directAsset) : null,
143     _ => null
    };
…
158 public async UniTask<IAssetScope<TAsset>?> LoadScopeAsync() => mode switch
    {
160     // 최대한 박싱을 피하기 위해 의도적으로 GetHandle 메소드를 사용하지 않았습니다.
161     AssetRefMode.direct => directAsset != null ? new InstanceAssetHandle<TAsset>(directAsset).GetScope() : null,
162     AssetRefMode.automatic => await ResourceManager.LoadScopeAsync<TAsset>(assetId),
163     AssetRefMode.registry => await ResourceManager.LoadScopeAsync<TAsset>(resourceKey),
164     _ => null
    };
…
117 public bool IsSameTarget(IAssetScope<TAsset>? other)
    {
119     IAssetHandle<TAsset>? newHandle = GetHandle();                  // ① 지금 인덱스에서 다시 찾음
120     if (newHandle == null)
121         return other == null;                                       // ② 둘 다 없으면 같음
123     return other != null && other.handle.IsSameTarget(newHandle);  // ③ 들고 있는 스코프의 핸들과 비교
    }
```

`IsSameTarget`은 "지금 들고 있는 스코프가 이 참조가 지금 가리키는 에셋과 같은가"를 답합니다. ① 매번 현재 인덱스에서 핸들을 다시 찾으므로, 리로드로 인덱스가 바뀐 뒤 부르면 갈아타야 하는지 알 수 있습니다. ③ 비교는 핸들의 [`IsSameTarget`](07-handles-and-scopes.md#75-issametarget-언제-옛-핸들을-재사용하나)이라서, 파일이 그대로면 리로드 뒤에도 `true`(갈아탈 필요 없음)입니다.

> **관찰된 동작:** `direct` 모드의 `directAsset != null`(142·161행)은 C# 참조 비교라서, 파괴된 Unity 객체도 `null`이 아닌 것으로 봅니다. 이 경로는 박싱을 피하려 했다는 주석(160행)이 있지만, 결과 `InstanceAssetScope`는 `IAssetScope<TAsset>`로 돌려주면서 박싱됩니다.

## 8.3 `AssetScopeBinder`

[`AssetScopeBinder<TAsset>`](../../Runtime/Resource/AssetScopeBinder%601.cs#L9)는 `AssetRef` 하나를 직렬화하고, 활성화되어 있는 동안 그 에셋의 스코프를 하나 들고 있는 `MonoBehaviour`입니다. [`IReloadable`](04-reload-pipeline.md#44-완료-단계-이벤트-그다음-ireloadable)이라서 리로드가 끝날 때마다 갈아탑니다. 파생 클래스는 에셋을 실제로 적용하는 두 메서드만 구현합니다.

```csharp
// Runtime/Resource/AssetScopeBinder`1.cs
8   [ExecuteAlways]                                                      // 에디터 모드에서도 동작
9   public abstract class AssetScopeBinder<TAsset> : MonoBehaviour, IReloadable where TAsset : notnull
    {
11      public AssetRef<TAsset> assetRef
        {
            …
16          if (_assetRef == value)
17              return;
19          _assetRef = value;
20          Reload().Forget();                                           // ① 참조를 바꾸면 바로 갈아탐
        }
27      protected virtual void OnEnable()
        {
29          ResourceManager.AttachReloadable(this);                      // ② 리로드 끝날 때 불리도록 등록
30          Reload().Forget();
        }
33      protected void OnDisable()
        {
35          ResourceManager.DetachReloadable(this);
37          ClearAsset();
38          currentAssetScope?.Dispose();                                // ③ 비활성화 = 스코프 반환
39          currentAssetScope = null;
        }
42      readonly AsyncReloadGate asyncReloadGate = new AsyncReloadGate();
43      public UniTask Reload() => asyncReloadGate.Run(ReloadCore);      // ④ 겹친 Reload 합치기
45      async UniTask ReloadCore()
        {
47          if (assetRef.IsSameTarget(currentAssetScope))
48              return;                                                  // ⑤ 같은 에셋이면 아무것도 안 함
50          IAssetScope<TAsset>? newScope = await assetRef.LoadScopeAsync();
51          if (this == null || !isActiveAndEnabled)
            {
53              newScope?.Dispose();                                     // ⑥ 기다리는 사이 꺼졌거나 파괴됨
54              return;
            }
57          currentAssetScope?.Dispose();                                // ⑦ 새 스코프를 받은 뒤 옛 스코프 반환
58          currentAssetScope = newScope;
60          if (newScope != null)
61              Apply(newScope.asset);
            else
63              ClearAsset();
        }
66      protected abstract void Apply(TAsset asset);
67      protected abstract void ClearAsset();
    }
```

- ⑤ 같은 대상이면 스코프를 그대로 두므로, 리로드 뒤에도 파일이 바뀌지 않은 에셋은 다시 적용하거나 다시 불러오지 않습니다.
- ⑥ `LoadScopeAsync`를 기다리는 동안 컴포넌트가 꺼지거나 파괴될 수 있으므로, 돌아온 뒤 다시 확인하고 받은 스코프를 바로 반환합니다. `this == null`은 Unity의 파괴된 객체 판정입니다.
- ⑦ 새 스코프를 먼저 받고 옛 스코프를 나중에 반환합니다. 같은 핸들이면 그 사이 참조 수가 0이 되지 않아 언로드 예약이 걸리지 않습니다. 옛 핸들의 스코프가 0이 되면 그 핸들은 [지연 언로드](07-handles-and-scopes.md#73-returnscope-참조-세기와-지연-언로드)로 들어갑니다.

파생 클래스의 예는 texture 패키지의 `SpriteRendererBinder`, ui 패키지의 `ImageBinder`·`RawImageBinder`입니다. UI Toolkit 쪽에는 같은 역할의 `AssetScopeManipulator`(uielements 패키지)가 있습니다.

> **관찰된 동작:** `OnEnable`은 `virtual`이지만 `OnDisable`은 아닙니다. `assetRef` setter(20행)는 컴포넌트가 꺼져 있어도 `Reload`를 부릅니다. 이때는 ⑥에서 받은 스코프를 바로 반환합니다.
