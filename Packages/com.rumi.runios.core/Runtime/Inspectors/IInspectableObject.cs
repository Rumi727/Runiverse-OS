#nullable enable
namespace RuniOS.Inspectors;

public interface IInspectableObject : IInspectable
{
    IInspectorElement FindElement(string name, InspectorFlags flags = InspectorFlags.All)
    {
        IInspectorElement? element = GetElements(flags)
            .Where(x =>
            {
                // 명시적 인터페이스 구현 감지
                int index = x.name.LastIndexOf('.');
                if (index++ >= 0) // index++의 "반환" 값은 index + 1이 아닌 index 입니다.
                    return x.name[index..] == name;

                return x.name == name;
            })
            .FirstOrDefault();
            
        if (element == null)
            throw new InvalidOperationException($"Could not find element named {name}!");

        return element;
    }
        
    IInspectorVariableElement FindVariableElement(string name, InspectorFlags flags = InspectorFlags.All)
    {
        if (FindElement(name, flags) is not IInspectorVariableElement element)
            throw new InvalidOperationException($"Could not find variable element named {name}!");

        return element;
    }
        
    IInspectorActionElement FindActionElement(string name, InspectorFlags flags = InspectorFlags.All)
    {
        if (FindElement(name, flags) is not IInspectorActionElement element)
            throw new InvalidOperationException($"Could not find action element named {name}!");

        return element;
    }
        
    new IInspectableObject Clone();
}