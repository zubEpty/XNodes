using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Dispatch.Gameplay
{
public class DelayActionHandler : MonoBehaviour
{
    public UnityEvent OnDelayed;
    public float DelayTime;

    public void DoDelay()
    {
        DelayAsync().Forget();
    }

    private async UniTaskVoid DelayAsync()
    {
        if (DelayTime > 0f)
            await UniTask.Delay((int)(DelayTime * 1000f), cancellationToken: this.GetCancellationTokenOnDestroy());
        else
            await UniTask.Yield(this.GetCancellationTokenOnDestroy());

        OnDelayed?.Invoke();
    }
}
}
