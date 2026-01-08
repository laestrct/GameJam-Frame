using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    //子类自行实现Update，Awake，Start等方法，基类不提供默认实现
    protected string uiName;
    private ChildBindTool childBindTool;


    public virtual void OnEnter(object args)
    {
        // 当UI打开时
    }

    public virtual void OnPause()
    {
        // 被上层UI盖住时调用 
        // gameObject.SetActive(false); 
    }

    public virtual void OnResume()
    {
        // 上层UI关闭，恢复到顶层时调用
        // gameObject.SetActive(true);
    }

    public virtual void OnClose()
    {
        // 播放退出动画，然后 Destroy
        Destroy(gameObject);
    }

    public T Get<T>(string name)
    {
        childBindTool ??= new(this, this.transform);
        return childBindTool.Get<T>(name);
    }

    private void Close()
    {
        UIManager.Instance.CloseUI(this);
    }
}