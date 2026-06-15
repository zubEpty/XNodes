using UnityEngine;

namespace Outsiders.Auditory.Helpers
{
    public class ResumableWrapper : MonoBehaviour
    {
        public AudioEventSO AudioEvent;

        [Tooltip("Which BGM source slot to use (A or B) for crossfade.")]
        public bool UseSlotA = true;

        [Tooltip("Crossfade duration in seconds. -1 uses AudioManager default.")]
        public float FadeTime = -1f;

        private float _volumeBeforePause;
        private bool _paused;

        /// <summary>
        /// If paused, restores volume to resume playback.
        /// Otherwise starts BGM only when it is not already playing.
        /// </summary>
        public void TryPlayEvent()
        {
            if (AudioEvent == null) return;

            if (_paused)
            {
                _paused = false;
                AudioManager.Instance.SetBGMVolume(AudioEvent, _volumeBeforePause);
                return;
            }

            AudioManager.Instance.PlayBGMIfNotPlaying(AudioEvent, UseSlotA, FadeTime);
        }

        public void TryPlayEvent(float? overrideVolume, bool force2D = true)
        {
            if (AudioEvent == null) return;

            if (_paused)
            {
                _paused = false;
                AudioManager.Instance.SetBGMVolume(AudioEvent, overrideVolume ?? _volumeBeforePause);
                return;
            }

            AudioManager.Instance.PlayBGMIfNotPlaying(AudioEvent, UseSlotA, FadeTime, overrideVolume, force2D);
        }

        public void PauseEvent()
        {
            if (_paused || AudioEvent == null) return;

            if (!AudioManager.Instance.IsBGMPlaying(AudioEvent)) return;

            _volumeBeforePause = AudioManager.Instance.GetBGMVolume(AudioEvent);
            if (_volumeBeforePause <= 0f) return; // not playing

            _paused = true;
            AudioManager.Instance.SetBGMVolume(AudioEvent, 0f);
        }

        public void StopEvent()
        {
            if (AudioEvent != null)
                AudioManager.Instance.StopBGM(AudioEvent, FadeTime);
            _paused = false;
        }

        public bool IsPlaying => AudioEvent != null && !_paused && AudioManager.Instance.IsBGMPlaying(AudioEvent);
        public bool IsPaused => _paused;

        void OnDestroy()
        {
            StopEvent();
        }
    }
}
