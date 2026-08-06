#nullable enable
using RuniOS.IO;
using RuniOS.Sounds;
using RuniOS.Threading;
using System.Runtime.CompilerServices;

namespace RuniOS.Editor.Sounds
{
    public sealed partial class AudioPreview
    {
        static readonly ConditionalWeakTable<Player, object> globalPlayers = new ConditionalWeakTable<Player, object>();

        public IReadOnlyDictionary<PhysicalPath, Player> players => _players;
        readonly Dictionary<PhysicalPath, Player> _players = [];

        public static float globalVolume
        {
            get => _globalVolume;
            set
            {
                _globalVolume = value;
                foreach (var player in globalPlayers)
                    player.Key.UpdateVolumeAndLoop();
            }
        }
        static float _globalVolume = 0.5f;

        public static bool globalLoop
        {
            get => _globalLoop;
            set
            {
                _globalLoop = value;
                foreach (var player in globalPlayers)
                    player.Key.UpdateVolumeAndLoop();
            }
        }
        static bool _globalLoop = false;

        public Player GetOrCreatePlayer(PhysicalPath path)
        {
            if (!players.TryGetValue(path, out var player))
                player = new Player(this, path);

            return player;
        }

        public sealed class Player : IPlayControl, IStoppable, ISeekable
        {
            internal Player(AudioPreview preview, PhysicalPath path)
            {
                this.preview = preview;
                this.path = path;

                preview.GetAudio(path);
                preview._players.Add(path, this);

                globalPlayers.Add(this, path);
            }

            readonly AudioPreview preview;
            readonly PhysicalPath path;

            SoundChannel? channel;

            public WaveAudioClip? clip => preview.GetAudio(path);

            public bool isPlaying => channel != null;

            public double time
            {
                get
                {
                    try
                    {
                        return channel?.time ?? 0;
                    }
                    catch (ObjectDisposedException)
                    {
                        return 0;
                    }
                }
                set
                {
                    try
                    {
                        if (channel != null)
                            channel.time = value;
                        else
                            Play(value);
                    }
                    catch (ObjectDisposedException)
                    {
                        Stop();
                        Play(value);
                    }
                }
            }

            public double length => clip?.length ?? 0;

            public float volume
            {
                get => globalVolume;
                set
                {
                    globalVolume = value;
                    UpdateVolumeAndLoop();
                }
            }

            public bool loop
            {
                get => globalLoop;
                set
                {
                    globalLoop = value;
                    UpdateVolumeAndLoop();
                }
            }

            public void Play(double startTime = 0)
            {
                if (clip == null)
                    return;

                if (channel == null)
                {
                    if (!SoundSystem.main.Execute(system => system.PlaySound(clip, true), out channel) || channel == null)
                        return;

                    channel.volume = volume;
                    channel.loop = loop;
                    channel.time = startTime;

                    channel.onStop += OnStop;
                    channel.isPaused = false;
                }
                else
                    time = startTime;
            }

            public void Stop()
            {
                if (channel != null)
                {
                    channel.onStop -= OnStop;
                    channel.Stop();
                }

                channel = null;
            }

            void OnStop(SoundChannel channel)
            {
                ThreadDispatcher.Execute(() =>
                {
                    channel.onStop -= OnStop;

                    if (channel == this.channel)
                        this.channel = null;
                });
            }

            internal void UpdateVolumeAndLoop()
            {
                try
                {
                    if (channel != null)
                    {
                        channel.volume = volume;
                        channel.loop = loop;
                    }
                }
                catch (ObjectDisposedException) { }
            }
        }
    }
}