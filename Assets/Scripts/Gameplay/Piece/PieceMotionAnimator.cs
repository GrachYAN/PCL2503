using System;
using System.Collections;
using UnityEngine;

public enum PieceAnimationState
{
    Idle,
    Lifting,
    Lifted,
    Moving,
    Turning,
    Dropping,
}

[DisallowMultipleComponent]
public class PieceMotionAnimator : MonoBehaviour
{
    [Header("Lift Animation")]
    [SerializeField] private float liftHeightMultiplier = 1.0f;
    [SerializeField] private float liftDuration = 0.35f;
    [SerializeField] private float liftTiltAngle = 10f;
    [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Move Animation")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Drop Animation")]
    [SerializeField] private float dropDuration = 0.25f;
    [SerializeField] private AnimationCurve dropCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Facing Turn")]
    [SerializeField] private float facingTurnDuration = 0.15f;

    [Header("Rotation Recovery")]
    [SerializeField] private float rotationLerpSpeed = 8f;

    public PieceAnimationState CurrentState { get; private set; } = PieceAnimationState.Idle;

    public bool IsAnimating => CurrentState == PieceAnimationState.Lifting ||
                               CurrentState == PieceAnimationState.Moving ||
                               CurrentState == PieceAnimationState.Turning ||
                               CurrentState == PieceAnimationState.Dropping;

    public bool IsSelected => CurrentState == PieceAnimationState.Lifting ||
                              CurrentState == PieceAnimationState.Lifted ||
                              CurrentState == PieceAnimationState.Moving;

    public bool IsLifted => CurrentState == PieceAnimationState.Lifted;
    public float GroundY => groundY;
    public Quaternion GroundRotation => originalRotation;
    public float PieceHeight => pieceHeight;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float groundY;
    private float liftTargetY;
    private float pieceHeight;
    private Coroutine currentAnimation;

    public event Action<string, bool> OnAnimationComplete;
    public event Action<Vector3> OnMoveComplete;

    private void Awake()
    {
        CaptureGroundState();
        CalculatePieceHeight();
    }

    public void CaptureGroundState()
    {
        originalPosition = transform.position;
        groundY = transform.position.y;
        originalRotation = transform.rotation;
    }

    public void SyncFacing(Quaternion groundRotation)
    {
        originalRotation = groundRotation;

        Quaternion appliedRotation = groundRotation;
        if (CurrentState == PieceAnimationState.Lifting ||
            CurrentState == PieceAnimationState.Lifted ||
            CurrentState == PieceAnimationState.Moving)
        {
            appliedRotation = groundRotation * Quaternion.Euler(-liftTiltAngle, 0f, 0f);
        }

        transform.rotation = appliedRotation;
    }

    public bool PlayFacingTurnAnimation(Quaternion groundRotation, Action onComplete = null)
    {
        if (Quaternion.Angle(originalRotation, groundRotation) <= 0.1f &&
            Quaternion.Angle(transform.rotation, groundRotation) <= 0.1f)
        {
            SyncFacing(groundRotation);
            onComplete?.Invoke();
            return true;
        }

        if (CurrentState != PieceAnimationState.Idle)
        {
            SyncFacing(groundRotation);
            onComplete?.Invoke();
            return true;
        }

        StopCurrentAnimation();
        currentAnimation = StartCoroutine(FacingTurnCoroutine(groundRotation, onComplete));
        return true;
    }

    public void UpdateGroundY(float newY)
    {
        groundY = newY;
        originalPosition.y = newY;
    }

    private void CalculatePieceHeight()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        pieceHeight = rend != null ? rend.bounds.size.y : 0.5f;
    }

    public bool PlayLiftAnimation(Action onComplete = null)
    {
        if (IsAnimating)
        {
            Debug.LogWarning($"[{nameof(PieceMotionAnimator)}] Cannot lift while animating: {CurrentState}");
            return false;
        }

        if (IsLifted)
        {
            onComplete?.Invoke();
            return true;
        }

        StopCurrentAnimation();
        CaptureGroundState();
        liftTargetY = groundY + (pieceHeight * liftHeightMultiplier);

        currentAnimation = StartCoroutine(LiftCoroutine(onComplete));
        return true;
    }

    public bool PlayCancelDropAnimation(Action onComplete = null)
    {
        if (!IsSelected && CurrentState == PieceAnimationState.Idle)
        {
            onComplete?.Invoke();
            return true;
        }

        StopCurrentAnimation();
        currentAnimation = StartCoroutine(DropCoroutine(originalPosition, originalRotation, onComplete));
        return true;
    }

    public bool PlayMoveAnimation(Vector3 targetGroundPos, Action onComplete = null)
    {
        if (CurrentState == PieceAnimationState.Moving)
        {
            Debug.LogWarning($"[{nameof(PieceMotionAnimator)}] Cannot move while animating: {CurrentState}");
            return false;
        }

        StopCurrentAnimation();
        currentAnimation = StartCoroutine(MoveCoroutine(targetGroundPos, onComplete));
        return true;
    }

    public void StopAndReset()
    {
        StopCurrentAnimation();
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        transform.rotation = originalRotation;
        CurrentState = PieceAnimationState.Idle;
    }

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

            transform.position = Vector3.Lerp(startPos, targetPos, curvedT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, curvedT);

            yield return null;
        }

        transform.position = targetPos;
        transform.rotation = targetRot;
        CurrentState = PieceAnimationState.Lifted;

        onComplete?.Invoke();
        OnAnimationComplete?.Invoke("Lift", true);
    }

    private IEnumerator DropCoroutine(Vector3 targetPos, Quaternion targetRot, Action onComplete)
    {
        CurrentState = PieceAnimationState.Dropping;

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Vector3 finalPos = new Vector3(targetPos.x, groundY, targetPos.z);

        float elapsed = 0f;
        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / dropDuration);
            float curvedT = dropCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, finalPos, curvedT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, curvedT);

            yield return null;
        }

        transform.position = finalPos;
        transform.rotation = targetRot;
        CurrentState = PieceAnimationState.Idle;
        originalPosition = finalPos;
        originalRotation = targetRot;

        onComplete?.Invoke();
        OnAnimationComplete?.Invoke("Drop", true);
    }

    private IEnumerator FacingTurnCoroutine(Quaternion targetGroundRotation, Action onComplete)
    {
        CurrentState = PieceAnimationState.Turning;

        Vector3 groundedPosition = new Vector3(transform.position.x, groundY, transform.position.z);
        Quaternion startRot = transform.rotation;
        float duration = Mathf.Max(0.01f, facingTurnDuration);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            transform.position = groundedPosition;
            transform.rotation = Quaternion.Slerp(startRot, targetGroundRotation, t);

            yield return null;
        }

        transform.position = groundedPosition;
        transform.rotation = targetGroundRotation;
        originalPosition = groundedPosition;
        originalRotation = targetGroundRotation;
        CurrentState = PieceAnimationState.Idle;

        onComplete?.Invoke();
        OnAnimationComplete?.Invoke("Turn", true);
    }

    private IEnumerator MoveCoroutine(Vector3 targetGroundPos, Action onComplete)
    {
        if (!IsLifted)
        {
            CaptureGroundState();
            liftTargetY = groundY + (pieceHeight * liftHeightMultiplier);
            yield return StartCoroutine(LiftCoroutine(null));
        }

        yield return StartCoroutine(HorizontalMoveCoroutine(targetGroundPos));
        yield return StartCoroutine(DropCoroutine(targetGroundPos, originalRotation, null));

        onComplete?.Invoke();
        OnMoveComplete?.Invoke(targetGroundPos);
        OnAnimationComplete?.Invoke("Move", true);
    }

    private IEnumerator HorizontalMoveCoroutine(Vector3 targetGroundPos)
    {
        CurrentState = PieceAnimationState.Moving;

        Vector3 startPos = transform.position;
        Vector3 targetPos = new Vector3(targetGroundPos.x, transform.position.y, targetGroundPos.z);

        float distance = Vector3.Distance(
            new Vector3(startPos.x, 0, startPos.z),
            new Vector3(targetPos.x, 0, targetPos.z));
        float duration = distance / moveSpeed;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = moveCurve.Evaluate(t);

            transform.position = Vector3.Lerp(startPos, targetPos, curvedT);

            Quaternion targetRot = originalRotation * Quaternion.Euler(-liftTiltAngle, 0f, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationLerpSpeed);

            yield return null;
        }

        transform.position = targetPos;
    }

    private void StopCurrentAnimation()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
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
}
