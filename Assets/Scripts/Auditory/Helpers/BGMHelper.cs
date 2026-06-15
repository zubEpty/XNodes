using UnityEngine;

namespace Outsiders.Auditory.Helpers
{
    public class BGMHelper : MonoBehaviour
    {
        [SerializeField] private AudioEventSO _trackA;
        [SerializeField] private AudioEventSO _trackB;
        [SerializeField] private float _fadeTime = 1f;

        private AudioEventSO _currentTrack;
        private bool _useAForNextTrack = true;

        [ContextMenu("Play Track A")]
        public void PlayTrackA()
        {
            AudioManager.Instance?.PlayBGM(_trackA, useA: true, fadeTime: _fadeTime);
            //Debug.Log($"Playing {_trackA.name}");
        }

        [ContextMenu("Play Track B")]
        public void PlayTrackB()
        {
            AudioManager.Instance?.PlayBGM(_trackB, useA: false, fadeTime: _fadeTime);
            //Debug.Log($"Playing {_trackB.name}");

        }

        [ContextMenu("Fade Out A (stop)")]
        public void FadeOutA_Stop()
        {
            AudioManager.Instance?.FadeOutBGM(fadeOutA: true, fadeTime: _fadeTime);
        }

        [ContextMenu("Fade Out B (stop)")]
        public void FadeOutB_Stop()
        {
            AudioManager.Instance?.FadeOutBGM(fadeOutA: false, fadeTime: _fadeTime);
        }

        public void PlayTrack(AudioEventSO track, bool forceReplay = false)
        {
            if (track == null || (!forceReplay && track == _currentTrack))
            {
                return;
            }

            AudioManager.Instance?.PlayBGM(track, useA: _useAForNextTrack, fadeTime: _fadeTime);
            _currentTrack = track;
            _useAForNextTrack = !_useAForNextTrack;
        }

        public void StopTrack(AudioEventSO track)
        {
            if (track == null)
            {
                return;
            }

            AudioManager.Instance?.StopBGM(track, _fadeTime);

            if (track == _currentTrack)
            {
                _currentTrack = null;
            }
        }

        public void StopCurrentTrack()
        {
            StopTrack(_currentTrack);
        }
    }
}
