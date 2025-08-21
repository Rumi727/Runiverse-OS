#nullable enable
using RuniOS.IO;
using RuniOS.Resource;
using RuniOS.UIElements.IO;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace RuniOS.UIElements.Resource
{
    [UxmlElement]
    public partial class PackIdentifierField : RuniBaseCompositeField<PackIdentifier>
    {
        public new const string ussClassName = "runios-pack-identifier-field";
        public new const string labelUssClassName = ussClassName + "__label";
        public new const string inputUssClassName = ussClassName + "__input";
        
        public PackIdentifierField() : this(null) { }
        public PackIdentifierField(string? label) : base(label)
        {
            labelElement.AddToClassList(labelUssClassName);
            visualInput.AddToClassList(inputUssClassName);
            
            AddToClassList(ussClassName);
            SetFieldsByHorizontal();
        }

        protected override IEnumerable<IElementDescription> GetElementDescriptions()
        {
            yield return new FieldDescription<IdentifierField, Identifier>
            (
                "_identifier",
                static x => x.identifier ?? Identifier.empty,
                static (ref PackIdentifier identifier, Identifier fieldValue) => identifier.identifier = fieldValue
            );
            
            yield return new FieldDescription<FilePathField, FilePath>
            (
                "_path",
                static x => x.path ?? FilePath.empty,
                static (ref PackIdentifier identifier, FilePath fieldValue) => identifier.path = fieldValue
            );
            
            yield return new FieldDescription<FilePathField, FilePath>
            (
                "_path",
                static x => x.path ?? FilePath.empty,
                static (ref PackIdentifier identifier, FilePath fieldValue) => identifier.path = fieldValue
            );
            
            yield return GetSpacer();
        }
    }
}
