#nullable enable
using System.Globalization;

namespace RuniOS.Inspectors
{
    public static class InspectorUtility
    {
        public static T FindElement<T>(this IInspectableObject inspectable, string name, InspectorFlags flags = InspectorFlags.All) where T : IInspectorElement
        {
            IEnumerable<T> elements = inspectable.GetElements(flags)
                .OfType<T>()
                .Where(x => x.name == name);
            
            // Linq를 사용할 수도 있지만, 루프를 가능한 한번만 돌리기 위해 직접 구현
            
            T? element = default;
            bool exists = false;
            foreach (var item in elements)
            {
                if (exists)
                    throw new InvalidOperationException($"There are two or more elements named {name}!");
                
                element = item;
                exists = true;
            }
            
            if (!exists || element == null)
                throw new InvalidOperationException($"Could not find element named {name}!");

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