using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Outsiders.Auditory
{
    public interface IAudioService
    {
        UniTask InitializeAsync(IAudioClipProvider provider);
        AudioHandle PlaySFX(AudioEventSO evt, Vector3? worldPos = null, float? overrideVolume = null, int priority = 0);
        void PlayUI(AudioEventSO evt, float? overrideVolume = null);
        //UniTask PlayBGMAsync(AudioEventSO evt, float fadeSeconds = 1f);
        //UniTask StopBGMAsync(float fadeSeconds = 1f);
        //void SetChannelVolume(AudioChannel channel, float linear01); // 0..1
        //float GetChannelVolume(AudioChannel channel);
        UniTask PreloadAsync(AudioEventSO[] events);
    }
}