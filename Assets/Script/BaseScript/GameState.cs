/// <summary>
/// 状态基类
/// </summary>
public abstract class GameState
{
    protected GameManager manager;

    // 构造函数注入Manager引用，方便访问全局数据
    public GameState()
    {
        this.manager = GameManager.Instance;
    }

    /// <summary>
    /// 进入状态时调用一次 (初始化UI，重置参数)
    /// </summary>
    public abstract void OnEnter();

    /// <summary>
    /// 每帧调用 (处理输入，检测游戏结束条件)
    /// </summary>
    public abstract void OnUpdate();

    /// <summary>
    /// 退出状态时调用 (清理UI，解除事件监听)
    /// </summary>
    public abstract void OnExit();
}