using System;
using System.Collections.Generic;

/// <summary>
/// 현재 선택된 차량 파츠들의 견적 정보를 담는 DTO
/// </summary>
[Serializable]
public class EstimateData
{
    /// <summary>
    /// 장착된 전체 파츠들의 총 금액 (기본 차량 가격 포함 가능)
    /// </summary>
    public int totalPrice;

    /// <summary>
    /// 현재 적용(선택)된 파츠 상세 목록
    /// </summary>
    public List<CarPart> details = new List<CarPart>();

    public override string ToString()
    {
        string detailStr = details != null && details.Count > 0
            ? string.Join(", ", details.ConvertAll(d => $"{d.name}({d.price:N0}원)"))
            : "없음";
        return $"[총 견적] {totalPrice:N0}원 | 파츠: {detailStr}";
    }
}

