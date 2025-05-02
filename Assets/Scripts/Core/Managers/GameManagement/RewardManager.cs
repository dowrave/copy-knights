using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Overlays;

public class RewardManager : MonoBehaviour
{
    // stars�� ���� �������� ������ �����ؼ� ��� �������� �����մϴ�.
    public void SetAndGiveStageRewards(StageData stageData, int stars)
    {
        // ������ �ٲܰŶ� �������� ��� ó����
        var (fcr, bcr) = SetStageRewards(stageData, stars);

        // ����Ʈ�� �ٲ㼭 �����ؾ� ����ȭ �ÿ� �� �����ϴٰ� ��
        List<ItemWithCount> firstClearRewards = new List<ItemWithCount>(fcr);
        List<ItemWithCount> basicClearRewards = new List<ItemWithCount>(bcr);

        GameManagement.Instance!.PlayerDataManager.GrantStageRewards(firstClearRewards, basicClearRewards);
    }

    // Ŭ���� �� ������ ���� ������ ����Ͽ� ��ȯ�մϴ�.
    public (IReadOnlyList<ItemWithCount> firstClearRewards, IReadOnlyList<ItemWithCount> basicClearRewards) 
        SetStageRewards(StageData stageData, int stars)
    {
        var stageResultInfo = GameManagement.Instance!.PlayerDataManager.GetStageResultInfo(stageData.stageId);

        // ���� Ŭ���� ���� ���
        List<ItemWithCount> perfectFirstClearRewards = stageData.FirstClearRewardItems;

        float firstClearExpItemRate = SetFirstClearExpItemRate(stageResultInfo, stars);
        float firstClearPromoItemRate = SetFirstClearPromotionItemRate(stageResultInfo, stars);
        var firstClearRewards = MultiplyRewards(perfectFirstClearRewards, firstClearExpItemRate, firstClearPromoItemRate);

        // �⺻ Ŭ���� ���� ���
        List<ItemWithCount> perfectBasicClearRewards = stageData.BasicClearRewardItems;

        float basicClearExpItemRate = SetBasicClearItemRate(stars);
        var basicClearRewards = MultiplyRewards(perfectBasicClearRewards, basicClearExpItemRate);

        return (firstClearRewards, basicClearRewards);
    }

    // �� reward�� count�� itemRate�� ���Ͽ� �� ����Ʈ�� ��ȯ�մϴ�.
    private List<ItemWithCount> MultiplyRewards(List<ItemWithCount> rewards, float expItemRate, float promoItemRate = 0f)
    {
        List<ItemWithCount> scaledRewards = new List<ItemWithCount>();

        // 3�� Ŭ��� �ݺ����� ��� ������ 0�� �� ������, �� ���� �� ����Ʈ�� ��ȯ�� 
        if (expItemRate == 0f && promoItemRate == 0f) return scaledRewards;

        foreach (var reward in rewards)
        {
            // ����ȭ ������ ó��
            if (reward.itemData.type == ItemData.ItemType.EliteItem)
            {
                int scaledCount = Mathf.FloorToInt(reward.count * promoItemRate);
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }
            // ����ġ ������ ó��
            else
            {
                int scaledCount = Mathf.FloorToInt(reward.count * expItemRate);
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }
        }
        return scaledRewards;
    }

    // n���� ���ʷ� �޼����� ���� ����ġ ������ ���� ������ ����Ѵ�.
    private float SetFirstClearExpItemRate(StageResultData.StageResultInfo? resultInfo, int stars)
    {
        if (resultInfo == null)
        {
            if (stars == 1) return 0.25f;
            else if (stars == 2) return 0.5f;
            else if (stars == 3) return 1f;
        }

        if (resultInfo.stars == 3) return 0f;

        if (resultInfo.stars < stars)
        {
            if (resultInfo.stars == 1)
            {
                if (stars == 2) return 0.25f;
                if (stars == 3) return 0.75f;
            }
            else if (resultInfo.stars == 2)
            {
                if (stars == 3) return 0.5f;
            }
        }

        throw new InvalidOperationException("FirstClearItemRate�� ����ġ ���� ����");
    }

    // �̹��� 3������ ����, ������ �� ����� 3�� �̸��̾����� ����
    private float SetFirstClearPromotionItemRate(StageResultData.StageResultInfo? resultInfo, int stars)
    {
        if (stars != 3)
        {
            return 0f;
        }

        if (resultInfo == null || resultInfo.stars < 3)
        {
            return 1f;
        }

        throw new InvalidOperationException("FirstClearPromotionItemRate�� ����ġ ���� ����");
    }

    // n������ Ŭ�������� ���� ������ ���� ������ ����Ѵ�.
    private float SetBasicClearItemRate(int stars)
    {
        if (stars == 1) return 0.25f;
        else if (stars == 2) return 0.5f;
        else if (stars == 3) return 1f;
        else throw new InvalidOperationException("BasicClearItemRate�� ����ġ ���� ����");
    }

    // UI ǥ�ÿ� - 3�� ���� Ŭ��� �Ѵٰ� �������� �� ���� ���� ������ ���� ���
    public float GetResultFirstClearItemRate(int prevStars)
    {
        // ù Ŭ���� ������ ���� ���޷� ���
        if (prevStars == 0)
        {
            return 1f;
        }
        else if (prevStars == 1)
        {
            return 0.75f;
        }
        else if (prevStars == 2)
        {
            return 0.5f;
        }

        throw new InvalidOperationException("��?");

    }
}
