#nullable enable
using RuniOS.Resource;
using System.Collections;

namespace RuniOS.Texts
{
    /// <summary>
    /// Represents a text value that is resolved from a localization identifier and format arguments.<br/>
    /// 로컬라이징 식별자와 format 인수에서 해석되는 텍스트 값을 나타냅니다.
    /// </summary>
    public class LocalizationText : Text
    {
        /// <summary>
        /// Initializes a localization text with the specified identifier and arguments.<br/>
        /// 지정된 식별자와 인수로 로컬라이징 텍스트를 초기화합니다.
        /// </summary>
        /// <param name="identifier">
        /// The localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자입니다.
        /// </param>
        /// <param name="args">
        /// The text arguments to copy into <see cref="args"/>.<br/>
        /// <see cref="args"/>에 복사할 텍스트 인수입니다.
        /// </param>
        /// <param name="languageCode">
        /// The language code to use, or <see langword="null"/> to use the current default language.<br/>
        /// 사용할 언어 코드이며, <see langword="null"/>이면 현재 기본 언어를 사용합니다.
        /// </param>
        public LocalizationText(Identifier identifier, ReadOnlySpan<Text> args, string? languageCode = null)
        {
            this.identifier = identifier;
            this.args = new ArgumentList(this, args);
            this.languageCode = languageCode;
        }

        /// <summary>
        /// Initializes a localization text with the specified identifier and arguments.<br/>
        /// 지정된 식별자와 인수로 로컬라이징 텍스트를 초기화합니다.
        /// </summary>
        /// <param name="identifier">
        /// The localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자입니다.
        /// </param>
        /// <param name="args">
        /// The text arguments to copy into <see cref="args"/>.<br/>
        /// <see cref="args"/>에 복사할 텍스트 인수입니다.
        /// </param>
        /// <param name="languageCode">
        /// The language code to use, or <see langword="null"/> to use the current default language.<br/>
        /// 사용할 언어 코드이며, <see langword="null"/>이면 현재 기본 언어를 사용합니다.
        /// </param>
        public LocalizationText(Identifier identifier, IEnumerable<Text> args, string? languageCode = null)
        {
            this.identifier = identifier;
            this.args = new ArgumentList(this, args);
            this.languageCode = languageCode;
        }

