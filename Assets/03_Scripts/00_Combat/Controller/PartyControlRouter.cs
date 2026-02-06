using System.Collections.Generic;
using UnityEngine;
using PartySelection.Installer;
using MyGame.Combat;

namespace MyGame.Party
{
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

        // ✅ 외부(Formation 등)에서 파티 멤버를 읽을 수 있게 제공
        public int PartySize => partyMembers != null ? partyMembers.Length : 0;
        public IReadOnlyList<Transform> PartyMembers => partyMembers;

        public Transform GetMember(int slotIndex)
        {
            if (partyMembers == null) return null;
            if (slotIndex < 0 || slotIndex >= partyMembers.Length) return null;
            return partyMembers[slotIndex];
        }

        private void Awake()
        {
            if (installer == null) installer = FindObjectOfType<PartySelectionInstaller>();
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
