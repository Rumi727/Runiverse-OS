#nullable enable

namespace RuniOS.Resource
{
    /// <summary>
    /// Provides the default namespace used when deserializing shorthand <see cref="Identifier"/> JSON values.<br/>
    /// 축약형 <see cref="Identifier"/> JSON 값을 역직렬화할 때 사용할 기본 네임스페이스를 제공합니다.
    /// </summary>
    /// <param name="defaultNamespace">
    /// The namespace used when an identifier string does not include an explicit namespace.<br/>
    /// 식별자 문자열에 명시적인 네임스페이스가 없을 때 사용할 네임스페이스입니다.
    /// </param>
    /// <remarks>
    /// Pass this value through <see cref="Newtonsoft.Json.JsonSerializerSettings.Context"/> as the context object when loading JSON that has a known owner namespace.<br/>
    /// If no context is supplied, <see cref="Identifier.defaultNamespace"/> is used by <see cref="RuniOS.Json.Converters.Resource.IdentifierConverter"/>.
    /// <br/><br/>
    /// 소유 네임스페이스가 알려진 JSON을 로드할 때 이 값을 <see cref="Newtonsoft.Json.JsonSerializerSettings.Context"/>의 컨텍스트 객체로 전달합니다.<br/>
    /// 컨텍스트가 제공되지 않으면 <see cref="RuniOS.Json.Converters.Resource.IdentifierConverter"/>는 <see cref="Identifier.defaultNamespace"/>를 사용합니다.
    /// </remarks>
    public readonly record struct IdentifierJsonContext(string defaultNamespace);
}