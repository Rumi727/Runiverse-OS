#nullable enable
using System;

namespace RuniEngine
{
    public static class EventUtility
    {
        /// <summary>예외를 핸들링하여 이벤트 호출이 중지되지 않도록 합니다.</summary>
        public static void EventInvoke(Delegate? e)
        {
            if (e == null)
                return;

            Delegate[] delegates = e.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                try
                {
                    e.DynamicInvoke(delegates[i]);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }

        /// <summary>예외를 핸들링하여 이벤트 호출이 중지되지 않도록 합니다.</summary>
        public static void EventInvoke(Delegate? e, params object[] args)
        {
            if (e == null)
                return;

            Delegate[] delegates = e.GetInvocationList();
            for (int i = 0; i < delegates.Length; i++)
            {
                try
                {
                    e.DynamicInvoke(delegates[i], args);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }
        }
    }
}
