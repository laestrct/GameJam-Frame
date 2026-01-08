# Unity GameJam Framework

> **核心设计**：约定优于配置 (Convention over Configuration) 与 高容错性 (Robustness)。  
> 为 **48小时 Game Jam** 设计的 Unity 极速开发框架。无需繁琐初始化，拖入即用。

## 🛠️ 核心模块

### 1. Audio Manager (音频管理)
支持 **自动对象池**、**防爆音**、**BGM 平滑过渡 (CrossFade)**。

* **特性**：
    * **自动缓存**：自动管理已加载的 AudioClip。
    * **动态扩容**：基于对象池管理的 AudioSource。
    * **纯代码控制**：无需手动挂载 AudioSource 组件。
    * **3D音效**：框架内提供可调用的3D音效Prefab和3D音效方法
```csharp
// 播放 BGM (自动处理 1秒 淡入淡出)
AudioManager.Instance.PlayBGM("BattleTheme", fadeDuration: 1.0f);

// 播放普通音效
AudioManager.Instance.PlayEffect("Explosion");

// 播放带有随机音调的音效 
AudioManager.Instance.PlayEffectRandom("Footstep");
```

### 2. UI Manager (界面管理)
基于 **三层级 (FullScreen, Panel, Popup)** 设计，采用 List 模拟 **栈结构**，支持智能关闭。

* **特性**：
    * **自动层级**：全屏层 (底) -> 面板层 (中) -> 弹窗层 (顶)。
    * **栈式管理**：打开新面板自动 **暂停** 下层面板，关闭后自动 **恢复**。
    * **通用关闭**：`ui.Close()` 自动识别 UI 类型并执行正确的销毁/出栈逻辑。

```csharp
// 打开面板 (入栈)
UIManager.Instance.OpenPanel<InventoryPanel>();

// 打开全屏窗口 (与其他全屏互斥)
UIManager.Instance.OpenFullScreen<GamePlayPanel>();

// 在 UI 脚本内部关闭自己
public void OnCloseBtnClick()
{
    this.Close(); // 自动从栈中移除并恢复下层状态
}

// 外部强制关闭指定 UI
UIManager.Instance.CloseUI<InventoryPanel>();
```

### 3. Input Manager (输入管理)
**去除了 Rewired 依赖**，原生支持键鼠与手柄自动切换，集成 **输入锁定机制**。

* **特性**：
    * **反射绑定**：自动识别代码中的 `KeyCode` 变量生成 Inspector 文本标签。
    * **文本解析**：支持 `ParseInputString("按 [Interact] 开门")` -> 自动输出 "按 [E] 开门"。
    * **输入锁定**：支持 `LockMove`, `LockInteract`, `LockAll` 等分级锁定，方便剧情演出。

```csharp
// 假设在 Inspector 中配置 interactKey 为 E

if (InputManager.Instance.Interact())
{
    // 执行交互逻辑
}

// 动态生成提示文本 (自适应手柄/键盘)
string tip = InputManager.Instance.ParseInputString("请按 [Interact] 进行互动");
// 结果示例: "请按 [E] 进行互动" (若切手柄会自动变为 "请按 A 进行互动")
```

### 4. Event Center (事件中心)
基于 `Enum` 的静态即时消息中心，彻底 **解耦** Gameplay 与 UI/Audio。

* **特性**：
    * **类型安全**：强制使用 `GameEvent` 枚举，彻底避免字符串拼写错误。
    * **静态访问**：无需获取 Instance，任何地方均可调用。
    * **泛型支持**：支持无参、1参、2参传递。

```csharp
// 1. 定义事件
public enum GameEvent { PlayerDead, ScoreChange }

// 2. 发送事件
EventCenter.Broadcast(GameEvent.ScoreChange, 100);

// 3. 监听事件
EventCenter.AddListener<int>(GameEvent.ScoreChange, OnScoreChange);

// 4. 移除监听 (有监听务必移除监听)
EventCenter.RemoveListener<int>(GameEvent.ScoreChange, OnScoreChange);
```

### 5. Game Manager (状态机)
基于继承式 **FSM (Finite State Machine)** 管理游戏流程，避免 God Class。

* **结构**：
    * **MenuState**: 处理主菜单逻辑。
    * **GameplayState**: 处理核心循环、监听玩家死亡。
    * **PauseState**: 处理时间暂停、暂停菜单。

```csharp
// 切换状态
GameManager.Instance.ChangeState(new GameplayState(GameManager.Instance));

// 在 State 类内部
public override void OnEnter()
{
    Time.timeScale = 1f;
    UIManager.Instance.OpenFullScreen<GamePanel>();
}
```

### 6. Camera Manager (相机管理)
结合 **ProCamera2D** 与 **DOTween** 的高层封装，提供相机控制接口。
> *注意：此模块依赖 ProCamera2D 和 DOTween 插件，框架内置*

* **特性**：
    * **智能跟随**：支持瞬间切换或平滑过渡到新目标 (`Follow`)，以及临时聚焦 (`FocusOn`)。
    * **震动反馈**：支持 PC2D 预设震动 (`Shake`) 或代码动态震动 (`ShakeSimple`)。
    * **动态缩放**：支持平滑缩放 (`ZoomTo`) 和 **冲击变焦** (`ZoomPunch`，增加打击感)。

```csharp
// 1. 跟随玩家 (瞬间)
CameraManager.Instance.Follow(playerTransform, true);

// 2. 剧情演出：看一眼门口，2秒后自动看回玩家
CameraManager.Instance.FocusOn(doorTransform, 2.0f);

// 3. 震动：播放 Inspector 中配置好的 "Explosion" 预设
CameraManager.Instance.Shake("Explosion");

// 4. 打击感：攻击命中时，瞬间推拉镜头 (Zoom Punch)
CameraManager.Instance.ZoomPunch();
```
---

## 🚀 快速开始

1.  **初始化**：创建一个空场景 `Main`，挂载 `GameManager`。创建一个Canvas，在其下挂载一个`UIManager`.（框架中有提供Main场景，可以直接使用）
2.  **配置**：在 `InputManager`中配置你的按键。
3.  **开发**：
    * 创建 UI Prefab 放入 `Resources/UI`(继承UIBase)。
    * 编写 State 类控制流程。

## 📝 约定与规范

* **命名**：UI Prefab 的名称必须与 C# 类名 **完全一致**。
* **输入**：逻辑中尽量使用 `InputManager` 而非 `Input.GetKeyDown`，以便支持输入锁定和提示符动态替换。
* **资源**：所有动态加载资源统一存放于 `Resources` 文件夹下。