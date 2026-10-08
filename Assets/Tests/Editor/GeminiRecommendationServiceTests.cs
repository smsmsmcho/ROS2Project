using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;

/// <summary>
/// IAiRecommendationService 규격 및 CarParts.json, AiRecommendationData 검증을 위한 테스트
/// - MockGeminiRecommendationService를 통한 100% 격리 단위 테스트
/// - GeminiRecommendationService를 통한 실제 API 호출 테스트 메뉴 지원
/// </summary>
public class GeminiRecommendationServiceTests
{
    private List<CarPart> carParts;
    private IAiRecommendationService mockAiService;

    [SetUp]
    public void SetUp()
    {
        carParts = CarPartDataLoader.LoadCarParts();
        mockAiService = new MockGeminiRecommendationService();
    }

    #region 1. Mock 기반 단위 테스트 (100% 격리 검증)

    [Test]
    public void CarPartsJson_LoadsSuccessfully_HasValidProperties()
    {
        Assert.IsNotNull(carParts, "CarParts 목록을 로드하지 못했습니다.");
        Assert.Greater(carParts.Count, 0, "CarParts 목록이 비어 있습니다.");

        foreach (var part in carParts)
        {
            Assert.IsFalse(string.IsNullOrEmpty(part.id), "part.id가 비어있습니다.");
            Assert.IsFalse(string.IsNullOrEmpty(part.name), $"part.name이 비어있습니다. id: {part.id}");
            Assert.GreaterOrEqual(part.price, 0, $"part.price는 0 이상이어야 합니다. id: {part.id}");
            Assert.IsFalse(string.IsNullOrEmpty(part.description), $"part.description이 비어있습니다. id: {part.id}");
            Assert.IsFalse(string.IsNullOrEmpty(part.colorHex), $"part.colorHex가 비어있습니다. id: {part.id}");
        }

        int trimCount = carParts.Count(p => p.category == PartCategory.Trim);
        int wheelCount = carParts.Count(p => p.category == PartCategory.Wheel);
        int colorCount = carParts.Count(p => p.category == PartCategory.Color);

        Assert.Greater(trimCount, 0, "Trim 파츠가 최소 1개 이상 있어야 합니다.");
        Assert.Greater(wheelCount, 0, "Wheel 파츠가 최소 1개 이상 있어야 합니다.");
        Assert.Greater(colorCount, 0, "Color 파츠가 최소 1개 이상 있어야 합니다.");

        Debug.Log($"[파츠 데이터 검증 완료] 총 {carParts.Count}개 파츠 (Trim: {trimCount}, Wheel: {wheelCount}, Color: {colorCount})");
    }

    [Test]
    public void Services_Implement_IAiRecommendationService()
    {
        Assert.IsInstanceOf<IAiRecommendationService>(mockAiService, "MockGeminiRecommendationService는 IAiRecommendationService를 구현해야 합니다.");
        
        var realService = new GeminiRecommendationService();
        Assert.IsInstanceOf<IAiRecommendationService>(realService, "GeminiRecommendationService는 IAiRecommendationService를 구현해야 합니다.");
    }

    [UnityTest]
    public IEnumerator RequestRecommendationAsync_ReturnsValidAiRecommendationData()
    {
        string prompt = "서킷 트랙 데이에서 가장 빠르고 공격적으로 보일 수 있는 레이싱 머신 세팅으로 추천해줘.";
        Task<AiRecommendationData> task = mockAiService.RequestRecommendationAsync(prompt, carParts);

        while (!task.IsCompleted)
        {
            yield return null;
        }

        Assert.IsNull(task.Exception, "추천 요청 중 예외가 발생하지 않아야 합니다.");
        AiRecommendationData result = task.Result;

        Assert.IsNotNull(result, "추천 결과 데이터가 null입니다.");
        Assert.IsFalse(string.IsNullOrEmpty(result.conceptTitle), "conceptTitle이 채워져 있어야 합니다.");
        Assert.IsFalse(string.IsNullOrEmpty(result.reasoning), "reasoning이 채워져 있어야 합니다.");
        Assert.IsNotNull(result.partIds, "partIds 리스트가 null이 아니어야 합니다.");
        Assert.Greater(result.partIds.Count, 0, "최소 1개 이상의 partId가 추천되어야 합니다.");

        var validIds = carParts.Select(p => p.id).ToHashSet();
        foreach (string partId in result.partIds)
        {
            Assert.IsTrue(validIds.Contains(partId), $"추천된 partId '{partId}'는 CarParts에 존재하지 않는 잘못된 ID입니다!");
        }

        Debug.Log($"[Mock AI 추천 검증 성공]\n{result}");
    }

