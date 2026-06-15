using UnityEngine;
using UnityEngine.UI;

namespace Outsiders.Auditory
{
    public class UIAudioButton : MonoBehaviour
    {
        public AudioEventSO ClickSound;
        private Button _btn;

        private void Awake()
        {
            _btn = GetComponent<Button>();
            _btn.onClick.AddListener(PlayOnClick);
        }

        private void OnDestroy()
        {
            if (_btn != null) _btn.onClick.RemoveListener(PlayOnClick);
        }

        private void PlayOnClick()
        {
            if (ClickSound == null) return;
            AudioManager.Instance.PlayUI(ClickSound);
        }
    }
}