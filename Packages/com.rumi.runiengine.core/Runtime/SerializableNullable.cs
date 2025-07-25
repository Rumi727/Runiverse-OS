#nullable enable
using System;

namespace RuniEngine
{
    public static class SerializableNullable
    {
        public static Type? GetUnderlyingType(Type nullableType)
        {
            {
                Type? resultType = Nullable.GetUnderlyingType(nullableType);
                if (resultType != null)
                    return resultType;
            }

            if (nullableType.IsGenericType && !nullableType.IsGenericTypeDefinition)
            {
                Type genericType = nullableType.GetGenericTypeDefinition();
                if (genericType == typeof(SerializableNullable<>))
                    return nullableType.GetGenericArguments()[0];
            }

            return null;
        }
    }
}
