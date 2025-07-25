#nullable enable
using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RuniEngine
{
    public static class ComponentUtility
    {
        public static T? GetComponentFieldSave<T>(this Component component, T? fieldToSave, GetComponentMode mode = GetComponentMode.addIfNull) where T : Component
        {
            if (fieldToSave != null && fieldToSave.gameObject == component.gameObject)
                return fieldToSave;
            
            fieldToSave = component.GetComponent<T>();
            
            if (fieldToSave != null)
                return fieldToSave;
                
            switch (mode)
            {
                case GetComponentMode.addIfNull:
                    return component.gameObject.AddComponent<T>();
                case GetComponentMode.destroyIfNull:
                    Object.DestroyImmediate(component);
                    return null;
                case GetComponentMode.none:
                default:
                    return fieldToSave;
            }
        }

        public static T[]? GetComponentsFieldSave<T>(this Component component, T[]? fieldToSave, GetComponentsMode mode = GetComponentsMode.addZeroLengthIfNull) where T : Component
        {
            if (fieldToSave != null)
                return fieldToSave;
            fieldToSave = component.GetComponents<T>();
            if (fieldToSave != null)
                return fieldToSave;
            switch (mode)
            {
                case GetComponentsMode.addZeroLengthIfNull:
                    return Array.Empty<T>();
                case GetComponentsMode.destroyIfNull:
                    Object.DestroyImmediate(component);
                    return null;
                case GetComponentsMode.none:
                default:
                    return fieldToSave;
            }
        }



        public static T? GetComponentInParentFieldSave<T>(this Component component, T? fieldToSave, bool includeInactive = false, GetComponentInMode mode = GetComponentInMode.none) where T : Component
        {
            if (fieldToSave != null && fieldToSave.gameObject == component.gameObject)
                return fieldToSave;
            
            fieldToSave = component.GetComponentInParent<T>(includeInactive);

            if (fieldToSave != null || mode != GetComponentInMode.destroyIfNull)
                return fieldToSave;
            
            Object.DestroyImmediate(component);
            return null;
        }

        public static T[]? GetComponentsInParentFieldSave<T>(this Component component, T[]? fieldToSave, bool includeInactive = false, GetComponentsMode mode = GetComponentsMode.addZeroLengthIfNull) where T : Component
        {
            if (fieldToSave != null)
                return fieldToSave;
            
            fieldToSave = component.GetComponentsInParent<T>(includeInactive);
            
            if (fieldToSave != null)
                return fieldToSave;
            
            switch (mode)
            {
                case GetComponentsMode.addZeroLengthIfNull:
                    return Array.Empty<T>();
                case GetComponentsMode.destroyIfNull:
                    Object.DestroyImmediate(component);
                    return null;
                case GetComponentsMode.none:
                default:
                    return fieldToSave;
            }
        }



        public static T? GetComponentInChildrenFieldSave<T>(this Component component, T? fieldToSave, bool includeInactive = false, GetComponentInMode mode = GetComponentInMode.none) where T : Component
        {
            if (fieldToSave != null && fieldToSave.gameObject == component.gameObject)
                return fieldToSave;
            
            fieldToSave = component.GetComponentInChildren<T>(includeInactive);
            
            if (fieldToSave != null || mode != GetComponentInMode.destroyIfNull)
                return fieldToSave;
            
            Object.DestroyImmediate(component);
            return null;
        }

        public static T[]? GetComponentsInChildrenFieldSave<T>(this Component component, T[]? fieldToSave, bool includeInactive = false, GetComponentsMode mode = GetComponentsMode.addZeroLengthIfNull) where T : Component
        {
            if (fieldToSave != null)
                return fieldToSave;
            
            fieldToSave = component.GetComponentsInChildren<T>(includeInactive);
            
            if (fieldToSave != null)
                return fieldToSave;
            
            switch (mode)
            {
                case GetComponentsMode.addZeroLengthIfNull:
                    return Array.Empty<T>();
                case GetComponentsMode.destroyIfNull:
                    Object.DestroyImmediate(component);
                    return null;
                case GetComponentsMode.none:
                default:
                    return fieldToSave;
            }

        }
    }
}
