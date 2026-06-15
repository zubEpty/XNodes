using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Outsiders.Auditory
{
    public class AudioSourcePool
    {
        class PSource
        {
            public AudioSource Source;
            public bool InUse;
            public int Priorty;
            public double EndTime;
        }

        readonly Transform _parent;
        readonly AudioMixerGroup _mixerGroup;
        readonly List<PSource> _pool = new List<PSource>();
        readonly int _initialSize;
        readonly bool _spatialDefault;

        public AudioSourcePool(Transform parent, AudioMixerGroup mixerGroup, int initialSize = 8, bool spatialDefault = false)
        {
            _parent = parent;
            _mixerGroup = mixerGroup;
            _initialSize = Mathf.Max(1, initialSize);
            _spatialDefault = spatialDefault;
            Warmup();
        }

        private void Warmup()
        {
            for (int i = 0; i < _pool.Count; i++)
                _pool.Add(CreatePooledSource());
        }

        private PSource CreatePooledSource()
        {
            var go = new GameObject("Pooled Audio Source");
            go.transform.parent = _parent;
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.outputAudioMixerGroup = _mixerGroup;
            src.spatialBlend = _spatialDefault ? 1f : 0f;
            return new PSource { Source = src, InUse = false, Priorty = 0, EndTime = 0 };
        }

        public AudioSource Get(AudioClip clip, bool spatial = false, int priority = 0)
        {
            PSource free = null;
            foreach (var obj in _pool)
            {
                if (obj.InUse == false)
                {
                    free = obj; break;
                }
            }

            if (free == null)
            {
                free = CreatePooledSource();
                _pool.Add(free);
            }

            free.InUse = true;
            free.Priorty = priority;
            free.Source.clip = clip;
            free.Source.spatialBlend = spatial ? 1f : 0f;
            return free.Source;
        }

        public void Release(AudioSource s)
        {
            var p = _pool.Find(x => x.Source == s);
            if (p != null)
            {
                p.InUse = false;
                p.Priorty = 0;
                p.EndTime = 0;
                p.Source.clip = null;
                p.Source.loop = false;
            }
        }

        public void ReleaseAll()
        {
            foreach (var pSource in _pool)
            {
                if (pSource.Source.isPlaying) pSource.Source.Stop();
                pSource.InUse = false;
                pSource.Source.clip = null;
            }
        }
    }
}