        /// <summary>
        /// Gets or sets the localization identifier to resolve.<br/>
        /// 해석할 로컬라이징 식별자를 가져오거나 설정합니다.
        /// </summary>
        public Identifier identifier
        {
            get;
            set
            {
                field = value;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Gets the text arguments used by the localized format string.<br/>
        /// 로컬라이징된 format 문자열에서 사용할 텍스트 인수를 가져옵니다.
        /// </summary>
        public ArgumentList args { get; }

        /// <summary>
        /// Gets or sets the language code used to resolve this translation.<br/>
        /// 이 번역을 해석할 때 사용하는 언어 코드를 가져오거나 설정합니다.
        /// </summary>
        public string? languageCode
        {
            get;
            set
            {
                field = value;
                NotifyChanged();
            }
        }

        /// <summary>
        /// Represents the format arguments owned by a <see cref="LocalizationText"/>.<br/>
        /// Modifying this list raises <see cref="Text.onChanged"/> of the owning text.
        /// <br/><br/>
        /// <see cref="LocalizationText"/>가 소유하는 format 인수 목록을 나타냅니다.<br/>
        /// 이 목록을 수정하면 소유한 텍스트의 <see cref="Text.onChanged"/>가 발생합니다.
        /// </summary>
        public sealed class ArgumentList : IReadOnlyList<Text>, IList<Text>
        {
            internal ArgumentList(LocalizationText owner, ReadOnlySpan<Text> items)
            {
                this.owner = owner;

                this.items.Capacity = items.Length;
                for (int i = 0; i < items.Length; i++)
                    this.items.Add(items[i]);
            }

            internal ArgumentList(LocalizationText owner, IEnumerable<Text> items)
            {
                this.owner = owner;
                this.items.AddRange(items);
            }

            readonly LocalizationText owner;
            readonly List<Text> items = [];

            /// <summary>
            /// Gets or sets the argument at the specified index.<br/>
            /// 지정된 인덱스의 인수를 가져오거나 설정합니다.
            /// </summary>
            /// <param name="index">
            /// The zero-based index of the argument.<br/>
            /// 인수의 0부터 시작하는 인덱스입니다.
            /// </param>
            /// <returns>
            /// The argument at <paramref name="index"/>.<br/>
            /// <paramref name="index"/> 위치의 인수를 반환합니다.
            /// </returns>
            public Text this[int index]
            {
                get => items[index];
                set
                {
                    items[index] = value;
                    owner.NotifyChanged();
                }
            }

            /// <summary>
            /// Gets the number of arguments in this list.<br/>
            /// 이 목록에 포함된 인수 수를 가져옵니다.
            /// </summary>
            public int count => items.Count;
            int IReadOnlyCollection<Text>.Count => count;
            int ICollection<Text>.Count => count;

            bool ICollection<Text>.IsReadOnly => ((ICollection<Text>)items).IsReadOnly;

            /// <summary>
            /// Appends an argument to the end of this list.<br/>
            /// 이 목록의 끝에 인수를 추가합니다.
            /// </summary>
            /// <param name="item">
            /// The argument to append.<br/>
            /// 추가할 인수입니다.
            /// </param>
            public void Add(Text item)
            {
                items.Add(item);
                owner.NotifyChanged();
            }

            /// <summary>
            /// Inserts an argument at the specified index.<br/>
            /// 지정된 인덱스에 인수를 삽입합니다.
            /// </summary>
            /// <param name="index">
            /// The zero-based index at which to insert <paramref name="item"/>.<br/>
            /// <paramref name="item"/>을 삽입할 0부터 시작하는 인덱스입니다.
            /// </param>
            /// <param name="item">
            /// The argument to insert.<br/>
            /// 삽입할 인수입니다.
            /// </param>
            public void Insert(int index, Text item)
            {
                items.Insert(index, item);
                owner.NotifyChanged();
            }

            /// <summary>
            /// Removes the first occurrence of an argument from this list.<br/>
            /// 이 목록에서 인수의 첫 번째 일치 항목을 제거합니다.
            /// </summary>
            /// <param name="item">
            /// The argument to remove.<br/>
            /// 제거할 인수입니다.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="item"/> was removed; otherwise, <see langword="false"/>.<br/>
            /// <paramref name="item"/>이 제거되었으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            public bool Remove(Text item)
            {
                if (!items.Remove(item))
                    return false;

                owner.NotifyChanged();
                return true;
            }

            /// <summary>
            /// Removes the argument at the specified index.<br/>
            /// 지정된 인덱스의 인수를 제거합니다.
            /// </summary>
            /// <param name="index">
            /// The zero-based index of the argument to remove.<br/>
            /// 제거할 인수의 0부터 시작하는 인덱스입니다.
            /// </param>
            public void RemoveAt(int index)
            {
                items.RemoveAt(index);
                owner.NotifyChanged();
            }

            /// <summary>
            /// Removes all arguments from this list.<br/>
            /// 이 목록에서 모든 인수를 제거합니다.
            /// </summary>
            public void Clear()
            {
                items.Clear();
                owner.NotifyChanged();
            }

            /// <summary>
            /// Determines whether this list contains the specified argument.<br/>
            /// 이 목록이 지정된 인수를 포함하는지 확인합니다.
            /// </summary>
            /// <param name="item">
            /// The argument to locate.<br/>
            /// 찾을 인수입니다.
            /// </param>
            /// <returns>
            /// <see langword="true"/> if <paramref name="item"/> is found; otherwise, <see langword="false"/>.<br/>
            /// <paramref name="item"/>을 찾으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>를 반환합니다.
            /// </returns>
            public bool Contains(Text item) => items.Contains(item);

            /// <summary>
            /// Gets the index of the specified argument.<br/>
            /// 지정된 인수의 인덱스를 가져옵니다.
            /// </summary>
            /// <param name="item">
            /// The argument to locate.<br/>
            /// 찾을 인수입니다.
            /// </param>
            /// <returns>
            /// The zero-based index of <paramref name="item"/>, or -1 if it is not found.<br/>
            /// <paramref name="item"/>의 0부터 시작하는 인덱스이며, 찾지 못하면 -1을 반환합니다.
            /// </returns>
            public int IndexOf(Text item) => items.IndexOf(item);

            /// <summary>
            /// Copies the arguments to an array starting at the specified array index.<br/>
            /// 지정된 배열 인덱스부터 인수를 배열에 복사합니다.
            /// </summary>
            /// <param name="array">
            /// The destination array.<br/>
            /// 대상 배열입니다.
            /// </param>
            /// <param name="arrayIndex">
            /// The zero-based index in <paramref name="array"/> at which copying begins.<br/>
            /// 복사를 시작할 <paramref name="array"/>의 0부터 시작하는 인덱스입니다.
            /// </param>
            public void CopyTo(Text[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

            /// <inheritdoc/>
            public IEnumerator<Text> GetEnumerator() => items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)items).GetEnumerator();
        }
    }
}
