#nullable enable
namespace RuniOS.Utility
{
    public static class ProgressUtility
    {
        /// <summary>예외를 핸들링하여 리포트 호출에서 예외가 던져져도 계속 진행하게 합니다.</summary>
        public static void SafeReport<T>(this IProgress<T>? progress, T value)
        {
            if (progress == null)
                return;

            try
            {
                progress.Report(value);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}