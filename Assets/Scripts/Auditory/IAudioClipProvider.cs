using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Outsiders.Auditory
{
    public interface IAudioClipProvider
    {
        UniTask<AudioClip> LoadClipAsync(AudioEventSO evt);
        UniTask PreloadAsync(AudioEventSO evt); // optional
    }
}