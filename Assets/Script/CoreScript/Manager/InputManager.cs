using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

public enum InputLockLevel
{
    None = 0,
    MovementLocked = 1,    // 锁定移动，允许交互/菜单
    InteractionLocked = 2, // 锁定交互 + 移动
    AllLocked = 3          // 全锁定 (除了 ESC)
}

public class InputManager : MonoSingleton<InputManager>
{
    [Header("Key Bindings (Keyboard)")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode spyKey = KeyCode.F;
    public KeyCode lightKey = KeyCode.Q;
    public KeyCode mapKey = KeyCode.M;
    public KeyCode ruleKey = KeyCode.Tab;
    public KeyCode runKey = KeyCode.LeftShift;
    public KeyCode upStairKey = KeyCode.W;
    public KeyCode downStairKey = KeyCode.S;
    public KeyCode pauseKey = KeyCode.Escape;


    private Dictionary<string, string> keyDisplayCache = new ();//键位映射表
    // 锁定状态
    private InputLockLevel currentLockLevel = InputLockLevel.None;

    // 状态检查属性
    private bool NoLock => currentLockLevel == InputLockLevel.None;
    private bool LockMove => currentLockLevel >= InputLockLevel.MovementLocked;
    private bool LockInteract => currentLockLevel >= InputLockLevel.InteractionLocked;
    private bool LockAll => currentLockLevel == InputLockLevel.AllLocked;

    protected void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        BuildKeyCache();
    }

    /// <summary>
    /// 利用反射自动构建 "动作名" 到 "按键字符" 的映射
    /// </summary>
    private void BuildKeyCache()
    {
        keyDisplayCache.Clear();

        keyDisplayCache["Move"] = "WASD";
        keyDisplayCache["Esc"] = "ESC";

        FieldInfo[] fields = this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);

        foreach (var field in fields)
        {
            if (field.FieldType == typeof(KeyCode) && field.Name.EndsWith("Key"))
            {
                string rawName = field.Name.Substring(0, field.Name.Length - 3);
                // 2. 首字母大写 (interact -> Interact) 以匹配标签 [Interact]
                string actionName = char.ToUpper(rawName[0]) + rawName.Substring(1);

                // 获取当前按键的值
                KeyCode code = (KeyCode) field.GetValue(this);

                // 存入缓存: Key="Interact", Value="[E]"
                if (!keyDisplayCache.ContainsKey(actionName))
                {
                    keyDisplayCache.Add(actionName, $"[{code.ToString()}]");
                }
            }
        }

        // foreach(var kv in keyDisplayCache) Debug.Log($"Loaded: [{kv.Key}] => {kv.Value}");
    }

    #region 锁定控制 API

    public void SetLockLevel(InputLockLevel level)
    {
        currentLockLevel = level;
    }

    public void IncreaseLockLevel(InputLockLevel level)
    {
        if (level > currentLockLevel)
        {
            currentLockLevel = level;
        }
    }

    // 快捷重置
    public void ResetLock()
    {
        currentLockLevel = InputLockLevel.None;
    }

    #endregion

    #region 核心输入查询 API

    // --- 移动类 ---
    // 为了手感顺滑，移动依然建议保留 GetAxisRaw，这样利用了 Unity 默认的 Input Manager (WASD/Arrows)
    // 同时也支持 GetKey

    public float GetMoveX()
    {
        if (LockMove) return 0f;
        return Input.GetAxisRaw("Horizontal");
    }

    public float GetMoveY()
    {
        if (LockMove) return 0f;
        return Input.GetAxisRaw("Vertical");
    }

    public bool WalkLeft()
    {
        if (LockMove) return false;
        return Input.GetAxisRaw("Horizontal") < -0.1f;
    }

    public bool WalkRight()
    {
        if (LockMove) return false;
        return Input.GetAxisRaw("Horizontal") > 0.1f;
    }

    public bool WalkBack()
    {
        if (LockMove) return false;
        // 假设后退是 S 键或下方向键
        return Input.GetAxisRaw("Vertical") < -0.1f;
    }

    public bool Run()
    {
        if (LockMove) return false;
        return Input.GetKey(runKey);
    }

    // --- 楼层/交互类 ---

    public bool UpStair()
    {
        if (LockMove) return false;
        return Input.GetKeyDown(upStairKey);
    }

    public bool DownStair()
    {
        if (LockMove) return false;
        return Input.GetKeyDown(downStairKey);
    }

    public bool Interact()
    {
        if (LockInteract) return false;
        // 允许 E 键 
        return Input.GetKeyDown(interactKey);
    }

    // --- 功能类 ---

    public bool Spy()
    {
        if (LockMove) return false;
        return Input.GetKeyDown(spyKey);
    }

    public bool Light()
    {
        if (LockMove) return false;
        return Input.GetKeyDown(lightKey);
    }

    public bool Map()
    {
        if (LockAll) return false;
        return Input.GetKeyDown(mapKey);
    }

    public bool Rule()
    {
        if (LockAll) return false;
        return Input.GetKeyDown(ruleKey);
    }

    public bool Esc()
    {
        // 永不锁定
        return Input.GetKeyDown(pauseKey);
    }

    #endregion

    #region UI 显示支持 (GetInputDisplay)

    /// <summary>
    /// 获取按键提示字符
    /// </summary>
    private string GetInputDisplay(string actionName)
    {
        if (keyDisplayCache.ContainsKey(actionName))
        {
            return keyDisplayCache[actionName];
        }
        // 如果找不到映射，原样返回，方便排查错误
        return $"[{actionName}?]";
    }

    /// <summary>
    /// 解析字符串：使用正则自动查找所有 [Xxx] 格式
    /// </summary>
    public string ParseInputString(string rawText)
    {
        if (string.IsNullOrEmpty(rawText)) return "";

        // 正则表达式：匹配中括号内的单词，例如 [Interact] 会匹配出 "Interact"
        // 解释：\[ 匹配左括号, (\w+) 匹配并捕获中间的单词, \] 匹配右括号
        return Regex.Replace(rawText, @"\[(\w+)\]", (match) =>
        {
            string key = match.Groups[1].Value;
            return GetInputDisplay(key);
        });
    }

    #endregion
}