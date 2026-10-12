using System;
using System.Collections.Generic;

/// <summary>
/// Gemini AI 분석 결과로 반환되는 차량 커스터마이징 추천 데이터 DTO.
/// </summary>
[Serializable]
public class AiRecommendationData
{
    /// <summary>
    /// AI가 추천한 CarPart ID 목록.
    /// 각 ID는 CarParts.json의 CarPart.id와 매칭됩니다.
    /// </summary>
    public List<string> partIds;

    /// <summary>
    /// AI가 작명한 커스텀 차량 컨셉 타이틀
    /// </summary>
    public string conceptTitle;

    /// <summary>
    /// 사용자 요청과 추천 조합을 설명하는 AI의 추천 근거
    /// </summary>
    public string reasoning;

    public override string ToString()
    {
        return $"[{conceptTitle}]\n" +
               $"- Parts: {string.Join(", ", partIds)}\n" +
               $"- 이유: {reasoning}";
    }
}