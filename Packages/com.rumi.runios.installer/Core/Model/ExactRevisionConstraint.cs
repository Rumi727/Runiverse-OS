#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Requires an exact opaque revision without interpreting its version syntax.<br/>
    /// 버전 문법을 해석하지 않고 정확한 리비전을 요구합니다.
    /// </summary>
    public sealed class ExactRevisionConstraint : PackageConstraint
    {
        /// <summary>
        /// Identifies the built-in opaque exact-revision evaluator.<br/>
        /// 기본 정확한 리비전 평가기를 식별합니다.
        /// </summary>
        public const string constraintKind = "package:exact-revision";
        /// <summary>
        /// Gets the exact opaque revision; symbolic references must already be resolved.<br/>
        /// 정확한 리비전을 가져오며 기호 참조는 이미 해석된 상태여야 합니다.
        /// </summary>
        public string revision { get; }

        /// <summary>
        /// Captures an exact revision without parsing its format.<br/>
        /// 형식을 해석하지 않고 정확한 리비전을 저장합니다.
        /// </summary>
        /// <param name="revision">
        /// The exact opaque revision.<br/>
        /// 정확한 리비전입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="revision"/> is empty or whitespace.<br/>
        /// <paramref name="revision"/>이 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public ExactRevisionConstraint(string revision) : base(constraintKind) =>
            this.revision = Snapshots.Text(revision, nameof(revision));
    }
}
