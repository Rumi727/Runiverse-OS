#nullable enable
using UnityEngine.Serialization;

namespace RuniOS.Effects
{
    [ExecuteAlways]
    [RequireComponent(typeof(Renderer))]
    public class MergeOutline : MonoBehaviour
    {
        public int profileId
        {
            get => _profileId;
            set
            {
                if (_profileId == value)
                    return;
                
                _profileId = value;
                Refresh();
            }
        }
        [FormerlySerializedAs("profileId"),Tooltip("MergeOutlineEffect의 Profiles 리스트 인덱스 번호입니다.")]
        [SerializeField] int _profileId = 0;
        
        public Renderer? renderer { get; private set; }
        
        void OnEnable()
        {
            renderer = GetComponent<Renderer>();
            Refresh();
        }

        void OnDisable() => Refresh();

        void Refresh()
        {
            if (isActiveAndEnabled)
                MergeOutlineManager.Register(this);
            else
                MergeOutlineManager.Unregister(this);
        }

#if UNITY_EDITOR
        void OnValidate() => Refresh();
#endif
    }
}