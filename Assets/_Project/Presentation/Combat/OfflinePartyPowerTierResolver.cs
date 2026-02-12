using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace MyGame.Presentation.Combat
{
    [Serializable]
    public struct OfflinePartyPowerMemberScore
    {
        public string memberName;
        public int score;
        public bool hasStats;
    }

    public struct OfflinePartyPowerTierReport
    {
        public bool success;
        public string failureReason;
        public int partyPowerScore;
        public int autoTierIndex;
        public int tableTierIndex;
        public OfflinePartyPowerMemberScore[] members;
    }

    /// <summary>
    /// Presentation 계층에서 Combat asmdef를 직접 참조하지 않고
    /// 출전 파티(최대 4명) 환산 전투력과 tier를 계산한다.
    /// </summary>
    public static class OfflinePartyPowerTierResolver
    {
        private const string PartyRouterTypeName = "MyGame.Party.PartyControlRouter";
        private const string ActorStatsTypeName = "MyGame.Combat.ActorStats";
        private const string StatIdTypeName = "MyGame.Combat.StatId";

        private static readonly string[] PowerStatNames =
        {
            "AP", "AC", "AS",
            "MP", "MA", "MD",
            "HP", "DP", "HV",
            "BF", "TA", "LK"
        };

        private static readonly double[] PowerStatDividers =
        {
            60d, 5d, 1d,
            720d, 5d, 60d,
            720d, 60d, 5d,
            1d, 5d, 5d
        };

        public static OfflinePartyPowerTierReport Resolve(
            MonoBehaviour explicitRouter,
            Transform[] fallbackMembers,
            int powerPerTier,
            int maxAutoTierCount,
            int maxOfflineBalanceTierIndex)
        {
            var report = new OfflinePartyPowerTierReport
            {
                success = false,
                failureReason = "unknown",
                partyPowerScore = 0,
                autoTierIndex = 0,
                tableTierIndex = 0,
                members = Array.Empty<OfflinePartyPowerMemberScore>()
            };

            Type actorStatsType = FindType(ActorStatsTypeName);
            Type statIdType = FindType(StatIdTypeName);
            if (actorStatsType == null || statIdType == null)
            {
                report.failureReason = "ActorStats/StatId type not found";
                return report;
            }

            MethodInfo getBaseFinalStat = actorStatsType.GetMethod("GetBaseFinalStat", BindingFlags.Instance | BindingFlags.Public);
            if (getBaseFinalStat == null)
            {
                report.failureReason = "GetBaseFinalStat not found";
                return report;
            }

            List<Transform> deployed = CollectDeployedPartyMembers(explicitRouter, fallbackMembers);
            if (deployed.Count == 0)
            {
                report.failureReason = "deployed party empty";
                return report;
            }

            var memberScores = new OfflinePartyPowerMemberScore[deployed.Count];
            double partyScore = 0d;
            bool anyHasStats = false;

            for (int i = 0; i < deployed.Count; i++)
            {
                Transform member = deployed[i];
                var m = new OfflinePartyPowerMemberScore
                {
                    memberName = member != null ? member.name : $"Member-{i}",
                    score = 0,
                    hasStats = false
                };

                if (member != null)
                {
                    Component statsComponent = member.GetComponent(actorStatsType);
                    if (statsComponent != null)
                    {
                        m.hasStats = true;
                        m.score = Math.Max(0, (int)Math.Floor(ComputeSingleActorPowerScore(statsComponent, statIdType, getBaseFinalStat)));
                        partyScore += m.score;
                        anyHasStats = true;
                    }
                }

                memberScores[i] = m;
            }

            int finalScore = Math.Max(0, (int)Math.Floor(partyScore));
            int maxAutoTierIndex = Math.Max(0, Mathf.Clamp(maxAutoTierCount, 1, 5) - 1); // 테스트 정책: 0~4만
            int autoTierIndex = Mathf.Clamp(finalScore / Math.Max(1, powerPerTier), 0, maxAutoTierIndex);
            int tableTierIndex = Mathf.Clamp(autoTierIndex, 0, Math.Max(0, maxOfflineBalanceTierIndex));

            report.members = memberScores;
            report.partyPowerScore = finalScore;
            report.autoTierIndex = autoTierIndex;
            report.tableTierIndex = tableTierIndex;
            report.success = anyHasStats;
            report.failureReason = anyHasStats ? string.Empty : "no ActorStats on deployed members";

            return report;
        }

        private static List<Transform> CollectDeployedPartyMembers(MonoBehaviour explicitRouter, Transform[] fallbackMembers)
        {
            var result = new List<Transform>(4);

            if (TryCollectFromRouter(explicitRouter, result))
                return result;

            Type routerType = FindType(PartyRouterTypeName);
            if (routerType != null)
            {
                MonoBehaviour router = FindFirstMonoBehaviourOfType(routerType);
                if (TryCollectFromRouter(router, result))
                    return result;
            }

            if (fallbackMembers != null)
            {
                for (int i = 0; i < fallbackMembers.Length && result.Count < 4; i++)
                {
                    Transform t = fallbackMembers[i];
                    if (t != null)
                        result.Add(t);
                }
            }

            return result;
        }

        private static bool TryCollectFromRouter(MonoBehaviour router, List<Transform> output)
        {
            output.Clear();
            if (router == null) return false;

            Type type = router.GetType();
            PropertyInfo partyMembersProp = type.GetProperty("PartyMembers", BindingFlags.Instance | BindingFlags.Public);
            if (partyMembersProp == null) return false;

            object value = partyMembersProp.GetValue(router);
            if (value is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    if (item is Transform t && t != null)
                        output.Add(t);

                    if (output.Count >= 4)
                        break;
                }
            }

            return output.Count > 0;
        }

        private static MonoBehaviour FindFirstMonoBehaviourOfType(Type type)
        {
            if (type == null) return null;

            UnityEngine.Object[] all = Resources.FindObjectsOfTypeAll(type);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is MonoBehaviour mb && mb.gameObject.scene.IsValid())
                    return mb;
            }

            return null;
        }

        private static double ComputeSingleActorPowerScore(object statsComponent, Type statIdType, MethodInfo getBaseFinalStat)
        {
            double score = 0d;

            for (int i = 0; i < PowerStatNames.Length; i++)
            {
                object statIdValue;
                try
                {
                    statIdValue = Enum.Parse(statIdType, PowerStatNames[i]);
                }
                catch
                {
                    continue;
                }

                object rawValue = getBaseFinalStat.Invoke(statsComponent, new[] { statIdValue });
                if (rawValue == null) continue;

                int statValue;
                try
                {
                    statValue = Convert.ToInt32(rawValue);
                }
                catch
                {
                    continue;
                }

                score += Math.Max(0, statValue) / PowerStatDividers[i];
            }

            return score;
        }

        private static Type FindType(string fullName)
        {
            Type type = Type.GetType(fullName);
            if (type != null) return type;

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < assemblies.Length; i++)
            {
                type = assemblies[i].GetType(fullName);
                if (type != null)
                    return type;
            }

            return null;
        }
    }
}
