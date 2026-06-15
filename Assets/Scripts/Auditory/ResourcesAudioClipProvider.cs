namespace Outsiders.Auditory
{
    using Cysharp.Threading.Tasks;
    using UnityEngine;

    public class ResourcesAudioClipProvider : IAudioClipProvider
    {
        public UniTask<AudioClip> LoadClipAsync(AudioEventSO evt)
        {
            var clip = evt.GetRandomClip();
            return UniTask.FromResult(clip);
        }
        public UniTask PreloadAsync(AudioEventSO evt)
        {
            // assuming we have the clips provided in inspector
            return UniTask.CompletedTask;
        }
    }
}