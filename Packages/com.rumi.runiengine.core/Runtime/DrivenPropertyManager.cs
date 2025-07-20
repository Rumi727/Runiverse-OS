#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;

using Object = UnityEngine.Object;
using UniDrivenPropertyManager = RuniEngine.APIBridge.UnityEngine.DrivenPropertyManager;

namespace RuniEngine
{
    public static class DrivenPropertyManager
    {
        static readonly List<DrivenPropertyData> _drivenProperties = new List<DrivenPropertyData>();
        public static IReadOnlyList<DrivenPropertyData> drivenProperties { get; } = _drivenProperties.AsReadOnly();



        [Conditional("UNITY_EDITOR")]
        public static void RegisterProperty(Object driver, Object target, string propertyPath)
        {
            try
            {
#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.
                UniDrivenPropertyManager.RegisterProperty(driver, target, propertyPath);
#pragma warning restore CS0618 // 형식 또는 멤버는 사용되지 않습니다.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }
            
            _drivenProperties.Add(new DrivenPropertyData(driver, target, propertyPath));
        }

        [Conditional("UNITY_EDITOR")]
        public static void UnregisterProperty(Object driver, Object target, string propertyPath)
        {
            try
            {
#pragma warning disable CS0618 // 형식 또는 멤버는 사용되지 않습니다.
                UniDrivenPropertyManager.UnregisterProperty(driver, target, propertyPath);
#pragma warning restore CS0618 // 형식 또는 멤버는 사용되지 않습니다.
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            _drivenProperties.Remove(new DrivenPropertyData(driver, target, propertyPath));
        }

        public struct DrivenPropertyData : IEquatable<DrivenPropertyData>
        {
            public Object driver;
            public Object target;

            public string propertyPath;

            public DrivenPropertyData(Object driver, Object target, string propertyPath)
            {
                this.driver = driver;
                this.target = target;

                this.propertyPath = propertyPath;
            }

            public readonly bool Equals(DrivenPropertyData other) => driver == other.driver && target == other.target && propertyPath == other.propertyPath;

            public override readonly bool Equals(object? obj) => obj is DrivenPropertyData data && Equals(data);
            public override readonly int GetHashCode() => HashCode.Combine(driver, target, propertyPath);

            public static bool operator ==(DrivenPropertyData left, DrivenPropertyData right) => left.Equals(right);
            public static bool operator !=(DrivenPropertyData left, DrivenPropertyData right) => !(left == right);
        }
    }
}
