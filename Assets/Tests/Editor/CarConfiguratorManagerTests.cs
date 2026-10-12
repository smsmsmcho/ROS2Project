using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// CarConfiguratorManager의 핵심 기능(옵션 조회, 수동 파츠 적용, AI 추천 및 일괄 적용, 견적 계산, 이벤트 통지)을 검증하는 단위 테스트
/// </summary>
public class CarConfiguratorManagerTests
{
    private GameObject rootObject;
    private CarConfiguratorManager manager;

    private GameObject accessoryObj;
    private CarPartConfigurator accessoryConfigurator;

    private GameObject wheelObj;
    private CarPartConfigurator wheelConfigurator;

    private GameObject colorObj;
    private ColorConfigurator colorConfigurator;

    private MockGeminiRecommendationService mockAiService;

    // 테스트용 파츠 데이터
    private CarPart accA, accB;
    private CarPart wheelA, wheelB;
    private CarPart colorA, colorB;

    [SetUp]
    public void SetUp()
    {
        // 1. 매니저 GameObject 생성
        rootObject = new GameObject("Test_CarConfiguratorManager");
        manager = rootObject.AddComponent<CarConfiguratorManager>();

        // 2. 각 컨피규레이터 GameObject 및 컴포넌트 생성
        accessoryObj = new GameObject("AccessoryConfigurator");
        accessoryConfigurator = accessoryObj.AddComponent<CarPartConfigurator>();

        wheelObj = new GameObject("WheelConfigurator");
        wheelConfigurator = wheelObj.AddComponent<CarPartConfigurator>();

        colorObj = new GameObject("ColorConfigurator");
        colorConfigurator = colorObj.AddComponent<ColorConfigurator>();

        // 3. 테스트용 파츠 세팅 (Accessory, Wheel, Color)
        accA = new CarPart { id = "acc_spoiler_std", name = "Standard Spoiler", category = PartCategory.Accessory, price = 1000 };
        accB = new CarPart { id = "acc_carbon_wing", name = "Carbon Racing Wing", category = PartCategory.Accessory, price = 2500 };

        wheelA = new CarPart { id = "wheel_std", name = "18 Inch Wheel", category = PartCategory.Wheel, price = 500 };
        wheelB = new CarPart { id = "wheel_forged", name = "19 Inch Forged", category = PartCategory.Wheel, price = 1500 };

        colorA = new CarPart { id = "color_white", name = "Polar White", category = PartCategory.Color, price = 200, colorHex = "#FFFFFF" };
        colorB = new CarPart { id = "color_red", name = "Racing Red", category = PartCategory.Color, price = 400, colorHex = "#FF0000" };

        // 4. private 필드에 리플렉션으로 컨피규레이터 및 Mock AI 서비스 주입 (프로덕션 코드 오염 방지)
        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        typeof(CarConfiguratorManager).GetField("accessoryConfigurator", flags).SetValue(manager, accessoryConfigurator);
        typeof(CarConfiguratorManager).GetField("wheelConfigurator", flags).SetValue(manager, wheelConfigurator);
        typeof(CarConfiguratorManager).GetField("colorConfigurator", flags).SetValue(manager, colorConfigurator);

        mockAiService = new MockGeminiRecommendationService { SimulatedDelayMs = 0 };
        typeof(CarConfiguratorManager).GetField("aiService", flags).SetValue(manager, mockAiService);

        // 테스트용 baseVehiclePrice를 0으로 설정하여 계산 용이성 확보
        typeof(CarConfiguratorManager).GetField("baseVehiclePrice", flags).SetValue(manager, 0);

        // 5. 초기 파츠 목록으로 Initialize
        var allParts = new List<CarPart> { accA, accB, wheelA, wheelB, colorA, colorB };
        manager.Initialize(allParts);
    }

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null) Object.DestroyImmediate(rootObject);
        if (accessoryObj != null) Object.DestroyImmediate(accessoryObj);
        if (wheelObj != null) Object.DestroyImmediate(wheelObj);
        if (colorObj != null) Object.DestroyImmediate(colorObj);
    }

    /// <summary>
    /// [초기화] Initialize 호출 시 각 카테고리(Accessory, Wheel, Color)의 첫 번째 옵션이
    /// 기본 선택되고, 초기 총 견적(totalPrice) 및 상세 목록(details)이 올바르게 계산되는지 검증합니다.
    /// </summary>
    [Test]
    public void Initialize_SelectsFirstOptionPerCategory_AndCalculatesInitialEstimate()
    {
        // Initialize 시 각 카테고리의 첫 번째 옵션이 선택됨 (accA: 1000, wheelA: 500, colorA: 200 => 1700)
        EstimateData estimate = manager.CalculateEstimate();

        Assert.IsNotNull(estimate);
        Assert.AreEqual(1700, estimate.totalPrice);
        Assert.AreEqual(3, estimate.details.Count);
        Assert.Contains(accA, estimate.details);
        Assert.Contains(wheelA, estimate.details);
        Assert.Contains(colorA, estimate.details);
    }

    /// <summary>
    /// [옵션 조회] 특정 카테고리를 지정하여 GetOptions를 호출했을 때,
    /// 해당 카테고리에 속한 CarPart 목록만 정확하게 필터링되어 반환되는지 검증합니다.
    /// </summary>
    [Test]
    public void GetOptions_ReturnsCorrectPartsForCategory()
    {
        var accessories = manager.GetOptions(PartCategory.Accessory);
        var wheels = manager.GetOptions(PartCategory.Wheel);
        var colors = manager.GetOptions(PartCategory.Color);

        Assert.AreEqual(2, accessories.Count);
        Assert.AreEqual(accA.id, accessories[0].id);

        Assert.AreEqual(2, wheels.Count);
        Assert.AreEqual(wheelA.id, wheels[0].id);

        Assert.AreEqual(2, colors.Count);
        Assert.AreEqual(colorA.id, colors[0].id);
    }

    /// <summary>
    /// [수동 파츠 적용] 단일 파츠를 ApplyPart로 적용했을 때, 해당 컨피규레이터의 선택이 갱신되고,
    /// 견적 변경 이벤트(OnConfigUpdated)가 정확히 1회 호출되며 총 견적이 올바르게 재계산되는지 검증합니다.
    /// </summary>
    [Test]
    public void ApplyPart_UpdatesSelectionAndInvokesOnConfigUpdated()
    {
        EstimateData lastEstimate = null;
        int eventCallCount = 0;

        manager.OnConfigUpdated += (data) =>
        {
            lastEstimate = data;
            eventCallCount++;
        };

        // Wheel을 wheelB(1500원)로 변경 (기존: accA 1000 + wheelA 500 + colorA 200 = 1700 -> 신규: 1000 + 1500 + 200 = 2700)
        manager.ApplyPart(wheelB);

        Assert.AreEqual(1, eventCallCount, "이벤트가 1회 호출되어야 합니다.");
        Assert.IsNotNull(lastEstimate);
        Assert.AreEqual(2700, lastEstimate.totalPrice);
        Assert.AreEqual(wheelB, wheelConfigurator.GetSelectedPart());
    }

    /// <summary>
    /// [AI 추천 일괄 적용] AI 추천 결과(여러 partIds)를 적용할 때, 각 컨피규레이터에 파츠들이
    /// 모두 일괄 반영되고, 루프 중간에 이벤트가 남발되지 않고 최종 완료 후 1회만 이벤트가 발생하는지 검증합니다.
    /// </summary>
    [Test]
    public void ApplyAiRecommendation_AppliesMultiplePartsAndFiresSingleEvent()
    {
        EstimateData lastEstimate = null;
        int eventCallCount = 0;

        manager.OnConfigUpdated += (data) =>
        {
            lastEstimate = data;
            eventCallCount++;
        };

        var aiData = new AiRecommendationData
        {
            conceptTitle = "Full Upgrade Package",
            reasoning = "고성능 레이싱 세팅",
            partIds = new List<string> { accB.id, wheelB.id, colorB.id }
        };

        // AI 추천 적용 (accB: 2500, wheelB: 1500, colorB: 400 => 총 4400)
        manager.ApplyAiRecommendation(aiData);

        Assert.AreEqual(1, eventCallCount, "루프 순회 후 최종 1회만 이벤트가 발생해야 합니다.");
        Assert.IsNotNull(lastEstimate);
        Assert.AreEqual(4400, lastEstimate.totalPrice);
        Assert.AreEqual(accB, accessoryConfigurator.GetSelectedPart());
        Assert.AreEqual(wheelB, wheelConfigurator.GetSelectedPart());
        Assert.AreEqual(colorB, colorConfigurator.GetSelectedPart());
    }

    /// <summary>
    /// [AI 추천 비동기 요청] 사용자의 자연어 프롬프트를 넘겨 RequestRecommendationAsync를 호출했을 때,
    /// AI 서비스로부터 AiRecommendationData 응답을 정상적으로 수신하여 반환하는지 검증합니다.
    /// </summary>
    [Test]
    public async Task RequestRecommendationAsync_ReturnsRecommendationFromAiService()
    {
        mockAiService.CustomResponse = new AiRecommendationData
        {
            conceptTitle = "모던 화이트",
            reasoning = "깔끔한 화이트 스타일",
            partIds = new List<string> { accA.id, wheelA.id, colorA.id }
        };

        var result = await manager.RequestRecommendationAsync("깔끔한 화이트 스타일 추천해줘");

        Assert.IsNotNull(result);
        Assert.AreEqual("모던 화이트", result.conceptTitle);
        Assert.AreEqual(3, result.partIds.Count);
    }

    /// <summary>
    /// [안전 방어 - null 입력 방어] 잘못된 null 파츠를 ApplyPart에 전달했을 때,
    /// 크래시(예외) 없이 경고 로그만 남기고 기존 장착 상태와 견적을 안전하게 보존하는지 검증합니다.
    /// </summary>
    [Test]
    public void ApplyPart_WithNull_DoesNotThrowAndMaintainsState()
    {
        EstimateData before = manager.CalculateEstimate();

        LogAssert.Expect(LogType.Warning, "[CarConfiguratorManager] 적용하려는 CarPart가 null입니다.");
        manager.ApplyPart(null);

        EstimateData after = manager.CalculateEstimate();
        Assert.AreEqual(before.totalPrice, after.totalPrice);
    }

    /// <summary>
    /// [안전 방어 - 네트워크/API 예외 방어] AI 추천 요청 중 네트워크 단절이나 API 예외가 터지더라도,
    /// 상위(UI)로 크래시를 전파하지 않고 에러 로그 기록 후 안전하게 null을 반환하는지 검증합니다.
    /// </summary>
    [Test]
    public async Task RequestRecommendationAsync_OnException_LogsErrorAndReturnsNull()
    {
        // availableParts가 비어있을 때 Mock이 ArgumentException을 던지도록 유도
        var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(CarConfiguratorManager).GetField("accessoryConfigurator", flags).SetValue(manager, null);
        typeof(CarConfiguratorManager).GetField("wheelConfigurator", flags).SetValue(manager, null);
        typeof(CarConfiguratorManager).GetField("colorConfigurator", flags).SetValue(manager, null);

        LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(@"\[CarConfiguratorManager\] AI 추천 요청 실패:.*"));
        var result = await manager.RequestRecommendationAsync("추천해줘");

        Assert.IsNull(result, "예외 발생 시 크래시 없이 null을 반환해야 합니다.");
    }
}
