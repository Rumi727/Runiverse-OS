#nullable enable
using RuniOS.UIElements.Nullables;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RuniOS.UIElements
{
    [UxmlElement]
#if !UNITY_EDITOR && ENABLE_IL2CPP
    [System.Obsolete("IL2CPP environment is not supported.", true)]
#endif
    public partial class VersionField : RuniBaseCompositeField<Version>
    {
        public new const string ussClassName = "runios-version-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        
        public VersionField() : this(string.Empty) { }
        public VersionField(string label) : base(label)
        {
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            
            AddToClassList(ussClassName);
            SetFieldsByHorizontal();
        }

        protected override IEnumerable<IElementDescription> GetElementDescriptions()
        {
            yield return new FieldDescription<NullableIntegerField, SerializableNullable<int>>
            (
                "_major",
                static x => x.major,
                static (ref Version version, SerializableNullable<int> fieldValue) => version.major = fieldValue
            );

            yield return new ElementDescription<TextElement>(nameof(Version.separator), new TextElement { text = Version.separator.ToString() });
            
            yield return new FieldDescription<NullableIntegerField, SerializableNullable<int>>
            (
                "_minor",
                static x => x.minor,
                static (ref Version version, SerializableNullable<int> fieldValue) => version.minor = fieldValue
            );

            yield return new ElementDescription<TextElement>(nameof(Version.separator), new TextElement { text = Version.separator.ToString() });
            
            yield return new FieldDescription<NullableIntegerField, SerializableNullable<int>>
            (
                "_patch",
                static x => x.patch,
                static (ref Version version, SerializableNullable<int> fieldValue) => version.patch = fieldValue
            );
        }
    }
}
