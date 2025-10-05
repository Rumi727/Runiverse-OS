#nullable enable
using System;

namespace RuniOS
{
    public static class EventUtility
    {
        /// <summary>예외를 핸들링하여 이벤트 호출이 중지되지 않도록 합니다.</summary>
        public static void SafeInvoke(this Delegate? e)
        {
            if (e == null)
                return;

            Delegate[] delegates = e.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                try
                {
                    delegates[i].DynamicInvoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>예외를 핸들링하여 이벤트 호출이 중지되지 않도록 합니다.</summary>
        public static void SafeInvoke(this Delegate? e, params object[] args)
        {
            if (e == null)
                return;

            Delegate[] delegates = e.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                try
                {
                    delegates[i].DynamicInvoke(e, args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
