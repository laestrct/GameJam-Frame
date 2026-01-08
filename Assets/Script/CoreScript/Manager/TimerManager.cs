using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 计时器系统的静态入口 (API层)
/// </summary>
public static class Timer
{
    /// <summary>
    /// 注册一个延时任务
    /// </summary>
    /// <param name="duration">时间(秒)</param>
    /// <param name="onComplete">完成回调</param>
    /// <param name="onUpdate">每帧回调(可选), 参数是剩余时间比例0~1</param>
    /// <param name="isLooped">是否循环</param>
    /// <param name="useRealTime">是否使用真实时间(不受TimeScale影响)</param>
    /// <returns>返回计时器对象，可用于中途取消或暂停</returns>
    public static TimerTask Register(float duration, Action onComplete, Action<float> onUpdate = null, bool isLooped = false, bool useRealTime = false)
    {
        return TimerManager.Instance.Register(duration, onComplete, onUpdate, isLooped, useRealTime);
    }

    /// <summary>
    /// 取消所有指定标签的计时器
    /// </summary>
    public static void CancelAll(string tag) => TimerManager.Instance.CancelAll(tag);

    /// <summary>
    /// 取消所有计时器
    /// </summary>
    public static void CancelAll() => TimerManager.Instance.CancelAll();
}

/// <summary>
/// 计时器任务对象 (代表一个正在运行的计时器)
/// </summary>
public class TimerTask
{
    public float Duration;
    public float TimeElapsed;
    public bool IsLooped;
    public bool UseRealTime;
    public bool IsPaused;
    public bool IsCancelled;
    public bool IsCompleted;
    public string Tag; // 用于归类管理

    public Action OnComplete;
    public Action<float> OnUpdate; // float param: percentage complete (0 to 1)

    // 重置状态（对象池复用）
    public void Reset()
    {
        Duration = 0;
        TimeElapsed = 0;
        IsLooped = false;
        UseRealTime = false;
        IsPaused = false;
        IsCancelled = false;
        IsCompleted = false;
        Tag = null;
        OnComplete = null;
        OnUpdate = null;
    }

    // --- API for User ---

    /// <summary>
    /// 取消此计时器
    /// </summary>
    public void Cancel() => IsCancelled = true;

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    /// 恢复
    /// </summary>
    public void Resume() => IsPaused = false;

    /// <summary>
    /// 设置标签 (用于批量管理)
    /// </summary>
    public TimerTask SetTag(string tag)
    {
        this.Tag = tag;
        return this;
    }
}

/// <summary>
/// 计时器管理器 (驱动层，单例，包含对象池)
/// </summary>
public class TimerManager : MonoSingleton<TimerManager>
{
    private readonly List<TimerTask> activeTimers = new List<TimerTask>();
    private readonly List<TimerTask> addedBuffer = new List<TimerTask>(); // 缓存帧内新增的，防止遍历报错

    // 简易对象池
    private readonly Stack<TimerTask> taskPool = new Stack<TimerTask>();

    protected void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (activeTimers.Count == 0 && addedBuffer.Count == 0) return;

        // 将缓存区的新任务加入主列表
        if (addedBuffer.Count > 0)
        {
            activeTimers.AddRange(addedBuffer);
            addedBuffer.Clear();
        }

        // 遍历更新
        for (int i = activeTimers.Count - 1; i >= 0; i--)
        {
            var timer = activeTimers[i];

            // 检查无效状态
            if (timer.IsCancelled || timer.IsCompleted)
            {
                Recycle(timer);
                activeTimers.RemoveAt(i);
                continue;
            }

            if (timer.IsPaused) continue;

            // 更新时间
            float dt = timer.UseRealTime ? Time.unscaledDeltaTime : Time.deltaTime;
            timer.TimeElapsed += dt;

            // 执行 Update 回调 (传入进度 0~1)
            if (timer.OnUpdate != null)
            {
                timer.OnUpdate.Invoke(Mathf.Clamp01(timer.TimeElapsed / timer.Duration));
            }

            // 检查完成
            if (timer.TimeElapsed >= timer.Duration)
            {
                timer.OnComplete?.Invoke();

                if (timer.IsLooped && !timer.IsCancelled)
                {
                    // 循环：重置时间，不移除
                    timer.TimeElapsed = 0;
                }
                else
                {
                    // 完成：标记为完成，下一帧回收
                    timer.IsCompleted = true;
                }
            }
        }
    }

    public TimerTask Register(float duration, Action onComplete, Action<float> onUpdate, bool isLooped, bool useRealTime)
    {
        TimerTask task = taskPool.Count > 0 ? taskPool.Pop() : new TimerTask();
        task.Reset(); // 确保干净

        task.Duration = duration;
        task.OnComplete = onComplete;
        task.OnUpdate = onUpdate;
        task.IsLooped = isLooped;
        task.UseRealTime = useRealTime;

        // 加入缓存区，避免 Update 遍历时修改集合
        addedBuffer.Add(task);
        return task;
    }

    public void CancelAll(string tag)
    {
        foreach (var timer in activeTimers)
        {
            if (timer.Tag == tag) timer.Cancel();
        }
        foreach (var timer in addedBuffer)
        {
            if (timer.Tag == tag) timer.Cancel();
        }
    }

    public void CancelAll()
    {
        foreach (var timer in activeTimers) timer.Cancel();
        foreach (var timer in addedBuffer) timer.Cancel();
    }

    private void Recycle(TimerTask task)
    {
        task.Reset();
        taskPool.Push(task);
    }
}