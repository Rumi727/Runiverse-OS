namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public static void LogUndisposedResource(ISoundSystemResource resource, string? name = null, string? constructorStackTrace = null)
        {
            try
            {
                name = string.IsNullOrWhiteSpace(name) ? resource.GetType().FullName : $"{name} ({resource.GetType().FullName})";

                string logText = $"The FMOD resource '{name}' was removed from managed memory without a Dispose call!\n" +
                    "The actual native FMOD resource was not released to protect the sound system and its remaining resources.\n" +
                    "Before discarding an FMOD resource, please call the Dispose method of that instance.";

                if (!string.IsNullOrEmpty(constructorStackTrace))
                    logText += $"\nThe constructor's stack trace is as follows: {constructorStackTrace}";

                Debug.RuntimeLogError(logText);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
    }
}