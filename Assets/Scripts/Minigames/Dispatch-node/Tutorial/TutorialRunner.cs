using Cysharp.Threading.Tasks;
using Outsiders.SimpleTutorial;
using System.Collections.Generic;
using UnityEngine;

namespace XR23.Tutorial.System
{
    public sealed class TutorialRunner : MonoBehaviour
    {
        [SerializeField] private List<TutorialStep> _steps;

        private int _currentStepIndex = -1;

        private void Start()
        {
            Initialize();
            PlayNextStep().Forget();
        }

        private void Initialize()
        {
            foreach (var step in _steps)
            {
                step.Deactivate();
                step.OnStepCompleted += HandleStepCompleted;
            }
        }

        private async UniTaskVoid PlayNextStep()
        {
            _currentStepIndex++;

            if (_currentStepIndex >= _steps.Count)
            {
                CompleteTutorial();
                return;
            }

            var step = _steps[_currentStepIndex];
            step.Activate();

            await UniTask.CompletedTask;
        }

        private void HandleStepCompleted(TutorialStep completedStep)
        {
            completedStep.Deactivate();
            PlayNextStep().Forget();
        }

        private void CompleteTutorial()
        {
            Debug.Log("Tutorial completed.");
        }

        private void OnDestroy()
        {
            foreach (var step in _steps)
            {
                step.OnStepCompleted -= HandleStepCompleted;
            }
        }
    }
}