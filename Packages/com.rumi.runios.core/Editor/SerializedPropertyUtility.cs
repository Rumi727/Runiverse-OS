#nullable enable
using RuniOS.APIBridge.UnityEditor;
using RuniOS.Collections.Generic;
using System;
using UnityEditor;

namespace RuniOS.Editor
{
    /// <summary>
    /// Provides utility methods for PropertyField related operations.
    /// <br/>
    /// PropertyField 관련 작업을 위한 유틸리티 메서드를 제공합니다.
    /// </summary>
    public static class SerializedPropertyUtility
    {
        public static Type GetPropertyTypeWithoutList(this SerializedProperty property)
        {
            ScriptAttributeUtilityBridge.GetFieldInfoFromProperty(property, out Type type);
            if (property.isArray)
            {
                while (true)
                {
                    if (type.IsArray)
                    {
                        type = type.GetElementType()!;
                        continue;
                    }
                    else
                    {
                        Type? elementType = CollectionGenericUtility.GetListElementType(type);
                        if (elementType != null)
                        {
                            type = elementType;
                            continue;
                        }
                    }

                    break;
                }
            }

            return type;
        }
    }
}