#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity.Editor
{
    /// <summary>
    /// Stores shared authoring inputs for package definitions implemented as assets.<br/>
    /// asset으로 구현한 package 정의의 공통 authoring 입력을 저장합니다.
    /// </summary>
    public abstract class PackageAsset : ScriptableObject, IPackage
    {
        [SerializeField] string _id = string.Empty;
        [SerializeField] string _displayName = string.Empty;
        [SerializeField, PackageReference] ScriptableObject?[] _dependencies = Array.Empty<ScriptableObject?>();
        /// <summary>
        /// Gets or sets the logical identifier text used by the asset.<br/>
        /// asset이 사용하는 논리적 식별자 문자열을 가져오거나 설정합니다.
        /// </summary>
        public string packageId { get => _id; set => _id = value; }
        /// <summary>
        /// Gets the logical identifier.<br/>
        /// 논리적 식별자를 가져옵니다.
        /// </summary>
        public PackageId id => new(_id);
        /// <summary>
        /// Gets or sets the display name; empty text falls back to the asset name.<br/>
        /// 표시 이름을 가져오거나 설정하며 빈 문자열은 asset 이름을 사용합니다.
        /// </summary>
        public string displayName { get => string.IsNullOrEmpty(_displayName) ? name : _displayName; set => _displayName = value; }
        /// <summary>
        /// Gets or sets the native UPM name override; empty text uses the logical identifier.<br/>
        /// native UPM 이름 override를 가져오거나 설정하며 빈 문자열은 논리적 식별자를 사용합니다.
        /// </summary>
        public string packageName = string.Empty;
        /// <summary>
        /// Gets the effective native package name.<br/>
        /// 실제로 사용하는 native package 이름을 가져옵니다.
        /// </summary>
        protected string nativeName => string.IsNullOrEmpty(packageName) ? _id : packageName;
        /// <inheritdoc/>
        public abstract string exactIdentity { get; }
        /// <inheritdoc/>
        public abstract IInstallation installation { get; }
        /// <summary>
        /// Gets direct dependencies, preserving missing reference slots.<br/>
        /// 누락된 참조 슬롯을 유지하며 직접 dependencies를 가져옵니다.
        /// </summary>
        public IReadOnlyList<IPackage?> dependencies => ReadReferences(_dependencies);
        /// <summary>
        /// Converts asset references to package references without discarding invalid entries.<br/>
        /// 유효하지 않은 항목을 버리지 않고 asset 참조를 package 참조로 변환합니다.
        /// </summary>
        /// <param name="references">
        /// Asset references authored by the caller.<br/>
        /// caller가 작성한 asset 참조들입니다.
        /// </param>
        /// <returns>
        /// Package references including missing slots.<br/>
        /// 누락된 슬롯을 포함한 package 참조를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when references is null.<br/>
        /// references가 null이면 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when a non-null asset does not implement IPackage.<br/>
        /// null이 아닌 asset이 IPackage를 구현하지 않으면 발생합니다.
        /// </exception>
        public static IReadOnlyList<IPackage?> ReadReferences(IEnumerable<ScriptableObject?> references)
        {
            if (references is null) throw new ArgumentNullException(nameof(references));
            var packages = new List<IPackage?>();
            int index = 0;
            foreach (ScriptableObject? asset in references)
            {
                if (asset is null || asset == null) packages.Add(null);
                else if (asset is IPackage package) packages.Add(package);
                else throw new ArgumentException($"Asset '{asset.name}' at slot {index} does not implement IPackage.", nameof(references));
                index++;
            }
            return packages.AsReadOnly();
        }
    }
}
