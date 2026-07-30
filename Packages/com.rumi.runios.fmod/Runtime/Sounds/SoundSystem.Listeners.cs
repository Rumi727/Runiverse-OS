using FMOD;
using FMODUnity;

namespace RuniOS.Sounds
{
    public sealed partial class SoundSystem
    {
        public Listeners listeners { get; }

        public sealed class Listeners
        {
            internal Listeners(SoundSystem system) => this.system = system;

            readonly SoundSystem system;

            public AudioSpatialState this[int index]
            {
                get => system.UseNative(native =>
                {
                    native.get3DListenerAttributes(index, out VECTOR pos, out VECTOR vel, out VECTOR forward, out VECTOR up).ThrowIfNotOk();
                    return new AudioSpatialState(pos.ToUnityVector(), vel.ToUnityVector(), forward.ToUnityVector(), up.ToUnityVector());
                });
                set => system.UseNative((native, value) =>
                {
                    VECTOR pos = value.position.ToFMODVector();
                    VECTOR vel = value.velocity.ToFMODVector();
                    VECTOR forward = (value.rotation * Vector3.forward).ToFMODVector();
                    VECTOR up = (value.rotation * Vector3.up).ToFMODVector();

                    native.set3DListenerAttributes(index, ref pos, ref vel, ref forward, ref up).ThrowIfNotOk();
                }, value);
            }

            public int count
            {
                get => system.UseNative(native =>
                {
                    native.get3DNumListeners(out int result).ThrowIfNotOk();
                    return result;
                });
                set => system.UseNative((native, value) => native.set3DNumListeners(value).ThrowIfNotOk(), value);
            }
        }
    }
}