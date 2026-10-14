using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// CarPartConfigurator의 독립 토글 동작(초기화, 파츠 개별 토글, 다중 장착 목록 반환, 예외 방어)을 검증하는 단위 테스트
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

        // 2. 가상 CarPart 데이터 준비
        partA = new CarPart { id = "acc_spoiler_std", name = "Standard Spoiler", category = PartCategory.Accessory, price = 1000 };
        partB = new CarPart { id = "acc_carbon_wing", name = "Carbon Racing Wing", category = PartCategory.Accessory, price = 2000 };

        // 3. 가상 3D 모델 GameObject 준비
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
    /// [핵심 1] 초기화 시 옵션 목록이 등록되는지 검증
    /// </summary>
    [Test]
    public void Initialize_RegistersOptionsCorrectly()
    {
        var parts = new List<CarPart> { partA, partB };
        configurator.Initialize(parts);

        Assert.AreEqual(2, configurator.GetOptions().Count);
    }

    /// <summary>
    /// [핵심 2] 파츠 선택(SelectOption) 시 해당 파츠가 단독 토글(On/Off) 및 다중 장착되는지 검증
    /// </summary>
    [Test]
    public void SelectOption_TogglesActiveGameObjectAndSupportsMultiEquip()
    {
        var parts = new List<CarPart> { partA, partB };
        configurator.Initialize(parts);

        // 1. partA 토글 On
        configurator.SelectOption(partA);
        Assert.IsTrue(modelObjA.activeSelf, "A 모델이 켜져야 합니다.");
        Assert.AreEqual(1, configurator.GetSelectedParts().Count);

        // 2. partB 토글 On (A와 동시 장착)
        configurator.SelectOption(partB);
        Assert.IsTrue(modelObjA.activeSelf, "A 모델은 계속 켜져있어야 합니다.");
        Assert.IsTrue(modelObjB.activeSelf, "B 모델도 켜져있어야 합니다.");
        Assert.AreEqual(2, configurator.GetSelectedParts().Count);

        // 3. partA 다시 클릭 -> 토글 Off
        configurator.SelectOption(partA);
        Assert.IsFalse(modelObjA.activeSelf, "A 모델은 토글되어 꺼져야 합니다.");
        Assert.IsTrue(modelObjB.activeSelf, "B 모델은 켜진 상태를 유지해야 합니다.");
        Assert.AreEqual(1, configurator.GetSelectedParts().Count);
        Assert.AreEqual(partB, configurator.GetSelectedParts()[0]);
    }

    /// <summary>
    /// [핵심 3] null 등 비정상 입력 시 기존 장착 상태를 안전하게 유지하는지 검증
    /// </summary>
    [Test]
    public void SelectOption_WithNull_PreservesCurrentActiveState()
    {
        var parts = new List<CarPart> { partA, partB };
        configurator.Initialize(parts);

        configurator.SelectOption(partA);
        Assert.IsTrue(modelObjA.activeSelf);

        LogAssert.Expect(LogType.Warning, "[Test_CarPartConfigurator] 선택하려는 CarPart가 null입니다.");
        configurator.SelectOption(null);

        Assert.IsTrue(modelObjA.activeSelf);
        Assert.AreEqual(1, configurator.GetSelectedParts().Count);
    }
}
