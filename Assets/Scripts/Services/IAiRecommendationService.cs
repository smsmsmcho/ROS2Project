using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// AI 기반 차량 파츠 추천 서비스 인터페이스
/// </summary>
public interface IAiRecommendationService
{
    /// <summary>
    /// 사용자 프롬프트와 선택 가능한 파츠 목록을 기반으로 AI 추천 결과를 비동기 요청합니다.
    /// </summary>
    /// <param name="prompt">사용자의 자연어 요청 프롬프트</param>
    /// <param name="availableParts">선택 가능한 CarPart 목록</param>
    /// <returns>AI가 추천한 partIds, conceptTitle, reasoning을 담은 AiRecommendationData</returns>
    Task<AiRecommendationData> RequestRecommendationAsync(string prompt, List<CarPart> availableParts);
}
