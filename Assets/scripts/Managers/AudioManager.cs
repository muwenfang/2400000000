using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance; // 单例，全局调用
    private AudioSource audioSource;

    void Awake()
    {
        // 唯一不销毁（切场景不中断音乐）
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        audioSource = GetComponent<AudioSource>();
    }

    // 播放背景音乐
    public void PlayBGM(AudioClip clip)
    {
        audioSource.clip = clip;
        audioSource.Play();
    }

    // 停止
    public void StopBGM()
    {
        audioSource.Stop();
    }

    // 设置音量
    public void SetVolume(float volume)
    {
        audioSource.volume = volume;
    }
    /*
    // 播放
    AudioManager.instance.PlayBGM(你的音乐片段);

    // 停止
    AudioManager.instance.StopBGM();

    // 设置音量 0~1
    AudioManager.instance.SetVolume(0.5f);
    */
}
