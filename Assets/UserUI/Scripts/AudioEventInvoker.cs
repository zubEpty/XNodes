using System;
using Outsiders.Auditory;
using UnityEngine;

public class AudioEventInvoker : MonoBehaviour
{
    [SerializeField] private bool playOnEnable = false;
    [SerializeField] private AudioChannel audioChannel;
    [SerializeField] private AudioEventSO audioEventToInvoke;
    
    [Tooltip("Assign only the voiceover clip here if necessary")]
    [SerializeField] private AudioClip voiceOverClip;

    private void OnEnable()
    {
        if(!playOnEnable)
            return;
        PlayAudioEvent();
    }

    public void PlayAudioEvent()
    {
        if (audioEventToInvoke == null)
        {
            Debug.LogWarning("AudioEventInvoker not assigned!");
            return;
        }
        switch (audioChannel)
        {
            case AudioChannel.SFX:
                Outsiders.Auditory.AudioManager.Instance.PlaySFX(audioEventToInvoke);
                break;
            case AudioChannel.BGM:
                Outsiders.Auditory.AudioManager.Instance.PlayBGM(audioEventToInvoke);
                break;
            case AudioChannel.UI:
                Outsiders.Auditory.AudioManager.Instance.PlayUI(audioEventToInvoke);
                break;
            case AudioChannel.Voice:
                Outsiders.Auditory.AudioManager.Instance.PlayVoice(voiceOverClip);
                break;
            case AudioChannel.Master:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void StopAudioEvent()
    {
        if (audioEventToInvoke == null)
        {
            Debug.LogWarning("AudioEventInvoker not assigned!");
            return;
        }
        switch (audioChannel)
        {
            case AudioChannel.SFX:
                Outsiders.Auditory.AudioManager.Instance.StopSFX(audioEventToInvoke);
                break;
            case AudioChannel.BGM:
                Outsiders.Auditory.AudioManager.Instance.StopBGMFromEditor(audioEventToInvoke);
                break;
            case AudioChannel.UI:
                Outsiders.Auditory.AudioManager.Instance.StopAllUI();
                break;
            case AudioChannel.Voice:
                Outsiders.Auditory.AudioManager.Instance.StopAll();
                break;
            case AudioChannel.Master:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}