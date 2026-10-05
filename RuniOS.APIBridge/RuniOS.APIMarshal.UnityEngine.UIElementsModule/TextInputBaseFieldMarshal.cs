#pragma warning disable CS1591 // 공개된 형식 또는 멤버에 대한 XML 주석이 없습니다.
using UnityEngine.UIElements;

namespace RuniOS.Editor.APIMarshal.UnityEngine.UIElements
{
    public abstract class TextInputBaseFieldMarshal<TValueType> : TextInputBaseField<TValueType> 
    {
        protected TextInputBaseFieldMarshal(int maxLength, char maskChar, TextInputBase textInputBase) : base(maxLength, maskChar, textInputBase) { }
        protected TextInputBaseFieldMarshal(string? label, int maxLength, char maskChar, TextInputBase textInputBase) : base(label, maxLength, maskChar, textInputBase) { }
        
        protected new TextInputBaseMarshal textInputBase => (TextInputBaseMarshal)base.textInputBase;

        protected abstract class TextInputBaseMarshal : TextInputBase
        {
            public new TextElement textElement => base.textElement;
        }
    }
}