using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// CarPartConfigurator의 핵심 3대 동작(초기화, 파츠 교체 토글, 예외 방어)을 검증하는 단위 테스트
/// </summary>
public class CarPartConfiguratorTests
{
    private GameObject rootObject;
    private CarPartConfigurator configurator;

    private CarPart partA;
    private CarPart partB;

    private GameObject modelObjA;
    private GameObject modelObjB;

    [SetUp]
    public void SetUp()
    {
        // 1. 테스트용 게임오브젝트 및 컴포넌트 생성
        rootObject = new GameObject("Test_CarPartConfigurator");
        configurator = rootObject.AddComponent<CarPartConfigurator>();

        // 2. 가짜 CarPart 데이터 준비
        partA = new CarPart { id = "acc_spoiler_std", name = "Standard Spoiler", category = PartCategory.Accessory, price = 1000 };
        partB = new CarPart { id = "acc_carbon_wing", name = "Carbon Racing Wing", category = PartCategory.Accessory, price = 2000 };

        // 3. 가짜 3D 모델 GameObject 준비
        modelObjA = new GameObject("Model_A");
        modelObjB = new GameObject("Model_B");

        // 4. private 직렬화 필드 optionObjects에 3D 모델 리스트 주입 (리플렉션)
        var optionObjectsField = typeof(CarPartConfigurator).GetField("optionObjects", BindingFlags.NonPublic | BindingFlags.Instance);
        var optionObjectsList = new List<GameObject> { modelObjA, modelObjB };
        optionObjectsField.SetValue(configurator, optionObjectsList);
    }

    [TearDown]
    public void TearDown()
    {
        if (rootObject != null) Object.DestroyImmediate(rootObject);
        if (modelObjA != null) Object.DestroyImmediate(modelObjA);
        if (modelObjB != null) Object.DestroyImmediate(modelObjB);
    }

    /// <summary>
    /// [핵심 1] 초기화 시 첫 번째 파츠가 기본 선택되고 첫 번째 3D 모델만 켜지는지 검증
    /// </summary>
    [Test]
    public void Initialize_SelectsFirstOptionByDefault_AndActivatesOnlyFirstGameObject()
    {
        var parts = new List<CarPart> { partA, partB };

        configurator.Initialize(parts);

        // 첫 번째 파츠 선택 및 3D 모델 활성화 상태 검증
        Assert.AreEqual(2, configurator.GetOptions().Count);
        Assert.AreEqual(partA, configurator.GetSelectedPart());
        Assert.IsTrue(modelObjA.activeSelf, "첫 번째 모델만 켜져야 합니다.");
        Assert.IsFalse(modelObjB.activeSelf, "두 번째 모델은 꺼져야 합니다.");
    }

    /// <summary>
    /// [핵심 2] 파츠 변경(SelectOption) 시 이전 3D 모델은 꺼지고 새 3D 모델만 켜지는지 검증
    /// </summary>
    [Test]
    public void SelectOption_SwitchesActiveGameObjectCorrectly()
    {
        var parts = new List<CarPart> { partA, partB };
        configurator.Initialize(parts);

        // 두 번째 파츠로 교체
        configurator.SelectOption(partB);

        // 파츠 갱신 및 3D 모델 On/Off 토글 검증
        Assert.AreEqual(partB, configurator.GetSelectedPart());
        Assert.IsFalse(modelObjA.activeSelf, "이전 모델은 꺼져야 합니다.");
        Assert.IsTrue(modelObjB.activeSelf, "새로 선택된 모델만 켜져야 합니다.");
    }

    /// <summary>
    /// [핵심 3] null 등 비정상 입력 시 기존 장착 상태를 안전하게 유지하는지 검증
    /// </summary>
    [Test]
    public void SelectOption_WithNull_PreservesCurrentSelectionAndActiveState()
    {
        var parts = new List<CarPart> { partA, partB };
        configurator.Initialize(parts);

        // 의도된 Warning 로그 선언 (콘솔에 경고 느낌표가 뜨지 않도록 처리)
        LogAssert.Expect(LogType.Warning, "[Test_CarPartConfigurator] 선택하려는 CarPart가 null입니다.");

        // null 전달 시도
        configurator.SelectOption(null);

        // 기존 선택과 모델 활성 상태가 안전하게 보존되는지 검증
        Assert.AreEqual(partA, configurator.GetSelectedPart());
        Assert.IsTrue(modelObjA.activeSelf);
    }
}
