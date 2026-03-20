using UnityEngine;

namespace SoundSelf.Sequence
{
    [CreateAssetMenu(fileName = "NewSequence", menuName = "SoundSelf/Sequence Definition")]
    public class SequenceDefinition : ScriptableObject
    {
        public string displayName = "Protocol Stacks Ascending";
        public SequenceStage[] stages = new SequenceStage[0];

        /// <summary>Returns stages array, or empty array if null. Use to avoid NullReferenceException on new/unconfigured assets.</summary>
        public SequenceStage[] StagesOrEmpty => stages ?? System.Array.Empty<SequenceStage>();
    }
}
