using System;
using System.Collections.Generic;

namespace GateHelper.PocketFarm
{
    public class DeliveryContract
    {
        public string Id { get; set; }
        public FarmTabType ContractType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string RequiredItemId { get; set; }
        public int RequiredAmount { get; set; }
        public int RewardGold { get; set; }
        public int RewardFame { get; set; }
        public bool IsCompleted { get; set; }

        public DeliveryContract(string id, FarmTabType type, string title, string desc, string itemId, int amount, int gold, int fame)
        {
            Id = id;
            ContractType = type;
            Title = title;
            Description = desc;
            RequiredItemId = itemId;
            RequiredAmount = amount;
            RewardGold = gold;
            RewardFame = fame;
            IsCompleted = false;
        }
    }

    public class FarmContractSystem
    {
        public List<DeliveryContract> DailyContracts { get; private set; } = new List<DeliveryContract>();
        public bool DailyBonusClaimed { get; set; } = false;

        private static readonly Random _rnd = new Random();

        public FarmContractSystem()
        {
            GenerateContracts();
        }

        public void Tick(float dt)
        {
            // 의뢰 유지 및 실시간 검증 (필요시 타이머 기반 자정 갱신 가능)
        }

        public void GenerateContracts()
        {
            DailyContracts.Clear();
            DailyBonusClaimed = false;

            // 의뢰 1: 농경 납품 (밀 or 황금밀)
            int wheatReq = _rnd.Next(8, 16);
            DailyContracts.Add(new DeliveryContract(
                "contract_agri",
                FarmTabType.Farm,
                "[농업 납품] 마을 제빵소 곡물 수급",
                "향긋한 빵을 굽기 위해 신선한 수확 곡물이 대량으로 필요합니다.",
                "crop_wheat",
                wheatReq,
                wheatReq * 12 + 60,
                15
            ));

            // 의뢰 2: 수산 납품 (피라미 or 잉어)
            int fishReq = _rnd.Next(2, 5);
            DailyContracts.Add(new DeliveryContract(
                "contract_fish",
                FarmTabType.Fishing,
                "[수산 납품] 포구 시장 횟감 조달",
                "주말 포구 어시장에 방문한 손님들을 위해 싱싱한 생선이 필요합니다.",
                "fish_minnow",
                fishReq,
                fishReq * 50 + 100,
                20
            ));

            // 의뢰 3: 목축 납품 (달걀 or 양모 or 우유)
            int eggReq = _rnd.Next(2, 4);
            DailyContracts.Add(new DeliveryContract(
                "contract_live",
                FarmTabType.Livestock,
                "[목축 납품] 동네 식료품점 신선란 공급",
                "아침 식사를 준비하는 마을 주민들을 위해 신선한 달걀을 납품해주세요.",
                "live_egg",
                eggReq,
                eggReq * 45 + 120,
                25
            ));
        }

        public bool CanFulfill(DeliveryContract contract, PocketFarmModel model)
        {
            if (contract == null || contract.IsCompleted || model == null) return false;
            return model.Inventory.ContainsKey(contract.RequiredItemId) && model.Inventory[contract.RequiredItemId] >= contract.RequiredAmount;
        }
    }
}
