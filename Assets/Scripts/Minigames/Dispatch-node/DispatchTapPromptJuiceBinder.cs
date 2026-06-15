using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Dispatch.Gameplay
{
    [DisallowMultipleComponent]
    public sealed class DispatchTapPromptJuiceBinder : MonoBehaviour
    {
        [SerializeField] private string targetNameToken = "goal";

        private readonly List<DispatchTapPromptBounce> prompts = new List<DispatchTapPromptBounce>();
        private readonly List<SequenceUnlockGate> gates = new List<SequenceUnlockGate>();
        private readonly List<KeyPickupNodeInteraction> keyPickups = new List<KeyPickupNodeInteraction>();

        private void OnDestroy()
        {
            Unbind();
        }

        public void Bind(DispatchLevel level)
        {
            Unbind();

            if (level == null)
                return;

            CachePromptButtons(level);
            if (prompts.Count == 0)
                return;

            gates.AddRange(level.GetComponentsInChildren<SequenceUnlockGate>(true));
            for (int i = 0; i < gates.Count; i++)
                gates[i].onSequenceSolved.AddListener(PlayPrompts);

            keyPickups.AddRange(level.GetComponentsInChildren<KeyPickupNodeInteraction>(true));
            for (int i = 0; i < keyPickups.Count; i++)
                keyPickups[i].OnKeyCollected += HandleKeyCollected;
        }

        private void CachePromptButtons(DispatchLevel level)
        {
            prompts.Clear();

            RectTransform[] rectTransforms = level.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectTransforms.Length; i++)
            {
                RectTransform rectTransform = rectTransforms[i];
                if (!IsPromptTarget(rectTransform))
                    continue;

                DispatchTapPromptBounce bounce = rectTransform.GetComponent<DispatchTapPromptBounce>();
                if (bounce == null)
                    bounce = rectTransform.gameObject.AddComponent<DispatchTapPromptBounce>();

                prompts.Add(bounce);
            }
        }

        private bool IsPromptTarget(RectTransform rectTransform)
        {
            if (rectTransform == null || !rectTransform.TryGetComponent(out Button button))
                return false;

            if (!string.IsNullOrEmpty(targetNameToken)
                && rectTransform.name.IndexOf(targetNameToken, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            return CallsCompletionPresenter(button);
        }

        private static bool CallsCompletionPresenter(Button button)
        {
            if (button == null)
                return false;

            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
            {
                if (button.onClick.GetPersistentTarget(i) is DispatchNodeCompletionPresenter
                    && string.Equals(button.onClick.GetPersistentMethodName(i), nameof(DispatchNodeCompletionPresenter.CompleteNode), StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void HandleKeyCollected(PlayerController player, string keyCode)
        {
            PlayPrompts();
        }

        private void PlayPrompts()
        {
            for (int i = 0; i < prompts.Count; i++)
                prompts[i].Play();
        }

        private void Unbind()
        {
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] != null)
                    gates[i].onSequenceSolved.RemoveListener(PlayPrompts);
            }

            for (int i = 0; i < keyPickups.Count; i++)
            {
                if (keyPickups[i] != null)
                    keyPickups[i].OnKeyCollected -= HandleKeyCollected;
            }

            gates.Clear();
            keyPickups.Clear();
            prompts.Clear();
        }
    }
}
