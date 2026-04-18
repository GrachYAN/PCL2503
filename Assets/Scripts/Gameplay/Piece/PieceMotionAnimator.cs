using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 棋子动画状态枚举
/// </summary>
public enum PieceAnimationState
{
    Idle,           // 静止状态
    Lifting,        // 升起中
    Lifted,         // 已升起（悬浮状态）
    Moving,         // 平移中
    Dropping,       // 落下中
}

/// <summary>
/// 棋子移动动画控制器
/// 负责管理棋子的所有动画状态和过渡：选中升起、悬浮、平移、落下
/// </summary>
[DisallowMultipleComponent]
public class PieceMotionAnimator : MonoBehaviour
{
    #region 动画参数配置

    [Header("升起动画参数")]
    [Tooltip("升起高度（相对于棋子自身高度的倍数）")]
    [SerializeField] private float liftHeightMultiplier = 1.0f;
    
    [Tooltip("升起动画持续时间（秒）")]
    [SerializeField] private float liftDuration = 0.35f;
    
    [Tooltip("升起时的向前倾斜角度（度）")]
    [SerializeField] private float liftTiltAngle = 10f;
    
    [Tooltip("升起动画的缓动曲线")]
    [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("平移动画参数")]
    [Tooltip("平移基础速度（格/秒）")]
    [SerializeField] private float moveSpeed = 4f;
    
