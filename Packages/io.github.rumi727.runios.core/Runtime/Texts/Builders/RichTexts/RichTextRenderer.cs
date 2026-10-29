#nullable enable
using RuniOS.Reflection;
using System.Collections.Concurrent;
using System.Text;

namespace RuniOS.Texts.Builders.RichTexts
{
    /// <summary>
    /// Serves as the base type for renderers that write the rich-text output of a specific <see cref="Text"/> type.<br/>
    /// A renderer is registered for its text type with <see cref="TextRendererAttribute"/> and used by <see cref="RichTextBuilder"/>.
    /// <br/><br/>
    /// 특정 <see cref="Text"/> 타입의 rich text 출력을 작성하는 렌더러의 기본 타입 역할을 합니다.<br/>
    /// 렌더러는 <see cref="TextRendererAttribute"/>로 자신의 텍스트 타입에 등록되며 <see cref="RichTextBuilder"/>가 사용합니다.
    /// </summary>
    /// <remarks>
    /// <b>Renderers must be stateless.</b><br/>
    /// Only one instance is created and cached per text type, and that instance is shared by every build.<br/>
    /// The same instance is also re-entered within a single build when texts of the same type are nested, such as a <see cref="GroupText"/> inside another <see cref="GroupText"/>.<br/>
    /// Keep per-build state in <see cref="TextBuildContext"/> or in local variables, not in instance fields.
    /// <br/><br/>
    /// <b>렌더러는 상태를 가지면 안 됩니다.</b><br/>
    /// 텍스트 타입마다 인스턴스가 하나만 생성되어 캐시되며, 모든 빌드가 그 인스턴스를 공유합니다.<br/>
    /// 같은 타입의 텍스트가 중첩되면(<see cref="GroupText"/> 안의 <see cref="GroupText"/> 등) 하나의 빌드 안에서도 같은 인스턴스에 다시 진입합니다.<br/>
    /// 빌드별 상태는 인스턴스 필드가 아니라 <see cref="TextBuildContext"/>나 지역 변수에 보관하십시오.
    /// </remarks>
    public abstract partial class RichTextRenderer
    {
        static RichTextRenderer() => registry.onChanged += cache.Clear;
        static readonly ConcurrentDictionary<Type, RichTextRenderer> cache = new ConcurrentDictionary<Type, RichTextRenderer>();

        [GenerateAttributedTypeRegistry]
        public static partial AttributedTypeRegistry<TextRendererAttribute> registry { get; }

        /// <summary>
        /// Finds the rich-text renderer registered for the runtime type of a text instance.<br/>
        /// 텍스트 인스턴스의 런타임 타입에 등록된 rich text 렌더러를 찾습니다.
        /// </summary>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <returns>
        /// The renderer registered for the text type.<br/>
        /// 텍스트 타입에 등록된 렌더러를 반환합니다.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="text"/> is <see langword="null"/>.<br/>
        /// <paramref name="text"/>가 <see langword="null"/>인 경우 발생합니다.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown when no rich-text renderer is registered for the text type.<br/>
        /// 텍스트 타입에 등록된 rich text 렌더러가 없는 경우 발생합니다.
        /// </exception>
        protected internal static RichTextRenderer FindRenderer(Text text)
        {
            ExceptionUtility.ThrowIfArgumentNull(text);
            Type textType = text.GetType();

            return cache.GetOrAdd(textType, x =>
            {
                Type? rendererType = registry.Resolve(x);
                if (rendererType == null)
                    throw new InvalidOperationException($"{x} is an invalid entry type. An entry type with an {nameof(RichTextRenderer)} implementation is required.");

                return (RichTextRenderer)Activator.CreateInstance(rendererType);
            });
        }

        /// <summary>
        /// Appends rich-text output for a text instance using an existing build context.<br/>
        /// 기존 빌드 컨텍스트를 사용하여 텍스트 인스턴스의 rich text 출력을 추가합니다.
        /// </summary>
        /// <param name="builder">
        /// The destination string builder.<br/>
        /// 대상 문자열 빌더입니다.
        /// </param>
        /// <param name="text">
        /// The text instance to render.<br/>
        /// 렌더링할 텍스트 인스턴스입니다.
        /// </param>
        /// <param name="context">
        /// The build context shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 빌드 컨텍스트입니다.
        /// </param>
        public void Append(StringBuilder builder, Text text, TextBuildContext context)
        {
            BeginAppend(builder, text, context);
            AppendCore(builder, text, context);
            EndAppend(builder, text, context);
        }

        // 모든 Append 경로가 텍스트를 의존성으로 보고하고 스타일을 열고 닫도록 공유합니다.
        private protected static void BeginAppend(StringBuilder builder, Text text, TextBuildContext context)
        {
            context.AddDependency(text);

            if (text.style != null)
            {
                context.styleState.Open(text.style);
                RichTextUtility.OpenStyle(builder, context.styleState);
            }
        }

        private protected static void EndAppend(StringBuilder builder, Text text, TextBuildContext context)
        {
            if (text.style != null)
            {
                RichTextUtility.CloseStyle(builder, context.styleState);
                context.styleState.Close();
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
        /// <param name="context">
        /// The build context shared across nested render calls.<br/>
        /// 중첩 렌더 호출 간 공유되는 빌드 컨텍스트입니다.
        /// </param>
        protected abstract void AppendCore(StringBuilder builder, Text text, TextBuildContext context);

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