    [UnityTest]
    public IEnumerator RequestRecommendationAsync_RacingRedPrompt_MatchesExpectedParts()
    {
        string prompt = "강렬한 레드 컬러의 와이드바디 레이싱카로 만들어줘";
        Task<AiRecommendationData> task = mockAiService.RequestRecommendationAsync(prompt, carParts);

        while (!task.IsCompleted)
        {
            yield return null;
        }

        AiRecommendationData result = task.Result;
        Assert.IsNotNull(result);
        Assert.IsTrue(result.partIds.Contains("color_monza_red"), "레드 요청 시 'color_monza_red'가 추천되어야 합니다.");
        Assert.IsTrue(result.partIds.Contains("trim_widebody_racing"), "레이싱 요청 시 'trim_widebody_racing'이 추천되어야 합니다.");

        Debug.Log($"[프롬프트 매칭 검증 성공] 타이틀: {result.conceptTitle}");
    }

    [UnityTest]
    public IEnumerator RequestRecommendationAsync_WithCustomInjectedResponse_ReturnsInjectedData()
    {
        var mock = (MockGeminiRecommendationService)mockAiService;
        var expectedData = new AiRecommendationData
        {
            conceptTitle = "테스트 주입 에디션",
            reasoning = "단위 테스트용 주입 데이터입니다.",
            partIds = new List<string> { "trim_standard_aero", "wheel_std_twin5_spoke" }
        };

        mock.CustomResponse = expectedData;

        Task<AiRecommendationData> task = mock.RequestRecommendationAsync("임의의 프롬프트", carParts);

        while (!task.IsCompleted)
        {
            yield return null;
        }

        AiRecommendationData result = task.Result;
        Assert.AreEqual(expectedData.conceptTitle, result.conceptTitle);
        Assert.AreEqual(expectedData.reasoning, result.reasoning);
        Assert.AreEqual(expectedData.partIds.Count, result.partIds.Count);

        Debug.Log("[커스텀 주입 검증 성공] 주입된 데이터가 그대로 반환됨을 확인");
    }

    #endregion

    #region 2. 실제 API 통합 테스트 (Test Runner 수동 실행용)

