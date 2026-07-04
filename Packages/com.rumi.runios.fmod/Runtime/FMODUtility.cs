#nullable enable
using FMOD;
using System.Runtime.CompilerServices;

namespace RuniOS
{
    public static class FMODUtility
    {
        public static void ThrowIfNotOk(this RESULT result, [CallerArgumentExpression("result")] string location = "")
        {
            if (result != RESULT.OK)
                throw new InvalidOperationException($"An error occurred while executing the {location} method : {result} : {Error.String(result)}");
        }

        // ReSharper disable Unity.PerformanceAnalysis
        public static RESULT LogErrorIfNotOk(this RESULT result, [CallerArgumentExpression("result")] string location = "")
        {
            if (result != RESULT.OK)
                Debug.RuntimeLogError($"An error occurred while executing the {location} method : {result} : {Error.String(result)}");

            return result;
        }

        // ReSharper disable once InconsistentNaming
        public static RESULT set3DAttributes(this Channel channel, ATTRIBUTES_3D attributes3D) => channel.set3DAttributes(ref attributes3D.position, ref attributes3D.velocity);
    }
}