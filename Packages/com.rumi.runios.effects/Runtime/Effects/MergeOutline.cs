#nullable enable
namespace RuniOS.Effects
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class MergeOutline : MonoBehaviour
    {
        [Tooltip("MergeOutlineEffect의 Profiles 리스트 인덱스 번호입니다.")]
        public int profileId = 0;
        
        // 변경 감지용 (단순 비교용)
        [SerializeField, HideInInspector] int _cachedProfileId; 
        
        public new Renderer? renderer { get; private set; }

        void OnEnable()
        {
            renderer = GetComponent<Renderer>();
            _cachedProfileId = profileId;
            
            MergeOutlineManager.Register(this);
        }

        void OnDisable() => MergeOutlineManager.Unregister(this);

        void OnValidate()
        {
            if (!renderer) renderer = GetComponent<Renderer>();
            
            // 값이 바뀌었을 때만 갱신 시도
            if (_cachedProfileId != profileId)
            {
                if (isActiveAndEnabled)
                {
                    // 그냥 Register(Refresh)만 호출하면 매니저가 알아서 
                    // "너 원래 0번이었네? 거기서 빼고 1번에 넣어줄게" 라고 처리함
                    MergeOutlineManager.Register(this);
                }
                _cachedProfileId = profileId;
            }
        }
    }
}