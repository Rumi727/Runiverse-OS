#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Stores an optional package inventory without becoming a package or discovering definitions.<br/>
    /// package가 되거나 정의를 검색하지 않고 선택적인 package 목록을 저장합니다.
    /// </summary>
    [CreateAssetMenu(fileName = "PackageCatalog", menuName = "Runiverse OS/Installer/Package Catalog")]
    public sealed class PackageCatalog : ScriptableObject
    {
        [SerializeField, PackageReference] ScriptableObject?[] _packages = Array.Empty<ScriptableObject?>();
        /// <summary>
        /// Gets the authored definition inventory including missing reference slots.<br/>
        /// 누락된 참조 슬롯을 포함한 작성된 정의 목록을 가져옵니다.
        /// </summary>
        public IReadOnlyList<IPackage?> packages => PackageAsset.ReadReferences(_packages);
    }
}
