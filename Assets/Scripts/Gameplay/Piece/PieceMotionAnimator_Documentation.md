# PieceMotionAnimator 组件说明文档

## 概述

`PieceMotionAnimator` 是Unity棋盘游戏中负责棋子移动动画的专用控制组件。它实现了完整的"选中升起→悬浮→平移→落下"动画流程，为棋子移动提供流畅自然的视觉反馈。

## 功能特性

### 1. 完整动画流程
- **选中升起**：棋子平滑升起并向前倾斜，营造"被拿起"的视觉效果
- **悬浮保持**：升起后保持悬浮状态，直观表示棋子已被选中
- **水平平移**：保持悬浮高度平滑移动至目标格子上方
- **垂直落下**：从悬浮高度自然下落至目标格子
- **取消复位**：取消选中时平滑落回原位

### 2. 状态机管理
组件内置完整的状态机系统：
- `Idle` - 静止状态
- `Lifting` - 升起中
- `Lifted` - 已升起（悬浮状态）
- `Moving` - 平移中
- `Dropping` - 落下中

### 3. 输入保护机制
- `IsAnimating` - 标识是否正在动画中（防止重复输入）
- `IsSelected` - 标识是否处于选中/悬浮状态
- `IsLifted` - 标识是否已升起

## Inspector参数配置

### 升起动画参数 (Lift Settings)

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `liftHeightMultiplier` | float | 1.0 | 升起高度倍数（相对于棋子自身高度） |
| `liftDuration` | float | 0.35 | 升起动画持续时间（秒） |
| `liftTiltAngle` | float | 10 | 升起时的向前倾斜角度（度） |
| `liftCurve` | AnimationCurve | EaseInOut | 升起动画的缓动曲线 |

### 平移动画参数 (Move Settings)

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `moveSpeed` | float | 4 | 平移基础速度（格/秒） |
| `moveCurve` | AnimationCurve | EaseInOut | 平移动画的缓动曲线 |

### 落下动画参数 (Drop Settings)

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `dropDuration` | float | 0.25 | 落下动画持续时间（秒） |
| `dropCurve` | AnimationCurve | EaseInOut | 落下动画的缓动曲线 |

### 旋转恢复参数 (Rotation Settings)

| 参数名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| `rotationLerpSpeed` | float | 8 | 旋转恢复速度 |

## 公共API

### 动画播放方法

```csharp
// 播放选中升起动画
public bool PlayLiftAnimation(Action onComplete = null)

// 播放取消选中落下动画（落回原位）
public bool PlayCancelDropAnimation(Action onComplete = null)

// 播放移动到目标位置的完整动画（平移+落下）
public bool PlayMoveAnimation(Vector3 targetGroundPos, Action onComplete = null)
```

### 状态查询属性

```csharp
// 当前动画状态
public PieceAnimationState CurrentState { get; }

// 是否正在动画中（用于输入保护）
public bool IsAnimating { get; }

// 是否处于选中/悬浮状态
public bool IsSelected { get; }

// 是否已升起
public bool IsLifted { get; }
```

### 事件回调

```csharp
// 动画完成回调 - 参数：动画类型，是否成功完成
public event Action<string, bool> OnAnimationComplete;

// 移动动画完成回调 - 参数：目标位置
public event Action<Vector3> OnMoveComplete;
```

### 控制方法

```csharp
// 立即停止所有动画并复位
public void StopAndReset()

// 强制设置到指定状态（用于网络同步等场景）
public void ForceSetState(Vector3 position, Quaternion rotation, PieceAnimationState state)

// 捕获当前地面状态（位置和旋转）
public void CaptureGroundState()

// 更新地面Y坐标（用于移动后更新参考点）
public void UpdateGroundY(float newY)
```

## 参数调整指南

### 调整升起高度

**参数**: `liftHeightMultiplier`

**效果**: 
- 值 < 1.0：升起高度小于棋子自身高度（较保守）
- 值 = 1.0：升起高度等于棋子自身高度（推荐默认值）
- 值 > 1.0：升起高度大于棋子自身高度（更夸张的效果）

**建议**: 
- 小型棋子（如兵）：1.0 - 1.2
- 大型棋子（如王、后）：0.8 - 1.0

### 调整升起速度

**参数**: `liftDuration`

**效果**: 
- 值 < 0.3：快速升起，更敏捷的感觉
- 值 = 0.35：平衡的速度（默认）
- 值 > 0.5：缓慢升起，更沉稳的感觉

### 调整倾斜角度

**参数**: `liftTiltAngle`

**效果**: 
- 值 = 0：无倾斜，垂直升起
- 值 = 5-10：轻微倾斜，自然的"拿起"效果（推荐）
- 值 = 15+：明显倾斜，更戏剧化的效果

