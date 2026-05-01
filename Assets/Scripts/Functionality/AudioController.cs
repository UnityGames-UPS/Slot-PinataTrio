using UnityEngine;

public class AudioController : MonoBehaviour
{
    [SerializeField]
    private AudioSource bgAudio;
    [SerializeField]
    private AudioSource buttonAudio;
    [SerializeField]
    private AudioSource sfxAudio;

    [SerializeField]
    private AudioClip bgMusic;
    [SerializeField]
    private AudioClip buttonClickClip;

    private void Start()
    {
        if (bgAudio) bgAudio.Play();
    }

    internal void PlayButtonAudio()
    {
        if (buttonAudio && buttonClickClip)
        {
            buttonAudio.clip = buttonClickClip;
            buttonAudio.Play();
        }
    }

    internal void ToggleMute(bool mute, string type = "all")
    {
        switch (type)
        {
            case "bg":
                if (bgAudio) bgAudio.mute = mute;
                break;
            case "button":
                if (buttonAudio) buttonAudio.mute = mute;
                break;
            case "sfx":
                if (sfxAudio) sfxAudio.mute = mute;
                break;
            case "all":
                if (bgAudio) bgAudio.mute = mute;
                if (buttonAudio) buttonAudio.mute = mute;
                if (sfxAudio) sfxAudio.mute = mute;
                break;
        }
    }
}