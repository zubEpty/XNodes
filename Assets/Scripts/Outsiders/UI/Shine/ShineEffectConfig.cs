using UnityEngine;

namespace Outsiders.UI.Shine
{
    [CreateAssetMenu(fileName = "ShineEffectConfig", menuName = "Outsiders/UI/Shine Effect Config")]
    public class ShineEffectConfig : ScriptableObject
    {
        [Header("Shine Appearance")]
        [SerializeField, Range(0.01f, 0.5f)] private float _shineThickness = 0.08f;
        [SerializeField] private Color _shineColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField, Range(0f, 89f)] private float _shineAngle = 30f;
        [SerializeField, Range(0f, 1f)] private float _shineSoftness = 0.3f;

        [Header("Animation")]
        [SerializeField, Min(0.1f)] private float _duration = 0.8f;
        [SerializeField] private AnimationCurve _easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Multi-Shine")]
        [SerializeField, Range(1, 5)] private int _shineCount = 1;
        [SerializeField, Range(0f, 0.5f)] private float _gapBetweenShines = 0.1f;

        [Header("Repeat")]
        [SerializeField] private bool _loopEndlessly;
        [SerializeField, Min(0)] private int _repeatCount = 0;
        [SerializeField, Min(0f)] private float _delayBetweenRepeats = 1f;

        public float ShineThickness => _shineThickness;
        public Color ShineColor => _shineColor;
        public float ShineAngle => _shineAngle;
        public float ShineSoftness => _shineSoftness;
        public float Duration => _duration;
        public AnimationCurve EaseCurve => _easeCurve;
        public int ShineCount => _shineCount;
        public float GapBetweenShines => _gapBetweenShines;
        public bool LoopEndlessly => _loopEndlessly;
        public int RepeatCount => _repeatCount;
        public float DelayBetweenRepeats => _delayBetweenRepeats;
    }
}