#nullable enable
using System;
using System.Globalization;

namespace RuniOS.Inspectors
{
    public interface IInspectorElement
    {
        IInspectable inspectable { get; }
        
        string name { get; }
        string displayName { get; set; }

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