
using UnityEngine;
//此处写入需要进行全局沟通的数据
//如何获取和修改：
//DataManager持有数据，全局共享一份数据
//使用DataManager.Instance.GameData.属性名 进行访问

public class GameData
{
    public int Score { get; set; }

    public bool IsGameOver { get; set; }

}
