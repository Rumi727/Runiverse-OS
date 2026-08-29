#nullable enable
using RuniOS.Reflection;
using System.Collections.Concurrent;
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Converts <see cref="Text"/> objects into rich-text strings.<br/>
    /// <see cref="Text"/> 객체를 rich text 문자열로 변환합니다.
    /// </summary>
    public abstract partial class RichTextBuilder : TextBuilder
    {
        static RichTextBuilder() => registry.onChanged += cache.Clear;
        static readonly ConcurrentDictionary<Type, RichTextBuilder> cache = new ConcurrentDictionary<Type, RichTextBuilder>();

        [GenerateTypeRegistry]
        public static partial AttributedTypeRegistry<TextRendererAttribute> registry { get; }

        /// <summary>
        /// Finds the rich-text builder registered for the runtime type of a text instance.<br/>
        /// 텍스트 인스턴스의 런타임 타입에 등록된 rich text 빌더를 찾습니다.
        /// </summary>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <returns>
        /// The builder registered for the text type.<br/>
        /// 텍스트 타입에 등록된 빌더를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="text"/> is <see langword="null"/>.<br/>
        /// <paramref name="text"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no rich-text builder is registered for the text type.<br/>
        /// 텍스트 타입에 등록된 rich text 빌더가 없는 경우 발생합니다.
        /// </exception>
        protected static RichTextBuilder FindBuilder(Text text)
        {
            ExceptionUtility.ThrowIfArgumentNull(text);
            Type textType = text.GetType();

            return cache.GetOrAdd(textType, x =>
            {
                Type? rendererType = registry.Resolve(x);
                if (rendererType == null)
                    throw new InvalidOperationException($"{x} is an invalid entry type. An entry type with an {nameof(RichTextBuilder)} implementation is required.");

                return (RichTextBuilder)Activator.CreateInstance(rendererType);
            });
        }

        /// <summary>
        /// Builds a rich-text string from a text instance.<br/>
        /// 텍스트 인스턴스에서 rich text 문자열을 만듭니다.
        /// </summary>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <returns>
        /// The rendered rich-text string.<br/>
        /// 렌더링된 rich text 문자열을 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="text"/> is <see langword="null"/>.<br/>
        /// <paramref name="text"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no rich-text builder is registered for the text type.<br/>
        /// 텍스트 타입에 등록된 rich text 빌더가 없는 경우 발생합니다.
        /// </exception>
        public static string Build(Text text)
        {
            ExceptionUtility.ThrowIfArgumentNull(text);

            StringBuilder builder = StringBuilderCache.Acquire();
            try
            {
                BuildTo(text, builder);
                return builder.ToString();
            }
            finally
            {
                StringBuilderCache.Release(builder);
            }
        }

        /// <summary>
        /// Appends rich-text output for a text instance to an existing builder.<br/>
        /// 텍스트 인스턴스의 rich text 출력을 기존 빌더에 추가합니다.
        /// </summary>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="builder">
        /// The destination string builder.<br/>
        /// 대상 문자열 빌더입니다.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="text"/> is <see langword="null"/>.<br/>
        /// <paramref name="text"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no rich-text builder is registered for the text type.<br/>
        /// 텍스트 타입에 등록된 rich text 빌더가 없는 경우 발생합니다.
        /// </exception>
        public static void BuildTo(Text text, StringBuilder builder)
        {
            TextStyleState styleState = TextStyleStateCache.Acquire();
            try
            {
                FindBuilder(text).Append(builder, text, styleState);
            }
            finally
            {
                TextStyleStateCache.Release(styleState);
            }
        }

        /// <summary>
        /// Appends rich-text output for a text instance using an existing style state.<br/>
        /// 기존 스타일 상태를 사용하여 텍스트 인스턴스의 rich text 출력을 추가합니다.
        /// </summary>
        /// <param name="builder">
        /// The destination string builder.<br/>
        /// 대상 문자열 빌더입니다.
        /// </param>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="styleState">
        /// The style state shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 스타일 상태입니다.
        /// </param>
        public void Append(StringBuilder builder, Text text, TextStyleState styleState)
        {
            if (text.style != null)
            {
                styleState.Open(text.style);
                RichTextUtility.OpenStyle(builder, styleState);
            }

            AppendCore(builder, text, styleState);

            if (text.style != null)
            {
                RichTextUtility.CloseStyle(builder, styleState);
                styleState.Close();
            }
        }
        /// <summary>
        /// Appends the rich-text output for the concrete text type.<br/>
        /// 구체 텍스트 타입의 rich text 출력을 추가합니다.
        /// </summary>
        /// <param name="builder">
        /// The destination string builder.<br/>
        /// 대상 문자열 빌더입니다.
        /// </param>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="styleState">
        /// The style state shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 스타일 상태입니다.
        /// </param>
        protected abstract void AppendCore(StringBuilder builder, Text text, TextStyleState styleState);

        /*protected static void AppendStyle(string content, TextStyle style, StringBuilder builder)
        {
            Add<HexColor>("bold", "b");
            Add2<HexColor>("color", "color", x => x.value);

            void Add<T>(string key, string richKey) where T : notnull
            {
                Optional<T> optional = style.Get(new StyleProperty<T>(key));
                if (optional.hasValue)
                {
                    builder.Append("<").Append(richKey).Append(">");
                    builder.Append(content);
                    builder.Append("</").Append(richKey).Append(">");
                }
            }

            void Add2<T>(string key, string richKey, Func<T, string> option) where T : notnull
            {
                Optional<T> optional = style.Get(new StyleProperty<T>(key));
                if (optional.hasValue)
                {
                    builder.Append("<").Append(richKey).Append("=").Append(option.Invoke(optional.value)).Append(">");
                    builder.Append(content);
                    builder.Append("</").Append(richKey).Append(">");
                }
            }
        }*/
    }
}
