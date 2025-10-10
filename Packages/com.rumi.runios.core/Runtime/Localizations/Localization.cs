#nullable enable
using RuniOS.Resource;
using RuniOS.Resource.Languages;
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace RuniOS.Localizations
{
    [Serializable]
    public struct Localization : IEquatable<Localization>
    {
        public Localization(Identifier identifier, string? languageCode = null)
        {
            _identifier = identifier;
            _languageCode = languageCode;
        }

        public static Localization empty => new Localization();
        
        [SerializeField] Identifier _identifier;
        public Identifier identifier
        {
            get => _identifier;
            set => _identifier = value;
        }

        [SerializeField] string? _languageCode;
        public string? languageCode
        {
            get => string.IsNullOrEmpty(_languageCode) ? null : _languageCode;
            set => _languageCode = value;
        }
        
        public bool Equals(Localization other) => identifier.Equals(other.identifier);
        
        public override bool Equals(object? obj) => obj is Localization other && Equals(other);
        public override int GetHashCode() => identifier.GetHashCode();

        public override string ToString() => GetTextOrKey(identifier, languageCode);
        public string ToFormat(params object[] args) => string.Format(CultureInfo.InvariantCulture, ToString(), args);
        
        public static string GetTextOrKey(Identifier identifier, string? languageCode = null) => GetText(identifier, languageCode) ?? identifier;
        public static string? GetText(Identifier identifier, string? languageCode = null)
        {
            LanguageAssetRegistry? registry = ResourceManager.GetRegistry<LanguageAssetRegistry>();
            return registry?.preloadedAsset.GetValueOrDefault(languageCode ?? string.Empty /* TODO : 이거 바꿔라 */)?.GetValueOrDefault(identifier);
        }

        public static bool operator ==(Localization left, Localization right) => left.Equals(right);
        public static bool operator !=(Localization left, Localization right) => !left.Equals(right);

        public static implicit operator Identifier(Localization value) => value.identifier;
        public static implicit operator Localization(Identifier value) => new Localization(value);
        public static implicit operator Localization(string value) => new Localization(value);
    }
}