using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Audio;

namespace Outsiders.Auditory
{
    public class AudioManager : MonoBehaviour, IAudioService
    {
        public static AudioManager Instance { get; private set; }
        [SerializeField] private AudioMixerGroup _masterGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _voGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private AudioMixerGroup _bgmGroup;
        [SerializeField] private int _sfxPoolCount = 4;
        [SerializeField] private int _voPoolCount = 2;
        [SerializeField] private int _uiPoolCount = 2;

        [SerializeField] AudioSource _bgmSource;
        [SerializeField] float _defaultFade = 0.5f;

        IAudioClipProvider _provider;
        private AudioSourcePool _sfxPool;
        private AudioSourcePool _voPool;
        private AudioSourcePool _uiPool;

        private AudioSource _bgmA;
        private AudioSource _bgmB;
        [SerializeField] private AudioSource _reusableSfxSource;
        bool _bgmAActive = true;

        Transform _poolParent;

        private readonly Dictionary<AudioEventSO, List<AudioSource>> _sfxByEvent = new();
        private readonly Dictionary<AudioSource, CancellationTokenSource> _sfxReleaseTokens = new();
        private readonly Dictionary<AudioSource, AudioEventSO> _bgmBySource = new();
        private readonly Dictionary<AudioSource, int> _bgmFadeVersions = new();

        private readonly Dictionary<AudioSource, CancellationTokenSource> _bgmFadeTokens = new();

        private readonly Dictionary<AudioSource, CancellationTokenSource> _uiReleaseTokens = new();

        [SerializeField] private AudioSource _voiceSource;

        private async void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            _poolParent = new GameObject("AudioPools").transform;
            _poolParent.parent = transform;
            await InitializeAsync(new ResourcesAudioClipProvider());
        }

        public async UniTask InitializeAsync(IAudioClipProvider provider)
        {
            _provider = provider;

            _sfxPool = new AudioSourcePool(_poolParent, _sfxGroup, _sfxPoolCount, spatialDefault: false);
            _voPool = new AudioSourcePool(_poolParent, _voGroup, _voPoolCount, spatialDefault: false);
            _uiPool = new AudioSourcePool(_poolParent, _uiGroup, _uiPoolCount, spatialDefault: false);
            _bgmA = CreateBgmSource("BGM_A");
            _bgmB = CreateBgmSource("BGM_B");
            await UniTask.CompletedTask;
        }

        private AudioSource CreateBgmSource(string name)
        {
            var go = new GameObject(name);
            go.transform.parent = transform;
            var s = go.AddComponent<AudioSource>();
            s.outputAudioMixerGroup = _bgmGroup;
            s.loop = true;
            s.playOnAwake = false;
            return s;
        }

        public async UniTask PreloadAsync(AudioEventSO[] events)
        {
            if (_provider == null) Debug.Log($"Audiomanager not initializled");
            if (events == null) return;
            foreach (var item in events)
                await _provider.PreloadAsync(item);
        }

        public AudioHandle PlaySFX(AudioEventSO evt, Vector3? worldPos = null, float? overrideVolume = null, int priority = 0)
        {
            if (evt == null) return null;
            var clip = evt.GetRandomClip();
            if (clip == null)
            {
                Debug.LogWarning($"AudioEvent {evt.name} has no clips assigned");
                return null;
            }

            var src = _sfxPool.Get(clip, spatial: evt.SpatialBlend > 0.5f, priority: priority);
            src.transform.position = worldPos ?? Camera.main?.transform.position ?? Vector3.zero;
            src.volume = (overrideVolume ?? evt.Volume);
            src.pitch = evt.GetRandomPitch();
            src.spatialBlend = evt.SpatialBlend;
            src.maxDistance = evt.MaxDist;
            src.loop = evt.Loop;
            src.Play();

            // Track this source under the event
            if (!_sfxByEvent.TryGetValue(evt, out var list))
            {
                list = new List<AudioSource>();
                _sfxByEvent[evt] = list;
            }
            list.Add(src);

            // If it's NOT looping, schedule an auto-release with a token so we can cancel if stopped early
            if (!evt.Loop)
            {
                var cts = new CancellationTokenSource();
                _sfxReleaseTokens[src] = cts;
                ReleaseAfterDoneAsync(src, clip.length, _sfxPool, evt, cts.Token).Forget();
            }

            return new AudioHandle
            {
                Source = src,
                Manager = this
            };
        }

