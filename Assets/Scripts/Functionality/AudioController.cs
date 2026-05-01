using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using DG.Tweening;
using Best.SocketIO;

public class AudioController : MonoBehaviour
{
    [SerializeField] private AudioSource bg_adudio;
    [SerializeField] internal AudioSource audioPlayer_wl;
    [SerializeField] internal AudioSource audioPlayer_button;
    [SerializeField] internal AudioSource audioSpin_button;
    [SerializeField] private AudioClip[] clips;
    [SerializeField] private AudioClip[] Bonusclips;
    [SerializeField] private AudioSource bg_audioBonus;
    [SerializeField] private AudioSource audioPlayer_Bonus;

    private void Start()
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (bg_adudio) bg_adudio.Play();
        if (audioPlayer_button && clips != null && clips.Length >= 1) audioPlayer_button.clip = clips[clips.Length - 1];
        if (audioSpin_button && clips != null && clips.Length >= 2) audioSpin_button.clip = clips[clips.Length - 2];
    }

    internal void CheckFocusFunction(bool focus, bool IsSpinning)
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!focus)
        {
            if (bg_adudio) bg_adudio.Pause();
            if (audioPlayer_wl) audioPlayer_wl.Pause();
            if (audioPlayer_button) audioPlayer_button.Pause();
        }
        else
        {
            if (bg_adudio && !bg_adudio.mute) bg_adudio.UnPause();
            if (IsSpinning)
            {
                if (audioPlayer_wl && !audioPlayer_wl.mute) audioPlayer_wl.UnPause();
            }
            else
            {
                StopWLAaudio();
            }
            if (audioPlayer_button && !audioPlayer_button.mute) audioPlayer_button.UnPause();
        }
    }

    internal void SwitchBGSound(bool isbonus)
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (isbonus)
        {
            if (bg_audioBonus) bg_audioBonus.enabled = true;
            if (bg_adudio) bg_adudio.enabled = false;
        }
        else
        {
            if (bg_audioBonus) bg_audioBonus.enabled = false;
            if (bg_adudio) bg_adudio.enabled = true;
        }
    }

    internal void PlayWLAudio(string type)
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!audioPlayer_wl || clips == null || clips.Length == 0) return;
        audioPlayer_wl.loop = false;
        int index = 0;
        switch (type)
        {
            case "spin":
                index = 0;
                audioPlayer_wl.loop = true;
                break;
            case "win":
                index = 1;
                break;
            case "lose":
                index = 2;
                break;
            case "spinStop":
                index = 3;
                break;
            case "megaWin":
                index = 4;
                break;
        }
        StopWLAaudio();
        audioPlayer_wl.clip = clips[index];
        audioPlayer_wl.Play();
    }

    internal void PlayBonusAudio(string type)
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!audioPlayer_Bonus || Bonusclips == null || Bonusclips.Length == 0) return;
        audioPlayer_wl.loop = false;
        int index = 0;
        switch (type)
        {
            case "win":
                index = 0;
                break;
            case "lose":
                index = 1;
                break;
            case "cycleSpin":
                index = 2;
                break;
        }
        StopBonusAaudio();
        audioPlayer_Bonus.clip = Bonusclips[index];
        audioPlayer_Bonus.Play();
    }

    internal void PlayButtonAudio()
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!audioPlayer_button) return;
        audioPlayer_button.Play();
    }

    internal void PlaySpinButtonAudio()
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!audioSpin_button) return;
        audioSpin_button.Play();
    }

    internal void StopWLAaudio()
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!audioPlayer_wl) return;
        audioPlayer_wl.Stop();
        audioPlayer_wl.loop = false;
    }

    internal void StopBonusAaudio()
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!audioPlayer_Bonus) return;
        audioPlayer_Bonus.Stop();
        audioPlayer_Bonus.loop = false;
    }

    internal void StopBgAudio()
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        if (!bg_adudio) return;
        bg_adudio.Stop();
    }

    internal void ToggleMute(bool toggle, string type = "all")
    {
        // NULL GUARDS SINCE I DON'T HAVE AUDIO ASSETS YET. REMOVE ONCE AUDIO ASSETS ARE ADDED
        switch (type)
        {
            case "bg":
                if (bg_adudio) bg_adudio.mute = toggle;
                if (bg_audioBonus) bg_audioBonus.mute = toggle;
                break;
            case "button":
                if (audioPlayer_button) audioPlayer_button.mute = toggle;
                if (audioSpin_button) audioSpin_button.mute = toggle;
                break;
            case "wl":
                if (audioPlayer_wl) audioPlayer_wl.mute = toggle;
                if (audioPlayer_Bonus) audioPlayer_Bonus.mute = toggle;
                break;
            case "all":
                if (audioPlayer_wl) audioPlayer_wl.mute = toggle;
                if (bg_adudio) bg_adudio.mute = toggle;
                if (audioPlayer_button) audioPlayer_button.mute = toggle;
                if (audioSpin_button) audioSpin_button.mute = toggle;
                break;
        }
    }
}
