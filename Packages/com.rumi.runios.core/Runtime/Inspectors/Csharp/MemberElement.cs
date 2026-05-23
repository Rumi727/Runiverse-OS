#nullable enable
using RuniOS.Inspectors.Attributes;
using RuniOS.Reflection;
using System.Collections.Immutable;
using System.Reflection;

namespace RuniOS.Inspectors.Csharp
{
    /// <summary>
    /// C# 멤버(필드, 프로퍼티 등)를 나타내는 인스펙터 요소의 추상 기본 클래스입니다.
    /// </summary>
    public abstract class MemberElement : IInspectorElement
    {
        /// <summary>
        /// <see cref="MemberElement"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="inspectable">이 멤버가 속한 검사 가능한 객체입니다.</param>
        /// <param name="member">이 요소가 나타내는 멤버 정보입니다.</param>
        protected MemberElement(InspectableObject inspectable, MemberInfo member)
        {
            name = member.Name;
            displayName = InspectorUtility.ToDisplayName(name);

            this.inspectable = inspectable;
            this.member = member;

            attributes =
            [
                ..member.GetCustomAttributes(true)
                    .OfType<IInspectorAttribute>()
                    .InheritFrom(inspectable)
            ];
        }

        /// <summary>
        /// 멤버의 이름을 가져옵니다.
        /// </summary>
        public string name { get; }

        /// <summary>
        /// UI에서 표시되는 멤버의 이름을 가져옵니다.
        /// </summary>
        public string displayName { get; set; }
        
        public string path
        {
            get
            {
                if (inspectable.parentElement != null)
                    return $"{inspectable.parentElement.path}.{name}";
                else
                    return name;
            }
        }

        /// <summary>
        /// 이 멤버가 속한 <see cref="InspectableObject"/>를 가져옵니다.
        /// </summary>
        public InspectableObject inspectable { get; }
        IInspectable IInspectorElement.inspectable => inspectable;

        /// <summary>
        /// 이 요소가 나타내는 <see cref="MemberInfo"/>를 가져옵니다.
        /// </summary>
        public MemberInfo member { get; }
        
        /// <summary>
        /// 멤버가 공개되어있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public abstract bool isPublic { get; }

        /// <summary>
        /// 멤버가 정적인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        public abstract bool isStatic { get; }

        public ImmutableArray<IInspectorAttribute> attributes { get; }

        public virtual bool HasFlags(InspectorFlags flags)
        {
            if (flags == InspectorFlags.None)
                return false;
            
            if (isPublic ? !flags.HasFlagFast(InspectorFlags.Public) : !flags.HasFlagFast(InspectorFlags.NonPublic))
                return false;
            
            if (isStatic ? !flags.HasFlagFast(InspectorFlags.Static) : !flags.HasFlagFast(InspectorFlags.Instance))
                return false;

            if (member.IsCompilerGenerated() && !flags.HasFlagFast(InspectorFlags.Hidden))
                return false;

            return true;
        }
        
        /// <inheritdoc cref="IInspectorElement.Clone"/>
        public abstract MemberElement Clone();
        IInspectorElement IInspectorElement.Clone() => Clone();
    }
}