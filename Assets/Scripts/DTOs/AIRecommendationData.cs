using System;

/// <summary>
/// Gemini AI 분석 결과로 반환되는 차량 커스터마이징 추천 데이터 DTO.
/// Unity JsonUtility 파싱 규격에 맞춰 [Serializable] 및 public 필드로 구성되어 있습니다.
/// </summary>
[Serializable]
public class AiRecommendationData
{
    /// <summary>
    /// 추천된 바디 트림 ID (CarParts.json의 Trim 항목 id와 매칭)
    /// 예: "trim_standard_aero", "trim_widebody_racing"
    /// </summary>
    public string trimId;

    /// <summary>
    /// 추천된 휠 ID (CarParts.json의 Wheel 항목 id와 매칭)
    /// 예: "wheel_std_twin5_spoke", "wheel_deep_dish_forged"
    /// </summary>
    public string wheelId;

    /// <summary>
    /// 추천된 외장 컬러 ID (CarParts.json의 Color 항목 id와 매칭)
    /// 예: "color_polar_white", "color_monza_red", "color_matte_satin_black"
    /// </summary>
    public string colorId;

    /// <summary>
    /// AI가 작명한 커스텀 빌드 컨셉 타이틀
    /// 예: "도심형 스트리트 레이서", "클래식 모터스포츠 에디션"
    /// </summary>
    public string conceptTitle;

    /// <summary>
    /// 사용자의 자연어 입력 의도와 파츠 선택을 연결 짓는 AI의 추천 근거 설명
    /// </summary>
    public string reasoning;

    /// <summary>
    /// 선택된 3개 파츠(Trim + Wheel + Color)의 가격 합산 금액 (KRW 단위)
    /// </summary>
    public int estimatedTotalPrice;

    /// <summary>
    /// 디버그 로그 출력용 포맷팅
    /// </summary>
    public override string ToString()
    {
        return $"[{conceptTitle}] (총 {estimatedTotalPrice:N0}원)\n" +
               $"- Trim: {trimId}\n" +
               $"- Wheel: {wheelId}\n" +
               $"- Color: {colorId}\n" +
               $"- 이유: {reasoning}";
    }
}