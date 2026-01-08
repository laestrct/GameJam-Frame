using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音频管理器Simple
/// 2026/1/15 LE
/// </summary>
public class AudioManager : MonoSingleton<AudioManager>
{
    // 路径常量
    private const string BGM_PATH = "Audio/BGM/";
    private const string EFFECT_PATH = "Audio/Effect/";
    private const string MIXER_PATH = "Audio/Mixer/MainMixers";

    // 核心组件
    private AudioSource bgmSource;
    private AudioMixerGroup effectMixerGroup;
    private AudioMixerGroup bgmMixerGroup;

    // 动态对象池
    private readonly List<AudioSource> effectSourcesPool = new();

    // 资源缓存
    private readonly Dictionary<string, AudioClip> clipCache = new ();

    //静音变量
    public bool IsSoundMuted; // BGM静音
    public bool IsEffectMuted; // 音效静音

    protected void Awake()
    {
        InitComponents();
    }

    private void InitComponents()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }

        // 尝试加载 Mixer
        var mixer = Resources.Load<AudioMixer>(MIXER_PATH);
        if (mixer != null)
        {
            // 需确保Mixer中有名为 "Effect" 和 "BGM" 的 Group
            var bgmGroups = mixer.FindMatchingGroups("BGM");
            var effectGroups = mixer.FindMatchingGroups("Effect");

            if (bgmGroups.Length > 0)
            {
                bgmMixerGroup = bgmGroups[0];
                bgmSource.outputAudioMixerGroup = bgmMixerGroup;
            }

            if (effectGroups.Length > 0)
            {
                effectMixerGroup = effectGroups[0];
            }
        }
        else
        {
            Debug.LogWarning("[AudioManager] 未加载到 Mixer，将使用直接音量控制模式。");
        }
    }

    #region BGM Control 

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBGM(string name, float fadeDuration = 1.0f)
    {
        if (string.IsNullOrEmpty(name)) return;

        var clip = LoadClip(name, true);
        if (clip == null) return;

        // 如果是同一首，不重播
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        StartCoroutine(FadeSwitchBGM(clip, fadeDuration));
    }

    private IEnumerator FadeSwitchBGM(AudioClip newClip, float duration)
    {
        // 淡出
        if (bgmSource.isPlaying && duration > 0)
        {
            float startVol = bgmSource.volume;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(startVol, 0, t / duration);
                yield return null;
            }
        }

        bgmSource.Stop();
        bgmSource.clip = newClip;
        bgmSource.volume = 0; // 准备淡入
        bgmSource.Play();

        // 淡入
        if (duration > 0)
        {
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(0, 1, t / duration);
                yield return null;
            }
        }
        bgmSource.volume = 1f;
    }

    public void StopBGM() => bgmSource.Stop();
    public void PauseBGM() => bgmSource.Pause();
    public void ResumeBGM() => bgmSource.UnPause();

    #endregion

    #region Effect Control

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="name">音效文件名</param>
    /// <param name="volumeScale">音量缩放 (0~1)</param>
    /// <param name="pitch">音调 (1为原声，0.8-1.2常用于随机化)</param>
    public void PlayEffect(string name, float volumeScale = 1f, float pitch = 1f)
    {
        if (IsEffectMuted) return;

        var clip = LoadClip(name, false);
        if (clip == null) return;

        var source = GetAvailableSource();
        source.pitch = pitch;
        source.volume = volumeScale; // 注意：Source本身的Volume和Mixer是叠加关系
        source.clip = clip;
        source.Play();
    }

    /// <summary>
    /// 快捷播放音效：带有微小随机音调
    /// </summary>
    public void PlayEffectRandom(string name)
    {
        PlayEffect(name, 1f, Random.Range(0.9f, 1.1f));
    }

    // 从池中获取闲置 Source
    private AudioSource GetAvailableSource()
    {
        foreach (var source in effectSourcesPool)
        {
            if (!source.isPlaying) return source;
        }
        var newSource = gameObject.AddComponent<AudioSource>();
        newSource.outputAudioMixerGroup = effectMixerGroup; // 自动应用 Mixer
        newSource.playOnAwake = false;
        effectSourcesPool.Add(newSource);

        return newSource;
    }

    #endregion

    #region 3D Effect Control (空间音效)

    /// <summary>
    /// 在指定位置播放3D音效
    /// </summary>
    /// <param name="name">音效文件名</param>
    /// <param name="position">世界坐标</param>
    /// <param name="prefab">【可选】包含AudioSource配置的预制体。如果为null，将代码生成一个默认3D源。</param>
    /// <param name="volumeScale">音量</param>
    public void PlayEffectAt(string name, Vector3 position, GameObject prefab = null, float volumeScale = 1f)
    {
        if (IsEffectMuted) return;
        var clip = LoadClip(name, false);
        if (clip == null) return;

        CreateAndPlay3DSource(clip, position, null, prefab, volumeScale);
    }

    /// <summary>
    /// 在指定物体上播放3D音效（跟随移动）
    /// </summary>
    /// <param name="name">音效文件名</param>
    /// <param name="target">跟随的目标Transform</param>
    /// <param name="prefab">【可选】预制体</param>
    /// <param name="volumeScale">音量</param>
    public void PlayEffectFollow(string name, Transform target, GameObject prefab = null, float volumeScale = 1f)
    {
        if (IsEffectMuted) return;
        var clip = LoadClip(name, false);
        if (clip == null) return;

        CreateAndPlay3DSource(clip, target.position, target, prefab, volumeScale);
    }

    /// <summary>
    /// 创建临时的3D音效对象
    /// </summary>
    private void CreateAndPlay3DSource(AudioClip clip, Vector3 pos, Transform parent, GameObject prefab, float volume)
    {
        GameObject audioObj;
        AudioSource source;

        if (prefab != null)
        {
            audioObj = Instantiate(prefab, pos, Quaternion.identity);
            source = audioObj.GetComponent<AudioSource>();
            if (source == null)
            {
                Debug.LogWarning($"[AudioManager] 传入的 Prefab '{prefab.name}' 缺少 AudioSource 组件，已自动添加。");
                source = audioObj.AddComponent<AudioSource>();
            }
        }
        else
        {
            audioObj = new GameObject($"TempSFX_{clip.name}");
            audioObj.transform.position = pos;
            source = audioObj.AddComponent<AudioSource>();

            // 默认3D设置 (如果没传prefab)
            source.spatialBlend = 1.0f; // 1.0是完全3D，0.0是2D
            source.minDistance = 2f;    // 最小衰减距离
            source.maxDistance = 20f;   // 最大听到距离
            source.rolloffMode = AudioRolloffMode.Logarithmic; // 对数衰减
        }

        if (parent != null)
        {
            audioObj.transform.SetParent(parent);
            audioObj.transform.localPosition = Vector3.zero; // 归零，位于父物体中心
        }

        source.clip = clip;
        source.volume = volume;
        source.outputAudioMixerGroup = effectMixerGroup; // 别忘了应用Mixer
        source.Play();

        //  (Clip时长 + 0.1秒缓冲)
        Destroy(audioObj, clip.length + 0.1f);
    }

    #endregion

    #region Resource Management

    // 统一资源加载与缓存逻辑
    private AudioClip LoadClip(string name, bool isBgm)
    {
        if (clipCache.TryGetValue(name, out var cachedClip))
        {
            return cachedClip;
        }

        string path = (isBgm ? BGM_PATH : EFFECT_PATH) + name;
        var clip = Resources.Load<AudioClip>(path);

        if (clip == null)
        {
            Debug.LogError($"[AudioManager] 找不到音频文件: {path}");
            return null;
        }

        clipCache.Add(name, clip);
        return clip;
    }

    /// <summary>
    /// 切换场景时调用，清理不再需要的内存
    /// </summary>
    public void ClearCache()
    {
        clipCache.Clear();
    }

    #endregion

    #region Volume Control

    // 设置 BGM 音量
    public void SetBGMVolume(float volume)
    {
        if (bgmMixerGroup != null)
        {
            var db = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20;
            bgmMixerGroup.audioMixer.SetFloat("BGMVolume", db);
        }
        else
        {
            bgmSource.volume = volume;
        }
    }

    // 设置音效音量
    public void SetEffectVolume(float volume)
    {
        if (effectMixerGroup != null)
        {
            var db = volume <= 0.0001f ? -80f : Mathf.Log10(volume) * 20;
            effectMixerGroup.audioMixer.SetFloat("EffectsVolume", db);
        }
        else
        {
            foreach (var s in effectSourcesPool) s.volume = volume;
        }
    }

    #endregion
}