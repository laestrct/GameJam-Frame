
using UnityEngine;
//此处写入需要进行全局沟通的数据

//如何增加一个新的数据项：使用BindableProperty<T>包装数据类型T（见下方示例）
/*
如何修改数据？从DataManager获得GameData，然后修改其数值的value，
DataManager.Instance.GameData.Score.Value = 10;
会自动广播数据变更事件，参数是变量名"Score"
*/

public class GameData
{
    //e.g:一个为int类型的数据项Score,初始值为0
    public BindableProperty<int> Score { get; } = new(0);

    public BindableProperty<bool> IsGameOver { get; } = new(false);

    public BindableProperty<float> PlayerHp { get; } = new(100.0f);

}





/// <summary>
/// 自动广播数据的包装类
/// </summary>
public class BindableProperty<T> : IBindable
{
    private T _value;
    private string _name; // 数据名称（如 "Score"）

    public T Value
    {
        get => _value;
        set
        {
            // 如果值确实改变了
            if (!System.Collections.Generic.EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                // 自动广播 DataChange 事件，参数是变量名
                if (!string.IsNullOrEmpty(_name))
                {
                    EventManager.Broadcast(GameEvent.DataChange, _name);
                }
            }
        }
    }

    public BindableProperty(T initialValue = default)
    {
        _value = initialValue;
    }

    // 用于DataManager自动注入名字
    public void SetName(string name) => _name = name;

    // 允许直接用 float a = GameData.Score; (读值方便)
    public static implicit operator T(BindableProperty<T> property) => property.Value;

    // 重写ToString方便调试
    public override string ToString() => _value.ToString();
}

/// <summary>
/// 非泛型接口，用于反射初始化
/// </summary>
public interface IBindable
{
    void SetName(string name);
}