        private async UniTask ReleaseAfterDoneAsync(AudioSource src, float clipLength, AudioSourcePool pool, AudioEventSO evt, CancellationToken token)
        {
            if (clipLength <= 0f) clipLength = 0.1f;

            try
            {
                await UniTask.Delay(TimeSpan.FromSeconds(clipLength), cancellationToken: token);
            }
            catch (OperationCanceledException)
            {
                // Stopped early; just exit.
                return;
            }

            // Cleanup tracking and release
            if (src == null) return;

            if (_sfxReleaseTokens.TryGetValue(src, out var cts))
            {
                cts.Dispose();
                _sfxReleaseTokens.Remove(src);
            }

            if (evt != null && _sfxByEvent.TryGetValue(evt, out var list))
            {
                list.Remove(src);
                if (list.Count == 0) _sfxByEvent.Remove(evt);
            }

            pool.Release(src);
        }

        /// <summary>
        /// Stops the most recently started instance of this AudioEventSO (if any),
        /// cancels its scheduled release, and returns it to the pool.
        /// </summary>
        public void StopSFX(AudioEventSO evt)
        {
            if (evt == null) return;
            if (!_sfxByEvent.TryGetValue(evt, out var list) || list.Count == 0) return;

            var src = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            if (list.Count == 0) _sfxByEvent.Remove(evt);

            if (src != null)
            {
                if (_sfxReleaseTokens.TryGetValue(src, out var cts))
                {
                    cts.Cancel();
                    cts.Dispose();
                    _sfxReleaseTokens.Remove(src);
                }

                src.Stop();
                _sfxPool.Release(src);
            }
        }

        public void PlayUI(AudioEventSO evt, float? overrideVolume = null)
        {
            if (evt == null) return;
            var clip = evt.GetRandomClip();
            if (clip == null) return;

            var src = _uiPool.Get(clip, spatial: false, priority: evt.Priority);
            src.volume = (overrideVolume ?? evt.Volume);
            src.pitch = evt.GetRandomPitch();
            src.loop = evt.Loop;
            src.Play();

            var cts = new CancellationTokenSource();
            _uiReleaseTokens[src] = cts;
            ReleaseAfterDoneAsync(src, clip.length, _uiPool, evt, cts.Token).Forget();
        }

        public void PlaySFX(AudioClip clip, float volume = 0f)
        {
            var src = _sfxPool.Get(clip);
            if (volume != 0f)
                src.volume = volume;
            src.Play();
            ReleaseAfterDoneAsync(src, clip.length, _sfxPool).Forget();
        }

        public void PlayVoice(AudioClip clip, float volume = 0f)
        {
            _voiceSource.Stop();
            if (volume != 0f)
                _voiceSource.volume = volume;
            _voiceSource.clip = clip;
            _voiceSource.Play();
        }

        public void SetVoiceVolume(float vol = 0f)
        {
            _voiceSource.volume = vol > 0f ? vol : 0f;
        }

        public void PlayReusableSFX(AudioClip clip)
        {
            if (clip == null || _reusableSfxSource == null) return;
            _reusableSfxSource.Stop();
            _reusableSfxSource.clip = clip;
            _reusableSfxSource.Play();
        }

        public void PlayReusableSFXEvent(AudioEventSO evt)
        {
            if (evt == null || _reusableSfxSource == null) return;
            var clip = evt.GetRandomClip();
            if (clip == null) return;
            _reusableSfxSource.Stop();
            _reusableSfxSource.clip = clip;
            _reusableSfxSource.volume = evt.Volume;
            _reusableSfxSource.Play();
        }

        async UniTask ReleaseAfterDoneAsync(AudioSource src, float clipLength, AudioSourcePool pool)
        {
            if (clipLength <= 0f) clipLength = 0.1f;
            await UniTask.Delay(TimeSpan.FromSeconds(clipLength), ignoreTimeScale: false);
            pool.Release(src);
        }

