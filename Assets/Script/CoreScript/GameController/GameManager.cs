using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 核心流程控制器
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
            // 打印日志方便调试流程
            Debug.Log($"[GameManager] 进入状态: {currentState.GetType().Name}");
            currentState.OnEnter();
        }
    }

    private void Update()
    {
        // 将Update驱动权下放给当前状态
        currentState?.OnUpdate();
    }

    #endregion

    #region Global Data

    // 玩家引用
    [HideInInspector] public GameObject Player;

    // 游戏数据
    public GameData GameData = new();

    #endregion

    #region 生命周期

    protected void Awake()
    {
        DontDestroyOnLoad(gameObject);
        // 初始化时进入菜单状态
        ChangeState(new MenuState(this));
    }

    #endregion

    #region 辅助方法

    public void AddScore(int amount)
    {
        GameData.Score += amount;
        EventManager.Broadcast(GameEvent.PlayerScore, GameData.Score);
    }

    public void ResetGameData()
    {
        GameData.Score = 0;
        GameData.IsGameOver = false;
        Time.timeScale = 1f;
    }

    public void RegisterPlayer(GameObject player)
    {
        this.Player = player;
    }

    #endregion
}
