#nullable enable
namespace RuniOS.Inspectors;

/// <summary>
/// 변수의 읽기/쓰기를 커스텀할 수 있는 인스펙터 요소입니다.<br/>
/// 변수의 읽기/쓰기를 커스텀할 수 있다는 점을 제외하면 타겟 요소와 동일합니다.
/// <br/><br/>
/// 구조체처럼 내부 필드는 읽기 전용이지만 새 구조체를 만드는 것과 같이 값을 바꿀 수는 있을때 사용할 수 있습니다.
/// </summary>
public class CustomAccessVariableElement : IInspectorVariableElement
{
    public delegate object? ReadFunc(IInspectorVariableElement element);
    public delegate void WriteAction(IInspectorVariableElement element, object? value);
        
    public delegate IEnumerable<object?> GetValuesFunc(IInspectorVariableElement element);
    public delegate void SetValuesAction(IInspectorVariableElement element, IEnumerable<object?> values);

    public delegate bool IsReadableFunc(IInspectorVariableElement element, InspectorFlags flags = InspectorFlags.Public);
    public delegate bool IsWritableFunc(IInspectorVariableElement element, InspectorFlags flags = InspectorFlags.Public);
        
    CustomAccessVariableElement(IInspectorVariableElement targetElement, ReadFunc? readFunc, GetValuesFunc? getValuesFunc, WriteAction? writeAction, SetValuesAction? setValuesAction, IsReadableFunc? isReadableFunc, IsWritableFunc? isWritableFunc)
    {
        this.targetElement = targetElement;

        this.readFunc = readFunc ?? (x => x.value);
        this.getValuesFunc = getValuesFunc ?? (x => x.GetValues());
            
        this.writeAction = writeAction ?? ((element, value) => element.value = value);
        this.setValuesAction = setValuesAction ?? ((element, values) => element.SetValues(values));

        this.isReadableFunc = isReadableFunc ?? ((element, flags) => element.IsReadable(flags));
        this.isWritableFunc = isWritableFunc ?? ((element, flags) => element.IsWritable(flags));

        inspectableObjectElement = targetElement.inspectableObjectElement.Clone();
        inspectableObjectElement.parentElement = this;

        if (targetElement.inspectableListElement != null)
        {
            inspectableListElement = targetElement.inspectableListElement.Clone();
            inspectableListElement.parentElement = this;
        }
            
        if (targetElement.inspectableDictionaryElement != null)
        {
            inspectableDictionaryElement = targetElement.inspectableDictionaryElement.Clone();
            inspectableDictionaryElement.parentElement = this;
        }
    }
        
    public IInspectorVariableElement targetElement { get; }

    public string name => targetElement.name;

    public string displayName
    {
        get => targetElement.displayName;
        set => targetElement.displayName = value;
    }

    public IInspectable inspectable => targetElement.inspectable;
        
    public Type variableType => targetElement.variableType;
        
    public RuniNullabilityInfo? nullabilityInfo => targetElement.nullabilityInfo;
        
    public bool isPublic => targetElement.isPublic;
        
    public bool isStatic => targetElement.isStatic;
        
    public object? value
    {
        get => readFunc.Invoke(targetElement);
        set => writeAction.SafeInvoke(targetElement, value);
    }

    public bool isMixedValue => targetElement.isMixedValue;
        
    public IInspectableObject inspectableObjectElement { get; }
        
    public IInspectableList? inspectableListElement { get; }
        
    public IInspectableDictionary? inspectableDictionaryElement { get; }

    public event ReadFunc readFunc;
    public event GetValuesFunc getValuesFunc;
        
    public event WriteAction writeAction;
    public event SetValuesAction setValuesAction;
        
    public event IsReadableFunc isReadableFunc;
    public event IsWritableFunc isWritableFunc;

    public IEnumerable<object?> GetValues() => getValuesFunc.Invoke(targetElement);
    public void SetValues(IEnumerable<object?> values) => setValuesAction.Invoke(targetElement, values);

    public bool HasFlags(InspectorFlags flags) => targetElement.HasFlags(flags);
        
    public bool IsReadable(InspectorFlags flags = InspectorFlags.Public) => isReadableFunc.Invoke(targetElement, flags);

    public bool IsWritable(InspectorFlags flags = InspectorFlags.Public) => isWritableFunc.Invoke(targetElement, flags);

    public void UpdateChildInspectable() => targetElement.UpdateChildInspectable();

    public sealed class Builder
    {
        readonly IInspectorVariableElement targetElement;
            
        ReadFunc? readFunc;
        GetValuesFunc? getValuesFunc;
            
        WriteAction? writeAction;
        SetValuesAction? setValuesAction;

        IsReadableFunc? isReadableFunc;
        IsWritableFunc? isWritableFunc;

        public Builder(IInspectorVariableElement targetElement) => this.targetElement = targetElement;

        public Builder SetReadFunc(ReadFunc readFunc)
        {
            this.readFunc = readFunc;
            return this;
        }
            
        public Builder SetGetValuesFunc(GetValuesFunc getValuesFunc)
        {
            this.getValuesFunc = getValuesFunc;
            return this;
        }
            
        public Builder AddWriteAction(WriteAction writeAction)
        {
            this.writeAction += writeAction;
            return this;
        }
            
        public Builder AddSetValuesAction(SetValuesAction setValuesAction)
        {
            this.setValuesAction += setValuesAction;
            return this;
        }

        public Builder SetIsReadableFunc(IsReadableFunc isReadableFunc)
        {
            this.isReadableFunc = isReadableFunc;
            return this;
        }
            
        public Builder SetIsWritableFunc(IsWritableFunc isWritableFunc)
        {
            this.isWritableFunc = isWritableFunc;
            return this;
        }
            
        public CustomAccessVariableElement Build() => new CustomAccessVariableElement(targetElement, readFunc, getValuesFunc, writeAction, setValuesAction, isReadableFunc, isWritableFunc);
    }
}