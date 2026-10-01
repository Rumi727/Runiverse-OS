#nullable enable
using System;
using System.Collections.Generic;
using RuniOS.PackageManagement.Internal;

namespace RuniOS.PackageManagement.Model
{
    /// <summary>
    /// Locates a declaration using a document identifier and an adapter-defined position.<br/>
    /// 문서 식별자와 어댑터가 정의한 위치로 선언 위치를 나타냅니다.
    /// </summary>
    public sealed class DeclarationLocation
    {
        /// <summary>
        /// Gets the document identifier supplied by the declaration adapter.<br/>
        /// 선언 어댑터가 제공한 문서 식별자를 가져옵니다.
        /// </summary>
        public string document { get; }
        /// <summary>
        /// Gets the adapter-defined location within the document.<br/>
        /// 어댑터가 정의한 문서 내 위치를 가져옵니다.
        /// </summary>
        public string position { get; }

        /// <summary>
        /// Captures a document location without interpreting its format.<br/>
        /// 형식을 해석하지 않고 문서 위치를 저장합니다.
        /// </summary>
        /// <param name="document">
        /// The document identifier.<br/>
        /// 문서 식별자입니다.
        /// </param>
        /// <param name="position">
        /// The adapter-defined position, which may be empty.<br/>
        /// 빈 문자열일 수 있는 어댑터 정의 위치입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required value is <see langword="null"/>.<br/>
        /// 필수 값이 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="document"/> is empty or whitespace.<br/>
        /// <paramref name="document"/>가 비어 있거나 공백이면 발생합니다.
        /// </exception>
        public DeclarationLocation(string document, string position)
        {
            this.document = Snapshots.Text(document, nameof(document));
            this.position = position ?? throw new ArgumentNullException(nameof(position));
        }
    }
}
