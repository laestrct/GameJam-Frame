using System;
using System.Collections.Generic;
using UnityEngine;

#region 使用示例
/*
 //发送事件
 public class PlayerHealth : MonoBehaviour 
{
    private float hp = 100;

    public void TakeDamage(float damage) 
    {
        hp -= damage;
        // 广播事件，不仅通知UI，也可以通知音效管理器播放受伤音效
        EventCenter.Broadcast(GameEvent.PlayerHurt, hp);
    }
}

//接受事件
public class HUDManager : MonoBehaviour 
{
    private void Awake() 
    {
        // 注册监听
        EventCenter.AddListener<float>(GameEvent.PlayerHurt, OnPlayerHurt);
    }

    private void OnDestroy() 
    {
        // 务必移除监听！否则会报错 MissingReferenceException
        EventCenter.RemoveListener<float>(GameEvent.PlayerHurt, OnPlayerHurt);
    }

    private void OnPlayerHurt(float currentHp) 
    {
        Debug.Log($"更新血条UI: {currentHp}");
    }
}
 *
 */

#endregion
/// <summary>
/// 静态事件中心。
/// </summary>
public static class EventManager
{
    private static readonly Dictionary<GameEvent, Delegate> EventTable =new();

    #region 添加监听 (AddListener)

    /// <summary>
    /// 添加无参监听
    /// </summary>
    public static void AddListener(GameEvent evt, Action action)
    {
        OnListenerAdding(evt, action);
        EventTable[evt] = (Action) EventTable[evt] + action;
    }

    /// <summary>
    /// 添加 1 个参数的监听
    /// </summary>
    public static void AddListener<T>(GameEvent evt, Action<T> action)
    {
        OnListenerAdding(evt, action);
        EventTable[evt] = (Action<T>) EventTable[evt] + action;
    }

    /// <summary>
    /// 添加 2 个参数的监听
    /// </summary>
    public static void AddListener<T1, T2>(GameEvent evt, Action<T1, T2> action)
    {
        OnListenerAdding(evt, action);
        EventTable[evt] = (Action<T1, T2>) EventTable[evt] + action;
    }

    #endregion

    #region 移除监听 (RemoveListener)

    /// <summary>
    /// 移除无参监听
    /// </summary>
    public static void RemoveListener(GameEvent evt, Action action)
    {
        if (OnListenerRemoving(evt, action))
        {
            EventTable[evt] = (Action) EventTable[evt] - action;
            OnListenerRemoved(evt);
        }
    }

    /// <summary>
    /// 移除 1 个参数的监听
    /// </summary>
    public static void RemoveListener<T>(GameEvent evt, Action<T> action)
    {
        if (OnListenerRemoving(evt, action))
        {
            EventTable[evt] = (Action<T>) EventTable[evt] - action;
            OnListenerRemoved(evt);
        }
    }

    /// <summary>
    /// 移除 2 个参数的监听
    /// </summary>
    public static void RemoveListener<T1, T2>(GameEvent evt, Action<T1, T2> action)
    {
        if (OnListenerRemoving(evt, action))
        {
            EventTable[evt] = (Action<T1, T2>) EventTable[evt] - action;
            OnListenerRemoved(evt);
        }
    }

    #endregion

    #region 广播事件 (Broadcast)

    /// <summary>
    /// 广播无参事件
    /// </summary>
    public static void Broadcast(GameEvent evt)
    {
        if (EventTable.TryGetValue(evt, out var d))
        {
            if (d is Action callback)
            {
                callback.Invoke();
            }
            else
            {
                Debug.LogError($"[EventCenter] 广播错误: 事件 {evt} 对应的委托类型不匹配（应为 Action）。");
            }
        }
    }

    /// <summary>
    /// 广播 1 个参数的事件
    /// </summary>
    public static void Broadcast<T>(GameEvent evt, T arg1)
    {
        if (EventTable.TryGetValue(evt, out var d))
        {
            if (d is Action<T> callback)
            {
                callback.Invoke(arg1);
            }
            else
            {
                Debug.LogError($"[EventCenter] 广播错误: 事件 {evt} 对应的委托类型不匹配。确保广播和监听的参数类型一致。");
            }
        }
    }

    /// <summary>
    /// 广播 2 个参数的事件
    /// </summary>
    public static void Broadcast<T1, T2>(GameEvent evt, T1 arg1, T2 arg2)
    {
        if (EventTable.TryGetValue(evt, out var d))
        {
            if (d is Action<T1, T2> callback)
            {
                callback.Invoke(arg1, arg2);
            }
            else
            {
                Debug.LogError($"[EventCenter] 广播错误: 事件 {evt} 对应的委托类型不匹配。");
            }
        }
    }

    #endregion

    #region 内部私有方法 & 清理

    /// <summary>
    /// 切换场景时调用，防止静态字典持有空引用
    /// </summary>
    public static void Clear()
    {
        EventTable.Clear();
    }

    private static void OnListenerAdding(GameEvent evt, Delegate listener)
    {
        if (!EventTable.ContainsKey(evt))
        {
            EventTable.Add(evt, null);
        }

        var d = EventTable[evt];
        if (d != null && d.GetType() != listener.GetType())
        {
            Debug.LogError($"[EventCenter] 尝试为事件 {evt} 添加不同类型的委托。当前: {d.GetType().Name}, 添加: {listener.GetType().Name}");
        }
    }

    private static bool OnListenerRemoving(GameEvent evt, Delegate listener)
    {
        if (EventTable.ContainsKey(evt))
        {
            var d = EventTable[evt];
            if (d == null)
            {
                return false;
            }
            else if (d.GetType() != listener.GetType())
            {
                // 类型不匹配，无法移除
                return false;
            }
        }
        else
        {
            return false;
        }
        return true;
    }

    private static void OnListenerRemoved(GameEvent evt)
    {
        if (EventTable[evt] == null)
        {
            EventTable.Remove(evt);
        }
    }

    #endregion
}

/// <summary>
/// 游戏事件枚举
/// 需要时添加
/// </summary>
public enum GameEvent
{
    GameStart,
    GamePause,
    GameOver,

    // 玩家相关
    PlayerDead,
    PlayerHurt,   // 可以带参: int damage
    PlayerScore,  // 可以带参: int score

    // UI相关
    ShowSettlementPanel,
    InputDeviceChanged, //手柄控制切换
}

