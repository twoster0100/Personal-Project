namespace MyGame.Combat
{
    /// <summary>
    /// 스케일링에 사용할 "스탯 값의 출처"
    /// - StatLevelSum     : 성장/투자 합(= ActorStats.GetTotalStatLevel)
    /// - BaseFinal        : 변환식 + 장비까지(버프 제외)(= ActorStats.GetBaseFinalStat)
    /// - FinalWithStatus  : BaseFinal에 상태이상(버프/디버프)까지 적용(= Actor.GetFinalStat)
    /// </summary>
    public enum StatValueSource
    {
        StatLevelSum,
        BaseFinal,
        FinalWithStatus
    }
}
