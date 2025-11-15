#nullable enable
using RuniOS.Editor.UIElements.IO;
using RuniOS.IO;
using RuniOS.Resource;
using UnityEngine.UIElements;

namespace RuniOS.Editor.UIElements.Resource;

[UxmlElement]
public partial class PackIdentifierField : RuniBaseCompositeField<PackIdentifier>
{
    public new const string ussClassName = "runios-pack-identifier-field";
    public new const string labelUssClassName = ussClassName + "__label";
    public new const string inputUssClassName = ussClassName + "__input";

    public PackIdentifierMode mode
    {
        get => value.path != null ? PackIdentifierMode.path : PackIdentifierMode.id;
        set
        {
            if (mode == value)
                return;
                
            PackIdentifier packIdentifier = this.value;
            if (value == PackIdentifierMode.id)
                packIdentifier.path = null;
            else if (value == PackIdentifierMode.path)
                packIdentifier.identifier = null;

            this.value = packIdentifier;
        }
    }
        
    public IdentifierField identifierField { get; }
    public FilePathField pathField { get; }
    public EnumField modeField { get; }
        
    public PackIdentifierField() : this(string.Empty) { }
    public PackIdentifierField(string label) : base(label)
    {
        labelElement.AddToClassList(labelUssClassName);
        visualInput.AddToClassList(inputUssClassName);
            
        identifierField = new IdentifierField();
        pathField = new FilePathField { style = { display = DisplayStyle.None } };
        modeField = new EnumField(mode);
            
        AddToClassList(ussClassName);
        SetFieldsByHorizontal();
    }

    protected override IEnumerable<IElementDescription> GetElementDescriptions()
    {
        yield return new FieldDescription<IdentifierField, Identifier>
        (
            "_identifier",
            identifierField,
            static x => x.identifier ?? Identifier.empty,
            static (ref PackIdentifier packIdentifier, Identifier fieldValue) => packIdentifier.identifier = fieldValue
        );

        yield return new FieldDescription<FilePathField, FilePath>
        (
            "_path",
            pathField,
            static x => x.path ?? FilePath.empty,
            static (ref PackIdentifier packIdentifier, FilePath fieldValue) => packIdentifier.path = fieldValue
        );
            
        yield return new FieldDescription<EnumField, Enum>
        (
            nameof(mode),
            modeField,
            static x => x.path != null ? PackIdentifierMode.path : PackIdentifierMode.id,
            static (ref PackIdentifier packIdentifier, Enum fieldValue) =>
            {
                PackIdentifierMode mode = (PackIdentifierMode)fieldValue;
                if (mode == PackIdentifierMode.id)
                    packIdentifier.path = null;
                else if (mode == PackIdentifierMode.path)
                    packIdentifier.identifier = null;
            }
        );
    }
        
    public enum PackIdentifierMode
    {
        id,
        path
    }

    public override void SetValueWithoutNotify(PackIdentifier newValue)
    {
        base.SetValueWithoutNotify(newValue);
        if (mode == PackIdentifierMode.id)
        {
            identifierField.style.display = DisplayStyle.Flex;
            pathField.style.display = DisplayStyle.None;
        }
        else if (mode == PackIdentifierMode.path)
        {
            identifierField.style.display = DisplayStyle.None;
            pathField.style.display = DisplayStyle.Flex;
        }
    }
}