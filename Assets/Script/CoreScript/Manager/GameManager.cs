using Sirenix.OdinInspector;
using System;
using UnityEngine;

/// <summary>
/// 核心流程控制器
/// 游戏状态整体由状态机驱动
/// </summary>
public class GameManager : MonoSingleton<GameManager>
{
    #region FSM (状态机核心)
    [ShowInInspector]
    [SerializeReference]
    public GameState SelectedState;
    private GameState currentState;

    // 状态切换方法
    public void ChangeState(GameState newState)
    {
        currentState?.OnExit();

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

    //之后需要将Manager的Init逻辑全部集中到这里来
    public void Start()
    {
        ChangeState(SelectedState);
    }

    public void Update()
    {
        currentState?.OnUpdate();
    }

    #endregion 

    #region API实现
    //此处为示例方法，可根据需要自行添加
    //全局辅助方法应在此处添加

    /// <summary>
    /// 游戏的开始(new)
    /// </summary>
    public void GameStart()
    {
        Time.timeScale = 1;
    }

    /// <summary>
    /// 游戏暂停
    /// </summary>
    public void GamePause()
    {
        Time.timeScale = 0;
    }

    public void GameContinue()
    {
        Time.timeScale = 1;
    }

    public void GameOver()
    {
    }

    /// <summary>
    /// 游戏胜利
    /// </summary>
    public void GameWin()
    {
    }


    #endregion
}
