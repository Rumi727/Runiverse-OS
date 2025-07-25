#nullable enable
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

namespace RuniEngine.IO
{
    /// <summary>
    /// 플랫폼에 독립적인 파일 또는 디렉터리 경로를 나타내는 구조체입니다.
    /// 경로 정규화, 결합, 비교 및 다양한 경로 부분 추출 유틸리티 메서드를 제공합니다.
    /// 모든 경로는 내부적으로 표준 구분자(<see cref="directorySeparatorChar"/>)를 사용하도록 정규화됩니다.
    /// </summary>
    [Serializable]
    public struct FilePath : IEquatable<FilePath>, ISerializationCallbackReceiver
    {
        /// <summary>
        /// 표준 디렉터리 구분 문자로, 항상 '/'입니다.<br/>
        /// 모든 경로는 내부적으로 이 구분자를 사용하도록 정규화됩니다.
        /// </summary>
        public const char directorySeparatorChar = '/';

        /// <summary>
        /// Windows 운영체제에서 사용되는 대체 디렉터리 구분 문자로, 항상 '\'입니다.<br/>
        /// 경로 입력 시 이 문자가 사용될 수 있으며, 내부적으로 <see cref="directorySeparatorChar"/>로 정규화됩니다.
        /// </summary>
        public const char windowsDirectorySeparatorChar = '\\';

        /// <summary>
        /// 파일 또는 경로 이름에서 유효하지 않은 문자를 대체할 때 사용되는 문자입니다. 항상 '_'입니다.<br/>
        /// 예를 들어, Windows에서 파일 이름에 사용할 수 없는 ':' 문자는 이 문자로 대체될 수 있습니다.
        /// </summary>
        public const char alternativeNameChar = '_';

        /// <summary>
        /// 로컬 파일 URL에 사용되는 접두사입니다. 항상 "file:///"입니다.<br/>
        /// 이 접두사는 로컬 파일 시스템의 경로를 웹 URL 형식으로 변환할 때 사용됩니다.
        /// </summary>
        public const string urlPathPrefix = "file:///";

        /// <summary>
        /// 인식되는 모든 디렉터리 구분자 문자 배열입니다. 현재 '/' 및 '\'를 포함합니다.<br/>
        /// 경로 정규화 시 이 문자들을 기준으로 분리 및 처리됩니다.
        /// </summary>
        public static readonly char[] directorySeparatorChars = new char[] { '/', '\\' };

        /// <summary>
        /// 빈 파일 경로를 나타내는 정적 읽기 전용 인스턴스입니다.<br/>
        /// <c>FilePath.Create(string.Empty)</c>와 동일하며, 경로가 없는 상태를 표현할 때 사용됩니다.
        /// </summary>
        public static readonly FilePath empty = new FilePath();

        static readonly char[] invalidPathChars = System.IO.Path.GetInvalidPathChars();
        static readonly char[] invalidFileNameChars = System.IO.Path.GetInvalidFileNameChars();



        /// <summary>
        /// 현재 <see cref="FilePath"/> 인스턴스가 나타내는 정규화된 경로의 문자열 표현을 가져오거나 설정합니다.<br/>
        /// 값을 설정할 때 입력된 문자열은 <see cref="NormalizePath"/> 메서드를 통해 자동으로 정규화됩니다.<br/>
        /// 이 과정에서 Windows 스타일의 역슬래시('\')는 슬래시('/')로 변경되고, 불필요한 시작/끝 구분자는 제거됩니다.<br/>
        /// 경로가 null이거나 비어있을 경우 <see cref="string.Empty"/>를 반환합니다.
        /// </summary>
        [AllowNull]
        public string value
        {
            readonly get => _value ?? string.Empty;
            set => _value = NormalizePath(value ?? string.Empty);
        }
        [SerializeField, FieldName("gui.value"), NotNullField, JsonIgnore] string? _value;

        /// <summary>
        /// 경로 문자열의 길이를 반환합니다.<br/>
        /// 경로가 비어있거나 null인 경우 0을 반환합니다.
        /// </summary>
        public readonly int length => _value?.Length ?? 0;



        /// <summary>
        /// 지정된 문자열 경로로부터 새 <see cref="FilePath"/> 인스턴스를 생성하고 정규화합니다.<br/>
        /// 입력된 경로는 <see cref="NormalizePath"/>를 통해 표준 형식으로 변환됩니다.
        /// </summary>
        /// <param name="path">생성할 파일 경로 문자열입니다. null이거나 비어있을 수 있습니다.</param>
        public FilePath(string? path) => _value = NormalizePath(path ?? string.Empty);

        public FilePath(string? path1, string? path2) => _value = NormalizePath(path1 ?? string.Empty) + directorySeparatorChar + NormalizePath(path2 ?? string.Empty);
        public FilePath(string? path1, string? path2, string? path3) => _value = NormalizePath(path1 ?? string.Empty) + directorySeparatorChar + NormalizePath(path2 ?? string.Empty) + directorySeparatorChar + NormalizePath(path3 ?? string.Empty);
        public FilePath(string? path1, string? path2, string? path3, string? path4) => _value = NormalizePath(path1 ?? string.Empty) + directorySeparatorChar + NormalizePath(path2 ?? string.Empty) + directorySeparatorChar + NormalizePath(path3 ?? string.Empty) + directorySeparatorChar + NormalizePath(path4 ?? string.Empty);
        public FilePath(string? path1, string? path2, string? path3, string? path4, string? path5) => _value = NormalizePath(path1 ?? string.Empty) + directorySeparatorChar + NormalizePath(path2 ?? string.Empty) + directorySeparatorChar + NormalizePath(path3 ?? string.Empty) + directorySeparatorChar + NormalizePath(path4 ?? string.Empty) + directorySeparatorChar + NormalizePath(path5 ?? string.Empty);

