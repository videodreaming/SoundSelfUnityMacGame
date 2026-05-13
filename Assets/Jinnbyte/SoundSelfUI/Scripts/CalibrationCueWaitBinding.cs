using UnityEngine;

/// <summary>Optional per-button hook for polite-next / conclusion wait visuals. <see cref="CalibrationStageHandler"/> + <see cref="UIManager"/> toggle these while Wwise catches up.</summary>
/// <remarks>
/// Idle state is restored in <see cref="OnEnable"/> (loading off, label on) whenever the screen root is activated.
/// Pending Next only calls <see cref="SetPendingCueWaitActive"/> with <c>true</c>; we avoid calling <c>false</c> on the outgoing
/// step when the UI changes screen so the loading icon is not cleared mid-fade — the next activation resets bindings on the new card.
/// </remarks>
public class CalibrationCueWaitBinding : MonoBehaviour
{
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private GameObject labelWhileWaiting;
    [Tooltip("Bindings on Start → LightGlasses primary Next Step buttons.")]
    [SerializeField] private bool driveNextStepCueWaitVisual = true;
    [Tooltip("Enable on Conclusion confirm when using loading UI while waiting for Cue_Calibration_Instruction_OFF.")]
    [SerializeField] private bool driveConclusionCueWaitVisual;

    public bool DriveNextStepCueWaitVisual => driveNextStepCueWaitVisual;
    public bool DriveConclusionCueWaitVisual => driveConclusionCueWaitVisual;

    private void OnEnable()
    {
        // Fresh card: label visible, loading hidden. Pending wait is applied only via SetPendingCueWaitActive(true) after Next/confirm.
        SetPendingCueWaitActive(false);
    }

    public void SetPendingCueWaitActive(bool pending)
    {
        if (loadingRoot != null)
            loadingRoot.SetActive(pending);
        if (labelWhileWaiting != null)
            labelWhileWaiting.SetActive(!pending);
    }
}
