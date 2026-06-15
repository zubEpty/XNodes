using UnityEngine;

namespace XR23.Phishing.Cutscene.Data
{
    [CreateAssetMenu(menuName = "XR23/Cutscene/Dialogue Layout Profile")]
    public sealed class DialogueLayoutProfile : ScriptableObject
    {
        [Header("Anchoring")]
        public Vector2 AnchorMin;
        public Vector2 AnchorMax;
        public Vector2 Pivot;

        [Header("Offsets")]
        public Vector2 AnchoredPosition;
        public Vector2 SizeDelta;

        [Header("Speaker Capsule")]
        public bool ShowSpeakerName;
    }
}