#nullable enable
using System.Globalization;

namespace RuniOS.Inspectors
{
    public interface IInspectorElement
    {
        IInspectable inspectable { get; }
        
        string name { get; }
        string displayName { get; set; }

        string path
        {
            get
            {
                if (inspectable.parentElement != null)
                    return $"{inspectable.parentElement.path}.{name}";
                else
                    return name;
            }
        }
        
        /// <summary>
        /// 요소가 공개되어있는지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isPublic { get; }

        /// <summary>
        /// 요소가 정적인지 여부를 나타내는 값을 가져옵니다.
        /// </summary>
        bool isStatic { get; }

        bool HasFlags(InspectorFlags flags);

        protected static string ToDisplayName(string name)
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