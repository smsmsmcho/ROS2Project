using UnityEngine;

/// <summary>
/// CarPartConfigurator를 상속받아, 선택된 파츠의 색상 정보(colorHex)로
/// 차량 바디 Renderer 머티리얼의 색상을 변경하는 컬러 설정 컴포넌트
/// </summary>
public class ColorConfigurator : CarPartConfigurator
{
    [Header("차량 바디 렌더러 설정")]
    [Tooltip("외장 색상을 적용할 차량 바디의 Renderer 컴포넌트")]
    [SerializeField] private Renderer bodyRenderer;

    /// <summary>
    /// 선택된 파츠의 colorHex 정보를 기반으로 차량 바디 머티리얼 색상을 변경합니다.
    /// </summary>
    protected override void ApplyPart(CarPart part)
    {
        if (part == null) return;

        if (bodyRenderer == null)
        {
            Debug.LogError($"[{gameObject.name}] ColorConfigurator에 bodyRenderer가 할당되지 않았습니다. 인스펙터에서 차량 바디 렌더러를 연결해주세요!");
            return;
        }

        if (ColorUtility.TryParseHtmlString(part.colorHex, out Color color))
        {
            bodyRenderer.material.color = color;
            Debug.Log($"[Color] 바디 색상 적용: {part.name} ({part.colorHex})");
        }
    }
}

