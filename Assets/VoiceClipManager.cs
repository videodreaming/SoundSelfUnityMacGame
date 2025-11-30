using System;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class VoiceClipManager : MonoBehaviour
{
    // ---------- Config ----------
    [Header("Recording Settings")]
    [SerializeField] private string deviceName = "";
    [SerializeField, Min(1)] private int maxDurationSeconds = 120;
    [SerializeField] private int sampleRate = 48000;

    [Header("Storage")]
    [SerializeField, Range(1, 12)] private int capacity = 12;

    public enum CapacityPolicy { BlockWhenFull, OverwriteOldest }
    [SerializeField] private CapacityPolicy capacityPolicy = CapacityPolicy.BlockWhenFull;

    public enum DeletionMode { Oldest, LowestIndex, ShortestLength }
    [SerializeField] private DeletionMode deletionMode = DeletionMode.Oldest;

    public enum PlaybackPick { SelectedSlot, MostRecent, HighestIndex, LowestIndex, LastRecordedSlot }
    [SerializeField] private PlaybackPick preferredPlaybackMode = PlaybackPick.SelectedSlot;

    [Header("Hotkeys")]
    [SerializeField] private KeyCode startKey = KeyCode.Alpha1;   // begin recording
    [SerializeField] private KeyCode stopKey  = KeyCode.Alpha2;   // stop & save
    [SerializeField] private KeyCode playKey  = KeyCode.Alpha3;   // play chosen clip
    [SerializeField] private KeyCode delKey   = KeyCode.Alpha4;   // delete per DeletionMode
    [SerializeField] private KeyCode leftKey  = KeyCode.LeftArrow;  // select previous slot
    [SerializeField] private KeyCode rightKey = KeyCode.RightArrow; // select next slot

    // ---------- Selection (visible in Inspector) ----------
    [Header("Selection")]
    [SerializeField] private int selectedSlot = 0;   // current selection; arrow keys change this

    // ---------- Debug / Inspector View ----------
    [Header("Inspector View (read-only)")]
    [SerializeField] private bool[] slotFilledView;   // shows which slots have clips
    [SerializeField] private float[] slotLengthView;  // shows clip length in seconds

    // ---------- Runtime ----------
    private AudioClip[] _clips;
    private DateTime?[] _savedAt;
    private float[] _lengths;
    private AudioSource _player;

    private bool _isRecording;
    private int _recordingSlot = -1;
    private int _lastSavedSlot = -1;
    private AudioClip _workingMicClip;
    private float _recordingStartTime;

    // ---------- Events ----------
    public event Action<int> OnRecordingStarted;
    public event Action<int, AudioClip> OnRecordingSaved;
    public event Action<int> OnRecordingCancelled;
    public event Action<int> OnRecordingStopped;
    public event Action<int> OnPlaybackCompleted; // slot that just finished

    private int _currentPlaybackSlot = -1;
    private bool _isPlaying;

    public bool IsRecording => _isRecording;
    public int Capacity => _clips?.Length ?? 0;
    public int SelectedSlot => Mathf.Clamp(selectedSlot, 0, Capacity - 1);

    void Awake()
    {
        AllocateArrays();
        _player = GetComponent<AudioSource>();
        _player.playOnAwake = false;
        ClampSelected();
        UpdateInspectorView();
    }

    void OnValidate()
    {
        // Keep arrays and selection consistent in the editor
        AllocateArrays();
        ClampSelected();
        UpdateInspectorView();
    }

    void Update()
    {
        // Change selected slot with arrow keys
        if (Input.GetKeyDown(leftKey))
        {
            selectedSlot = (SelectedSlot - 1 + Capacity) % Capacity;
            Debug.Log($"[VoiceClipManager] Selected slot -> {SelectedSlot}");
        }
        if (Input.GetKeyDown(rightKey))
        {
            selectedSlot = (SelectedSlot + 1) % Capacity;
            Debug.Log($"[VoiceClipManager] Selected slot -> {SelectedSlot}");
        }

        if (Input.GetKeyDown(startKey))
        {
            int target = SelectedSlot;

            // If selected is empty, use it. If full and selected has clip:
            // - Block when full (unless selected is empty)
            // - Or overwrite oldest (choose oldest, even if not selected)
            if (_clips[target] == null)
            {
                StartRecordingToSlot(target);
            }
            else
            {
                // Need a free slot OR overwrite policy
                int free = NextFreeSlot();
                if (free >= 0)
                {
                    StartRecordingToSlot(free);
                    selectedSlot = free; // follow the actual recording slot
                }
                else
                {
                    if (capacityPolicy == CapacityPolicy.BlockWhenFull)
                    {
                        Debug.LogWarning("[VoiceClipManager] All slots full. Press 4 to delete or switch CapacityPolicy to OverwriteOldest.");
                    }
                    else
                    {
                        int oldest = OldestSlotIndex();
                        if (oldest >= 0)
                        {
                            Debug.Log($"[VoiceClipManager] Overwriting oldest slot {oldest}.");
                            StartRecordingToSlot(oldest);
                            selectedSlot = oldest;
                        }
                        else
                        {
                            Debug.LogWarning("[VoiceClipManager] No deletable slot found.");
                        }
                    }
                }
            }
        }

        if (Input.GetKeyDown(stopKey))
        {
            StopRecording();
        }

        if (Input.GetKeyDown(playKey))
        {
            int slot = PickPlaybackSlot();
            if (slot < 0 || _clips[slot] == null)
            {
                Debug.LogWarning("[VoiceClipManager] Nothing to play.");
            }
            else
            {
                PlaySlot(slot, loop: false, volume: 1f);
                Debug.Log($"[VoiceClipManager] Playing slot {slot} ({_clips[slot].length:0.00}s).");
            }
        }

        if (Input.GetKeyDown(delKey))
        {
            int victim = PickDeletionSlot();
            if (victim < 0)
            {
                Debug.Log("[VoiceClipManager] No slot to delete.");
            }
            else
            {
                ClearSlot(victim);
                Debug.Log($"[VoiceClipManager] Deleted slot {victim}.");
                // Nudge selection to a valid index
                ClampSelected();
            }
        }
        // Check if playback finished
        if (_isPlaying && !_player.isPlaying && !_player.loop && _player.clip != null)
        {
            int finishedSlot = _currentPlaybackSlot;
            _isPlaying = false;
            _currentPlaybackSlot = -1;

            OnPlaybackCompleted?.Invoke(finishedSlot);
            Debug.Log($"[VoiceClipManager] Playback completed for slot {finishedSlot}");
        }
    }

    // ====== Public API ======

    public bool StartRecordingToSlot(int slot)
    {
        if (_isRecording)
        {
            Debug.LogWarning("[VoiceClipManager] Already recording. Stop first.");
            return false;
        }
        if (!IsValidSlot(slot))
        {
            Debug.LogError($"[VoiceClipManager] Slot {slot} out of range 0..{Capacity - 1}.");
            return false;
        }
        if (!HasMicrophone())
        {
            Debug.LogError("[VoiceClipManager] No microphone devices available.");
            return false;
        }

        // Overwrite target slot safely
        SafeDestroySlot(slot);

        try
        {
            _workingMicClip = Microphone.Start(
                string.IsNullOrEmpty(deviceName) ? null : deviceName,
                false, maxDurationSeconds, sampleRate);

            if (_workingMicClip == null)
            {
                Debug.LogError("[VoiceClipManager] Microphone.Start returned null.");
                return false;
            }

            _isRecording = true;
            _recordingSlot = slot;
            _recordingStartTime = Time.unscaledTime;
            OnRecordingStarted?.Invoke(slot);
            Debug.Log($"[VoiceClipManager] Recording -> slot {slot} …");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceClipManager] Failed to start recording: {e}");
            _workingMicClip = null;
            _isRecording = false;
            _recordingSlot = -1;
            return false;
        }
    }

    public AudioClip StopRecording()
    {
        if (!_isRecording) return null;

        int slot = _recordingSlot;

        int pos = Microphone.GetPosition(string.IsNullOrEmpty(deviceName) ? null : deviceName);
        bool hitMax = false;
        if (pos <= 0)
        {
            hitMax = true;
            pos = maxDurationSeconds * sampleRate;
        }

        Microphone.End(string.IsNullOrEmpty(deviceName) ? null : deviceName);

        AudioClip saved = null;

        try
        {
            if (_workingMicClip != null)
            {
                int channels = _workingMicClip.channels;
                int micTotalSamples = _workingMicClip.samples;
                int trimmedSamples = Mathf.Clamp(pos, 0, micTotalSamples);

                if (trimmedSamples > 0)
                {
                    float[] buffer = new float[trimmedSamples * channels];
                    _workingMicClip.GetData(buffer, 0);

                    saved = AudioClip.Create(
                        $"Recorded_{slot}_{DateTime.Now:HHmmss}",
                        trimmedSamples, channels, sampleRate, false);
                    saved.SetData(buffer, 0);

                    SafeDestroySlot(slot);
                    _clips[slot] = saved;
                    _savedAt[slot] = DateTime.UtcNow;
                    _lengths[slot] = saved.length;
                    _lastSavedSlot = slot;

                    UpdateInspectorView();
                    OnRecordingSaved?.Invoke(slot, saved);
                    Debug.Log($"[VoiceClipManager] Saved slot {slot} ({saved.length:0.00}s){(hitMax ? " [max]" : "")}.");
                }
                else
                {
                    Debug.LogWarning("[VoiceClipManager] Recorded length is zero. Nothing saved.");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[VoiceClipManager] Error while trimming/saving: {e}");
        }
        finally
        {
            if (_workingMicClip != null) { Destroy(_workingMicClip); _workingMicClip = null; }
            _isRecording = false;
            _recordingSlot = -1;
            OnRecordingStopped?.Invoke(slot);
        }

        return saved;
    }

    public void CancelRecording()
    {
        if (!_isRecording) return;
        int slot = _recordingSlot;

        Microphone.End(string.IsNullOrEmpty(deviceName) ? null : deviceName);
        if (_workingMicClip != null) { Destroy(_workingMicClip); _workingMicClip = null; }

        _isRecording = false;
        _recordingSlot = -1;

        OnRecordingCancelled?.Invoke(slot);
        OnRecordingStopped?.Invoke(slot);
        Debug.Log("[VoiceClipManager] Recording cancelled.");
    }

    public bool PlaySlot(int slot, bool loop = false, float volume = 1f)
    {
        var clip = GetClip(slot);
        if (clip == null) return false;
        _player.Stop();
        _player.loop = loop;
        _player.volume = Mathf.Clamp01(volume);
        _player.clip = clip;
        _player.Play();

        _currentPlaybackSlot = slot;
        _isPlaying = true;
        return true;
    }

    public bool PlaySlot(int slot, AudioSource target, bool loop = false, float volume = 1f)
    {
        if (target == null) return false;
        var clip = GetClip(slot);
        if (clip == null) return false;
        target.Stop();
        target.loop = loop;
        target.volume = Mathf.Clamp01(volume);
        target.clip = clip;
        target.Play();
        return true;
    }

    public AudioClip GetClip(int slot) => IsValidSlot(slot) ? _clips[slot] : null;

    public void ClearSlot(int slot)
    {
        if (!IsValidSlot(slot)) return;
        SafeDestroySlot(slot);
        _clips[slot] = null;
        _savedAt[slot] = null;
        _lengths[slot] = 0f;
        UpdateInspectorView();
        if (_lastSavedSlot == slot) _lastSavedSlot = MostRecentSlotIndex();
    }

    public void ClearAll()
    {
        for (int i = 0; i < Capacity; i++) ClearSlot(i);
        UpdateInspectorView();
    }

    public float CurrentRecordingSeconds =>
        _isRecording ? (Time.unscaledTime - _recordingStartTime) : 0f;

    public float RemainingRecordingSeconds =>
        _isRecording ? Mathf.Max(0f, maxDurationSeconds - CurrentRecordingSeconds) : maxDurationSeconds;

    public void SetDevice(string newDeviceName)
    {
        if (_isRecording)
        {
            Debug.LogWarning("[VoiceClipManager] Cannot change device while recording.");
            return;
        }
        deviceName = newDeviceName ?? "";
    }

    public string[] GetAvailableDevices() => Microphone.devices ?? Array.Empty<string>();

    // ---------- Helpers ----------

    private void AllocateArrays()
    {
        if (capacity < 1) capacity = 1;
        if (capacity > 12) capacity = 12;

        // Recreate runtime arrays only if size changed
        if (_clips == null || _clips.Length != capacity)
        {
            var oldClips   = _clips;
            var oldSavedAt = _savedAt;
            var oldLens    = _lengths;

            _clips   = new AudioClip[capacity];
            _savedAt = new DateTime?[capacity];
            _lengths = new float[capacity];

            // migrate whatever fits
            if (oldClips != null)
            {
                int n = Mathf.Min(oldClips.Length, capacity);
                Array.Copy(oldClips, _clips, n);
                Array.Copy(oldSavedAt, _savedAt, n);
                Array.Copy(oldLens, _lengths, n);
            }
        }

        // Debug view arrays (purely for Inspector)
        if (slotFilledView == null || slotFilledView.Length != capacity)
            slotFilledView = new bool[capacity];
        if (slotLengthView == null || slotLengthView.Length != capacity)
            slotLengthView = new float[capacity];
    }

    private void UpdateInspectorView()
    {
        if (_clips == null) return;
        for (int i = 0; i < Capacity; i++)
        {
            slotFilledView[i]  = _clips[i] != null;
            slotLengthView[i]  = _clips[i] != null ? _clips[i].length : 0f;
        }
    }

    private void ClampSelected()
    {
        if (capacity <= 0) selectedSlot = 0;
        else selectedSlot = Mathf.Clamp(selectedSlot, 0, capacity - 1);
    }

    private bool IsValidSlot(int slot) => slot >= 0 && slot < Capacity;
    private bool HasMicrophone() => Microphone.devices != null && Microphone.devices.Length > 0;

    private void SafeDestroySlot(int slot)
    {
        if (!IsValidSlot(slot)) return;
        if (_clips[slot] != null)
        {
            Destroy(_clips[slot]);
            _clips[slot] = null;
        }
    }

    private int NextFreeSlot()
    {
        for (int i = 0; i < Capacity; i++)
            if (_clips[i] == null) return i;
        return -1;
    }

    private int OldestSlotIndex()
    {
        int best = -1;
        DateTime? bestTime = null;
        for (int i = 0; i < Capacity; i++)
        {
            if (_clips[i] == null) continue;
            if (bestTime == null || _savedAt[i] < bestTime)
            {
                bestTime = _savedAt[i];
                best = i;
            }
        }
        return best;
    }

    private int MostRecentSlotIndex()
    {
        int best = -1;
        DateTime? bestTime = null;
        for (int i = 0; i < Capacity; i++)
        {
            if (_clips[i] == null) continue;
            if (bestTime == null || _savedAt[i] > bestTime)
            {
                bestTime = _savedAt[i];
                best = i;
            }
        }
        return best;
    }

    private int ShortestSlotIndex()
    {
        int best = -1;
        float bestLen = float.MaxValue;
        for (int i = 0; i < Capacity; i++)
        {
            if (_clips[i] == null) continue;
            if (_lengths[i] < bestLen)
            {
                bestLen = _lengths[i];
                best = i;
            }
        }
        return best;
    }

    private int LowestIndexSlot()
    {
        for (int i = 0; i < Capacity; i++)
            if (_clips[i] != null) return i;
        return -1;
    }

    private int HighestIndexSlot()
    {
        // Fix for LINQ FirstOrDefault default value issue:
        // .Where(...).DefaultIfEmpty(-1).First();
        return Enumerable.Range(0, Capacity)
                         .Reverse()
                         .Where(i => _clips[i] != null)
                         .DefaultIfEmpty(-1)
                         .First();
    }

    private int PickDeletionSlot()
    {
        switch (deletionMode)
        {
            case DeletionMode.Oldest:         return OldestSlotIndex();
            case DeletionMode.LowestIndex:    return LowestIndexSlot();
            case DeletionMode.ShortestLength: return ShortestSlotIndex();
            default: return -1;
        }
    }

    private int PickPlaybackSlot()
    {
        switch (preferredPlaybackMode)
        {
            case PlaybackPick.SelectedSlot:     return SelectedSlot;
            case PlaybackPick.MostRecent:       return MostRecentSlotIndex();
            case PlaybackPick.HighestIndex:     return HighestIndexSlot();
            case PlaybackPick.LowestIndex:      return LowestIndexSlot();
            case PlaybackPick.LastRecordedSlot: return (_lastSavedSlot >= 0 && _clips[_lastSavedSlot] != null)
                                                    ? _lastSavedSlot
                                                    : MostRecentSlotIndex();
            default: return -1;
        }
    }
}
