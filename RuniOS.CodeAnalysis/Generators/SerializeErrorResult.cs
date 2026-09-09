namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// Represents one error found while preserving the original meaning during serialization.<br/>
/// 직렬화 과정에서 원래 의미를 보존하지 못해 발견된 단일 오류를 나타냅니다.
/// </summary>
/// <param name="error">
/// The kind of error.<br/>
/// 오류의 종류입니다.
/// </param>
/// <param name="problematicObject">
/// The object associated with the error; it may be <see langword="null"/>.<br/>
/// 오류와 관련된 객체이며, <see langword="null"/>일 수 있습니다.
/// </param>
public readonly record struct SerializeErrorResult(SerializeError error, object? problematicObject);