        /// <summary>
        /// 지정된 문자열 경로로부터 새 <see cref="FilePath"/> 인스턴스를 생성하고 정규화합니다.<br/>
        /// 입력된 경로는 <see cref="NormalizePath"/>를 통해 표준 형식으로 변환됩니다.
        /// </summary>
        /// <param name="paths">생성할 파일 경로 문자열입니다. null이거나 비어있을 수 있습니다.</param>
        /// <returns>정규화된 새 <see cref="FilePath"/> 인스턴스입니다. 입력이 null이거나 비어있으면 빈 경로를 나타내는 <see cref="empty"/> 인스턴스가 반환됩니다.</returns>
        public FilePath(params string[] paths) => _value = NormalizePath(string.Join(directorySeparatorChar, paths));

        /// <summary>
        /// 지정된 <see cref="ReadOnlySpan{T}"/> 경로로부터 새 <see cref="FilePath"/> 인스턴스를 생성하고 정규화합니다.<br/>
        /// 입력된 경로는 <see cref="NormalizePath"/>를 통해 표준 형식으로 변환됩니다.
        /// </summary>
        /// <param name="path">생성할 파일 경로를 나타내는 <see cref="ReadOnlySpan{T}"/>입니다.</param>
        /// <returns>정규화된 새 <see cref="FilePath"/> 인스턴스입니다. 입력이 비어있으면 빈 경로를 나타내는 <see cref="empty"/> 인스턴스가 반환됩니다.</returns>
        public FilePath(ReadOnlySpan<char> path) => _value = NormalizePath(path);



        /// <summary>
        /// 현재 경로에서 시스템에서 정의한 잘못된 경로 문자(<see cref="System.IO.Path.GetInvalidPathChars"/>)를 모두 제거한 새 <see cref="FilePath"/>를 반환합니다.<br/>
        /// 예를 들어, Windows에서 경로에 사용할 수 없는 '&lt;', '&gt;', '|' 등의 문자를 제거합니다.
        /// </summary>
        /// <returns>잘못된 문자가 제거된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public readonly FilePath CleanPath()
        {
            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            int lastPathIndex = value.LastIndexOf(directorySeparatorChar);
            if (lastPathIndex < 0)
                lastPathIndex = stringBuilder.Length;

            for (int i = 0; i < lastPathIndex; i++)
            {
                if (invalidPathChars.Contains(stringBuilder[i]))
                {
                    stringBuilder.Remove(i, 1);

                    lastPathIndex--;
                    i--;
                }
            }

            return StringBuilderCache.Release(stringBuilder);
        }

        /// <summary>
        /// 현재 경로에서 시스템에서 정의한 잘못된 경로 문자(<see cref="System.IO.Path.GetInvalidPathChars"/>)를 지정된 문자로 대체한 새 <see cref="FilePath"/>를 반환합니다.<br/>
        /// 기본 대체 문자는 <see cref="alternativeNameChar"/> ('_')입니다.
        /// </summary>
        /// <param name="newChar">잘못된 문자를 대체할 문자입니다. 기본값은 <see cref="alternativeNameChar"/>입니다.</param>
        /// <returns>잘못된 문자가 대체된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public readonly FilePath FixPathChars(char newChar = alternativeNameChar)
        {
            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            int lastPathIndex = value.LastIndexOf(directorySeparatorChar);
            if (lastPathIndex < 0)
                lastPathIndex = stringBuilder.Length;

            for (int i = 0; i < lastPathIndex; i++)
            {
                if (invalidPathChars.Contains(stringBuilder[i]))
                    stringBuilder[i] = newChar;
            }

            return StringBuilderCache.Release(stringBuilder);
        }



        /// <summary>
        /// 현재 경로의 파일 이름 부분에서 시스템에서 정의한 잘못된 파일 이름 문자(<see cref="System.IO.Path.GetInvalidFileNameChars"/>)를 모두 제거한 새 <see cref="FilePath"/>를 반환합니다.<br/>
        /// 이 메서드는 경로 전체가 아닌 파일 이름 부분에만 적용됩니다.
        /// </summary>
        /// <returns>잘못된 파일 이름 문자가 제거된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public readonly FilePath CleanFileName()
        {
            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            int lastPathIndex = value.LastIndexOf(directorySeparatorChar);
            for (int i = lastPathIndex + 1; i < stringBuilder.Length; i++)
            {
                if (invalidFileNameChars.Contains(stringBuilder[i]))
                {
                    stringBuilder.Remove(i, 1);
                    i--;
                }
            }

            return StringBuilderCache.Release(stringBuilder);
        }

