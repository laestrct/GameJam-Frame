using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataManager : MonoSingleton<DataManager>
{
    // 游戏数据
    public GameData GameData
    {
        get
        {
            gameData ??= new GameData();
            return gameData;
        }
        //禁止直接SetGameData
        private set=> gameData = value;    
    }

    private GameData gameData;

    #region API实现
    public void AddScore(int amount)
    {
        GameData.Score += amount;
        EventManager.Broadcast(GameEvent.PlayerScore, GameData.Score);
    }

    public void ResetGameData()
    {
        GameData.Score = 0;
        GameData.IsGameOver = false;
    }
    #endregion
}
