using UnityEngine;
using TMPro;

namespace SoundSelf.Sequence
{
    /// <summary>Subscribes to SequenceRunner.OnStageChanged and OnSequenceComplete. Logs for verification; optionally updates UI when references are assigned.</summary>
    public class SequenceProgressUI : MonoBehaviour
    {
        [SerializeField] private SequenceRunner sequenceRunner;
        [Tooltip("Optional. When assigned, shows current stage name.")]
        [SerializeField] private TMP_Text stageLabel;
        [Tooltip("Optional. When assigned, shows progress (e.g. 1/3).")]
        [SerializeField] private TMP_Text progressLabel;

        private void OnEnable()
        {
            if (sequenceRunner == null)
            {
                Debug.LogWarning("SequenceProgressUI: sequenceRunner is not assigned. Assign it in the Inspector to receive stage updates.");
                return;
            }
            sequenceRunner.OnStageChanged += OnStageChanged;
            sequenceRunner.OnSequenceComplete += OnSequenceComplete;
        }

        private void OnDisable()
        {
            if (sequenceRunner == null) return;
            sequenceRunner.OnStageChanged -= OnStageChanged;
            sequenceRunner.OnSequenceComplete -= OnSequenceComplete;
        }

        private void OnStageChanged(int index, StageType stageType)
        {
            Debug.Log($"SequenceProgressUI: Stage {index} ({stageType})");
            if (stageLabel != null)
                stageLabel.text = stageType.ToString();
            if (progressLabel != null && sequenceRunner != null)
                progressLabel.text = $"{index + 1}/{sequenceRunner.StageCount}";
        }

        private void OnSequenceComplete()
        {
            Debug.Log("SequenceProgressUI: Sequence complete.");
            if (stageLabel != null)
                stageLabel.text = "Complete";
            if (progressLabel != null)
                progressLabel.text = "—";
        }
    }
}
