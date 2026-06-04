#if UNITY_EDITOR
using ConversionUtilities;
using SoundSelf.Sequence;
using UnityEngine;

/// <summary>
/// Editor-only keyboard harness for Blocks 4/5/7 playtests in <see cref="StageVariant.Playground_Debug"/>.
/// Attach to the same GameObject as <see cref="InputReferences"/> (auto-added in editor) or any scene object.
/// </summary>
public class MusicDebugHarness : MonoBehaviour
{
    const string LogPrefix = "[MusicDebugHarness]";

    static readonly string[] SoundWorldCycle = { "SonoFlore", "Shadow", "Gentle", "Shruti" };
    static readonly string[] MusicLoopCycle = { "ShiftingEarth", "SitarAmbience", "PinkNoiseAtmosphere" };

    [SerializeField] private Sequencer sequencer;
    [SerializeField] private Director director;
    [SerializeField] private MusicDebugGuidedPlaytest guidedPlaytest;
    [SerializeField] private bool logKeyLegendOnStart = true;

    int _soundWorldIndex;
    int _musicLoopIndex;
    int _lornaTimelineIndex;
    bool _binauralAudibleVolume = true;

    void Awake()
    {
        if (sequencer == null) sequencer = FindObjectOfType<Sequencer>();
        if (director == null) director = FindObjectOfType<Director>();
        if (guidedPlaytest == null) guidedPlaytest = GetComponent<MusicDebugGuidedPlaytest>();
    }

    void Start()
    {
        if (logKeyLegendOnStart)
            LogKeyLegend();
    }

