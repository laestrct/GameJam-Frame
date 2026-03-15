using System;
using System.Collections.Generic;

/// <summary>
/// 事件监听组
/// 用于简化 MonoBehaviour 中多个事件的注册与注销管理，防止内存泄漏
/// </summary>
public class EventListenerGroup
{
    // 存储所有的注销操作委托
    private readonly List<Action> unregisterActions = new List<Action>();

    /// <summary>
    /// 注册事件监听（支持链式调用）
    /// </summary>
    /// <typeparam name="T">事件类型</typeparam>
    /// <param name="callback">回调函数</param>
    /// <returns>返回自身，支持链式写法</returns>
    public EventListenerGroup AddListener<T>(Action<T> callback) where T : IGameEvent
    {
        // 1. 向全局管理器注册
        EventManager.AddListener(callback);

        // 2. 将“注销逻辑”封装为一个匿名委托，存入列表
        unregisterActions.Add(() =>
        {
            EventManager.RemoveListener(callback);
        });

        return this;
    }

    /// <summary>
    /// 【新增】无参重载：忽略事件参数，直接调用无参方法
    /// 适用场景：多个不同事件触发同一个 UI 刷新逻辑
    /// </summary>
    public EventListenerGroup AddListener<T>(Action callback) where T : IGameEvent
    {
        // 1. 创建一个中间委托：接收 T 类型参数，但直接忽略它，去调用无参 callback
        Action<T> wrapper = (evt) =>
        {
            callback?.Invoke();
        };

        // 2. 向核心管理器注册这个 wrapper
        EventManager.AddListener(wrapper);

        // 3. 记录注销逻辑（注意：注销时也必须注销这个 wrapper）
        unregisterActions.Add(() =>
        {
            EventManager.RemoveListener(wrapper);
        });

        return this;
    }

    /// <summary>
    /// 注销该组管理的所有事件
    /// </summary>
    public void RemoveAllListeners()
    {
        foreach (var action in unregisterActions)
        {
            action?.Invoke();
        }
        unregisterActions.Clear();
    }
}