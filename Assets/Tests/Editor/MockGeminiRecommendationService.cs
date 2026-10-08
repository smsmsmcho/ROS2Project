using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// IAiRecommendationService 인터페이스를 준수하는 Mock Gemini 추천 서비스.
/// 외부 네트워크 호출 없이 CarParts.json의 실제 파츠 데이터와 AiRecommendationData 규격을 완벽하게 시뮬레이션합니다.
/// </summary>
public class MockGeminiRecommendationService : IAiRecommendationService
{
    /// <summary>
    /// 테스트에서 특정 응답 데이터를 강제로 주입하고 싶을 때 사용
    /// </summary>
    public AiRecommendationData CustomResponse { get; set; }

    /// <summary>
    /// 비동기 네트워크 지연을 흉내내기 위한 대기 시간(ms)
    /// </summary>
    public int SimulatedDelayMs { get; set; } = 50;

    public async Task<AiRecommendationData> RequestRecommendationAsync(string prompt, List<CarPart> availableParts)
    {
        if (SimulatedDelayMs > 0)
        {
            await Task.Delay(SimulatedDelayMs);
        }

        // 1. 커스텀 응답이 설정되어 있으면 최우선 반환
        if (CustomResponse != null)
        {
            return CustomResponse;
        }

        if (availableParts == null || availableParts.Count == 0)
        {
            throw new ArgumentException("availableParts 파츠 목록이 비어있습니다.", nameof(availableParts));
        }

        string p = prompt?.ToLower() ?? "";

        CarPart selectedTrim;
        CarPart selectedWheel;
        CarPart selectedColor;
        string title;
        string reasoning;

        // 2. 프롬프트 키워드에 따른 모의 응답 시뮬레이션
        if (p.Contains("레드") || p.Contains("서킷") || p.Contains("레이싱") || p.Contains("스포티"))
        {
            selectedTrim = availableParts.FirstOrDefault(x => x.id == "trim_widebody_racing") 
                           ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Trim);
            selectedWheel = availableParts.FirstOrDefault(x => x.id == "wheel_deep_dish_forged") 
                            ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Wheel);
            selectedColor = availableParts.FirstOrDefault(x => x.id == "color_monza_red") 
                            ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Color);

            title = "트랙 포커스 레이싱 익스트림 에디션";
            reasoning = "서킷 주행 감성을 극대화하기 위해 와이드바디 에어로 트림 키트와 19인치 단조 레이싱 휠, 그리고 강렬한 몬자 크림슨 레드를 매칭했습니다.";
        }
        else if (p.Contains("블랙") || p.Contains("스텔스") || p.Contains("어두"))
        {
            selectedTrim = availableParts.FirstOrDefault(x => x.id == "trim_widebody_racing") 
                           ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Trim);
            selectedWheel = availableParts.FirstOrDefault(x => x.id == "wheel_deep_dish_forged") 
                            ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Wheel);
            selectedColor = availableParts.FirstOrDefault(x => x.id == "color_matte_satin_black") 
                            ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Color);

            title = "다크 스텔스 섀도우 에디션";
            reasoning = "압도적인 묵직함과 세련된 카리스마를 연출하기 위해 사틴 무광 블랙 컬러와 19인치 단조 휠, 와이드바디 트림을 조합했습니다.";
        }
        else
        {
            // 기본 표준 스타일 (화이트 & 모던)
            selectedTrim = availableParts.FirstOrDefault(x => x.id == "trim_standard_aero") 
                           ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Trim);
            selectedWheel = availableParts.FirstOrDefault(x => x.id == "wheel_std_twin5_spoke") 
                            ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Wheel);
            selectedColor = availableParts.FirstOrDefault(x => x.id == "color_polar_white") 
                            ?? availableParts.FirstOrDefault(x => x.category == PartCategory.Color);

            title = "도심형 모던 퓨어 에디션";
            reasoning = "도심 주행에 어울리는 세련되고 깔끔한 퓨어 화이트 컬러와 순정 스타일 에어로 트림, 18인치 트윈 5스포크 휠의 조화로운 구성입니다.";
        }

        var partIds = new List<string>();
        if (selectedTrim != null) partIds.Add(selectedTrim.id);
        if (selectedWheel != null) partIds.Add(selectedWheel.id);
        if (selectedColor != null) partIds.Add(selectedColor.id);

        return new AiRecommendationData
        {
            conceptTitle = title,
            reasoning = reasoning,
            partIds = partIds
        };
    }
}
