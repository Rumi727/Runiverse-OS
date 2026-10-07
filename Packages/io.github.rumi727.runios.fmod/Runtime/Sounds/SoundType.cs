#nullable enable
using FMOD;

namespace RuniOS.Sounds
{
    /// <summary>
    /// Specifies the container or codec type of a sound.<br/>
    /// 사운드의 컨테이너 또는 코덱 형식을 지정합니다.
    /// </summary>
    public enum SoundType
    {
        Unknown = SOUND_TYPE.UNKNOWN,
        AIFF = SOUND_TYPE.AIFF,
        ASF = SOUND_TYPE.ASF,
        DLS = SOUND_TYPE.DLS,
        FLAC = SOUND_TYPE.FLAC,
        FSB = SOUND_TYPE.FSB,
        IT = SOUND_TYPE.IT,
        MIDI = SOUND_TYPE.MIDI,
        MOD = SOUND_TYPE.MOD,
        MPEG = SOUND_TYPE.MPEG,
        OggVorbis = SOUND_TYPE.OGGVORBIS,
        Playlist = SOUND_TYPE.PLAYLIST,
        Raw = SOUND_TYPE.RAW,
        S3M = SOUND_TYPE.S3M,
        User = SOUND_TYPE.USER,
        WAV = SOUND_TYPE.WAV,
        XM = SOUND_TYPE.XM,
        XMA = SOUND_TYPE.XMA,
        AudioQueue = SOUND_TYPE.AUDIOQUEUE,
        AT9 = SOUND_TYPE.AT9,
        Vorbis = SOUND_TYPE.VORBIS,
        MediaFoundation = SOUND_TYPE.MEDIA_FOUNDATION,
        MediaCodec = SOUND_TYPE.MEDIACODEC,
        FADPCM = SOUND_TYPE.FADPCM,
        Opus = SOUND_TYPE.OPUS
    }
}
