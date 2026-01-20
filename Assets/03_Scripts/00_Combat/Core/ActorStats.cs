using UnityEngine;

namespace MyGame.Combat
{
    /// <summary>
    /// ✅ 문서 기반 스탯 계산기
    /// - 자동 성장: (속성 가중치 × 레벨) → 속성 스탯레벨
    /// - 투자 성장: 각 스탯에 투자한 스탯레벨(+1씩)
    /// - 실제 스탯값 변환: /1, /12, /60, ×30 (내림 포함)
    /// - 장비 Flat: equipXX
    /// ※ 버프/디버프(상태이상)는 StatusController가 최종값에 적용
    /// </summary>
    public class ActorStats : MonoBehaviour
    {
        [Header("Level")]
        [Min(1)] public int level = 1;

        [Header("Growth Weights (sum=12, each 1~5)")]
        [Range(1, 5)] public int fireGrowth = 1;
        [Range(1, 5)] public int waterGrowth = 1;
        [Range(1, 5)] public int woodGrowth = 1;
        [Range(1, 5)] public int metalGrowth = 1;

        [Header("Invested Stat Levels (+1 each point)")]
        public int investAP, investAC, investDX;
        public int investMP, investMA, investMD;
        public int investHP, investDP, investHV;
        public int investWT, investTA, investLK;

        [Header("Equipment Flat Bonus (optional)")]
        public int equipAP, equipAC, equipDX;
        public int equipMP, equipMA, equipMD;
        public int equipHP, equipDP, equipHV;
        public int equipWT, equipTA, equipLK;

        public int GetTotalStatLevel(StatId id)
        {
            int fireLv = fireGrowth * level;
            int waterLv = waterGrowth * level;
            int woodLv = woodGrowth * level;
            int metalLv = metalGrowth * level;

            return id switch
            {
                // Fire group
                StatId.AP => fireLv + investAP,
                StatId.AC => fireLv + investAC,
                StatId.DX => fireLv + investDX,

                // Water group
                StatId.MP => waterLv + investMP,
                StatId.MA => waterLv + investMA,
                StatId.MD => waterLv + investMD,

                // Wood group
                StatId.HP => woodLv + investHP,
                StatId.DP => woodLv + investDP,
                StatId.HV => woodLv + investHV,

                // Metal group
                StatId.WT => metalLv + investWT,
                StatId.TA => metalLv + investTA,
                StatId.LK => metalLv + investLK,

                _ => 0
            };
        }

        public int ConvertLevelToValue(StatId id, int statLevel)
        {
            if (statLevel < 0) statLevel = 0;

            //  내림(Floor)은 int 나눗셈으로 자연스럽게 반영됨
            return id switch
            {
                // Fire
                StatId.AP => statLevel / 1,
                StatId.AC => statLevel / 12,
                StatId.DX => statLevel / 60,

                // Water
                StatId.MP => statLevel * 60,
                StatId.MA => statLevel / 12,
                StatId.MD => statLevel / 1,

                // Wood
                StatId.HP => statLevel * 60,
                StatId.DP => statLevel / 1,
                StatId.HV => statLevel / 12,

                // Metal
                StatId.WT => statLevel / 60,
                StatId.TA => statLevel / 12,
                StatId.LK => statLevel / 12,

                _ => 0
            };
        }

        public int GetEquipmentFlat(StatId id)
        {
            return id switch
            {
                StatId.AP => equipAP,
                StatId.AC => equipAC,
                StatId.DX => equipDX,

                StatId.MP => equipMP,
                StatId.MA => equipMA,
                StatId.MD => equipMD,

                StatId.HP => equipHP,
                StatId.DP => equipDP,
                StatId.HV => equipHV,

                StatId.WT => equipWT,
                StatId.TA => equipTA,
                StatId.LK => equipLK,
                _ => 0
            };
        }

        /// <summary>
        /// 상태이상/버프 적용 전의 기본 최종값
        /// </summary>
        public int GetBaseFinalStat(StatId id)
        {
            int lv = GetTotalStatLevel(id);
            int value = ConvertLevelToValue(id, lv);
            value += GetEquipmentFlat(id);
            return value;
        }
    }
}
