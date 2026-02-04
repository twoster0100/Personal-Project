namespace MyGame.Combat
{
    /// <summary>
    /// 픽업 타입(ExpOrb / Item).
    /// PickupObject에서 직렬화/스위치에 사용.
    /// 나중에 스킬경험치 오브젝트나 등등 추가하자
    /// </summary>
    public enum PickupKind
    {
        ExpOrb = 0,
        Item = 1,
    }
}