        // private async UniTask ReleaseAfterDoneAsync(AudioSource src, float clipLength, AudioSourcePool pool, AudioEventSO evt, CancellationToken token)
        // {
        //     if (clipLength <= 0f) clipLength = 0.1f;
        //
        //     try
        //     {
        //         await UniTask.Delay(TimeSpan.FromSeconds(clipLength), cancellationToken: token);
        //     }
        //     catch (OperationCanceledException)
        //     {
        //         return;
        //     }
        //
        //     if (src == null) return;
        //
        //     if (_uiReleaseTokens.TryGetValue(src, out var cts))
        //     {
        //         cts.Dispose();
        //         _uiReleaseTokens.Remove(src);
        //     }
        //
        //     pool.Release(src);
        // }

        /// <summary>
        /// Assign an AudioEventSO to BGM A or B (configures clip, volume, pitch, loop, etc.),
        /// but does NOT start playback.
        /// </summary>
        public bool AssignBGM(AudioEventSO evt, bool toA, float? overrideVolume = null, bool force2D = true)
        {
            if (evt == null)
            {
                Debug.LogWarning("AssignBGM: evt is null");
                return false;
            }

            var clip = evt.GetRandomClip();
            if (clip == null)
            {
                Debug.LogWarning($"AssignBGM: {evt.name} has no clips.");
                return false;
            }

            var src = toA ? _bgmA : _bgmB;

            // Configure from event
            src.clip = clip;
            src.loop = evt.Loop;
            src.pitch = evt.GetRandomPitch();
            src.spatialBlend = force2D ? 0f : Mathf.Clamp01(evt.SpatialBlend);

            // IMPORTANT: set source volume from event (or override)
            src.volume = Mathf.Clamp01(overrideVolume ?? evt.Volume);

            // Track which event is assigned to this source
            _bgmBySource[src] = evt;
            return true;
        }

        /// <summary>
        /// Plays the currently assigned clip on A or B and crossfades from the other source.
        /// Uses the source's configured volume (from AssignBGM) as the fade target.
        /// </summary>
        public void PlayAssignedBGM(bool useA, float fadeTime = -1f)
        {
            var src = useA ? _bgmA : _bgmB;
            var other = useA ? _bgmB : _bgmA;

            if (src.clip == null)
            {
                Debug.LogWarning("PlayAssignedBGM: No clip assigned on the target source.");
                return;
            }

            float fade = (fadeTime >= 0f) ? fadeTime : _defaultFade;
            float targetVol = Mathf.Clamp01(src.volume); // we already assigned this in AssignBGM

            // Start at 0, then fade up to the target volume configured from the event
            src.volume = 0f;
            src.Play();
            FadeInTo(src, targetVol, fade, CreateBgmFadeToken(src)).Forget();

            // Fade out the other source to 0 and stop it
            FadeOutToStop(other, fade, CreateBgmFadeToken(other)).Forget();

            _bgmAActive = useA;
        }

        /// <summary>
        /// Convenience: assign and immediately crossfade to the event (skips a separate Assign call).
        /// </summary>
        public void PlayBGM(AudioEventSO evt, bool useA = true, float fadeTime = -1f, float? overrideVolume = null, bool force2D = true)
        {
            PlayBGMIfNotPlaying(evt, useA, fadeTime, overrideVolume, force2D);
        }

        /// <summary>
        /// Starts the BGM event only if that event is not already playing on a BGM source.
        /// Returns true when a new play was started, false when skipped or invalid.
        /// </summary>
        public bool PlayBGMIfNotPlaying(AudioEventSO evt, bool useA = true, float fadeTime = -1f, float? overrideVolume = null, bool force2D = true)
        {
            if (evt == null)
            {
                Debug.LogWarning("PlayBGM: evt is null.");
                return false;
            }

            if (IsBGMPlaying(evt))
                return false;

            if (!AssignBGM(evt, toA: useA, overrideVolume: overrideVolume, force2D: force2D))
                return false;

            PlayAssignedBGM(useA, fadeTime);
            return true;
        }

