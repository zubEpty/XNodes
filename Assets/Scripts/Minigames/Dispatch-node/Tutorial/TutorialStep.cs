using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Outsiders.SimpleTutorial
{
    public class TutorialStep : MonoBehaviour
    {
        [Header("Completion")]
        [SerializeField] private Button _completionButton;
        public event Action<TutorialStep> OnStepCompleted;
        public UnityEvent OnStepStart;
        public UnityEvent OnStepFinish;

        private void Awake()
        {
            if (_completionButton != null)
            {
                _completionButton.onClick.AddListener(HandleCompletion);
            }
        }

        private void OnDestroy()
        {
            if (_completionButton != null)
            {
                _completionButton.onClick.RemoveListener(HandleCompletion);
            }
        }
        public void Activate()
        {
            gameObject.SetActive(true);
            OnStepStart?.Invoke();
        }
        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        private void HandleCompletion()
        {
            OnStepCompleted?.Invoke(this);
            OnStepFinish?.Invoke();
        }

        public void Complete()
        {
            HandleCompletion();
        }
    }
}