        /// <summary>
        /// 현재 경로의 파일 이름 부분에서 시스템에서 정의한 잘못된 파일 이름 문자(<see cref="System.IO.Path.GetInvalidFileNameChars"/>)를 지정된 문자로 대체한 새 <see cref="FilePath"/>를 반환합니다.<br/>
        /// 기본 대체 문자는 <see cref="alternativeNameChar"/> ('_')입니다. 이 메서드는 경로 전체가 아닌 파일 이름 부분에만 적용됩니다.
        /// </summary>
        /// <param name="newChar">잘못된 문자를 대체할 문자입니다. 기본값은 <see cref="alternativeNameChar"/>입니다.</param>
        /// <returns>잘못된 파일 이름 문자가 대체된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public readonly FilePath FixFileNameChars(char newChar = alternativeNameChar)
        {
            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            int lastPathIndex = value.LastIndexOf(directorySeparatorChar);
            for (int i = lastPathIndex + 1; i < stringBuilder.Length; i++)
            {
                if (invalidFileNameChars.Contains(stringBuilder[i]))
                    stringBuilder[i] = newChar;
            }

            return StringBuilderCache.Release(stringBuilder);
        }



        /// <summary>
        /// 현재 경로의 파일 확장자을 문자열로 가져옵니다.<br/>
        /// 예를 들어, "dir/file.txt"의 경우 "txt"를 반환합니다.<br/>
        /// 경로에 확장자가 없으면 <see cref="string.Empty"/>를 반환합니다.
        /// </summary>
        /// <returns>파일 확장자(점 포함) 또는 확장자가 없는 경우 <see cref="string.Empty"/>.</returns>
        public readonly FileExtension GetExtension() => new FileExtension(this);

        /// <summary>
        /// 현재 경로에서 마지막 디렉터리 구분자(<see cref="directorySeparatorChar"/>) 이후의 부분만 문자열로 가져옵니다.<br/>
        /// 예를 들어, "dir/file.txt"의 경우 "file.txt"를 반환합니다.<br/>
        /// 경로에 디렉터리 구분자가 없으면 전체 경로 문자열을 반환합니다.
        /// </summary>
        /// <returns>마지막 디렉터리 구분자(<see cref="directorySeparatorChar"/>) 이후의 부분 또는 경로에 디렉터리가 없는 경우 전체 경로 문자열.</returns>
        public readonly string GetFileName()
        {
            int index = value.LastIndexOf(directorySeparatorChar);
            if (index < 0)
                return value;

            return value.Substring(index + 1);
        }

        /// <summary>
        /// 현재 경로의 파일 이름에서 확장자를 제외한 부분만 문자열로 가져옵니다.<br/>
        /// 예를 들어, "dir/file.txt"의 경우 "file"을 반환합니다.<br/>
        /// 파일 이름에 확장자가 없으면 파일 이름 전체를 반환합니다.
        /// </summary>
        /// <returns>확장자를 제외한 파일 이름 부분.</returns>
        public readonly string GetFileNameWithoutExtension()
        {
            string fileName = GetFileName();
            int extIndex = fileName.LastIndexOf('.');

            if (extIndex < 0)
                return fileName;
            else
                return fileName.Remove(extIndex);
        }

        /// <summary>
        /// 현재 경로에서 파일 확장자를 제외한 새 <see cref="FilePath"/> 인스턴스를 반환합니다.<br/>
        /// 예를 들어, "dir/file.txt"의 경우 "dir/file"을 반환합니다.<br/>
        /// 경로에 확장자가 없으면 원래 <see cref="FilePath"/>를 반환합니다.
        /// </summary>
        /// <returns>확장자가 제거된 새 <see cref="FilePath"/> 인스턴스.</returns>
        public readonly FilePath GetPathWithoutExtension()
        {
            int extIndex = value.LastIndexOf('.');
            if (extIndex < 0)
                return _value;
            else
                return value.Remove(extIndex);
        }

        /// <summary>
        /// 현재 경로의 상위 디렉터리 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스를 반환합니다.<br/>
        /// 예를 들어, "dir/file.txt"의 경우 "dir"을 반환합니다.<br/>
        /// 경로에 상위 디렉터리가 없거나 루트 경로인 경우 빈 <see cref="empty"/>를 반환합니다.
        /// </summary>
        /// <returns>상위 디렉터리 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스 또는 <see cref="empty"/>.</returns>
        public readonly FilePath GetParentPath()
        {
            int index = value.LastIndexOf(directorySeparatorChar);
            if (index < 0)
                return string.Empty;

            return value.Substring(0, index);
        }




        /// <summary>
        /// 현재 경로의 시작 부분이 지정된 <paramref name="relativeTo"/> 경로와 일치하는 경우,
        /// 해당 접두사 부분을 제거한 새 <see cref="FilePath"/>를 반환합니다.<br/>
        /// 예를 들어, 현재 경로가 "project/data/file.txt"이고 <paramref name="relativeTo"/>가 "project/data"인 경우,
        /// "file.txt"를 반환합니다.
        /// </summary>
        /// <param name="relativeTo">현재 경로의 시작 부분에서 제거할 접두사 경로입니다.</param>
        /// <returns>
        /// 지정된 접두사가 제거된 새 <see cref="FilePath"/>입니다.<br/>
        /// 접두사가 비어있거나 일치하지 않으면 원래 <see cref="FilePath"/>가 반환됩니다.<br/>
        /// 접두사가 현재 경로와 완전히 일치하면 빈 <see cref="empty"/>가 반환됩니다.
        /// </returns>
        public readonly FilePath TrimStartPath(FilePath? relativeTo) => string.IsNullOrEmpty(relativeTo?.value) ? this : TrimStartPath(relativeTo.Value);

