namespace RuniOS.Editor.APIMarshal.UnityEditor
{
#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
    public abstract class EditorMarshal : global::UnityEditor.Editor
    {
        // ReSharper disable once RedundantOverriddenMember
        public override string targetTitle => base.targetTitle;

        // ReSharper disable once RedundantOverriddenMember
        public override int referenceTargetIndex
        {
            get => base.referenceTargetIndex;
            set => base.referenceTargetIndex = value;
        }
    }
#pragma warning restore CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
}