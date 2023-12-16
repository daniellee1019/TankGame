using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.Util;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : SingletonMonobehaviour<SoundManager>
{
    public const string MasterGroupName = "Master";
    public const string EffectGroupName = "Effect";
    public const string BGMGroupName = "BGM";
    public const string UIGroupName = "UI";
    public const string MixerName = "AudioMixer";
    public const string ContainerName = "SoundContainer";
    public const string FadeA = "FadeA";
    public const string FadeB = "FadeB";
    public const string UI = "UI";
    public const string EffectVolumeParam = "Volume_Effect";
    public const string BGMVolumeParam = "Volume_BGM";
    public const string UIVolumeParam = "Volume_UI";

    public enum MusicPlayingType
    {
        None = 0,
        SourceA = 1, // 페이드 관련
        SourceB = 2,
        AtoB = 3, // 배경음 관련
        BtoA = 4,
    }

    public AudioMixer mixer = null;
    public Transform audioRoot = null;
    public AudioSource fadeA_audio = null;
    public AudioSource fadeB_audio = null;
    public AudioSource[] effect_audios = null; // 많은 이펙트 사운드가 동시에 재생되면 오디오가 찢어지기에 이펙트 사운드 채널 개수를 제한을 두어 사전에 방지한다. 
    public AudioSource UI_audio = null;

    public float[] effect_PlayStartTime = null; //더이상 이펙트 사운드 채널이 추가될 공간이 없다면 가장 오래된 사운드를 끄고 new sound 재생 
    private int EffectChannelCount = 5;
    private MusicPlayingType currentPlayingType = MusicPlayingType.None;
    private bool isTricking = false;
    private SoundClip currentSound = null;
    private SoundClip lastSound = null;
    private float minVolume = -80.0f;
    private float maxVolume = 0.0f;


    void Start()
    {
        if(this.mixer == null)
        {
            this.mixer = Resources.Load(MixerName) as AudioMixer;
        }
        if( this.audioRoot == null)
        {
            audioRoot = new GameObject(ContainerName).transform;
            audioRoot.SetParent(transform);
            audioRoot.localPosition = Vector3.zero;
        }
        if(fadeA_audio == null)
        {
            GameObject fadeA = new GameObject(FadeA, typeof(AudioSource));
            fadeA.transform.SetParent(audioRoot);
            this.fadeA_audio = fadeA.GetComponent<AudioSource>(); // 사운드 재생.
            this.fadeA_audio.playOnAwake = false;
        }
        if(fadeB_audio == null)
        {
            GameObject fadeB = new GameObject(FadeB, typeof(AudioSource));
            fadeB.transform.SetParent(audioRoot);
            fadeB_audio = fadeB.GetComponent<AudioSource>();
            fadeB_audio.playOnAwake = false;
        }
        if(UI_audio == null)
        {
            GameObject ui = new GameObject(UI, typeof(AudioSource));
            ui.transform.SetParent(audioRoot);
            UI_audio = ui.GetComponent<AudioSource>();
            UI_audio.playOnAwake = false;
        }
        if(this.effect_audios == null || this.effect_audios.Length == 0)
        {
            this.effect_PlayStartTime = new float[EffectChannelCount];
            this.effect_audios = new AudioSource[EffectChannelCount];
            for(int i = 0; i < EffectChannelCount; i++)
            {
                effect_PlayStartTime[i] = 0.0f;
                GameObject effect = new GameObject("Effect" + i.ToString(), typeof(AudioSource));
                effect.transform.SetParent(audioRoot);
                this.effect_audios[i] = effect.GetComponent<AudioSource>();
                this.effect_audios[i].playOnAwake = false;
            }
        }
        if (this.mixer != null)
        {
            this.fadeA_audio.outputAudioMixerGroup = mixer.FindMatchingGroups(BGMGroupName)[0];
            this.fadeB_audio.outputAudioMixerGroup = mixer.FindMatchingGroups(BGMGroupName)[0];// 오디오 볼륨 조절.
            this.UI_audio.outputAudioMixerGroup = mixer.FindMatchingGroups(UIGroupName)[0];
            for (int i = 0; i < this.effect_audios.Length; i++) // 이펙트
            {
                this.effect_audios[i].outputAudioMixerGroup = mixer.FindMatchingGroups(EffectGroupName)[0];
            }
        }

        VolumeInit();

    }

    public void SetBGMVolume(float currentRatio)//현재비율
    {
        currentRatio = Mathf.Clamp01(currentRatio); // 0과 1 사이에 값을 정해줌.
        float volume = Mathf.Lerp(minVolume, maxVolume, currentRatio); // 보통 슬라이더바로 볼륨을 지정하기 때문에 퍼센트 비율로 설정.
        this.mixer.SetFloat(BGMVolumeParam, volume);
        PlayerPrefs.SetFloat(BGMVolumeParam, volume);
    }

    public float GetBGMVolume()
    {
        if (PlayerPrefs.HasKey(BGMVolumeParam))
        {
            return Mathf.Lerp(minVolume, maxVolume, PlayerPrefs.GetFloat(BGMVolumeParam));
        }
        else
        {
            return maxVolume;
        }
    }
    public void SetEffectVolume(float currentRatio)
    {
        currentRatio = Mathf.Clamp01(currentRatio);
        float volume = Mathf.Lerp(minVolume, maxVolume, currentRatio);
        this.mixer.SetFloat(EffectVolumeParam, volume);
        PlayerPrefs.SetFloat(EffectVolumeParam, volume);
    }

    public float GetEffectVolume()
    {
        if (PlayerPrefs.HasKey(EffectVolumeParam))
        {
            return Mathf.Lerp(minVolume, maxVolume, PlayerPrefs.GetFloat(EffectVolumeParam));
        }
        else
        {
            return maxVolume;
        }
    }
    public void SetUIVolume(float currentRatio)
    {
        currentRatio = Mathf.Clamp01(currentRatio);
        float volume = Mathf.Lerp(minVolume, maxVolume, currentRatio);
        this.mixer.SetFloat(UIVolumeParam, volume);
        PlayerPrefs.SetFloat(UIVolumeParam, volume);

    }
    public float GetUIVolume()
    {
        if (PlayerPrefs.HasKey(UIVolumeParam))
        {
            return Mathf.Lerp(minVolume, maxVolume, PlayerPrefs.GetFloat(UIVolumeParam));
        }
        else
        {
            return maxVolume;   
        }
    }
    /// <summary>
    /// 볼륨 초기화 함수
    /// </summary>
    void VolumeInit()
    {
        if(this.mixer != null)
        {
            this.mixer.SetFloat(BGMVolumeParam, GetBGMVolume());
            this.mixer.SetFloat(EffectVolumeParam, GetEffectVolume());
            this.mixer.SetFloat(UIVolumeParam, GetUIVolume());
        }
    }

    /// <summary>
    /// 이팩트, BGM, UI등 모든 오디오를 플레이하는 기본적인 기능
    /// </summary>
  
    void PlayAudioSource(AudioSource source, SoundClip clip, float volume)
    {
        if(source == null || clip == null)
        {
            return;
        }
        source.Stop();
        source.clip = clip.GetClip();
        source.volume = volume;
        source.loop = clip.isLoop;
        source.pitch = clip.pitch;
        source.dopplerLevel = clip.dopplerLevel;
        source.rolloffMode = clip.rolloffMode;
        source.minDistance = clip.minDIstance;
        source.maxDistance = clip.maxDistance;
        source.spatialBlend = clip.spatialBlend;
        source.Play();
    }
    /// <summary>
    /// 특정 포인트에서 재생되는 오디오 사운드를 위한 함수
    /// </summary>
    /// <param name="clip"></param>
    /// <param name="position"></param>
    /// <param name="volume"></param>
    void PlayAudioSourceAtPoint(SoundClip clip, Vector3 position, float volume)
    {
        AudioSource.PlayClipAtPoint(clip.GetClip(), position, volume);
    }

    public bool IsPlaying()
    {
        return (int)this.currentPlayingType > 0; // enum 형식. 즉 = type이 None만 아니면 재생중.
    }

    public bool IsDifferentSound(SoundClip clip)
    {
        if(clip == null)
        {
            return false;
        }
        if(currentSound != null && currentSound.realID == clip.realID && IsPlaying() && currentSound.isFadeOut == false)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    private IEnumerator CheckProcess()
    {
        while(this.isTricking == true && IsPlaying() == true)
        {
            yield return new WaitForSeconds(0.05f);
            if (this.currentSound.HasLoop())
            {
                if(currentPlayingType == MusicPlayingType.SourceA)
                {
                    currentSound.CheckLoop(fadeA_audio);
                }
                else if(currentPlayingType == MusicPlayingType.SourceB)
                {
                    currentSound.CheckLoop(fadeB_audio);
                }
                else if(currentPlayingType == MusicPlayingType.AtoB)
                {
                    this.lastSound.CheckLoop(this.fadeA_audio);
                    this.currentSound.CheckLoop(this.fadeB_audio);
                }
                else if(currentPlayingType == MusicPlayingType.BtoA)
                {
                    this.lastSound.CheckLoop(this.fadeB_audio);
                    this.currentSound.CheckLoop(this.fadeA_audio);
                }
            }
        }
    }
    public void DoCheck()
    {
        StartCoroutine(CheckProcess());
    }

    public void FadeIn(SoundClip clip, float time, Interpolate.EaseType ease)
    {
        if (this.IsDifferentSound(clip))
        {
            this.fadeA_audio.Stop();
            this.fadeB_audio.Stop();
            this.lastSound = this.currentSound;
            this.currentSound = clip;
            PlayAudioSource(fadeA_audio, currentSound, 0.0f);
            this.currentSound.FadeIn(time, ease);
            this.currentPlayingType = MusicPlayingType.SourceA;
            if(this.currentSound.HasLoop() == true)
            {
                this.isTricking = true;
                DoCheck();
            }
        }
    }

    /// <summary>
    /// 사운드 클립을 모를 때 index를 가져와 fadein 시키기 위한 함수.
    /// </summary>
    public void FadeIn(int index, float time, Interpolate.EaseType ease)
    {
        this.FadeIn(DataManager.SoundData().GetCopy(index), time, ease);
    }
    public void FadeOut(float time, Interpolate.EaseType ease)
    {
        if(this.currentSound != null)
        {
            this.currentSound.FadeOut(time, ease);
        }
    }

    /// <summary>
    /// fadein/out 볼륨조절
    /// </summary>
    void Update()
    {
        if (currentSound == null)
        {
            return;
        } 
        if(currentPlayingType == MusicPlayingType.SourceA)
        {
            currentSound.DoFade(Time.deltaTime, fadeA_audio);
        }
        else if(currentPlayingType == MusicPlayingType.SourceB)
        {
            currentSound.DoFade(Time.deltaTime, fadeB_audio);
        }
        else if(currentPlayingType == MusicPlayingType.AtoB)
        {
            this.lastSound.DoFade(Time.deltaTime, fadeA_audio);
            this.currentSound.DoFade(Time.deltaTime, fadeB_audio);
        }
        else if(currentPlayingType == MusicPlayingType.BtoA)
        {
            this.lastSound.DoFade(Time.deltaTime, fadeB_audio);
            this.currentSound.DoFade(Time.deltaTime, fadeA_audio);
        }
        if(fadeA_audio.isPlaying && this.fadeB_audio.isPlaying == false)
        {
            this.currentPlayingType = MusicPlayingType.SourceA;
        }
        else if(fadeB_audio.isPlaying && fadeA_audio.isPlaying == false)
        {
            this.currentPlayingType = MusicPlayingType.SourceB;
        }
        else if(fadeA_audio.isPlaying == false && fadeB_audio.isPlaying == false)
        {
            this.currentPlayingType = MusicPlayingType.None;
        }
    }

    /// <summary>
    /// 현재 sound에서 다른 sound로 바꾸기 위한 함수
    /// </summary>

    public void FadeTo(SoundClip clip, float time, Interpolate.EaseType ease)
    {
        if (currentPlayingType == MusicPlayingType.None)
        {
            FadeIn(clip, time, ease);
        }
        else if (this.IsDifferentSound(clip))
        {
            if(this.currentPlayingType == MusicPlayingType.AtoB)
            {
                this.fadeA_audio.Stop();
                this.currentPlayingType = MusicPlayingType.SourceB;
            }
            else if(this.currentPlayingType == MusicPlayingType.BtoA)
            {
                this.fadeB_audio.Stop();
                this.currentPlayingType = MusicPlayingType.SourceA;
            }
            lastSound = currentSound;
            currentSound = clip;
            this.lastSound.FadeOut(time, ease);
            this.currentSound.FadeIn(time, ease);
            if(currentPlayingType == MusicPlayingType.SourceA)
            {
                PlayAudioSource(fadeB_audio, currentSound, 0.0f);
                currentPlayingType = MusicPlayingType.AtoB;
            }
            else if(currentPlayingType == MusicPlayingType.SourceB)
            {
                PlayAudioSource(fadeA_audio, currentSound, 0.0f);
                currentPlayingType = MusicPlayingType.BtoA;
            }
            if (currentSound.HasLoop())
            {
                this.isTricking = true;
                DoCheck();
            }
        }
    }
    public void FadeTo(int index, float time, Interpolate.EaseType ease)
    {
        this.FadeTo(DataManager.SoundData().GetCopy(index), time, ease);
    }
    public void PlayBGM(SoundClip clip)
    {
        if (this.IsDifferentSound(clip))
        {
            this.fadeB_audio.Stop();
            this.lastSound = this.currentSound;
            this.currentSound = clip;
            PlayAudioSource(fadeA_audio, clip, clip.maxVolume);
            if (currentSound.HasLoop())
            {
                this.isTricking = true;
                DoCheck();
            }
        }
    }
    public void PlayBGM(int index)
    {
        SoundClip clip = DataManager.SoundData().GetCopy(index);
        PlayBGM(clip);
    }
    public void PlayUISound(SoundClip clip)
    {
        PlayAudioSource(UI_audio, clip, clip.maxVolume);
    }
    public void PlayEffectSound(SoundClip clip)
    {
        bool isPlaySuccess = false;
        for (int i = 0; i < this.EffectChannelCount; i++)
        {
            if (this.effect_audios[i].isPlaying == false)
            {
                PlayAudioSource(this.effect_audios[i], clip, clip.maxVolume);
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
            else if(this.effect_audios[i].clip == clip.GetClip())
            {
                this.effect_audios[i].Stop();
                PlayAudioSource(effect_audios[i], clip, clip.maxVolume);
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
        }
        if(isPlaySuccess == false)
        {
            // 플레이 시간이 오래된 sound 최댓값을 찾는다.
            float maxTime = 0.0f;
            int selectIndex = 0;
            for(int i =0; i < EffectChannelCount; i++)
            {
                if(this.effect_PlayStartTime[i] > maxTime)
                {
                    maxTime = this.effect_PlayStartTime[i];
                    selectIndex = i;
                }
            }
            PlayAudioSource(this.effect_audios[selectIndex], clip, clip.maxVolume);

        }
    }
    
    /// <summary>
    /// 원하는 위치에 effect sound를 재생시키기 위한 함수.
    /// </summary>
  
    public void PlayEffectSound(SoundClip clip, Vector3 position, float volume)
    {
        bool isPlaySuccess = false;
        for (int i = 0; i < this.EffectChannelCount; i++)
        {
            if (this.effect_audios[i].isPlaying == false)
            {
                PlayAudioSourceAtPoint(clip, position, volume);
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
            else if (this.effect_audios[i].clip == clip.GetClip())
            {
                this.effect_audios[i].Stop();
                PlayAudioSourceAtPoint(clip, position, volume); 
                this.effect_PlayStartTime[i] = Time.realtimeSinceStartup;
                isPlaySuccess = true;
                break;
            }
        }
        if (isPlaySuccess == false)
        {
            PlayAudioSourceAtPoint(clip, position, volume);
        }
    }
    
    public void PlayOneShotEffect(int index, Vector3 position, float volume)
    {
        if(index == (int)SoundList.None)
        {
            return;
        }

        SoundClip clip = DataManager.SoundData().GetCopy(index);
        if(clip == null)
        {
            return;
        }
        PlayEffectSound(clip, position, volume);
    }
    public void PlayOneShot(SoundClip clip)
    {
        if(clip == null)
        {
            return;
        }
        switch (clip.playType)
        {
            case SoundPlayType.EFFECT:
                PlayEffectSound(clip);
                break;
            case SoundPlayType.BGM:
                PlayBGM(clip);
                break;
            case SoundPlayType.UI:
                PlayUISound(clip);
                break;
        }
    }

    public void Stop(bool allStop = false)
    {
        if (allStop)
        {
            this.fadeA_audio.Stop();
            this.fadeB_audio.Stop();

        }

        this.FadeOut(0.5f, Interpolate.EaseType.Linear);
        this.currentPlayingType = MusicPlayingType.None;
        StopAllCoroutines();
    }

    /// <summary>
    /// enemy의 클래스에 따라 사격 사운드를 교체.
    /// </summary>

    public void PlayShotSound(string ClassID, Vector3 position, float volume)
    {
        SoundList sound = (SoundList)Enum.Parse(typeof(SoundList), ClassID.ToLower());
        PlayOneShotEffect((int)sound, position, volume);
    }
}
