using System.Reflection; // 必须引用

/// <summary>
/// 数据变更自动发出事件
/// </summary>
public class DataManager : MonoSingleton<DataManager>
{
    public GameData GameData { get; private set; }

    public void Awake()
    {
        InitGameData();
    }

    private void InitGameData()
    {
        GameData = new GameData();

        var properties = typeof(GameData).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in properties)
        {
            if (typeof(IBindable).IsAssignableFrom(prop.PropertyType))
            {
                var bindableObj = prop.GetValue(GameData) as IBindable;
                bindableObj?.SetName(prop.Name);
            }
        }
    }

    #region API 实现 

    public void AddScore(int amount)
    {
        GameData.Score.Value += amount;
    }

    public void ResetGameData()
    {
        GameData.Score.Value = 0;
        GameData.IsGameOver.Value = false;
        GameData.PlayerHp.Value = 100f;
    }

    #endregion
}