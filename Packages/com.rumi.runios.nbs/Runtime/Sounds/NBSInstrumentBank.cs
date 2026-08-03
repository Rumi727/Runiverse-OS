#nullable enable
using Cysharp.Threading.Tasks;
using RuniOS.NBS;
using RuniOS.Resource;

namespace RuniOS.Sounds
{
    public sealed class NBSInstrumentBank : IDisposable
    {
        static readonly string[] vanillaInstrumentPaths =
        [
            "block.note_block.harp",
            "block.note_block.bass",
            "block.note_block.bassdrum",
            "block.note_block.snare",
            "block.note_block.hat",
            "block.note_block.guitar",
            "block.note_block.flute",
            "block.note_block.bell",
            "block.note_block.chime",
            "block.note_block.xylophone",
            "block.note_block.iron_xylophone",
            "block.note_block.cow_bell",
            "block.note_block.didgeridoo",
            "block.note_block.bit",
            "block.note_block.banjo",
            "block.note_block.pling",
            "block.note_block.trumpet",
            "block.note_block.trumpet_exposed",
            "block.note_block.trumpet_weathered",
            "block.note_block.trumpet_oxidized"
        ];

        readonly IAssetScope<WaveAudioClip>?[] scopes;
        readonly InstrumentBinding?[] instruments;
        bool isDisposed;

        NBSInstrumentBank(int bankLength)
        {
            scopes = new IAssetScope<WaveAudioClip>?[bankLength];
            instruments = new InstrumentBinding?[bankLength];
        }

        public InstrumentBinding? this[byte instrument] => instrument < instruments.Length ? instruments[instrument] : null;

        public static async UniTask<NBSInstrumentBank> Create(NBSFile file, Identifier nbsAssetId)
        {
            int vanillaInstrumentCount = file.header.vanillaInstrumentCount;
            NBSInstrumentBank bank = new NBSInstrumentBank(vanillaInstrumentCount + file.customInstruments.Count);

            try
            {
                for (int instrument = 0; instrument < bank.instruments.Length; instrument++)
                {
                    ResourceKey key;
                    int keyOffset;
                    if (instrument < vanillaInstrumentCount)
                    {
                        if (instrument >= vanillaInstrumentPaths.Length)
                            continue;

                        key = new ResourceKey
                        (
                            new Identifier("runios", "waves"),
                            new Identifier("runios", vanillaInstrumentPaths[instrument])
                        );
                        keyOffset = 0;
                    }
                    else
                    {
                        NBSCustomInstrument custom = file.customInstruments[instrument - vanillaInstrumentCount];
                        if (custom.IsFunctionalInstrument() || !TryNormalizeCustomPath(custom.soundFile, out string path))
                            continue;

                        key = new ResourceKey
                        (
                            new Identifier("runios", "waves"),
                            new Identifier(nbsAssetId.nameSpace, path)
                        );
                        keyOffset = custom.key - 45;
                    }

                    IAssetScope<WaveAudioClip>? scope = await ResourceManager.LoadScopeAsync<WaveAudioClip>(key);
                    if (scope == null)
                    {
                        Debug.RuntimeLogWarning($"NBS instrument resource '{key.assetId}' is unavailable. Notes using it will be silent.", nameof(NBSPlayer));
                        continue;
                    }

                    bank.scopes[instrument] = scope;
                    bank.instruments[instrument] = new InstrumentBinding(scope.asset, keyOffset);
                }
            }
            catch
            {
                DisposeQueue.Enqueue(bank);
                throw;
            }

            return bank;
        }

        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;
            for (int i = 0; i < scopes.Length; i++)
                scopes[i]?.Dispose();
        }

        static bool TryNormalizeCustomPath(string soundFile, out string path)
        {
            path = soundFile.Trim().Replace('\\', '/');
            if (path.StartsWith('/') || path.Contains(':'))
                return false;

            if (path.StartsWith("sounds/", StringComparison.OrdinalIgnoreCase))
                path = path.Substring("sounds/".Length);

            string[] parts = path.Split('/');
            if (parts.Length == 0 || parts.Any(x => string.IsNullOrWhiteSpace(x) || x == "." || x == ".."))
                return false;

            int lastSlash = path.LastIndexOf('/');
            int lastDot = path.LastIndexOf('.');
            if (lastDot > lastSlash)
                path = path.Substring(0, lastDot);

            return path.Length > 0;
        }

        public readonly record struct InstrumentBinding(WaveAudioClip clip, int keyOffset);
    }
}
