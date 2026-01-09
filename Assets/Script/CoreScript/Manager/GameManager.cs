using System;
using UnityEngine;

/// <summary>
/// 核心流程控制器
/// 游戏状态整体由状态机驱动
/// </summary>
public class GameManager : MonoSingleton<GameManager>
{
    #region FSM (状态机核心)

    private GameState currentState;

    // 状态切换方法
    public void ChangeState(GameState newState)
    {
        if (currentState != null)
        {
            currentState.OnExit();
        }

        currentState = newState;

        if (currentState != null)
        {
            Debug.Log($"[GameManager] 进入状态: {currentState.GetType().Name}");
            currentState.OnEnter();
        }
    }



    #endregion

    #region 快速引用
    // 玩家引用
    [HideInInspector] public GameObject Player { get; private set; }

    #endregion

    #region 生命周期

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // 初始化时进入菜单状态
        ChangeState(new MenuState());
    }
    public void Update()
    {
        // 将Update驱动权下放给当前状态
        currentState?.OnUpdate();
    }

    #endregion 

    #region API实现
    //此处为示例方法，可根据需要自行添加
    //全局辅助方法应在此处添加

    public void RegisterPlayer(GameObject player)
    {
        this.Player = player;
    }

    public void UnregisterPlayer()
    {
        this.Player = null;
    }

    #endregion
}
