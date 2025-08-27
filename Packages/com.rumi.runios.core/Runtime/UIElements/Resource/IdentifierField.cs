#nullable enable
using RuniOS.IO;
using RuniOS.Resource;
using RuniOS.UIElements.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Resource
{
    [UxmlElement]
    public partial class IdentifierField : RuniBaseCompositeField<Identifier>
    {
        public new const string ussClassName = "runios-identifier-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        
        public IdentifierField() : this(null) { }
        public IdentifierField(string? label) : base(label)
        {
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            
            AddToClassList(ussClassName);
            SetFieldsByHorizontal();
        }

        protected override IEnumerable<IElementDescription> GetElementDescriptions()
        {
            yield return new FieldDescription<TextField, string>
            (
                "_nameSpace",
                static x => x.nameSpace,
                static (ref Identifier identifier, string fieldValue) =>
                {
                    if (Identifier.IsNamespaceValid(fieldValue))
                        identifier.nameSpace = fieldValue;
                    else
                        Debug.LogWarning(Identifier.GetInvalidNamespaceMessage(fieldValue));
                });

            yield return new ElementDescription<TextElement>(nameof(Identifier.separator), new TextElement { text = Identifier.separator.ToString() });
            
            yield return new FieldDescription<FilePathField, FilePath>
            (
                "_path",
                static x => x.path,
                static (ref Identifier identifier, FilePath fieldValue) =>
                {
                    if (Identifier.IsPathValid(fieldValue))
                        identifier.path = fieldValue;
                    else
                        Debug.LogWarning(Identifier.GetInvalidPathMessage(fieldValue));
                }
            );
        }
    }
}
