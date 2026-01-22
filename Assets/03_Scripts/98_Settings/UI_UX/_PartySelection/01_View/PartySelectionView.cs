using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PartySelection.View
{
    /// <summary>
    /// [View - 순수 UI]
    /// 역할:
    /// 1) 씬 UI 레퍼런스를 잡는다(버튼/텍스트/초상화 등)
    /// 2) 버튼 클릭 같은 UI 이벤트를 외부로 "발행"한다
    /// 3) Presenter가 내려준 "표시 명령"만 수행한다(텍스트/이미지 갱신)
    ///
    /// 금지:
    /// - DOTween/MMF 같은 연출 호출 금지 (연출은 Feedback에서만)
    /// - 상태(Model/State) 직접 변경 금지 (Presenter가 제어)
    /// </summary>
    public sealed class PartySelectionView : MonoBehaviour
    {
        [Header("Profile UI")]
        [SerializeField] private TMP_Text userNameText;

        // Portrait는 프로젝트마다 Image 또는 RawImage일 수 있음.
        // 우선 둘 다 지원(한쪽만 넣어도 동작).
        [SerializeField] private Image portraitImage;
        [SerializeField] private RawImage portraitRawImage;

        [Header("Slot Buttons (1~4)")]
        [SerializeField] private Button[] slotButtons = new Button[4];

        /// <summary>
        /// 슬롯 버튼 클릭 이벤트(0~3).
        /// Presenter가 구독해서 흐름을 제어한다.
        /// </summary>
        public event Action<int> OnSlotClicked;

        public Transform GetSlotTransform(int slotIndex)
        {
            if (slotButtons == null) return null;
            if (slotIndex < 0 || slotIndex >= slotButtons.Length) return null;
            return slotButtons[slotIndex] != null ? slotButtons[slotIndex].transform : null;
        }

        /// <summary>
        /// 좌상단 이름 텍스트 표시.
        /// </summary>
        public void SetUserName(string name)
        {
            if (userNameText == null) return;
            userNameText.text = name ?? string.Empty;
        }

        /// <summary>
        /// 좌상단 초상화 표시.
        /// - 보통은 Image+Sprite 조합이 정석.
        /// - RawImage는 Texture 기반이라 Sprite.texture를 넣으면
        ///   (아틀라스/부분 스프라이트인 경우) 전체 텍스처가 보일 수 있음.
        ///   => 가능하면 portraitImage 쪽을 쓰는 걸 권장.
        /// </summary>
        public void SetPortrait(Sprite sprite)
        {
            if (portraitImage != null)
            {
                portraitImage.sprite = sprite;
                portraitImage.enabled = (sprite != null);
            }

            if (portraitRawImage != null)
            {
                portraitRawImage.texture = (sprite != null) ? sprite.texture : null;
                portraitRawImage.enabled = (sprite != null);
            }
        }

        /// <summary>
        /// 슬롯 선택 비주얼(아주 단순 버전).
        /// - 선택된 슬롯은 interactable=false로 만들어 "이미 선택됨" 느낌을 줌.
        /// - 네 UI 룰이 "테두리 강조/색상 변경"이라면
        ///   이 함수만 확장하면 됨(다른 레이어는 건드릴 필요 없음).
        /// </summary>
        public void SetSlotSelectedVisual(int selectedIndex)
        {
            if (slotButtons == null) return;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null) continue;
                slotButtons[i].interactable = (i != selectedIndex);
            }
        }

        private void Awake()
        {
            // 안전장치: 배열 크기 확인(실수로 4개가 아니면 바로 눈치채게)
            if (slotButtons == null || slotButtons.Length != 4)
            {
                Debug.LogWarning("[PartySelectionView] slotButtons array size should be 4 (Slot_1~Slot_4).");
            }

            // 버튼 클릭 → OnSlotClicked 발행
            if (slotButtons == null) return;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int idx = i; // 클로저 캡처 방지
                if (slotButtons[idx] == null) continue;

                slotButtons[idx].onClick.AddListener(() =>
                {
                    OnSlotClicked?.Invoke(idx);
                });
            }
        }
    }
}
