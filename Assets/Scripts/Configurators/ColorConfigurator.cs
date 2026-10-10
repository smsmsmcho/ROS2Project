using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// CarPartConfigurator를 상속받아, 선택된 파츠의 색상 정보(colorHex)로
/// 차량 바디 Renderer 머티리얼의 색상을 변경하는 컬러 설정 컴포넌트
/// </summary>
public class ColorConfigurator : CarPartConfigurator
{
    [Header("차량 바디 렌더러 설정")]
    [Tooltip("외장 색상을 적용할 차량 바디의 Renderer 컴포넌트 목록 (다중 메쉬 지원)")]
    [SerializeField] private List<Renderer> bodyRenderers = new List<Renderer>();

    /// <summary>
    /// 선택된 파츠의 colorHex 정보를 기반으로 차량 바디 머티리얼 색상을 변경합니다.
    /// </summary>
    protected override void ApplyPart(CarPart part)
    {
        if (part == null) return;

        // null 뿐만 아니라 원소가 0개인 빈 리스트도 함께 방어
        if (bodyRenderers == null || bodyRenderers.Count == 0)
        {
            Debug.LogError($"[{gameObject.name}] ColorConfigurator에 bodyRenderers가 할당되지 않았거나 비어 있습니다. 인스펙터에서 바디 렌더러들을 연결해주세요!");
            return;
        }

        if (ColorUtility.TryParseHtmlString(part.colorHex, out Color color))
        {
            foreach (Renderer r in bodyRenderers)
            {
                if (r != null)
                {
                    r.material.color = color;
                }
            }

            Debug.Log($"[Color] 바디 색상 적용 완료: {part.name} ({part.colorHex}) - 적용 렌더러 {bodyRenderers.Count}개");
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 유효하지 않은 색상 코드 형식입니다: '{part.colorHex}' (파츠: {part.name})");
        }
    }
}
