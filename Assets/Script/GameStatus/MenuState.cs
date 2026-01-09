using UnityEngine;

public class MenuState : GameState
{
    public override void OnEnter()
    {
        Time.timeScale = 0f;

        // UIManager.Instance.PushPanel<MainMenuPanel>(); 
        Debug.Log("UI: 显示开始按钮");
    }

    public override void OnUpdate()
    {
        if (InputManager.Instance.Esc()) // 假设空格开始
        {
            //manager.ChangeState(new GameplayState(manager));
        }
    }

    public override void OnExit()
    {
        // 关闭主菜单UI
        // UIManager.Instance.PopPanel();
    }
}