using System.Globalization;

namespace RuniOS.Inspectors
{
    public static class InspectorUtility
    {
        public static IInspectorElement FindElement(this IInspectableObject inspectable, string name, InspectorFlags flags = InspectorFlags.All)
        {
            IInspectorElement? element = inspectable.GetElements(flags)
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
        
        public static IInspectorVariableElement FindVariableElement(this IInspectableObject inspectable, string name, InspectorFlags flags = InspectorFlags.All)
        {
            if (inspectable.FindElement(name, flags) is not IInspectorVariableElement element)
                throw new InvalidOperationException($"Could not find variable element named {name}!");

            return element;
        }
        
        public static IInspectorActionElement FindActionElement(this IInspectableObject inspectable, string name, InspectorFlags flags = InspectorFlags.All)
        {
            if (inspectable.FindElement(name, flags) is not IInspectorActionElement element)
                throw new InvalidOperationException($"Could not find action element named {name}!");

            return element;
        }
        
        public static string ToDisplayName(string name)
        {
            string displayName = name;
            if (displayName.StartsWith("m_", StringComparison.InvariantCulture))
            {
                if (displayName.Length <= 2)
                    return string.Empty;
                    
                displayName = displayName[2..];
            }
            else if (displayName.StartsWith("_", StringComparison.InvariantCulture) || displayName.StartsWith("k", StringComparison.InvariantCulture))
            {
                if (displayName.Length <= 1)
                    return string.Empty;
                    
                displayName = displayName[1..];
            }

            displayName = displayName.AddSpacesToSentence();
            
            char[] array = displayName.ToCharArray();
            array[0] = char.ToUpper(array[0], CultureInfo.InvariantCulture);

            return new string(array);
        }
    }
}