        public bool IsBGMPlaying(AudioEventSO evt)
        {
            if (evt == null)
                return false;

            foreach (var kvp in _bgmBySource)
            {
                if (kvp.Value == evt && kvp.Key != null && kvp.Key.isPlaying)
                    return true;
            }

            return false;
        }

        private async UniTask FadeInTo(AudioSource src, float targetVolume, float duration, CancellationToken token)
        {
            if (src == null) return;

            if (duration <= 0f)
            {
                src.volume = targetVolume;
                ClearBgmFadeToken(src, token);
                return;
            }

            float start = src.volume;
            float t = 0f;

            try
            {
                while (t < duration)
                {
                    token.ThrowIfCancellationRequested();
                    t += Time.unscaledDeltaTime;
                    src.volume = Mathf.Lerp(start, targetVolume, t / duration);
                    await UniTask.Yield(token);
                }

                src.volume = targetVolume;
                ClearBgmFadeToken(src, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private async UniTask FadeOutToStop(AudioSource src, float duration, CancellationToken token)
        {
            if (src == null)
                return;

            if (!src.isPlaying)
            {
                _bgmBySource.Remove(src);
                ClearBgmFadeToken(src, token);
                return;
            }

            if (duration <= 0f)
            {
                src.Stop();
                src.volume = 0f;
                _bgmBySource.Remove(src);
                ClearBgmFadeToken(src, token);
                return;
            }

            float start = src.volume;
            float t = 0f;

            try
            {
                while (t < duration)
                {
                    token.ThrowIfCancellationRequested();
                    t += Time.unscaledDeltaTime;
                    src.volume = Mathf.Lerp(start, 0f, t / duration);
                    await UniTask.Yield(token);
                }

                src.Stop();
                src.volume = 0f;
                _bgmBySource.Remove(src);
                ClearBgmFadeToken(src, token);
            }
            catch (OperationCanceledException)
            {
            }
        }

        private int NextBgmFadeVersion(AudioSource src)
        {
            if (src == null) return 0;

            _bgmFadeVersions.TryGetValue(src, out int version);
            version++;
            _bgmFadeVersions[src] = version;
            return version;
        }

        private bool IsCurrentBgmFade(AudioSource src, int fadeVersion)
        {
            return src != null &&
                   _bgmFadeVersions.TryGetValue(src, out int currentVersion) &&
                   currentVersion == fadeVersion;
        }

        // Public: fade out a specific BGM (A if fadeOutA = true, else B).
        public void FadeOutBGM(bool fadeOutA, float fadeTime = -1f)
        {
            var src = fadeOutA ? _bgmA : _bgmB;
            if (src == null) return;

            float dur = (fadeTime >= 0f) ? fadeTime : _defaultFade;

            FadeOutToStop(src, dur, CreateBgmFadeToken(src)).Forget();
        }

        public void StopBGMFromEditor(AudioEventSO evt)
        {
            if (evt == null) return;
            StopBGM(evt);
        }
        
        /// <summary>
        /// Stops the BGM source that is currently playing the specified AudioEventSO (if any),
        /// fading it out gracefully.
        /// </summary>
        public void StopBGM(AudioEventSO evt, float fadeTime = -1f)
        {
            if (evt == null) return;

            AudioSource targetSrc = GetBGMSource(evt);
            if (targetSrc == null) return;

            float dur = (fadeTime > 0f) ? fadeTime : _defaultFade;
            FadeOutToStop(targetSrc, dur, CreateBgmFadeToken(targetSrc)).Forget();
        }

        public float GetBGMVolume(AudioEventSO evt)
        {
            AudioSource source = GetBGMSource(evt);
            if (source == null)
                return evt != null ? Mathf.Clamp01(evt.Volume) : 0f;

            return Mathf.Clamp01(source.volume);
        }

        public void SetBGMVolume(AudioEventSO evt, float volume)
        {
            AudioSource source = GetBGMSource(evt);
            if (source == null)
                return;

            CancelBgmFade(source);
            source.volume = Mathf.Clamp01(volume);
        }

        private AudioSource GetBGMSource(AudioEventSO evt)
        {
            if (evt == null)
                return null;

            AudioSource fallback = null;
            foreach (var kvp in _bgmBySource)
            {
                if (kvp.Value != evt)
                    continue;

                if (kvp.Key != null && kvp.Key.isPlaying)
                    return kvp.Key;

                fallback ??= kvp.Key;
            }

            return fallback;
        }
        
        public void Stop(AudioHandle handle)
        {
            if (handle == null || handle.Source == null)
                return;

            var src = handle.Source;

            if (_sfxReleaseTokens.TryGetValue(src, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
                _sfxReleaseTokens.Remove(src);
            }

            src.Stop();
            _sfxPool.Release(src);

            handle.Source = null;
        }

        /// <summary>
        /// Stops all currently playing BGM sources (A and B) with a fade out.
        /// </summary>
        public void StopAllBGM(float fadeTime = -1f)
        {
            float dur = (fadeTime >= 0f) ? fadeTime : _defaultFade;

            if (_bgmA != null && _bgmA.isPlaying)
                FadeOutToStop(_bgmA, dur, CreateBgmFadeToken(_bgmA)).Forget();

            if (_bgmB != null && _bgmB.isPlaying)
                FadeOutToStop(_bgmB, dur, CreateBgmFadeToken(_bgmB)).Forget();
        }

        /// <summary>
        /// Stops all currently playing SFX sounds immediately and returns them to the pool.
        /// </summary>
        public void StopAllSFX()
        {
            // Cancel all pending release tokens
            foreach (var kvp in _sfxReleaseTokens)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _sfxReleaseTokens.Clear();

            // Stop and release all tracked sources
            foreach (var kvp in _sfxByEvent)
            {
                foreach (var src in kvp.Value)
                {
                    if (src != null)
                    {
                        src.Stop();
                        _sfxPool.Release(src);
                    }
                }
            }
            _sfxByEvent.Clear();
        }

        /// <summary>
        /// Stops all currently playing UI sounds immediately and returns them to the pool.
        /// </summary>
        public void StopAllUI()
        {
            // Cancel all pending release tokens
            foreach (var kvp in _uiReleaseTokens)
            {
                kvp.Value.Cancel();
                kvp.Value.Dispose();
            }
            _uiReleaseTokens.Clear();

            // Release all active UI sources from the pool
            _uiPool.ReleaseAll();
        }

        /// <summary>
        /// Stops all audio (BGM, SFX, UI, Voice, Reusable SFX) immediately.
        /// BGM fades out, everything else stops instantly.
        /// </summary>
        public void StopAll(float bgmFadeTime = -1f)
        {
            StopAllBGM(bgmFadeTime);
            StopAllSFX();
            StopAllUI();

            // Also stop voice and reusable SFX
            if (_voiceSource != null)
                _voiceSource.Stop();

            if (_reusableSfxSource != null)
                _reusableSfxSource.Stop();
        }

        private CancellationToken CreateBgmFadeToken(AudioSource source)
        {
            CancelBgmFade(source);

            if (source == null)
                return CancellationToken.None;

            CancellationTokenSource cts = new CancellationTokenSource();
            _bgmFadeTokens[source] = cts;
            return cts.Token;
        }

        private void CancelBgmFade(AudioSource source)
        {
            if (source == null || !_bgmFadeTokens.TryGetValue(source, out var cts))
                return;

            cts.Cancel();
            cts.Dispose();
            _bgmFadeTokens.Remove(source);
        }

        private void ClearBgmFadeToken(AudioSource source, CancellationToken token)
        {
            if (source == null || !_bgmFadeTokens.TryGetValue(source, out var cts) || cts.Token != token)
                return;

            cts.Dispose();
            _bgmFadeTokens.Remove(source);
        }
    }
    
    public class AudioHandle
    {
        internal AudioSource Source;
        internal AudioManager Manager;

        public bool IsValid =>
            Source != null;

        public void Stop()
        {
            Manager?.Stop(this);
        }
    }
}