        /// <summary>
        /// 현재 경로의 시작 부분이 지정된 <paramref name="relativeTo"/> 경로와 일치하는 경우,
        /// 해당 접두사 부분을 제거한 새 <see cref="FilePath"/>를 반환합니다.<br/>
        /// 예를 들어, 현재 경로가 "project/data/file.txt"이고 <paramref name="relativeTo"/>가 "project/data"인 경우,
        /// "file.txt"를 반환합니다.
        /// </summary>
        /// <param name="relativeTo">현재 경로의 시작 부분에서 제거할 접두사 경로입니다.</param>
        /// <returns>
        /// 지정된 접두사가 제거된 새 <see cref="FilePath"/>입니다.<br/>
        /// 접두사가 비어있거나 일치하지 않으면 원래 <see cref="FilePath"/>가 반환됩니다.<br/>
        /// 접두사가 현재 경로와 완전히 일치하면 빈 <see cref="empty"/>가 반환됩니다.
        /// </returns>
        public readonly FilePath TrimStartPath(FilePath relativeTo)
        {
            if (TryTrimStartPath(relativeTo, out FilePath result))
                return result;

            return this;
        }

        /// <summary>
        /// 현재 경로의 시작 부분이 지정된 <paramref name="relativeTo"/> 경로와 일치하는지 시도하고,
        /// 일치하는 경우 해당 접두사를 제거한 새 <see cref="FilePath"/>를 반환합니다.
        /// </summary>
        /// <param name="relativeTo">
        /// 현재 경로의 시작 부분에서 제거할 접두사 경로입니다.
        /// 이 경로가 비어있는 경우 (null 또는 빈 문자열), 현재 경로 자체가 결과로 반환되며 <see langword="true"/>를 반환합니다.
        /// </param>
        /// <param name="result">
        /// 메서드가 성공적으로 접두사를 제거하면, 제거된 접두사 부분을 제외한 새 <see cref="FilePath"/>가 여기에 할당됩니다.
        /// <paramref name="relativeTo"/>가 현재 경로와 완전히 일치하면, 빈 <see cref="empty"/>가 반환됩니다.
        /// <br/>
        /// <paramref name="relativeTo"/>가 비어있는 경우, 현재 <see cref="FilePath"/>의 값이 할당됩니다.
        /// <br/>
        /// 메서드가 실패하거나 현재 경로가 비어있는 경우, <see cref="empty"/>가 할당됩니다.
        /// </param>
        /// <returns>
        /// <paramref name="relativeTo"/>가 비어있거나 현재 경로가 <paramref name="relativeTo"/>로 시작하면 <see langword="true"/>를 반환하고,
        /// 현재 경로가 비어있거나 (그리고 <paramref name="relativeTo"/>가 비어있지 않은 경우)
        /// <paramref name="relativeTo"/>로 시작하지 않으면 <see langword="false"/>를 반환합니다.
        /// </returns>
        public readonly bool TryTrimStartPath(FilePath relativeTo, out FilePath result)
        {
            if (string.IsNullOrEmpty(relativeTo.value))
            {
                result = value;
                return true;
            }

            if (string.IsNullOrEmpty(value) || relativeTo == this)
            {
                result = empty;
                return false;
            }

            FilePath path = value;
            if (path.StartsWith(relativeTo))
            {
                result = path.value.Substring(relativeTo.value.Length + 1);
                return true;
            }

            result = empty;
            return false;
        }



        /// <summary>
        /// 현재 경로가 지정된 <paramref name="startPath"/>로 시작하는지 여부를 확인합니다.<br/>
        /// <paramref name="startPath"/>가 null이거나 비어 있으면 항상 <c>true</c>를 반환합니다.
        /// </summary>
        /// <param name="startPath">현재 경로의 시작 부분과 비교할 경로입니다.</param>
        /// <returns>현재 경로가 <paramref name="startPath"/>로 시작하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public readonly bool StartsWith(FilePath? startPath) => string.IsNullOrEmpty(startPath?.value) || StartsWith(startPath);

        /// <summary>
        /// 현재 경로가 지정된 <paramref name="startPath"/>로 시작하는지 여부를 확인합니다.<br/>
        /// 예를 들어, "dir/file.txt"가 "dir"로 시작하는지 확인합니다.
        /// </summary>
        /// <param name="startPath">현재 경로의 시작 부분과 비교할 경로입니다.</param>
        /// <returns>현재 경로가 <paramref name="startPath"/>로 시작하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public readonly bool StartsWith(FilePath startPath) => value.StartsWith(startPath.value, StringComparison.Ordinal);



        /// <summary>
        /// 현재 경로를 "file:///" 접두사가 붙은 URL 호환 문자열로 변환한 새 <see cref="FilePath"/> 인스턴스를 반환합니다.<br/>
        /// 경로 내의 특수 문자는 <see cref="UnityWebRequest.EscapeURL(string)"/>을 사용하여 URL 인코딩됩니다.
        /// </summary>
        /// <returns>"file:///" 접두사가 붙고 URL 인코딩된 새 <see cref="FilePath"/> 인스턴스.</returns>
        public readonly FilePath UrlPathPrefix() => urlPathPrefix + UnityWebRequest.EscapeURL(value);



        /// <summary>
        /// 현재 경로가 비어있는지 (null이거나 <see cref="string.Empty"/>) 여부를 확인합니다.<br/>
        /// 이 메서드는 <see cref="empty"/>와 동일한 상태를 확인하는 데 사용됩니다.
        /// </summary>
        /// <returns>경로가 비어있으면 <c>true</c>이고, 그렇지 않으면 <c>false</c>입니다.</returns>
        public readonly bool IsEmpty() => string.IsNullOrEmpty(_value);



