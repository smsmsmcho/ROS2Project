using System.Collections;
using UnityEngine;

/// <summary>
/// 좌측 도어, 우측 도어, 트렁크 등 개폐 가능한 차량 파츠 GameObject에 부착되어
/// 자기 자신의 Transform을 지정된 회전축과 각도로 부드럽게 열고 닫는 컨트롤러.
/// 인스펙터에서 OpenableType을 선택하면 해당 파츠에 맞는 기본 회전축과 각도가 자동으로 설정됩니다.
/// </summary>
[DisallowMultipleComponent]
public class CarOpenableController : MonoBehaviour
{
    public enum OpenableType
    {
        LeftDoor,
        RightDoor,
        Trunk
    }

    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [Header("개폐 파츠 종류 (선택 시 축·각도 자동 설정)")]
    [SerializeField] private OpenableType openableType = OpenableType.LeftDoor;

    [Header("회전 설정")]
    [SerializeField] private RotationAxis rotationAxis = RotationAxis.Y;
    [SerializeField, Range(-180f, 180f)] private float openAngle = 65f;

    [Header("애니메이션 설정")]
    [SerializeField, Min(0.01f)] private float smoothDuration = 0.8f;
    [SerializeField] private AnimationCurve smoothCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [SerializeField, HideInInspector] private OpenableType lastAppliedType = OpenableType.LeftDoor;

    private Quaternion closedRotation;
    private Coroutine rotationCoroutine;
    private bool isOpened;

    public OpenableType Type => openableType;
    public bool IsDoor => openableType == OpenableType.LeftDoor || openableType == OpenableType.RightDoor;
    public bool IsOpened => isOpened;

    private void Awake()
    {
        closedRotation = transform.localRotation;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (lastAppliedType == openableType) return;
        lastAppliedType = openableType;

        (rotationAxis, openAngle) = openableType switch
        {
            OpenableType.LeftDoor => (RotationAxis.Y, 65f),
            OpenableType.RightDoor => (RotationAxis.Y, -65f),
            OpenableType.Trunk => (RotationAxis.Z, 65f),
            _ => (rotationAxis, openAngle)
        };
    }
#endif

    /// <summary>
    /// 현재 열림/닫힘 상태를 반전시켜 부드럽게 회전시킵니다.
    /// </summary>
    public void Toggle()
    {
        SetOpened(!isOpened);
    }

    /// <summary>
    /// 지정한 열림/닫힘 상태로 부드럽게 회전시킵니다.
    /// </summary>
    public void SetOpened(bool opened)
    {
        isOpened = opened;
        Quaternion targetRotation = isOpened
            ? closedRotation * Quaternion.AngleAxis(openAngle, GetAxisVector(rotationAxis))
            : closedRotation;

        if (rotationCoroutine != null)
        {
            StopCoroutine(rotationCoroutine);
        }

        rotationCoroutine = StartCoroutine(RotateSmoothly(targetRotation));
    }

    private IEnumerator RotateSmoothly(Quaternion targetRotation)
    {
        Quaternion startRotation = transform.localRotation;

        float elapsedTime = 0f;
        while (elapsedTime < smoothDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = smoothCurve.Evaluate(Mathf.Clamp01(elapsedTime / smoothDuration));
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        transform.localRotation = targetRotation;
        rotationCoroutine = null;
    }

    private static Vector3 GetAxisVector(RotationAxis axis) => axis switch
    {
        RotationAxis.X => Vector3.right,
        RotationAxis.Y => Vector3.up,
        RotationAxis.Z => Vector3.forward,
        _ => Vector3.up
    };
}
