#nullable enable
namespace RuniOS.IO.Locations
{
    public abstract class Mount
    {
        public abstract PhysicalPath mountPoint { get; }

        public abstract string? fileSystemType { get; }

        public abstract bool isReadOnly { get; }

        /// <summary>
        /// 확실하게 시스템 내부 인터페이스라고 정의할 수 있을 때만 true. 조금이라도 사용자 filesystem으로 쓰일 수 있으면 false.
        /// </summary>
        public abstract bool isSystemInternal { get; }
    }
}