        /// <summary>
        /// 현재 경로에 지정된 확장자를 덧붙인 새 <see cref="FilePath"/> 인스턴스를 반환합니다.<br/>
        /// 예를 들어, 현재 경로가 "dir/file"이고 <paramref name="ext"/>가 ".txt"인 경우, "dir/file.txt"를 반환합니다.
        /// </summary>
        /// <param name="ext">추가할 확장자입니다. 점(.)을 포함해야 합니다 (예: ".txt").</param>
        /// <returns>확장자가 덧붙여진 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public readonly FilePath AddExtension(FileExtension ext) => new FilePath(value + ext);



        /// <summary>
        /// 두 <see cref="FilePath"/> 객체를 하나의 경로로 결합합니다.<br/>
        /// 두 경로 사이에 표준 디렉터리 구분자(<see cref="directorySeparatorChar"/>)가 자동으로 삽입됩니다.<br/>
        /// 어느 한쪽 또는 양쪽이 빈 경로인 경우, 유효한 경로를 그대로 반환하거나 양쪽 모두 빈 경우 <see cref="empty"/>를 반환합니다.
        /// </summary>
        /// <param name="left">결합할 첫 번째 경로입니다.</param>
        /// <param name="right">결합할 두 번째 경로입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath Combine(FilePath? left, FilePath? right) => Combine(left ?? empty, right ?? empty);

        /// <summary>
        /// 두 <see cref="FilePath"/> 객체를 하나의 경로로 결합합니다.<br/>
        /// 두 경로 사이에 표준 디렉터리 구분자(<see cref="directorySeparatorChar"/>)가 자동으로 삽입됩니다.<br/>
        /// 어느 한쪽 또는 양쪽이 빈 경로인 경우, 유효한 경로를 그대로 반환하거나 양쪽 모두 빈 경우 <see cref="empty"/>를 반환합니다.
        /// </summary>
        /// <param name="left">결합할 첫 번째 경로입니다.</param>
        /// <param name="right">결합할 두 번째 경로입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath Combine(FilePath left, FilePath right)
        {
            if (left.value.Length == 0 && right.value.Length == 0)
                return empty;
            else if (left.value.Length == 0) 
                return right;
            else if (right.value.Length == 0) 
                return left;

            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            stringBuilder.Append(left.value);
            stringBuilder.Append(directorySeparatorChar);
            stringBuilder.Append(right.value);

            return StringBuilderCache.Release(stringBuilder);
        }

        /// <summary>
        /// 여러 <see cref="FilePath"/> 객체를 순서대로 하나의 경로로 결합합니다.<br/>
        /// 각 경로 세그먼트 사이에 표준 디렉터리 구분자(<see cref="directorySeparatorChar"/>)가 자동으로 삽입됩니다.<br/>
        /// 입력 배열이 null이거나 모든 경로가 빈 경우 <see cref="empty"/>를 반환합니다.
        /// </summary>
        /// <param name="paths">결합할 경로들의 배열입니다. null 요소를 포함할 수 있습니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath Combine(params FilePath?[]? paths)
        {
            if (paths == null)
                return empty;

            StringBuilder stringBuilder = StringBuilderCache.Acquire();
            for (int i = 0; i < paths.Length; i++)
            {
                FilePath path = paths[i] ?? empty;
                if (stringBuilder.Length > 0 && stringBuilder[^1] != directorySeparatorChar)
                    stringBuilder.Append(directorySeparatorChar);

                stringBuilder.Append(path.value);
            }

            return StringBuilderCache.Release(stringBuilder);
        }



        /// <summary>
        /// 입력된 경로 문자열을 표준 형식으로 정규화합니다.<br/>
        /// 이 과정에서 다음 변환이 수행됩니다:<br/>
        /// 1. Windows 스타일의 역슬래시(<see cref="windowsDirectorySeparatorChar"/>)를 표준 슬래시(<see cref="directorySeparatorChar"/>)로 변경합니다.<br/>
        /// 2. 경로의 시작과 끝에 있는 불필요한 디렉터리 구분자(<see cref="directorySeparatorChars"/>)를 제거합니다.<br/>
        /// 3. 연속된 디렉터리 구분자(예: "a//b")를 단일 구분자로 축소합니다.
        /// </summary>
        /// <param name="path">정규화할 경로 문자열입니다.</param>
        /// <returns>정규화된 경로 문자열입니다. 입력이 비어있으면 <see cref="string.Empty"/>를 반환합니다.</returns>
        public static string NormalizePath(ReadOnlySpan<char> path)
        {
            if (path.IsEmpty)
                return string.Empty;

            StringBuilder stringBuilder = StringBuilderCache.Acquire();

            ReadOnlySpan<char> trimPath = path.Trim(directorySeparatorChars);
            bool lastCharWasSeparator = false; // 연속된 구분자 처리용 플래그
            foreach (char item in trimPath)
            {
                char result = item;
                if (item == windowsDirectorySeparatorChar)
                    result = directorySeparatorChar;

                // 연속된 구분자 제거 (예: "a//b" -> "a/b")
                if (result == directorySeparatorChar)
                {
                    if (lastCharWasSeparator)
                        continue; // 이전 문자가 이미 구분자였으면 현재 구분자 건너뛰기

                    lastCharWasSeparator = true;
                }
                else
                    lastCharWasSeparator = false;

                stringBuilder.Append(result);
            }

            return StringBuilderCache.Release(stringBuilder);
        }



