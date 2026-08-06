namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public static void LogUndisposedResource(ISoundSystemResource resource)
        {
            try
            {
                Debug.RuntimeLogError(
                    $"The FMOD resource '{resource.GetType().FullName}' was removed from managed memory without a Dispose call!\n" +
                    "The actual native FMOD resource was not released to protect the sound system and its remaining resources.\n" +
                    "Before discarding an FMOD resource, please call the Dispose method of that instance.");
            }
            catch
            {
                // Finalizers must never propagate exceptions.
            }
        }
    }
}