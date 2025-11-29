#nullable enable
namespace RuniOS.Effects
{
    public static class MergeOutlineManager
    {
        // ID -> 리스트 (렌더링용)
        static readonly Dictionary<int, List<MergeOutline>> _targets = new Dictionary<int, List<MergeOutline>>();
        
        // [추가] 타겟 -> 현재 ID (중복 방지용 역참조)
        static readonly Dictionary<MergeOutline, int> _lookup = new Dictionary<MergeOutline, int>();

        public static void Register(MergeOutline target)
        {
            // 1. 이미 등록된 상태인지 확인
            if (_lookup.TryGetValue(target, out int currentId))
            {
                // 이미 올바른 ID에 있다면 패스
                if (currentId == target.profileId) return;

                // 다른 ID에 있다면 거기서 제거 (이게 중복 방지 핵심!)
                if (_targets.TryGetValue(currentId, out var oldList))
                {
                    oldList.Remove(target);
                    // 빈 리스트 정리
                    if (oldList.Count == 0) _targets.Remove(currentId);
                }
                _lookup.Remove(target);
            }

            // 2. 새로운 ID 리스트에 추가
            if (!_targets.ContainsKey(target.profileId))
                _targets[target.profileId] = new List<MergeOutline>();
            
            // 리스트 중복 체크
            if (!_targets[target.profileId].Contains(target))
                _targets[target.profileId].Add(target);

            // 3. 역참조 갱신
            _lookup[target] = target.profileId;
        }

        public static void Unregister(MergeOutline target)
        {
            // 역참조를 통해 이 타겟이 '실제로' 있던 곳에서 제거
            if (_lookup.TryGetValue(target, out int currentId))
            {
                if (_targets.TryGetValue(currentId, out var list))
                {
                    list.Remove(target);
                    if (list.Count == 0) _targets.Remove(currentId);
                }
                _lookup.Remove(target);
            }
        }

        public static List<MergeOutline>? GetTargets(int profileId) => _targets.ContainsKey(profileId) ? _targets[profileId] : null;
    }
}