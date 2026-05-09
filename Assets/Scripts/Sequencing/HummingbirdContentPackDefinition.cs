using UnityEngine;

namespace SoundSelf.Sequence
{
    /// <summary>Hummingbird game mode lane for a content-pack asset (maps to <c>CSVLoader.gameMode</c> canonical strings).</summary>
    public enum SessionGameMode
    {
        Sonoflore,
        Activation,
        Adjunctive,
        Albums,
    }

    /// <summary>
    /// VO routing for this pack; aligns with <see cref="WwiseVOManager"/> SetTo* entry points.
    /// Phase 2 wires dispatch — enum should stay exhaustive vs <c>WwiseVOManager</c>.
    /// </summary>
    public enum ContentPackVoKind
    {
        None,
        Fireflies,
        Kindness,
        Metta,
        Peace,
        Narrative,
        Surrender,
        EsketamineAscending,
    }

    /// <summary>
    /// Maps <see cref="SessionGameMode"/> to the canonical strings Hummingbird uses (see <c>CSVLoader</c> constants).
    /// </summary>
    public static class SessionGameModeMapping
    {
        public static string ToCsvGameModeString(SessionGameMode mode)
        {
            switch (mode)
            {
                case SessionGameMode.Sonoflore:
                    return CSVLoader.GameModeSonoflore;
                case SessionGameMode.Activation:
                    return CSVLoader.GameModeActivation;
                case SessionGameMode.Adjunctive:
                    return CSVLoader.GameModeAdjunctive;
                case SessionGameMode.Albums:
                    return CSVLoader.GameModeAlbums;
                default:
                    return CSVLoader.GameModeSonoflore;
            }
        }

        /// <summary>True if <paramref name="normalizedCsvGameMode"/> equals the canonical string for <paramref name="mode"/>.</summary>
        public static bool Matches(SessionGameMode mode, string normalizedCsvGameMode)
        {
            if (string.IsNullOrEmpty(normalizedCsvGameMode))
                return false;
            return string.Equals(ToCsvGameModeString(mode), normalizedCsvGameMode.Trim(), System.StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Per–content-pack session configuration for Hummingbird. Registry rows (see <see cref="HummingbirdContentPackRegistry"/>)
    /// identify which CSV <c>(gameMode, contentPack)</c> pair resolves to each asset.
    /// </summary>
    [CreateAssetMenu(fileName = "HummingbirdContentPack", menuName = "SoundSelf/Hummingbird/Hummingbird Content Pack Definition")]
    public class HummingbirdContentPackDefinition : ScriptableObject
    {
        [SerializeField] private SessionGameMode gameMode;

        [Tooltip("UI title for this pack (consumers TBD).")]
        [SerializeField] private string title;

        [Tooltip("UI description for this pack (consumers TBD).")]
        [TextArea(2, 6)]
        [SerializeField] private string description;

        [Tooltip("Post-unguided duration (seconds). Use -1 for unset/stub. Use 0 when there is no post-unguided block at the end.")]
        [SerializeField] private float postUnguidedSeconds = -1f;

        [SerializeField] private ContentPackVoKind voKind;

        [Tooltip("Sequence started when this pack is the resolved session (same asset type as SequenceRunner mode refs).")]
        [SerializeField] private SequenceDefinition sequenceDefinition;

        public SessionGameMode GameMode => gameMode;
        public string Title => title;
        public string Description => description;
        public float PostUnguidedSeconds => postUnguidedSeconds;
        public ContentPackVoKind VoKind => voKind;
        public SequenceDefinition SequenceDefinition => sequenceDefinition;
    }
}
