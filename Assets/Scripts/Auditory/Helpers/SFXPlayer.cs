using UnityEngine;

namespace Outsiders.Auditory.Helpers
{
    public class SFXPlayer : MonoBehaviour
    {
        public AudioClip Clip;
        public AudioEventSO AudioEventSO;

        public void Play()
        {
            if (AudioEventSO != null)
            {
                AudioManager.Instance.PlaySFX(AudioEventSO);
                return;
            }

            PlaySetSFX();
        }

        public void PlaySetSFX()
        {
            if (Clip == null)
            {
                Debug.Log($"Clip is unassigned", this);
                return;
            }
            AudioManager.Instance.PlaySFX(Clip);
        }

        public void PlaySetSFXFromSO()
        {
            if (AudioEventSO == null)
            {
                Debug.Log($"AudioEventSO is unassigned", this);
                return;
            }
            AudioManager.Instance.PlaySFX(AudioEventSO);
        }

        public void StopSetSFXFromSO()
        {
            if (AudioEventSO == null)
            {
                Debug.Log($"AudioEventSO is unassigned", this);
                return;
            }
            AudioManager.Instance.StopSFX(AudioEventSO);
        }
    }
}
