#nullable enable
using FMOD;
using System.Runtime.CompilerServices;

namespace RuniOS.Sounds
{
    public static class SoundUtility
    {
        public static void ThrowIfNotOk(this RESULT result, [CallerArgumentExpression("result")] string location = "")
        {
            if (result != RESULT.OK)
                throw new FMODException(result, location);
        }

        internal static void ThrowIfNotOk
        (
            this RESULT result,
            SoundChannel channel,
            [CallerArgumentExpression("result")] string location = ""
        )
        {
            switch (result)
            {
                case RESULT.ERR_INVALID_HANDLE:
                case RESULT.ERR_CHANNEL_STOLEN:
                    channel.HandleInvalidHandle();
                    return;
                case RESULT.ERR_INVALID_PARAM when channel.isDisposed:
                    return;
                default:
                    result.ThrowIfNotOk(location);
                    break;
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static RESULT LogErrorIfNotOk(this RESULT result, [CallerArgumentExpression("result")] string location = "")
        {
            if (result != RESULT.OK)
                Debug.RuntimeLogError($"An error occurred while executing the {location} method : {result} : {Error.String(result)}", Debug.NameOfCallingClass());

            return result;
        }

        public static Vector3 ToUnityVector(this VECTOR vector) => new Vector3(vector.x, vector.y, vector.z);

        public static AudioSpatialState ToAudioSpatial(this Transform transform, Vector3 velocity) => new AudioSpatialState(transform.position, velocity, transform.rotation);
    }
}