    /// <summary>
    /// 실제 구글 Gemini API 통신 통합 테스트.
    /// [Explicit] 속성이 있어 'Run All' 시에는 자동으로 건너뛰고, Test Runner에서 직접 선택 실행할 때만 동작합니다.
    /// </summary>
    [UnityTest]
    [Explicit("실제 구글 Gemini API 네트워크 통신 테스트 (인터넷 및 API Key 필요)")]
    [Category("Integration")]
    public IEnumerator RealApi_RequestRecommendationAsync_ReturnsValidAiRecommendationData()
    {
        var realService = new GeminiRecommendationService();
        string prompt = "도시적인 세련미가 느껴지는 모던하고 깔끔한 화이트톤 스타일로 추천해줘";

        Task<AiRecommendationData> task = realService.RequestRecommendationAsync(prompt, carParts);

        while (!task.IsCompleted)
        {
            yield return null;
        }

        Assert.IsNull(task.Exception, $"Gemini API 호출 중 오류 발생: {task.Exception?.GetBaseException().Message}");
        AiRecommendationData result = task.Result;

        Assert.IsNotNull(result, "추천 결과 데이터가 null입니다.");
        Assert.IsFalse(string.IsNullOrEmpty(result.conceptTitle), "conceptTitle이 비어있습니다.");
        Assert.IsFalse(string.IsNullOrEmpty(result.reasoning), "reasoning이 비어있습니다.");
        Assert.IsNotNull(result.partIds, "partIds 리스트가 null입니다.");
        Assert.Greater(result.partIds.Count, 0, "최소 1개 이상의 partId가 추천되어야 합니다.");

        var validIds = carParts.Select(p => p.id).ToHashSet();
        foreach (string partId in result.partIds)
        {
            Assert.IsTrue(validIds.Contains(partId), $"추천된 partId '{partId}'는 CarParts에 존재하지 않는 잘못된 ID입니다!");
        }

        Debug.Log($"[실제 Gemini API 통합 테스트 성공]\n{result}");
    }

    #endregion

    #region 3. 에디터 메뉴 도구 (Mock & 실제 API 테스트)

    [MenuItem("Tools/CarConfigurator/Run AI Recommendation Test (Mock)")]
    public static async void RunAiTestMockFromMenu()
    {
        Debug.Log("<color=cyan>[CarConfigurator] Mock AI 추천 서비스 테스트를 시작합니다...</color>");
        try
        {
            var parts = CarPartDataLoader.LoadCarParts();
            IAiRecommendationService service = new MockGeminiRecommendationService();

            string prompt = "공격적인 와이드바디와 몬자 레드 컬러의 트랙 레이싱 스타일로 꾸며줘";
            var recommendation = await service.RequestRecommendationAsync(prompt, parts);

            Debug.Log("<color=lime><b>[Mock AI 추천 성공!]</b></color>\n" +
                      $"<b>컨셉:</b> {recommendation.conceptTitle}\n" +
                      $"<b>이유:</b> {recommendation.reasoning}\n" +
                      $"<b>추천 파츠 수:</b> {recommendation.partIds.Count}개");

            foreach (var id in recommendation.partIds)
            {
                var part = parts.FirstOrDefault(p => p.id == id);
                if (part != null)
                {
                    Debug.Log($" -> [{part.category}] {part.name} ({part.price:N0}원)");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[테스트 실패]: {ex.Message}\n{ex.StackTrace}");
        }
    }

    [MenuItem("Tools/CarConfigurator/Run Actual Gemini API Test (Real API)")]
    public static async void RunActualGeminiTestFromMenu()
    {
        Debug.Log("<color=yellow>[CarConfigurator] 실제 구글 Gemini API 비동기 통신 테스트를 시작합니다...</color>");
        try
        {
            var parts = CarPartDataLoader.LoadCarParts();
            IAiRecommendationService service = new GeminiRecommendationService();

            string prompt = "도시적인 세련미가 느껴지는 모던하고 깔끔한 화이트톤 스타일로 추천해줘";
            Debug.Log($"요청 프롬프트: \"{prompt}\"");

            var recommendation = await service.RequestRecommendationAsync(prompt, parts);

            Debug.Log("<color=lime><b>[실제 Gemini API 응답 성공!]</b></color>\n" +
                      $"<b>컨셉:</b> {recommendation.conceptTitle}\n" +
                      $"<b>이유:</b> {recommendation.reasoning}\n" +
                      $"<b>추천 파츠 목록:</b> {string.Join(", ", recommendation.partIds)}");

            foreach (var id in recommendation.partIds)
            {
                var part = parts.FirstOrDefault(p => p.id == id);
                if (part != null)
                {
                    Debug.Log($" -> 매칭 파츠: [{part.category}] {part.name} ({part.price:N0}원)");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[실제 Gemini API 호출 실패]: {ex.Message}");
        }
    }

    #endregion
}