    void Update()
    {
        if (!Input.anyKeyDown)
            return;

        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (!Input.GetKeyDown(key))
                continue;
            if (!MusicDebugHarnessKeyPolicy.TryGetActionForKey(key, out MusicDebugHarnessAction action))
                continue;
            ExecuteAction(action);
            break;
        }
    }

    public void ExecuteAction(MusicDebugHarnessAction action)
    {
        switch (action)
        {
            case MusicDebugHarnessAction.DumpState:
                DumpStateLine();
                break;
            case MusicDebugHarnessAction.CycleSoundWorld:
                CycleSoundWorld();
                break;
            case MusicDebugHarnessAction.CycleMusicLoop:
                CycleMusicLoop();
                break;
            case MusicDebugHarnessAction.StepLornaKeyCue:
                StepLornaKeyCue();
                break;
            case MusicDebugHarnessAction.LockFundamentalToC:
                LockFundamentalToC();
                break;
            case MusicDebugHarnessAction.UnlockFundamentalLocks:
                UnlockFundamentalLocks();
                break;
            case MusicDebugHarnessAction.ToggleBinauralPlay:
                ToggleBinauralPlay();
                break;
            case MusicDebugHarnessAction.ToggleBinauralVolume:
                ToggleBinauralVolume();
                break;
            case MusicDebugHarnessAction.DirectorQueueRepro:
                DirectorQueueRepro();
                break;
            case MusicDebugHarnessAction.JumpCountdownTo15Minutes:
                JumpCountdown(15f * 60f, "15:00");
                break;
            case MusicDebugHarnessAction.JumpCountdownToSavasanaLockWindow:
                JumpCountdown(60f, "savasana-lock-60s");
                break;
            case MusicDebugHarnessAction.EndThisSequenceStage:
                EndThisSequenceStage();
                break;
            case MusicDebugHarnessAction.GuidedStage1And2Playtest:
                ToggleGuidedPlaytest();
                break;
        }
    }

    void ToggleGuidedPlaytest()
    {
        if (guidedPlaytest == null)
            guidedPlaytest = gameObject.AddComponent<MusicDebugGuidedPlaytest>();
        guidedPlaytest.ToggleRun();
    }

    void EndThisSequenceStage()
    {
        if (sequencer == null)
            sequencer = FindObjectOfType<Sequencer>();
        if (sequencer == null)
        {
            Debug.LogWarning(LogPrefix + " E: No Sequencer in scene.");
            return;
        }

        bool handled = sequencer.HandleSequenceCommand(SequenceCommand.EndThisSequenceStage);
        Debug.Log(handled
            ? LogPrefix + " E: EndThisSequenceStage handled — stage should complete / advance."
            : LogPrefix + " E: EndThisSequenceStage not handled (no watcher or no active stage).");
    }

    public static string FormatStateLine(
        MusicSystem1.MusicMode? mode,
        NoteName fundamental,
        NoteName harmony,
        MusicSystem1.InteractionType? interaction,
        string soundscapeLabel,
        float binauralCenterHz,
        float? binauralBusVolume,
        bool? binauralAttenuated,
        float? binauralOutVolume,
        bool? gameOn)
    {
        string modeStr = mode.HasValue ? mode.Value.ToString() : "n/a";
        string interactionStr = interaction.HasValue ? interaction.Value.ToString() : "n/a";
        string gameOnStr = gameOn.HasValue ? (gameOn.Value ? "on" : "off") : "n/a";
        string binauralVolStr = binauralBusVolume.HasValue ? binauralBusVolume.Value.ToString("F0") : "n/a";
        string binauralOutStr = binauralOutVolume.HasValue ? binauralOutVolume.Value.ToString("F0") : "n/a";
        string attStr = binauralAttenuated.HasValue ? (binauralAttenuated.Value ? "on" : "off") : "n/a";
        return "mode=" + modeStr
            + " | fundamental=" + fundamental
            + " | harmony=" + harmony
            + " | interaction=" + interactionStr
            + " | soundscape=" + soundscapeLabel
            + " | binauralHz=" + binauralCenterHz.ToString("F1")
            + " | binauralBase=" + binauralVolStr
            + " | binauralAtt=" + attStr
            + " | binauralOut=" + binauralOutStr
            + " | gameOn=" + gameOnStr;
    }

    void DumpStateLine()
    {
        var ms = MusicSystem1.instance;
        var imitone = sequencer != null ? sequencer.imitoneVoiceInterpreter : null;
        string soundscape = ms != null && ms.worldShuffler != null
            ? ms.worldShuffler.EditorCurrentSoundscape
            : "";
        if (string.IsNullOrEmpty(soundscape))
            soundscape = "(none)";

        float binauralHz = ms != null
            ? NoteUtils.NoteToFrequencyA440(ms.fundamentalNoteName)
            : 0f;
        var beats = MusicBinauralBeats.instance;
        float? binauralVol = beats != null ? beats._volume : (float?)null;
        bool? binauralAtt = beats != null ? beats.IsAttenuated : (bool?)null;
        float? binauralOut = beats != null ? beats.EffectiveBusVolume : (float?)null;

        string line = FormatStateLine(
            ms != null ? ms.currentMusicMode : (MusicSystem1.MusicMode?)null,
            ms != null ? ms.fundamentalNoteName : NoteName.None,
            ms != null ? ms.harmonyNote : NoteName.None,
            ms != null ? ms.currentInteractionType : (MusicSystem1.InteractionType?)null,
            soundscape,
            binauralHz,
            binauralVol,
            binauralAtt,
            binauralOut,
            imitone != null ? imitone.gameOn : (bool?)null);

        Debug.Log(LogPrefix + " STATE " + line);
    }

    void CycleSoundWorld()
    {
        var ms = MusicSystem1.instance;
        if (ms == null)
        {
            Debug.LogWarning(LogPrefix + " MusicSystem1.instance is null.");
            return;
        }

        _soundWorldIndex = (_soundWorldIndex + 1) % SoundWorldCycle.Length;
        string world = SoundWorldCycle[_soundWorldIndex];
        ms.SetSoundWorld(world);
        Debug.Log(LogPrefix + " SetSoundWorld " + world);
        DumpStateLine();
    }

    void CycleMusicLoop()
    {
        var ms = MusicSystem1.instance;
        if (ms == null)
        {
            Debug.LogWarning(LogPrefix + " MusicSystem1.instance is null.");
            return;
        }

        _musicLoopIndex = (_musicLoopIndex + 1) % (MusicLoopCycle.Length + 1);
        if (_musicLoopIndex < MusicLoopCycle.Length)
        {
            string loop = MusicLoopCycle[_musicLoopIndex];
            ms.SetMusicLoop(loop);
            Debug.Log(LogPrefix + " SetMusicLoop " + loop);
        }
        else
        {
            ms.SetMusicModeTo(MusicSystem1.MusicMode.MusicLoopSilent);
            Debug.Log(LogPrefix + " SetMusicMode MusicLoopSilent (Silence bed)");
        }

        DumpStateLine();
    }

    void StepLornaKeyCue()
    {
        var ms = MusicSystem1.instance;
        if (ms == null)
        {
            Debug.LogWarning(LogPrefix + " MusicSystem1.instance is null.");
            return;
        }

        if (MusicKeyCuePolicy.LornaExampleTimeline.Length == 0)
            return;

        string cue = MusicKeyCuePolicy.LornaExampleTimeline[_lornaTimelineIndex];
        _lornaTimelineIndex = (_lornaTimelineIndex + 1) % MusicKeyCuePolicy.LornaExampleTimeline.Length;
        MusicKeyCuePolicy.TryApplyCue(cue, ms);
        DumpStateLine();
    }

    void LockFundamentalToC()
    {
        var ms = MusicSystem1.instance;
        if (ms == null)
            return;
        ms.SetFundamentalContentLock(NoteName.C);
        Debug.Log(LogPrefix + " SetFundamentalContentLock C (savasana-style)");
        DumpStateLine();
    }

    void UnlockFundamentalLocks()
    {
        var ms = MusicSystem1.instance;
        if (ms == null)
            return;
        ms.SetFundamentalContentLock(null);
        ms.SetFundamentalModeLock(false);
        ms.SetFundamentalDebugLock(null);
        Debug.Log(LogPrefix + " Cleared content/mode/debug fundamental locks");
        DumpStateLine();
    }

    void ToggleBinauralPlay()
    {
        var beats = MusicBinauralBeats.instance;
        if (beats == null)
        {
            Debug.LogWarning(LogPrefix + " MusicBinauralBeats.instance is null.");
            return;
        }

        if (beats.IsGeneratorRunning)
        {
            beats.StopBinauralBeats();
            Debug.Log(LogPrefix + " StopBinauralBeats");
        }
        else
        {
            beats.PlayBinauralBeats();
            Debug.Log(LogPrefix + " PlayBinauralBeats");
        }
    }

    void ToggleBinauralVolume()
    {
        var beats = MusicBinauralBeats.instance;
        if (beats == null)
            return;

        _binauralAudibleVolume = !_binauralAudibleVolume;
        beats.SetVolume(_binauralAudibleVolume ? BinauralStagePolicy.AudibleVolume : 0f, 0f);
        Debug.Log(LogPrefix + " Binaural volume " + (_binauralAudibleVolume ? BinauralStagePolicy.AudibleVolume.ToString("F0") : "0"));
    }

    void DirectorQueueRepro()
    {
        if (director == null)
        {
            Debug.LogWarning(LogPrefix + " Director is null.");
            return;
        }

        int id = director.AddActionToQueue(
            () => Debug.Log(LogPrefix + " Director repro action executed (ActivateEntireQueueOnNextTone)."),
            "MusicDebugHarness_Repro",
            true,
            false,
            0.05f,
            DirectorActivationBehavior.ActivateEntireQueueOnNextTone,
            DirectorExclusivityBehavior.None);

        Debug.Log(LogPrefix + " Queued repro id=" + id
            + " (0.05s, ActivateEntireQueueOnNextTone). Tone after expiry — watch for empty-queue log vs action executed. "
            + director.FormatQueueContents());
    }

    void JumpCountdown(float thisSectionSeconds, string label)
    {
        var tt = TimeTrackerScript.instance;
        if (tt == null)
        {
            Debug.LogWarning(LogPrefix + " TimeTrackerScript.instance is null.");
            return;
        }

        float full = Mathf.Max(tt.CountdownFull, thisSectionSeconds);
        tt.ConfigureCountdownPair(thisSectionSeconds, full);
        tt.BeginCountdownPair();
        Debug.Log(LogPrefix + " Countdown → " + label + " ([CountdownThisSection]=" + thisSectionSeconds + "s)");
    }

    void LogKeyLegend()
    {
        Debug.Log(LogPrefix + " Keys: P=state | E=end stage | G=guided Stage1+2 playtest | [=world ]=loop | ;=key cue | L=lock C | U=unlock | B/V=binaural | R=director repro | 1=15:00 cd | 2=60s cd (Shift+E also ends stage via InputReferences)");
    }
}
#endif
