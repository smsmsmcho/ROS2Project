using System;
using System.Collections.Generic;

[Serializable]
public class AiRecommendationData
{
    /// <summary>
    /// AI가 추천한 CarPart ID 목록.
    /// 각 ID는 CarParts.json의 CarPart.id와 매칭됩니다.
    /// </summary>
    public List<string> partIds;

    /// <summary>
    /// AI가 작명한 커스텀 빌드 컨셉 타이틀
    /// </summary>
    public string conceptTitle;

    /// <summary>
    /// 사용자의 요청과 추천 결과를 연결하는 AI의 추천 근거
    /// </summary>
    public string reasoning;

    public override string ToString()
    {
        return $"[{conceptTitle}]\n" +
               $"- Parts: {string.Join(", ", partIds)}\n" +
               $"- 이유: {reasoning}";
    }
}