#nullable enable
using RuniOS.IO;
using RuniOS.Sounds;
using System.Runtime.CompilerServices;

namespace RuniOS.Editor.Sounds
{
    public sealed partial class AudioPreview
    {
        static readonly ConditionalWeakTable<Player, object> globalPlayers = new ConditionalWeakTable<Player, object>();

        public IReadOnlyDictionary<IONode, Player> players => _players;
        readonly Dictionary<IONode, Player> _players = [];

        public static float globalVolume
        {
            get;
            set
            {
                field = value;
                foreach (var player in globalPlayers)
                    player.Key.UpdateVolumeAndLoop();
            }
        } = 0.5f;

        public static bool globalLoop
        {
            get;
            set
            {
                field = value;
                foreach (var player in globalPlayers)
                    player.Key.UpdateVolumeAndLoop();
            }
        } = false;

        public static bool autoPlay { get; set; }

        public Player GetOrCreatePlayer(IONode node)
        {
            ObjectDisposedException.ThrowIf(isDisposed, this);

            if (!players.TryGetValue(node, out var player))
                player = new Player(this, node);

            return player;
        }

        public sealed class Player : IPlayControl, IStoppable, ISeekable
        {
            internal Player(AudioPreview preview, IONode node)
            {
                this.preview = preview;
                this.node = node;

                WaveAudioClip? clip = preview.GetAudio(node);
                preview._players.Add(node, this);

                globalPlayers.Add(this, node);

                if (!autoPlay)
                    return;

                if (clip != null)
                    Play();
                else
                {
                    onLoadedAudio += OnLoadedAudio;

                    void OnLoadedAudio(IONode node)
                    {
                        if (this.node != node || isPlaying)
                            return;

                        Play();
                        onLoadedAudio -= OnLoadedAudio;
                    }
                }
            }

            readonly AudioPreview preview;
            readonly IONode node;

            SoundChannel? channel;

            public WaveAudioClip? clip
            {
                get
                {
                    if (preview.isDisposed)
                        return null;

                    return preview.GetAudio(node);
                }
            }

            public bool isPlaying => channel != null;

            // FMOD는 재생 위치를 DSP 버퍼 단위(예: 1024샘플, 48kHz에서 약 21ms)로만 갱신하므로,
            // 그 사이에는 위치가 바뀐 것을 처음 본 시각부터 흐른 실제 시간을 더해 미리보기가 계단식으로 움직이지 않게 합니다.
            double lastChannelTime = double.NaN;
            double lastChannelTimeChangedAt;

            public double time
            {
                get
                {
                    try
                    {
                        if (channel == null)
                            return 0;

                        double channelTime = channel.time;
                        double now = EditorApplication.timeSinceStartup;
                        if (!channelTime.Approximately(lastChannelTime))
                        {
                            lastChannelTime = channelTime;
                            lastChannelTimeChangedAt = now;
                        }

                        return Math.Min(channelTime + (now - lastChannelTimeChangedAt), length);
                    }
                    catch (ObjectDisposedException)
                    {
                        return 0;
                    }
                }
                set
                {
                    // 탐색한 위치가 마지막으로 본 위치와 같아도 이전 기준 시각이 이어지지 않도록 기준을 버립니다.
                    lastChannelTime = double.NaN;

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

                    lastChannelTime = double.NaN;

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
                channel.onStop -= OnStop;

                EditorThreadDispatcher.Execute(() =>
                {
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