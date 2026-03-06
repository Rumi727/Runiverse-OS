#nullable enable
using RuniOS.Inspectors.Attributes;
using System.Collections.Immutable;

namespace RuniOS.Inspectors
{
    public interface IInspectorElement : ICloneable
    {
        IInspectable inspectable { get; }
        
        string name { get; }
        string displayName { get; set; }

        string path { get; }
        
        /// <summary>
        /// 요소가 공개되어있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isPublic { get; }

        /// <summary>
        /// 요소가 정적인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isStatic { get; }
        
        /// <summary>
        /// 상속 받은 어트리뷰트를 포함한 현재 모든 인스펙터 어트리뷰트를 가져옵니다.<br/>
        /// </summary>
        ImmutableArray<IInspectorAttribute> attributes { get; }
        
        bool HasFlags(InspectorFlags flags);

        /// <summary>
        /// 복제본을 생성합니다. 검사 중인 객체의 목록까지 같이 복제합니다.<br/>
        /// 즉, 외부에서 인스턴스 목록을 교채해도, 이 복제본은 영향받지 않습니다.<br/>
        /// 언도 히스토리에 기록할 때 유용합니다.
        /// </summary>
        new IInspectorElement Clone();
        object ICloneable.Clone() => Clone();
    }
}