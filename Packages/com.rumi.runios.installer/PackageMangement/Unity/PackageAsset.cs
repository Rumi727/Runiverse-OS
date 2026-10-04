#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace RuniOS.PackageManagement.Unity
{
    /// <summary>
    /// Stores shared authoring inputs for package definitions implemented as assets.<br/>
    /// asset으로 구현한 package 정의의 공통 authoring 입력을 저장합니다.
    /// </summary>
    public abstract class PackageAsset : ScriptableObject, IPackage
    {
        [SerializeField] string _id = string.Empty;
        [SerializeField] string _displayName = string.Empty;

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
        /// Gets or sets the optional localization key for the display name.<br/>
        /// 표시 이름의 선택적인 번역 key를 가져오거나 설정합니다.
        /// </summary>
        public string labelKey = string.Empty;
        /// <summary>
        /// Gets or sets the localization key for the one-line description.<br/>
        /// 한 줄 설명의 번역 key를 가져오거나 설정합니다.
        /// </summary>
        public string oneLineDescriptionKey = string.Empty;
        /// <summary>
        /// Gets or sets the localization key for the full description.<br/>
        /// 상세 설명의 번역 key를 가져오거나 설정합니다.
        /// </summary>
        public string descriptionKey = string.Empty;
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
        public abstract IInstallation CreateInstallation(bool isRoot);
        /// <summary>
        /// Gets direct dependencies, preserving missing reference slots.<br/>
        /// 누락된 참조 슬롯을 유지하며 직접 dependencies를 가져옵니다.
        /// </summary>
        public IReadOnlyList<IPackage?> dependencies => _dependencies;
        [SerializeField] PackageAsset?[] _dependencies = [];
    }
}