        public static IEnumerable<FilePath> FilterFiles(IEnumerable<FilePath> files, WildcardPatterns extensionFilter)
        {
            IEnumerable<string> patterns = extensionFilter.patterns.Select(ConvertPatternToRegex);

            // `*` 패턴이 포함되어 있다면 바로 모든 파일 반환
            if (patterns.Contains(".*"))
                return files;

            return files.Where(file => patterns.Any(pattern => Regex.IsMatch(file, pattern, RegexOptions.IgnoreCase))).ToList();

            static string ConvertPatternToRegex(string pattern)
            {
                if (pattern == "*" || pattern == "*.*")
                    return ".*"; // 모든 파일을 허용하는 패턴

                string escaped = Regex.Escape(pattern).Replace(@"\*", ".*"); // '*'를 '.*'로 변환
                return $"^{escaped}$";
            }
        }



        /// <summary>
        /// 현재 <see cref="FilePath"/> 인스턴스의 문자열 표현을 반환합니다.<br/>
        /// 이는 <see cref="value"/> 속성과 동일합니다.
        /// </summary>
        /// <returns>현재 경로의 정규화된 문자열 표현입니다.</returns>
        public override readonly string ToString() => value;



        #region Equals
        /// <summary>
        /// 현재 <see cref="FilePath"/> 인스턴스가 지정된 객체와 동일한지 여부를 확인합니다.<br/>
        /// 비교 대상이 <see cref="FilePath"/> 타입이고 그 <see cref="value"/>가 현재 인스턴스의 <see cref="value"/>와 동일하면 <c>true</c>를 반환합니다.
        /// </summary>
        /// <param name="obj">현재 인스턴스와 비교할 객체입니다.</param>
        /// <returns>지정된 객체가 현재 <see cref="FilePath"/>와 동일하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public override readonly bool Equals(object? obj) => obj is FilePath path && Equals(path);

        /// <summary>
        /// 현재 <see cref="FilePath"/> 인스턴스가 지정된 다른 <see cref="FilePath"/> 인스턴스와 동일한지 여부를 확인합니다.<br/>
        /// 두 경로의 <see cref="value"/> 문자열이 동일하면 <c>true</c>를 반환합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 다른 <see cref="FilePath"/> 인스턴스입니다.</param>
        /// <returns>두 <see cref="FilePath"/> 인스턴스가 동일하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public readonly bool Equals(FilePath other) => value == other.value;

        /// <summary>
        /// 현재 <see cref="FilePath"/> 인스턴스가 지정된 nullable <see cref="FilePath"/> 인스턴스와 동일한지 여부를 확인합니다.<br/>
        /// <paramref name="other"/>가 null이면 빈 경로(<see cref="string.Empty"/>)와 비교합니다.
        /// </summary>
        /// <param name="other">현재 인스턴스와 비교할 nullable <see cref="FilePath"/> 인스턴스입니다.</param>
        /// <returns>두 <see cref="FilePath"/> 인스턴스(또는 null 처리된 빈 경로)가 동일하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public readonly bool Equals(FilePath? other) => value == (other?.value ?? string.Empty);
        #endregion



        /// <summary>
        /// 현재 <see cref="FilePath"/> 인스턴스의 해시 코드를 반환합니다.<br/>
        /// 해시 코드는 <see cref="value"/> 문자열의 해시 코드와 동일합니다.
        /// </summary>
        /// <returns>현재 <see cref="FilePath"/> 인스턴스의 해시 코드입니다.</returns>
        public override readonly int GetHashCode() => value.GetHashCode();



        #region operators

        /// <summary>
        /// <see cref="FilePath"/>를 <see cref="string"/>으로 암시적으로 변환합니다.<br/>
        /// 이는 <see cref="value"/> 속성을 반환합니다.
        /// </summary>
        /// <param name="path">변환할 <see cref="FilePath"/> 인스턴스입니다.</param>
        public static implicit operator string(FilePath path) => path.value;

        /// <summary>
        /// nullable <see cref="FilePath"/>를 <see cref="string"/>으로 암시적으로 변환합니다.<br/>
        /// <paramref name="path"/>가 null이면 <see cref="string.Empty"/>를 반환하고, 그렇지 않으면 <see cref="value"/>를 반환합니다.
        /// </summary>
        /// <param name="path">변환할 nullable <see cref="FilePath"/> 인스턴스입니다.</param>
        public static implicit operator string(FilePath? path) => path?.value ?? string.Empty;

        /// <summary>
        /// <see cref="string"/>을 <see cref="FilePath"/>로 암시적으로 변환합니다.<br/>
        /// 입력된 문자열은 <see cref="NormalizePath"/> 메서드를 통해 정규화됩니다.
        /// </summary>
        /// <param name="path">변환할 문자열 경로입니다.</param>
        public static implicit operator FilePath(string? path) => new FilePath(path);



        #region + operator
        /// <summary>
        /// 두 <see cref="FilePath"/> 객체를 하나의 경로로 결합합니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 첫 번째 경로입니다.</param>
        /// <param name="right">결합할 두 번째 경로입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(FilePath left, FilePath right) => Combine(left, right);

        /// <summary>
        /// <see cref="FilePath"/>와 nullable <see cref="FilePath"/>를 하나의 경로로 결합합니다.<br/>
        /// null <see cref="FilePath"/>는 <see cref="empty"/>로 처리됩니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 첫 번째 경로입니다.</param>
        /// <param name="right">결합할 두 번째 nullable 경로입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(FilePath left, FilePath? right) => Combine(left, right ?? empty);

