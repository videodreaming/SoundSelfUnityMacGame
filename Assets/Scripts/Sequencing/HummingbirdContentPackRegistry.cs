using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>
    /// One row in <see cref="HummingbirdContentPackRegistry"/>: canonical Hummingbird strings from <c>CSVLoader</c> constants
    /// plus the pack definition asset. Keys live here (strategy B), not duplicated on the pack SO.
    /// <para><b>Field order:</b> <see cref="contentPack"/> is declared before <see cref="gameMode"/> so the default Unity
    /// Inspector list uses the content-pack name in each row header (game mode repeats across rows).</para>
    /// </summary>
    [Serializable]
    public struct HummingbirdContentPackRegistryEntry
    {
        [Tooltip("Canonical content pack string for this mode, e.g. CSVLoader.ContentPackMindfulnessAndJoy")]
        public string contentPack;

        [Tooltip("Canonical game mode string, e.g. CSVLoader.GameModeSonoflore")]
        public string gameMode;

        public HummingbirdContentPackDefinition definition;
    }

    /// <summary>
    /// ScriptableObject registry: (gameMode, contentPack) → <see cref="HummingbirdContentPackDefinition"/>.
    /// Create via Assets → Create → SoundSelf → Hummingbird → Hummingbird Content Pack Registry; assign on <c>CSVLoader</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "HummingbirdContentPackRegistry", menuName = "SoundSelf/Hummingbird/Hummingbird Content Pack Registry")]
    public class HummingbirdContentPackRegistry : ScriptableObject
    {
        [Tooltip("Every supported (gameMode, contentPack) pair and its pack SO. Phase 2 resolves sessions through this list.")]
        [SerializeField] private List<HummingbirdContentPackRegistryEntry> entries = new List<HummingbirdContentPackRegistryEntry>();

        public IReadOnlyList<HummingbirdContentPackRegistryEntry> Entries => entries;

        /// <summary>
        /// Lookup after CSV normalization. Trims keys so inspector/registry whitespace matches <c>TryGetCsvKeysForPackDefinition</c> and <c>OnValidate</c>.
        /// </summary>
        public bool TryGetDefinition(string normalizedGameMode, string normalizedContentPack, out HummingbirdContentPackDefinition definition)
        {
            definition = null;
            string gm = TrimRegistryKey(normalizedGameMode);
            string cp = TrimRegistryKey(normalizedContentPack);
            if (string.IsNullOrEmpty(gm) || string.IsNullOrEmpty(cp))
                return false;

            if (entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.definition == null)
                    continue;
                string egm = TrimRegistryKey(e.gameMode);
                string ecp = TrimRegistryKey(e.contentPack);
                if (string.Equals(egm, gm, StringComparison.Ordinal) && string.Equals(ecp, cp, StringComparison.Ordinal))
                {
                    definition = e.definition;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// For editor overrides: find the canonical <c>(gameMode, contentPack)</c> strings for a registry-listed pack asset.
        /// </summary>
        public bool TryGetCsvKeysForPackDefinition(HummingbirdContentPackDefinition packDefinition, out string gameMode, out string contentPack)
        {
            gameMode = null;
            contentPack = null;
            if (packDefinition == null || entries == null)
                return false;

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.definition != packDefinition)
                    continue;
                gameMode = TrimRegistryKey(e.gameMode);
                contentPack = TrimRegistryKey(e.contentPack);
                return !string.IsNullOrEmpty(gameMode) && !string.IsNullOrEmpty(contentPack);
            }

            return false;
        }

        private static string TrimRegistryKey(string value)
        {
            return string.IsNullOrEmpty(value) ? string.Empty : value.Trim();
        }

        private void OnValidate()
        {
            if (entries == null || entries.Count == 0)
                return;

            var seenPairKeys = new HashSet<string>(StringComparer.Ordinal);
            var seenDefinitions = new HashSet<HummingbirdContentPackDefinition>();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                string gm = e.gameMode != null ? e.gameMode.Trim() : string.Empty;
                string cp = e.contentPack != null ? e.contentPack.Trim() : string.Empty;

                if (string.IsNullOrEmpty(gm) && string.IsNullOrEmpty(cp) && e.definition == null)
                    continue;

                if (e.definition == null)
                {
                    Debug.LogWarning($"{name}: Registry row {i} has no HummingbirdContentPackDefinition assigned.", this);
                    continue;
                }

                if (string.IsNullOrEmpty(gm) || string.IsNullOrEmpty(cp))
                {
                    Debug.LogError($"{name}: Registry row {i} must set both gameMode and contentPack when a definition is assigned.", this);
                    continue;
                }

                if (!seenDefinitions.Add(e.definition))
                    Debug.LogWarning(
                        $"{name}: Multiple rows reference the same pack definition asset '{e.definition.name}'. Row index {i}. Usually one row per pack SO; TryGetCsvKeysForPackDefinition uses the first matching row.",
                        this);

                string key = gm + "\u001f" + cp;
                if (!seenPairKeys.Add(key))
                    Debug.LogError($"{name}: Duplicate registry key (gameMode, contentPack) = ({gm}, {cp}). Row index {i}.", this);

                if (!SessionGameModeMapping.Matches(e.definition.GameMode, gm))
                    Debug.LogError(
                        $"{name}: Row {i} gameMode string \"{gm}\" does not match definition asset '{e.definition.name}' GameMode enum ({e.definition.GameMode} → \"{SessionGameModeMapping.ToCsvGameModeString(e.definition.GameMode)}\").",
                        this);
            }
        }
    }
}
