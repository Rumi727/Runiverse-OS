#nullable enable
using System;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Restricts inspector object assignment to assets implementing IPackage.<br/>
    /// inspector 객체 할당을 IPackage를 구현하는 asset으로 제한합니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class PackageReferenceAttribute : PropertyAttribute { }
}
