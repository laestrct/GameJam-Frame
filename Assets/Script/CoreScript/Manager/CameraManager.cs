using UnityEngine;
using DG.Tweening; 
using Com.LuisPedroFonseca.ProCamera2D;

/// <summary>
/// 相机管理器
/// 简单的封装了一些PC2D和DoTween的功能
/// </summary>
public class CameraManager : MonoSingleton<CameraManager>
{
    // 核心组件引用
    private ProCamera2D core;
    private ProCamera2DShake shake;

    // 状态记录
    private float defaultSize = 5f;
    private Tween zoomTween; // 记录当前的缩放Tween，防止冲突

    protected void Awake()
    {
        InitComponents();
    }

    private void InitComponents()
    {
        core = ProCamera2D.Instance;
        if (core == null)
        {
            Debug.LogError("[CameraManager] 场景中缺少 ProCamera2D 核心组件！");
            return;
        }

        shake = core.GetComponent<ProCamera2DShake>();
        if (shake == null)
        {
            Debug.LogWarning("[CameraManager] ProCamera2D 缺少 Shake 扩展，震动功能将不可用。");
        }

        // 记录初始大小，用于 ResetZoom
        if (core.GameCamera != null)
        {
            defaultSize = core.GameCamera.orthographicSize;
        }
    }

    #region 跟随 (Follow)

    /// <summary>
    /// 立即切换跟随目标 (用于玩家重生、切换角色)
    /// </summary>
    /// <param name="target">目标Transform</param>
    /// <param name="immediate">true=瞬移，false=平滑移动</param>
    public void Follow(Transform target, bool immediate = true)
    {
        if (core == null || target == null) return;

        // PC2D 支持多目标，但Jam通常只跟一个
        core.RemoveAllCameraTargets();
        core.AddCameraTarget(target);

        if (immediate)
        {
            core.MoveCameraInstantlyToPosition(target.position);
        }
    }

    /// <summary>
    /// 临时聚焦某个物体 (用于剧情演出：看一眼门开了，再看回玩家)
    /// </summary>
    public void FocusOn(Transform target, float duration, System.Action onComplete = null)
    {
        if (core == null) return;

        var oldTargets = new System.Collections.Generic.List<CameraTarget>(core.CameraTargets);
        Follow(target, false);

        // 延时恢复
        DOVirtual.DelayedCall(duration, () =>
        {
            core.RemoveAllCameraTargets();
            foreach (var t in oldTargets) core.AddCameraTarget(t.TargetTransform);
            onComplete?.Invoke();
        });
    }

    #endregion

    #region 震动 (Shake - Powered by PC2D)

    /// <summary>
    /// 播放震动预设 (需要在 Inspector 的 ProCamera2DShake 组件里配好 Preset)
    /// </summary>
    /// <param name="presetName">预设名称 (如 "Explosion", "Hit")</param>
    public void Shake(string presetName)
    {
        if (shake != null) shake.Shake(presetName);
    }

    /// <summary>
    /// 快速代码震动 (无需预设)
    /// </summary>
    /// <param name="strength">强度 (0.5 ~ 3)</param>
    /// <param name="duration">时间</param>
    public void ShakeSimple(float strength = 1f, float duration = 0.5f)
    {
        if (shake != null)
            shake.Shake(duration, new Vector2(strength, strength));
    }

    #endregion

    #region 缩放 (Zoom - Powered by DoTween)

    /// <summary>
    /// 缩放到指定大小 (平滑)
    /// </summary>
    /// <param name="targetSize">目标大小 (越小物体越大)</param>
    /// <param name="duration">时间</param>
    public void ZoomTo(float targetSize, float duration = 0.5f)
    {
        if (core == null || core.GameCamera == null) return;

        // 杀掉旧的动画，防止连点鬼畜
        zoomTween?.Kill();

        // 使用 DoTween 直接改变相机的 OrthographicSize
        zoomTween = core.GameCamera.DOOrthoSize(targetSize, duration)
            .SetEase(Ease.OutQuad) 
            .OnUpdate(() =>
            {
                // *重要*：如果在 PC2D 里开启了某些强制缩放选项，可能需要手动通知它更新
                // core.UpdateScreenSize(core.GameCamera.orthographicSize); 
            });
    }

    /// <summary>
    /// 恢复默认缩放
    /// </summary>
    public void ResetZoom(float duration = 0.5f)
    {
        ZoomTo(defaultSize, duration);
    }

    /// <summary>
    /// 冲击变焦- 瞬间拉近再弹回，用于强打击感
    /// </summary>
    public void ZoomPunch(float punchAmount = -2f, float duration = 0.3f)
    {
        if (core == null || core.GameCamera == null) return;

        zoomTween?.Kill();
        zoomTween = core.GameCamera.DOOrthoSize(defaultSize + punchAmount, duration)
            .SetEase(Ease.OutElastic) // 弹性效果
            .OnComplete(() => ResetZoom(0.2f)); // 确保归位
    }

    #endregion
}