using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RuniOS
{
    [Serializable]
    public struct ReplacePair : IEquatable<ReplacePair>
    {
        public ReplacePair(string oldText, string newText)
        {
            _oldText = oldText;
            _newText = newText;
        }
        
        [SerializeField, JsonIgnore, FieldName("gui.replace.old")] string? _oldText;
        [SerializeField, JsonIgnore, FieldName("gui.replace.new")] string? _newText;
        
        public string oldText
        {
            get => _oldText ?? string.Empty;
            set => _oldText = value;
        }
        
        public string newText
        {
            get => _newText ?? string.Empty;
            set => _newText = value;
        }
        
        public bool Equals(ReplacePair other) => oldText == other.oldText && newText == other.newText;
        
        public override bool Equals(object? obj) => obj is ReplacePair other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(oldText, newText);
        
        public static bool operator ==(ReplacePair left, ReplacePair right) => left.Equals(right);
        public static bool operator !=(ReplacePair left, ReplacePair right) => !left.Equals(right);
    }
}