        /// <summary>
        /// nullable <see cref="FilePath"/>와 <see cref="FilePath"/>를 하나의 경로로 결합합니다.<br/>
        /// null <see cref="FilePath"/>는 <see cref="empty"/>로 처리됩니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 첫 번째 nullable 경로입니다.</param>
        /// <param name="right">결합할 두 번째 경로입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(FilePath? left, FilePath right) => Combine(left ?? empty, right);

        /// <summary>
        /// 두 nullable <see cref="FilePath"/> 객체를 하나의 경로로 결합합니다.<br/>
        /// null <see cref="FilePath"/>는 <see cref="empty"/>로 처리됩니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 첫 번째 nullable 경로입니다.</param>
        /// <param name="right">결합할 두 번째 nullable 경로입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(FilePath? left, FilePath? right) => Combine(left ?? empty, right ?? empty);

        /// <summary>
        /// <see cref="FilePath"/>와 문자열 경로 세그먼트를 하나의 경로로 결합합니다.<br/>
        /// 문자열은 <see cref="FilePath"/>로 암시적으로 변환된 후 결합됩니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 <see cref="FilePath"/>입니다.</param>
        /// <param name="right">결합할 문자열 경로 세그먼트입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(FilePath left, string? right) => Combine(left, right);

        /// <summary>
        /// nullable <see cref="FilePath"/>와 문자열 경로 세그먼트를 하나의 경로로 결합합니다.<br/>
        /// null <see cref="FilePath"/>는 <see cref="empty"/>로 처리됩니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 nullable <see cref="FilePath"/>입니다.</param>
        /// <param name="right">결합할 문자열 경로 세그먼트입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(FilePath? left, string? right) => Combine(left ?? empty, right);

        /// <summary>
        /// 문자열 경로와 <see cref="FilePath"/>를 하나의 경로로 결합합니다.<br/>
        /// 문자열은 <see cref="FilePath"/>로 암시적으로 변환된 후 결합됩니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 문자열 경로입니다.</param>
        /// <param name="right">결합할 <see cref="FilePath"/>입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(string? left, FilePath right) => Combine(left, right);

        /// <summary>
        /// 문자열 경로와 nullable <see cref="FilePath"/>를 하나의 경로로 결합합니다.<br/>
        /// null <see cref="FilePath"/>는 <see cref="empty"/>로 처리됩니다.<br/>
        /// <see cref="Combine(FilePath, FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">결합할 문자열 경로입니다.</param>
        /// <param name="right">결합할 nullable <see cref="FilePath"/>입니다.</param>
        /// <returns>결합된 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator +(string? left, FilePath? right) => Combine(left, right ?? empty);
        #endregion



        #region - operator
        /// <summary>
        /// 두 <see cref="FilePath"/> 간의 상대 경로를 구합니다.<br/>
        /// <paramref name="right"/> 경로가 <paramref name="left"/> 경로의 접두사인 경우, <paramref name="left"/>에서 <paramref name="right"/> 부분을 제거한 나머지 경로를 반환합니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 경로입니다.</param>
        /// <param name="right">제거할 접두사 경로입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(FilePath left, FilePath right) => left.TrimStartPath(right);

        /// <summary>
        /// <see cref="FilePath"/>와 nullable <see cref="FilePath"/> 간의 상대 경로를 구합니다.<br/>
        /// <paramref name="right"/>가 null이면 <see cref="empty"/>로 처리됩니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 경로입니다.</param>
        /// <param name="right">제거할 nullable 접두사 경로입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(FilePath left, FilePath? right) => left.TrimStartPath(right ?? empty);

        /// <summary>
        /// nullable <see cref="FilePath"/>와 <see cref="FilePath"/> 간의 상대 경로를 구합니다.<br/>
        /// <paramref name="left"/>가 null이면 <see cref="empty"/>로 처리됩니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 nullable 경로입니다.</param>
        /// <param name="right">제거할 접두사 경로입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(FilePath? left, FilePath right) => left?.TrimStartPath(right) ?? empty;

        /// <summary>
        /// 두 nullable <see cref="FilePath"/> 간의 상대 경로를 구합니다.<br/>
        /// null 값은 <see cref="empty"/>로 처리됩니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 nullable 경로입니다.</param>
        /// <param name="right">제거할 nullable 접두사 경로입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(FilePath? left, FilePath? right) => left?.TrimStartPath(right ?? empty) ?? empty;

        /// <summary>
        /// <see cref="FilePath"/>와 문자열 접두사 간의 상대 경로를 구합니다.<br/>
        /// 문자열은 <see cref="FilePath"/>로 암시적으로 변환된 후 상대 경로가 계산됩니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 <see cref="FilePath"/>입니다.</param>
        /// <param name="right">제거할 문자열 접두사입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(FilePath left, string? right) => left.TrimStartPath(right);

        /// <summary>
        /// nullable <see cref="FilePath"/>와 문자열 접두사 간의 상대 경로를 구합니다.<br/>
        /// null <see cref="FilePath"/>는 <see cref="empty"/>로 처리됩니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 nullable <see cref="FilePath"/>입니다.</param>
        /// <param name="right">제거할 문자열 접두사입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(FilePath? left, string? right) => left?.TrimStartPath(right ?? empty) ?? empty;

        /// <summary>
        /// 문자열 경로와 <see cref="FilePath"/> 접두사 간의 상대 경로를 구합니다.<br/>
        /// 문자열은 <see cref="FilePath"/>로 암시적으로 변환된 후 상대 경로가 계산됩니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 문자열 경로입니다.</param>
        /// <param name="right">제거할 <see cref="FilePath"/> 접두사입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(string? left, FilePath right) => left.ToPath().TrimStartPath(right);

