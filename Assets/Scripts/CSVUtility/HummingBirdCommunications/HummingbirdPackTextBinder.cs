using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drop on a UI text GameObject (Legacy <see cref="Text"/> or <see cref="TMP_Text"/>). On enable, reads
/// <see cref="CSVLoader.ResolvedSessionPack"/> and writes either its
/// <see cref="SoundSelf.Sequence.HummingbirdContentPackDefinition.Title"/> or
/// <see cref="SoundSelf.Sequence.HummingbirdContentPackDefinition.Description"/> into the text component.
///
/// <para>The editor-only <c>hummingbirdContentPackOverride</c> on <see cref="CSVLoader"/> is honored automatically:
/// <c>CSVLoader.Awake</c> applies the override before resolving, so <c>ResolvedSessionPack</c> already reflects it.</para>
///
/// <para><b>Failure modes (all leave the inspector placeholder text untouched, by design):</b>
/// no <see cref="CSVLoader"/> in scene, no resolved pack (registry miss / no session), empty <c>Title</c>/<c>Description</c>
/// on the pack asset, or no text component on this GameObject. Each logs a warning so the cause is visible.</para>
/// </summary>
[DisallowMultipleComponent]
public class HummingbirdPackTextBinder : MonoBehaviour
{
    public enum Field
    {
        Title,
        Description,
    }

    [Tooltip("Which field of the resolved Hummingbird content pack to display in this text component.")]
    [SerializeField] private Field field = Field.Title;

    private void OnEnable()
    {
        var loader = CSVLoader.instance;
        if (loader == null)
        {
            Debug.LogWarning($"{name} ({nameof(HummingbirdPackTextBinder)}): CSVLoader.instance is null; leaving placeholder text.");
            return;
        }

        var pack = loader.ResolvedSessionPack;
        if (pack == null)
        {
            // CSVLoader already logs an error explaining why (registry miss, missing session, etc.); leave placeholder text.
            return;
        }

        string value = field == Field.Title ? pack.Title : pack.Description;
        if (string.IsNullOrEmpty(value))
        {
            Debug.LogWarning($"{name} ({nameof(HummingbirdPackTextBinder)}): Pack asset '{pack.name}' has empty {field}; leaving placeholder text.");
            return;
        }

        // Pack authoring convention: literal "\n" / "\r\n" / "\t" typed into the inspector TextArea is stored as two
        // characters (backslash + letter) and would render verbatim. Translate to real control chars so newlines work.
        value = UnescapeAuthoringControlChars(value);

        if (!TryApplyToLegacyText(value) && !TryApplyToTmpText(value))
        {
            Debug.LogWarning($"{name} ({nameof(HummingbirdPackTextBinder)}): No UnityEngine.UI.Text or TMP_Text component found on this GameObject; nothing to bind to.");
        }
    }

    /// <summary>
    /// Replaces the two-character authoring sequences <c>\\n</c>, <c>\\r\\n</c>, and <c>\\t</c> with their real
    /// control characters. Order matters: <c>\\r\\n</c> must be handled before <c>\\n</c> alone so we don't
    /// produce a stray CR.
    /// </summary>
    private static string UnescapeAuthoringControlChars(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        return raw
            .Replace("\\r\\n", "\n")
            .Replace("\\n", "\n")
            .Replace("\\t", "\t");
    }

    private bool TryApplyToLegacyText(string value)
    {
        var legacy = GetComponent<Text>();
        if (legacy == null) return false;
        legacy.text = value;
        return true;
    }

    private bool TryApplyToTmpText(string value)
    {
        var tmp = GetComponent<TMP_Text>();
        if (tmp == null) return false;
        tmp.text = value;
        return true;
    }
}
