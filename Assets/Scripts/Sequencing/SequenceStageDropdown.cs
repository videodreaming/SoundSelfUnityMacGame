using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace SoundSelf.Sequence
{
    /// <summary>Populates a dropdown with sequence stages and jumps to the selected stage when the user changes it.
    /// Syncs the dropdown selection when the sequence advances (cue, completion, etc.).</summary>
    public class SequenceStageDropdown : MonoBehaviour
    {
        [SerializeField] private SequenceRunner sequenceRunner;
        [SerializeField] private TMP_Dropdown dropdown;
        [Tooltip("Optional. Use this to populate the dropdown before the sequence starts (e.g. Adjunctive definition).")]
        [SerializeField] private SequenceDefinition definitionOverride;

        private void OnEnable()
        {
            if (sequenceRunner == null)
            {
                Debug.LogWarning("SequenceStageDropdown: sequenceRunner is not assigned.");
                return;
            }
            if (dropdown == null)
            {
                Debug.LogWarning("SequenceStageDropdown: dropdown is not assigned.");
                return;
            }

            dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
            sequenceRunner.OnStageChanged += OnStageChanged;
            sequenceRunner.OnSequenceComplete += OnSequenceComplete;

            TryPopulateDropdown();
        }

        private void OnDisable()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
            if (sequenceRunner != null)
            {
                sequenceRunner.OnStageChanged -= OnStageChanged;
                sequenceRunner.OnSequenceComplete -= OnSequenceComplete;
            }
        }

        private void TryPopulateDropdown()
        {
            if (dropdown == null || sequenceRunner == null) return;
            var def = sequenceRunner.Definition ?? definitionOverride;
            if (def == null || def.StagesOrEmpty.Length == 0) return;

            var stages = def.StagesOrEmpty;
            if (dropdown.options.Count == stages.Length) return;
            var options = new List<string>();
            for (int i = 0; i < stages.Length; i++)
                options.Add($"{i + 1}. {stages[i].type}" + (stages[i].variant == StageVariant.None ? "" : $" ({stages[i].variant})"));

            dropdown.ClearOptions();
            dropdown.AddOptions(options);

            var idx = sequenceRunner.CurrentStageIndex;
            if (idx >= 0 && idx < stages.Length)
                dropdown.SetValueWithoutNotify(idx);
        }

        private void OnStageChanged(int index, StageType stageType)
        {
            TryPopulateDropdown();
            if (dropdown != null && index >= 0 && index < dropdown.options.Count)
                dropdown.SetValueWithoutNotify(index);
        }

        private void OnSequenceComplete()
        {
            if (dropdown != null && dropdown.options.Count > 0)
                dropdown.SetValueWithoutNotify(dropdown.options.Count - 1);
        }

        private void OnDropdownValueChanged(int index)
        {
            if (sequenceRunner == null) return;
            sequenceRunner.AdvanceToStage(index);
        }

    }
}
