using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


/// <summary>
/// 注：三种UI的区别：
/// 1，全局UI,通常用于全局HUD(如状态和背包栏)，互斥显示，同时只能有一个
/// 2，面板UI,通常用于游戏内各种功能面板，采用栈管理，比如背包页面，以及背包内点开的物品详情页面
/// 3，弹窗UI,通常用于提示信息，独立显示，覆盖所有UI，通常自行管理生命周期
/// </summary>
public class UIManager : MonoSingleton<UIManager>
{
    // 配置路径：Resources/Prefabs/UI/
    private const string UI_PREFAB_PATH = "Prefabs/UI/";

    // 层级节点
    private Transform canvasTransform;
    private Transform layerFullScreen; // 底层
    private Transform layerPanel;      // 中层（栈管理）
    private Transform layerPopup;      // 顶层

    // 状态存储
    private UIBase currentFullScreenUI;
    private List<UIBase> panelList = new();
    private Dictionary<string, GameObject> prefabCache = new();

    public void Awake()
    {
        InitUIStructure();
    }

    #region 查询方法

    /// <summary>
    /// 获取指定类型的 UI 实例
    /// 查找顺序：全屏UI -> 面板栈 (栈顶优先) -> 弹窗层
    /// </summary>
    /// <typeparam name="T">具体的 UIBase 子类</typeparam>
    /// <returns>找到的实例，未找到返回 null</returns>
    public T GetUI<T>() where T : UIBase
    {
        // 1. 检查当前全屏 UI
        if (currentFullScreenUI is T fullScreenUI)
        {
            return fullScreenUI;
        }

        // 2. 检查面板列表
        // 倒序遍历，优先返回栈顶（当前显示）的面板
        for (var i = panelList.Count - 1; i >= 0; i--)
        {
            if (panelList[i] is T panel)
            {
                return panel;
            }
        }

        // 3. 检查弹窗层
        // 由于 OpenPopup 中没有将弹窗加入 List 管理，必须去 Transform 层级查找
        // includeInactive: true 确保即使 UI 处于非激活状态（如被对象池回收或隐藏）也能找到
        var popup = layerPopup.GetComponentInChildren<T>(true);
        if (popup != null)
        {
            return popup;
        }

        return null;
    }

    /// <summary>
    /// 尝试获取 UI，如果存在则执行操作
    /// </summary>
    public bool TryGetUI<T>(out T uiInstance) where T : UIBase
    {
        uiInstance = GetUI<T>();
        return uiInstance != null;
    }

    #endregion


    /// <summary>
    /// 自动初始化Canvas结构
    /// </summary>
    private void InitUIStructure()
    {
        var canvasObj = GameObject.Find("Canvas");
        if (canvasObj == null)
        {
            canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        canvasTransform = canvasObj.transform;
        DontDestroyOnLoad(canvasObj);

        // 创建三个层级父节点
        layerFullScreen = transform.Find("Layer_FullScreen") ?? CreateLayer("Layer_FullScreen", 0);
        layerPanel = transform.Find("Layer_Panel") ?? CreateLayer("Layer_Panel", 100);
        layerPopup = transform.Find("Layer_Popup") ?? CreateLayer("Layer_Popup", 200);
    }

    private Transform CreateLayer(string name, int sortOrder)
    {
        var layer = new GameObject(name);
        layer.transform.SetParent(canvasTransform, false);

        // 加上Canvas确保层级顺序，覆盖SortingOrder
        var canvas = layer.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = sortOrder;
        layer.AddComponent<GraphicRaycaster>(); // 确保能点击

        // 强制全屏拉伸
        var rect = layer.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return layer.transform;
    }

    #region 核心加载方法

    /// <summary>
    /// 加载UI Prefab，约定Prefab名必须和类名一致
    /// </summary>
    private T LoadUI<T>(Transform parent) where T : UIBase
    {
        string uiName = typeof(T).Name;
        if (!prefabCache.TryGetValue(uiName, out var prefab))
        {
            prefab = Resources.Load<GameObject>(UI_PREFAB_PATH + uiName);
            if (prefab == null)
            {
                Debug.LogError($"[UIManager] 找不到Prefab: {UI_PREFAB_PATH}{uiName}");
                return null;
            }
            prefabCache[uiName] = prefab;
        }

        var obj = Instantiate(prefab, parent);
        var rect = obj.GetComponent<RectTransform>();
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one; // 修正缩放

        return obj.GetComponent<T>();
    }

    #endregion

    #region 通用方法 

    /// <summary>
    /// 通用的 UI 关闭方法
    /// 自动识别 UI 类型（面板/全屏/弹窗）并执行对应逻辑
    /// </summary>
    /// <param name="ui">要关闭的 UI 实例</param>
    public void CloseUI(UIBase ui)
    {
        if (ui == null) return;

        if (panelList.Contains(ui))
        {
            ClosePanel(ui);
            return;
        }

        if (currentFullScreenUI == ui)
        {
            currentFullScreenUI = null;
            ui.OnClose();
            return;
        }

        ui.OnClose();
    }

    #endregion

    #region 全屏 UI (互斥，同时只有一个)

    public T OpenFullScreen<T>(object args = null) where T : UIBase
    {
        //如果有旧的，关掉
        if (currentFullScreenUI != null)
        {
            currentFullScreenUI.OnClose();
        }

        var ui = LoadUI<T>(layerFullScreen);
        if (ui != null)
        {
            currentFullScreenUI = ui;
            ui.OnEnter(args);
        }
        return ui;
    }

    #endregion

    #region 面板 UI (栈管理)
    /// <summary>
    /// 打开某个面板UI
    /// 注：该UI不应该在内部关闭自身
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    /// <returns></returns>
    public T Open<T>(object args = null) where T : UIBase
    {
        // 暂停栈顶的上一个面板
        if (panelList.Count > 0)
        {
            var top = panelList[^1]; // 获取末尾元素
            top.OnPause();
        }

        var ui = LoadUI<T>(layerPanel);
        if (ui != null)
        {
            panelList.Add(ui);
            ui.OnEnter(args);
        }
        return ui;
    }

    /// <summary>
    /// 关闭栈顶面板
    /// </summary>
    public void CloseTopPanel()
    {
        if (panelList.Count == 0) return;

        // 获取栈顶
        var topUI = panelList[^1];
        ClosePanel(topUI);
    }

    /// <summary>
    /// 关闭指定的面板实例
    /// </summary>
    public void ClosePanel(UIBase panel)
    {
        if (panel == null) return;

        if (!panelList.Contains(panel))
        {
            panel.OnClose();
            return;
        }
        bool isTop = panelList.IndexOf(panel) == panelList.Count - 1;

        panelList.Remove(panel);
        panel.OnClose();

        if (isTop && panelList.Count > 0)
        {
            var newTop = panelList[^1];
            newTop.OnResume();
        }
    }

    /// <summary>
    /// 关闭所有面板
    /// </summary>
    public void CloseAllPanels()
    {
        while (panelList.Count > 0)
        {
            ClosePanel(panelList[^1]);
        }
    }

    #endregion

    #region 弹窗 UI (独立，覆盖所有)

    /// <summary>
    /// 打开某个弹窗UI
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="args"></param>
    /// <returns></returns>
    public T OpenPopup<T>(object args) where T : UIBase
    {
        var ui = LoadUI<T>(layerPopup);
        if (ui != null)
        {
            ui.OnEnter(args);
        }
        return ui;
    }

    #endregion
}