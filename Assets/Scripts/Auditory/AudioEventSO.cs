using UnityEngine;

namespace Outsiders.Auditory
{
    [CreateAssetMenu(menuName = "Outsiders/Audio/Audio Event", fileName = "AudioEvent_")]
    public class AudioEventSO : ScriptableObject
    {
        public AudioChannel Channel = AudioChannel.SFX;
        public AudioClip[] Clips; // one or many variants
        [Range(0f, 1f)] public float Volume = 1f;
        public float PitchMin = 1f;
        public float PitchMax = 1f;
        public bool RandomizePitch = false;
        public bool Loop = false; // used for e.g. ambience
        [Range(0f, 1f)] public float SpatialBlend = 0f; // 0 = UI/2D, 1 = 3D
        public float MaxDist = 50f;
        public bool StreamFromDisk = false; // hint for loader
                                            // Optional: priority or tags
        public int Priority = 0;

        public AudioClip GetRandomClip()
        {
            if (Clips == null || Clips.Length == 0) return null;
            if (Clips.Length == 1) return Clips[0];
            return Clips[Random.Range(0, Clips.Length)];
        }

        public float GetRandomPitch()
        {
            if (!RandomizePitch) return PitchMin;
            return Random.Range(PitchMin, PitchMax);
        }
    }

}