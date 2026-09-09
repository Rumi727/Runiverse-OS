using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System;

namespace RuniOS.CodeAnalysis.Generators;

/// <summary>
/// <see cref="Location"/>의 진단 표시에 필요한 위치 정보를
/// 값 동등 비교가 가능한 형태로 보관합니다.
/// </summary>
/// <remarks>
/// 원본 <see cref="Location"/>이나 <see cref="SyntaxTree"/>는 보존하지 않습니다.
/// 따라서 <see cref="ToLocation"/>으로 생성되는 위치는 원본 위치 자체가 아니라,
/// 동일한 진단 위치를 나타내기 위한 새로운 <see cref="Location"/>입니다.
/// </remarks>
public readonly record struct LocationData
{
    public string filePath { get; }
    public TextSpan sourceSpan { get; }
    public LinePositionSpan span { get; }

    public LocationData(Location location)
    {
        FileLinePositionSpan lineSpan = location.GetMappedLineSpan();
        if (!lineSpan.IsValid)
            throw new ArgumentException("The location does not contain valid file position information.", nameof(location));

        filePath = lineSpan.Path;
        sourceSpan = location.SourceSpan;
        span = lineSpan.Span;
    }

    public Location ToLocation() => Location.Create(filePath, sourceSpan, span);
}