using UnityEngine;
using PartySelection.Installer;
using MyGame.Combat;

namespace MyGame.Party
{
    /// <summary>
    /// "현재 수동 컨트롤(카메라가 잡은) 파티 멤버"를 단일 소스로 관리한다.
    /// - 파티 멤버의 "자동 전투/이동"은 기본적으로 항상 돌아간다.
    /// - 오직 "컨트롤 중인 멤버"만 글로벌 AutoMode(ON/OFF) 및 조이스틱 입력의 영향을 받는다.
    ///
    ///  요구사항 대응
    /// 1) 수동 컨트롤(조이스틱/오토 토글)은 현재 컨트롤 중인 캐릭터만 영향
    ///    -> 다른 캐릭터의 자동전투는 계속 진행
    /// 2) 캐릭터 전환 시 이전 캐릭터는 즉시 자동 상태로 복귀(= 더 이상 글로벌 AutoMode 영향 X)
    /// </summary>
    public sealed class PartyControlRouter : MonoBehaviour
    {
        [Header("Selection Source (UI)")]
        [SerializeField] private PartySelectionInstaller installer;

        [Header("Party Members (order must match slot 0~3)")]
        [SerializeField] private Transform[] partyMembers = new Transform[4];

        [Header("Start")]
        [SerializeField] private int startSlotIndex = 0;

        public int ControlledSlotIndex { get; private set; } = -1;

        public Actor ControlledActor { get; private set; }
        public CombatController ControlledCombatController { get; private set; }

        private void Awake()
        {
            if (installer == null) installer = FindObjectOfType<PartySelectionInstaller>();
            // 방어: 길이 보정
            if (partyMembers == null || partyMembers.Length != 4)
                partyMembers = new Transform[4];

            SetControlledSlot(Mathf.Clamp(startSlotIndex, 0, 3));
        }

        private void OnEnable()
        {
            if (installer != null)
                installer.OnSelectedSlotChanged += HandleSelectedSlotChanged;
        }

        private void OnDisable()
        {
            if (installer != null)
                installer.OnSelectedSlotChanged -= HandleSelectedSlotChanged;
        }

        private void HandleSelectedSlotChanged(int slotIndex, string characterId)
        {
            SetControlledSlot(slotIndex);
        }

        public void SetControlledSlot(int slotIndex)
        {
            slotIndex = Mathf.Clamp(slotIndex, 0, 3);

            ControlledSlotIndex = slotIndex;

            var t = (partyMembers != null && slotIndex < partyMembers.Length) ? partyMembers[slotIndex] : null;

            ControlledActor = (t != null) ? t.GetComponent<Actor>() : null;
            ControlledCombatController = (t != null) ? t.GetComponent<CombatController>() : null;
        }

        public bool IsControlled(Actor actor)
        {
            return actor != null && actor == ControlledActor;
        }

        public bool IsControlled(Transform t)
        {
            if (t == null) return false;
            return partyMembers != null
                   && ControlledSlotIndex >= 0
                   && ControlledSlotIndex < partyMembers.Length
                   && partyMembers[ControlledSlotIndex] == t;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (partyMembers != null && partyMembers.Length != 4)
            {
                var tmp = new Transform[4];
                for (int i = 0; i < Mathf.Min(4, partyMembers.Length); i++)
                    tmp[i] = partyMembers[i];
                partyMembers = tmp;
            }

            startSlotIndex = Mathf.Clamp(startSlotIndex, 0, 3);
        }
#endif
    }
}
