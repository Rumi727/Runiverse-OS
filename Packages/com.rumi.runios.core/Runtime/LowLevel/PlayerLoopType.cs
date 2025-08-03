namespace RuniOS.LowLevel
{
    public enum PlayerLoopType
    {
        Initialization,
        EarlyUpdate,
        FixedUpdate,
        PreUpdate,
        Update,
        PreLateUpdate,
        PostLateUpdate,
#if UNITY_2020_2_OR_NEWER
        TimeUpdate
#endif
    }
}