        /// <summary>
        /// 문자열 경로와 nullable <see cref="FilePath"/> 접두사 간의 상대 경로를 구합니다.<br/>
        /// null <see cref="FilePath"/>는 <see cref="empty"/>로 처리됩니다.<br/>
        /// 이는 <see cref="TrimStartPath(FilePath)"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="left">기준이 되는 문자열 경로입니다.</param>
        /// <param name="right">제거할 nullable <see cref="FilePath"/> 접두사입니다.</param>
        /// <returns>상대 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator -(string? left, FilePath? right) => left.ToPath().TrimStartPath(right ?? empty);
        #endregion



        #region -- operator
        /// <summary>
        /// 현재 경로의 상위 디렉터리 경로를 구합니다.<br/>
        /// 이는 <see cref="GetParentPath()"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="path">상위 디렉터리를 구할 경로입니다.</param>
        /// <returns>상위 디렉터리 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스입니다.</returns>
        public static FilePath operator --(FilePath path) => path.GetParentPath();

        /// <summary>
        /// nullable <see cref="FilePath"/>의 상위 디렉터리 경로를 구합니다.<br/>
        /// <paramref name="path"/>가 null이면 <see cref="empty"/>를 반환합니다.<br/>
        /// 이는 <see cref="GetParentPath()"/> 메서드와 동일한 기능을 수행합니다.
        /// </summary>
        /// <param name="path">상위 디렉터리를 구할 nullable 경로입니다.</param>
        /// <returns>상위 디렉터리 경로를 나타내는 새 <see cref="FilePath"/> 인스턴스 또는 <see cref="empty"/>.</returns>
        public static FilePath? operator --(FilePath? path) => path?.GetParentPath() ?? empty;
        #endregion



        #region == operator
        /// <summary>
        /// 두 <see cref="FilePath"/> 객체가 동일한지 여부를 확인합니다.<br/>
        /// 이는 <see cref="Equals(FilePath)"/> 메서드와 동일합니다.
        /// </summary>
        /// <param name="left">비교할 첫 번째 <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 두 번째 <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator ==(FilePath left, FilePath right) => left.Equals(right);

        /// <summary>
        /// <see cref="FilePath"/>와 nullable <see cref="FilePath"/>가 동일한지 여부를 확인합니다.<br/>
        /// 이는 <see cref="Equals(FilePath?)"/> 메서드와 동일합니다.
        /// </summary>
        /// <param name="left">비교할 <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 nullable <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator ==(FilePath left, FilePath? right) => left.Equals(right);

        /// <summary>
        /// nullable <see cref="FilePath"/>와 <see cref="FilePath"/>가 동일한지 여부를 확인합니다.<br/>
        /// null <paramref name="left"/>는 <see cref="empty"/>로 처리됩니다.
        /// </summary>
        /// <param name="left">비교할 nullable <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator ==(FilePath? left, FilePath right) => (left ?? empty).Equals(right);

        /// <summary>
        /// 두 nullable <see cref="FilePath"/> 객체가 동일한지 여부를 확인합니다.<br/>
        /// null 값은 <see cref="empty"/>로 처리됩니다.
        /// </summary>
        /// <param name="left">비교할 첫 번째 nullable <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 두 번째 nullable <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator ==(FilePath? left, FilePath? right) => left.Equals(right);
        #endregion

        #region != operator
        /// <summary>
        /// 두 <see cref="FilePath"/> 객체가 동일하지 않은지 여부를 확인합니다.<br/>
        /// 이는 <c>operator ==</c>의 반대입니다.
        /// </summary>
        /// <param name="left">비교할 첫 번째 <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 두 번째 <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하지 않으면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator !=(FilePath left, FilePath right) => !(left == right);

        /// <summary>
        /// <see cref="FilePath"/>와 nullable <see cref="FilePath"/>가 동일하지 않은지 여부를 확인합니다.<br/>
        /// 이는 <c>operator ==</c>의 반대입니다.
        /// </summary>
        /// <param name="left">비교할 <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 nullable <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하지 않으면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator !=(FilePath left, FilePath? right) => !(left == right);

        /// <summary>
        /// nullable <see cref="FilePath"/>와 <see cref="FilePath"/>가 동일하지 않은지 여부를 확인합니다.<br/>
        /// 이는 <c>operator ==</c>의 반대입니다.
        /// </summary>
        /// <param name="left">비교할 nullable <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하지 않으면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator !=(FilePath? left, FilePath right) => !(left == right);

        /// <summary>
        /// 두 nullable <see cref="FilePath"/> 객체가 동일하지 않은지 여부를 확인합니다.<br/>
        /// 이는 <c>operator ==</c>의 반대입니다.
        /// </summary>
        /// <param name="left">비교할 첫 번째 nullable <see cref="FilePath"/>입니다.</param>
        /// <param name="right">비교할 두 번째 nullable <see cref="FilePath"/>입니다.</param>
        /// <returns>두 경로가 동일하지 않으면 <c>true</c>, 그렇지 않으면 <c>false</c>입니다.</returns>
        public static bool operator !=(FilePath? left, FilePath? right) => !(left == right);
        #endregion
        #endregion



        public void OnBeforeSerialize() => value = value;
        public void OnAfterDeserialize() => value = value;
    }
}