**建议**: 
- 写实风格：5-8度
- 卡通风格：10-15度

### 调整平移速度

**参数**: `moveSpeed`

**效果**: 
- 值 < 3：缓慢移动，更沉稳
- 值 = 4：平衡的速度（默认）
- 值 > 6：快速移动，更敏捷

**计算**: 移动时间 = 距离 / moveSpeed
- 移动1格（距离约1单位）约需 0.25秒

### 调整落下速度

**参数**: `dropDuration`

**效果**: 
- 值 < 0.2：快速落下，更干脆
- 值 = 0.25：平衡的速度（默认）
- 值 > 0.4：缓慢落下，更柔和

**建议**: 
- 配合 `dropCurve` 使用 EaseIn 曲线可以模拟重力加速度效果

### 调整缓动曲线

#### 升起曲线 (`liftCurve`)
- **EaseInOut** (默认): 平滑加速和减速
- **EaseOut**: 快速启动，缓慢结束
- **EaseIn**: 缓慢启动，快速结束

#### 平移曲线 (`moveCurve`)
- **EaseInOut** (默认): 平滑移动
- **Linear**: 匀速移动
- **EaseOut**: 快速启动，到达目标时减速

#### 落下曲线 (`dropCurve`)
- **EaseIn** (推荐): 模拟重力加速度，开始慢结束快
- **EaseInOut** (默认): 柔和的落下
- **Bounce**: 弹跳效果（需要自定义曲线）

## 使用示例

### 基本使用（在Piece类中）

```csharp
public class Piece : MonoBehaviour
{
    // 获取动画控制器
    public PieceMotionAnimator MotionAnimator { get; private set; }
    
    private void Awake()
    {
        MotionAnimator = GetComponent<PieceMotionAnimator>();
        if (MotionAnimator == null)
        {
            MotionAnimator = gameObject.AddComponent<PieceMotionAnimator>();
        }
    }
    
    // 选中时播放升起动画
    public void OnSelected()
    {
        MotionAnimator.PlayLiftAnimation();
    }
    
    // 取消选中时播放落下动画
    public void OnDeselected()
    {
        MotionAnimator.PlayCancelDropAnimation();
    }
    
    // 移动时播放完整动画
    public void MoveTo(Vector2 targetPosition)
    {
        Vector3 targetWorldPos = new Vector3(targetPosition.x, transform.position.y, targetPosition.y);
        MotionAnimator.PlayMoveAnimation(targetWorldPos, OnMoveComplete);
    }
    
    private void OnMoveComplete()
    {
        // 动画完成后的回调
        Debug.Log("移动完成！");
    }
}
```

### 在InputManager中使用

```csharp
public class InputManager : MonoBehaviour
{
    private Piece selectedPiece;
    
    // 选中棋子
    private void SelectPiece(Piece piece)
    {
        // 取消之前选中的棋子
        if (selectedPiece != null)
        {
            selectedPiece.MotionAnimator.PlayCancelDropAnimation();
        }
        
        selectedPiece = piece;
        
        // 播放新棋子的升起动画
        selectedPiece.MotionAnimator.PlayLiftAnimation();
    }
    
    // 移动棋子
    private IEnumerator MovePiece(Vector2 targetCoords)
    {
        // 等待动画完成
        bool complete = false;
        selectedPiece.MotionAnimator.PlayMoveAnimation(
            new Vector3(targetCoords.x, 0, targetCoords.y),
            () => complete = true
        );
        
        while (!complete)
        {
            yield return null;
        }
        
        // 动画完成后执行后续逻辑
        EndTurn();
    }
}
```

## 性能优化建议

1. **对象池**: 对于频繁创建销毁的棋子，使用对象池并复用PieceMotionAnimator组件
2. **LOD**: 对于远距离的棋子，可以降低动画精度或禁用动画
3. **批量动画**: 避免同时播放过多动画，可以使用队列系统

## 常见问题排查

### 动画不播放
- 检查 `IsAnimating` 属性，确保没有其他动画正在播放
- 检查目标位置是否与当前位置相同
- 查看Console中的警告信息

### 动画卡顿
- 检查 `liftDuration` 和 `dropDuration` 是否合理
- 确保缓动曲线没有异常的跳变点
- 检查是否有其他脚本在修改Transform

### 棋子位置不正确
- 确保 `CaptureGroundState()` 在正确的时机被调用
- 检查 `UpdateGroundY()` 是否在移动后被调用
- 使用 `ForceSetState()` 进行强制同步

## 扩展建议

1. **添加音效**: 在动画不同阶段播放音效（升起、移动、落下）
2. **粒子效果**: 在棋子落下时添加尘埃粒子效果
3. **阴影效果**: 添加动态阴影，随高度变化
4. **材质变化**: 选中时改变棋子材质或添加发光效果
