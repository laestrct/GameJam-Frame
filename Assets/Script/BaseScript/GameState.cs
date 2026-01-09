/// <summary>
/// 状态基类
/// </summary>
public abstract class GameState
{
    //便于替换，非必要使用
    protected GameManager GameManager=>GameManager.Instance;
    protected UIManager UIManager=>UIManager.Instance;
    protected AudioManager AudioManager=>AudioManager.Instance;

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