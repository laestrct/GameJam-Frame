using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
///     通用事件管理器
/// </summary>
public static class EventManager
{
    private static readonly Dictionary<Type, Delegate> eventTable = new();

    private static readonly Dictionary<object, List<(Type EventType, Delegate Callback)>> ownerListeners = new();

    #region 广播

    public static void Broadcast<T>(T evt) where T : IGameEvent
    {
        var type = typeof(T);

        if (eventTable.TryGetValue(type, out var d))
        {
            if (d is Action<T> callback)
                callback.Invoke(evt);
            else
                // 这里通常不会发生，除非反射篡改了数据
                Debug.LogError($"[EventManager] 事件类型 {type} 委托签名不匹配");
        }
    }

    #endregion

    public static void Clear()
    {
        eventTable.Clear();
        ownerListeners.Clear();
    }

    #region 注册监听

    /// <summary>
    ///     注册监听(不适用于匿名函数)
    /// </summary>
    public static void AddListener<T>(Action<T> callback) where T : IGameEvent
    {
        var owner = callback.Target;
        AddListener(owner, callback);
    }

    /// <summary>
    ///     注册监听（显式指定 Owner，推荐用于匿名函数）
    /// </summary>
    /// <param name="owner">事件的拥有者（通常传 this），用于 RemoveAllListeners 识别</param>
    public static void AddListener<T>(object owner, Action<T> callback) where T : IGameEvent
    {
        var type = typeof(T);

        if (!eventTable.ContainsKey(type)) eventTable.Add(type, null);
        eventTable[type] = (Action<T>)eventTable[type] + callback;

        if (owner != null)
        {
            if (!ownerListeners.ContainsKey(owner)) ownerListeners.Add(owner, new List<(Type, Delegate)>());

            // 记录具体的委托实例，以便后续精准移除
            ownerListeners[owner].Add((type, callback));
        }
    }

    /// <summary>
    ///     移除监听
    /// </summary>
    public static void RemoveListener<T>(Action<T> callback) where T : IGameEvent
    {
        var type = typeof(T);
        if (eventTable.TryGetValue(type, out var d))
        {
            eventTable[type] = (Action<T>)d - callback;

            if (eventTable[type] == null) eventTable.Remove(type);
        }

        var owner = callback.Target;
        if (owner != null && ownerListeners.TryGetValue(owner, out var list))
        {
            for (var i = list.Count - 1; i >= 0; i--)
                if (list[i].EventType == type && list[i].Callback == (Delegate)callback)
                {
                    list.RemoveAt(i);
                    break;
                }

            if (list.Count == 0) ownerListeners.Remove(owner);
        }
    }

    /// <summary>
    ///     移除某种特定事件类型的所有监听者
    ///     警告：这将移除该事件的所有监听者，慎用！
    ///     应该由事件发送方执行
    /// </summary>
    public static void ForceClearAll<T>() where T : IGameEvent
    {
        var type = typeof(T);

        if (!eventTable.TryGetValue(type, out var d) || d == null) return;

        var invocationList = d.GetInvocationList();

        foreach (var singleDelegate in invocationList)
        {
            var owner = singleDelegate.Target;

            if (owner != null && ownerListeners.TryGetValue(owner, out var list))
            {
                for (var i = list.Count - 1; i >= 0; i--)
                    if (list[i].EventType == type && list[i].Callback == singleDelegate)
                        list.RemoveAt(i);

                if (list.Count == 0) ownerListeners.Remove(owner);
            }
        }

        eventTable.Remove(type);
    }

    /// <summary>
    ///     移除某个对象身上的所有事件监听（包括匿名函数）
    /// </summary>
    /// <param name="listenerObject">在 AddListener 时传入的 owner 对象</param>
    public static void RemoveAllListeners(object listenerObject)
    {
        if (listenerObject == null) return;

        if (ownerListeners.TryGetValue(listenerObject, out var list))
        {
            foreach (var (eventType, callback) in list)
                if (eventTable.TryGetValue(eventType, out var currentDelegate))
                {
                    currentDelegate = Delegate.Remove(currentDelegate, callback);

                    if (currentDelegate == null)
                        eventTable.Remove(eventType);
                    else
                        eventTable[eventType] = currentDelegate;
                }

            ownerListeners.Remove(listenerObject);
        }
    }

    #endregion
}


/// <summary>
///     所有事件的基类（标记接口）
/// </summary>
public interface IGameEvent
{
}


public struct TurnStartEvent : IGameEvent
{
    public int NewTurnCount;
}

public struct TurnEndEvent : IGameEvent
{
    public int TurnCount;

    public TurnEndEvent(int count)
    {
        TurnCount = count;
    }
}


/// <summary>
///     数据变化事件
/// </summary>
public class DataChangeEvent : IGameEvent
{
    public string DataName;

    public DataChangeEvent(string dataName)
    {
        DataName = dataName;
    }
}

