#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Represents an FMOD operation failure.<br/>
    /// FMOD 작업 실패를 나타냅니다.
    /// </summary>
    public sealed class FMODException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FMODException"/> class.<br/>
        /// <see cref="FMODException"/> 클래스의 새 인스턴스를 초기화합니다.
        /// </summary>
        /// <param name="result">
        /// The result returned by FMOD.<br/>
        /// FMOD가 반환한 결과입니다.
        /// </param>
        /// <param name="location">
        /// The expression for the FMOD operation that returned <paramref name="result"/>.<br/>
        /// <paramref name="result"/>를 반환한 FMOD 작업의 식입니다.
        /// </param>
        public FMODException(RESULT result, string location) : base($"An error occurred while executing the {location} method : {result} : {Error.String(result)}") =>
            this.result = result;

        /// <summary>
        /// Gets the result returned by FMOD.<br/>
        /// FMOD가 반환한 결과를 가져옵니다.
        /// </summary>
        public RESULT result { get; }
    }
}