using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEditor.Overlays;

public class RewardManager : MonoBehaviour
{
    // stars에 따른 스테이지 보상을 설정해서 결과 아이템을 지급합니다.
    public void SetAndGiveStageRewards(StageData stageData, int stars)
    {
        // 어차피 바꿀거라서 변수명은 약어 처리함
        var (fcr, bcr) = SetStageRewards(stageData, stars);

        // 리스트로 바꿔서 전달해야 직렬화 시에 더 안전하다고 함
        List<ItemWithCount> firstClearRewards = new List<ItemWithCount>(fcr);
        List<ItemWithCount> basicClearRewards = new List<ItemWithCount>(bcr);

        GameManagement.Instance!.PlayerDataManager.GrantStageRewards(firstClearRewards, basicClearRewards);
    }

    // 클리어 별 갯수에 따른 보상을 계산하여 반환합니다.
    public (IReadOnlyList<ItemWithCount> firstClearRewards, IReadOnlyList<ItemWithCount> basicClearRewards) 
        SetStageRewards(StageData stageData, int stars)
    {
        var stageResultInfo = GameManagement.Instance!.PlayerDataManager.GetStageResultInfo(stageData.stageId);

        // 최초 클리어 보상 계산
        List<ItemWithCount> perfectFirstClearRewards = stageData.FirstClearRewardItems;

        float firstClearExpItemRate = SetFirstClearExpItemRate(stageResultInfo, stars);
        float firstClearPromoItemRate = SetFirstClearPromotionItemRate(stageResultInfo, stars);
        var firstClearRewards = MultiplyRewards(perfectFirstClearRewards, firstClearExpItemRate, firstClearPromoItemRate);

        // 기본 클리어 보상 계산
        List<ItemWithCount> perfectBasicClearRewards = stageData.BasicClearRewardItems;

        float basicClearExpItemRate = SetBasicClearItemRate(stars);
        var basicClearRewards = MultiplyRewards(perfectBasicClearRewards, basicClearExpItemRate);

        return (firstClearRewards, basicClearRewards);
    }

    // 각 reward의 count에 itemRate를 곱하여 새 리스트로 반환합니다.
    private List<ItemWithCount> MultiplyRewards(List<ItemWithCount> rewards, float expItemRate, float promoItemRate = 0f)
    {
        List<ItemWithCount> scaledRewards = new List<ItemWithCount>();

        // 3성 클리어를 반복했을 경우 배율이 0일 수 있으며, 이 경우는 빈 리스트를 반환함 
        if (expItemRate == 0f && promoItemRate == 0f) return scaledRewards;

        foreach (var reward in rewards)
        {
            // 정예화 아이템 처리
            if (reward.itemData.type == ItemData.ItemType.EliteItem)
            {
                int scaledCount = Mathf.FloorToInt(reward.count * promoItemRate);
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }
            // 경험치 아이템 처리
            else
            {
                int scaledCount = Mathf.FloorToInt(reward.count * expItemRate);
                scaledRewards.Add(new ItemWithCount(reward.itemData, scaledCount));
            }
        }
        return scaledRewards;
    }

    // n성을 최초로 달성했을 때의 경험치 아이템 지급 배율을 계산한다.
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

        throw new InvalidOperationException("FirstClearItemRate의 예상치 못한 동작");
    }

    // 이번에 3성으로 깨고, 이전에 깬 기록이 3성 미만이었으면 지급
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

        throw new InvalidOperationException("FirstClearPromotionItemRate의 예상치 못한 동작");
    }

    // n성으로 클리어했을 때의 아이템 지급 비율을 계산한다.
    private float SetBasicClearItemRate(int stars)
    {
        if (stars == 1) return 0.25f;
        else if (stars == 2) return 0.5f;
        else if (stars == 3) return 1f;
        else throw new InvalidOperationException("BasicClearItemRate의 예상치 못한 동작");
    }

    // UI 표시용 - 3성 기준 클리어를 한다고 가정했을 때 남은 최초 아이템 배율 계산
    public float GetResultFirstClearItemRate(int prevStars)
    {
        // 첫 클리어 보상의 남은 지급량 계산
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

        throw new InvalidOperationException("엉?");

    }
}