    [Tooltip("平移动画的缓动曲线")]
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("落下动画参数")]
    [Tooltip("落下动画持续时间（秒）")]
    [SerializeField] private float dropDuration = 0.25f;
    
    [Tooltip("落下动画的缓动曲线（建议用EaseIn模拟重力）")]
    [SerializeField] private AnimationCurve dropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("旋转恢复参数")]
    [Tooltip("旋转恢复速度")]
    [SerializeField] private float rotationLerpSpeed = 8f;

    #endregion

    #region 状态变量

    // 当前动画状态
    public PieceAnimationState CurrentState { get; private set; } = PieceAnimationState.Idle;
    
    // 是否正在动画中（用于输入保护）
    public bool IsAnimating => CurrentState == PieceAnimationState.Lifting || 
                               CurrentState == PieceAnimationState.Moving || 
                               CurrentState == PieceAnimationState.Dropping;
    
    // 是否处于选中/悬浮状态（包括正在升起的状态）
    public bool IsSelected => CurrentState == PieceAnimationState.Lifting || 
                              CurrentState == PieceAnimationState.Lifted || 
                              CurrentState == PieceAnimationState.Moving;
    
    // 是否已升起
    public bool IsLifted => CurrentState == PieceAnimationState.Lifted;

    // 原始位置和旋转
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float groundY;
    
    // 升起后的目标高度
    private float liftTargetY;
    
    // 棋子高度缓存（用于计算升起高度）
    private float pieceHeight;
    
    // 当前运行的动画协程
    private Coroutine currentAnimation;

    #endregion

    #region 事件回调

    /// <summary>
    /// 动画完成回调 - 参数：动画类型，是否成功完成
    /// </summary>
    public event Action<string, bool> OnAnimationComplete;

    /// <summary>
    /// 移动动画完成回调 - 参数：目标位置
    /// </summary>
    public event Action<Vector3> OnMoveComplete;

    #endregion

    #region 初始化

    private void Awake()
    {
        // 缓存初始状态
        CaptureGroundState();
        
        // 估算棋子高度（用于计算升起高度）
        CalculatePieceHeight();
    }

    /// <summary>
    /// 捕获当前地面状态（位置和旋转）
    /// </summary>
    public void CaptureGroundState()
    {
        originalPosition = transform.position;
        groundY = transform.position.y;
        originalRotation = transform.rotation;
    }

    /// <summary>
    /// 更新地面Y坐标（用于移动后更新参考点）
    /// </summary>
    public void UpdateGroundY(float newY)
    {
        groundY = newY;
        originalPosition.y = newY;
    }

    /// <summary>
    /// 计算棋子高度（用于升起高度计算）
    /// </summary>
    private void CalculatePieceHeight()
    {
        // 尝试从Renderer获取bounds高度
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            pieceHeight = rend.bounds.size.y;
        }
        else
        {
            // 默认高度
            pieceHeight = 0.5f;
        }
    }

    #endregion

    #region 公共动画接口

    /// <summary>
    /// 播放选中升起动画
    /// </summary>
    /// <param name="onComplete">动画完成回调</param>
    /// <returns>是否成功开始动画</returns>
    public bool PlayLiftAnimation(Action onComplete = null)
    {
        // 输入保护：如果正在动画中，不响应
        if (IsAnimating)
        {
            Debug.LogWarning($"[{nameof(PieceMotionAnimator)}] 无法升起：当前正在动画中 - {CurrentState}");
            return false;
        }

        // 如果已经升起，直接返回成功
        if (IsLifted)
        {
            onComplete?.Invoke();
            return true;
        }

        // 停止之前的动画
        StopCurrentAnimation();

        // 捕获当前地面状态
        CaptureGroundState();
        
        // 计算升起目标高度
        liftTargetY = groundY + (pieceHeight * liftHeightMultiplier);

        // 开始升起动画
        currentAnimation = StartCoroutine(LiftCoroutine(onComplete));
        return true;
    }

    /// <summary>
    /// 播放取消选中落下动画（落回原位）
    /// </summary>
    /// <param name="onComplete">动画完成回调</param>
    /// <returns>是否成功开始动画</returns>
    public bool PlayCancelDropAnimation(Action onComplete = null)
    {
        // 如果不在选中状态，无需动画
        if (!IsSelected && CurrentState == PieceAnimationState.Idle)
        {
            onComplete?.Invoke();
            return true;
        }

        // 停止之前的动画（包括正在升起的动画）
        StopCurrentAnimation();

        // 开始落下动画（落回originalPosition）
        currentAnimation = StartCoroutine(DropCoroutine(originalPosition, originalRotation, onComplete));
        return true;
    }

    /// <summary>
    /// 播放移动到目标位置的完整动画（平移+落下）
    /// </summary>
    /// <param name="targetGroundPos">目标地面位置</param>
    /// <param name="onComplete">动画完成回调</param>
    /// <returns>是否成功开始动画</returns>
    public bool PlayMoveAnimation(Vector3 targetGroundPos, Action onComplete = null)
    {
        // 输入保护：只在真正的动画状态下才阻止，Lifting 状态应该允许移动
        if (CurrentState == PieceAnimationState.Moving)
        {
            Debug.LogWarning($"[{nameof(PieceMotionAnimator)}] 无法移动：当前正在动画中 - {CurrentState}");
            return false;
        }

        // 停止之前的动画
        StopCurrentAnimation();

        // 开始移动动画
        currentAnimation = StartCoroutine(MoveCoroutine(targetGroundPos, onComplete));
        return true;
    }

    /// <summary>
    /// 立即停止所有动画并复位
    /// </summary>
    public void StopAndReset()
    {
        StopCurrentAnimation();
        
        // 立即复位到地面
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        transform.rotation = originalRotation;
        
        CurrentState = PieceAnimationState.Idle;
    }

    /// <summary>
    /// 强制设置到指定状态（用于网络同步等场景）
    /// </summary>
    public void ForceSetState(Vector3 position, Quaternion rotation, PieceAnimationState state)
    {
        StopCurrentAnimation();
        
        transform.position = position;
        transform.rotation = rotation;
        CurrentState = state;
        
        if (state == PieceAnimationState.Idle)
        {
            CaptureGroundState();
        }
    }

    #endregion

    #region 动画协程

    /// <summary>
    /// 升起动画协程
    /// </summary>
    private IEnumerator LiftCoroutine(Action onComplete)
    {
        CurrentState = PieceAnimationState.Lifting;
        
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        Vector3 targetPos = new Vector3(startPos.x, liftTargetY, startPos.z);
        Quaternion targetRot = originalRotation * Quaternion.Euler(-liftTiltAngle, 0f, 0f);
        
        float elapsed = 0f;
        
        while (elapsed < liftDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / liftDuration);
            float curvedT = liftCurve.Evaluate(t);
            
            // 位置插值
            transform.position = Vector3.Lerp(startPos, targetPos, curvedT);
            
            // 旋转插值
            transform.rotation = Quaternion.Slerp(startRot, targetRot, curvedT);
            
            yield return null;
        }
        
        // 确保最终状态
        transform.position = targetPos;
        transform.rotation = targetRot;
        
        CurrentState = PieceAnimationState.Lifted;
        
        onComplete?.Invoke();
        OnAnimationComplete?.Invoke("Lift", true);
    }

    /// <summary>
    /// 落下动画协程
    /// </summary>
    private IEnumerator DropCoroutine(Vector3 targetPos, Quaternion targetRot, Action onComplete)
    {
        CurrentState = PieceAnimationState.Dropping;
        
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        // 目标位置的Y使用地面Y
        Vector3 finalPos = new Vector3(targetPos.x, groundY, targetPos.z);
        
        float elapsed = 0f;
        
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            float curvedT = dropCurve.Evaluate(t);
            
            // 位置插值
            transform.position = Vector3.Lerp(startPos, finalPos, curvedT);
            
            // 旋转插值（恢复原始旋转）
            transform.rotation = Quaternion.Slerp(startRot, targetRot, curvedT);
            
            yield return null;
        }
        
        // 确保最终状态
        transform.position = finalPos;
        transform.rotation = targetRot;
        
        CurrentState = PieceAnimationState.Idle;
        
        // 更新原始位置
        originalPosition = finalPos;
        originalRotation = targetRot;
        
        onComplete?.Invoke();
        OnAnimationComplete?.Invoke("Drop", true);
    }

    /// <summary>
    /// 完整移动动画协程（平移+落下）
    /// </summary>
    private IEnumerator MoveCoroutine(Vector3 targetGroundPos, Action onComplete)
    {
        // 阶段1：确保已升起
        if (!IsLifted)
        {
            // 在“未先选中直接移动”（常见于网络同步远端客户端）时，
            // 必须先基于当前地面状态计算 liftTargetY。
            // 否则 liftTargetY 会沿用默认值(0)或旧值，导致动画顺序看起来像
            // “先下沉(drop) -> 平移(move) -> 最后上升(lift)”。
            CaptureGroundState();
            liftTargetY = groundY + (pieceHeight * liftHeightMultiplier);
            yield return StartCoroutine(LiftCoroutine(null));
        }
        
        // 阶段2：水平平移
        yield return StartCoroutine(HorizontalMoveCoroutine(targetGroundPos));
        
        // 阶段3：落下
        yield return StartCoroutine(DropCoroutine(targetGroundPos, originalRotation, null));
        
        onComplete?.Invoke();
        OnMoveComplete?.Invoke(targetGroundPos);
        OnAnimationComplete?.Invoke("Move", true);
    }

    /// <summary>
    /// 水平平移动画协程
    /// </summary>
    private IEnumerator HorizontalMoveCoroutine(Vector3 targetGroundPos)
    {
        CurrentState = PieceAnimationState.Moving;
        
        Vector3 startPos = transform.position;
        
        // 目标位置保持当前高度（悬浮高度）
        Vector3 targetPos = new Vector3(targetGroundPos.x, transform.position.y, targetGroundPos.z);
        
        // 计算距离和预计时间
        float distance = Vector3.Distance(
            new Vector3(startPos.x, 0, startPos.z),
            new Vector3(targetPos.x, 0, targetPos.z)
        );
        float duration = distance / moveSpeed;
        
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve.Evaluate(t);
            
            // 水平位置插值
            Vector3 newPos = Vector3.Lerp(startPos, targetPos, curvedT);
            transform.position = newPos;
            
            // 保持倾斜角度
            Quaternion targetRot = originalRotation * Quaternion.Euler(-liftTiltAngle, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);
            
            yield return null;
        }
        
        // 确保最终位置
        transform.position = targetPos;
        
        // 地面Y坐标保持不变，因为棋子在同一高度的地面上移动
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 停止当前动画
    /// </summary>
    private void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

    #endregion

    #region Gizmos调试

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 绘制升起高度指示
        if (Application.isPlaying && pieceHeight > 0)
        {
            Gizmos.color = Color.yellow;
            Vector3 pos = transform.position;
            float targetY = groundY + (pieceHeight * liftHeightMultiplier);
            Gizmos.DrawLine(pos, new Vector3(pos.x, targetY, pos.z));
            Gizmos.DrawWireSphere(new Vector3(pos.x, targetY, pos.z), 0.1f);
        }
    }
    #endif

    #endregion
}