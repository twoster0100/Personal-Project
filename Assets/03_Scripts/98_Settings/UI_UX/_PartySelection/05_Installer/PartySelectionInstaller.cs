using PartySelection.Feedback;
using PartySelection.Model;
using PartySelection.Presenter;
using PartySelection.Provider;
using PartySelection.View;
using UnityEngine;

namespace PartySelection.Installer
{
    public sealed class PartySelectionInstaller : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private PartySelectionView view;
        [SerializeField] private PartySelectionFeedback feedback;

        [Header("Profile Provider (MonoBehaviour that implements ICharacterProfileProvider)")]
        [SerializeField] private MonoBehaviour profileProviderBehaviour;

        private CharacterSlotPresenter _presenter;

        private void Awake()
        {
            var provider = profileProviderBehaviour as ICharacterProfileProvider;
            if (provider == null)
            {
                Debug.LogError("[PartySelectionInstaller] profileProviderBehaviour must implement ICharacterProfileProvider");
                enabled = false;
                return;
            }

            var state = new CharacterSlotState();
            var roster = new CharacterRoster();

            // 초기 캐릭터 ID 배치(임시)
            roster.SetCharacterId(0, "Bunny");
            roster.SetCharacterId(1, "X");
            roster.SetCharacterId(2, "A");
            roster.SetCharacterId(3, "Y");

            _presenter = new CharacterSlotPresenter(view, state, roster, provider, feedback);
            _presenter.Bind();
        }

        private void OnDestroy()
        {
            _presenter?.Unbind();
        }

        // (확장용) 외부에서 캐릭터 교체 호출 가능
        public void ReplaceSelectedSlotCharacter(string characterId)
            => _presenter?.ReplaceCharacterInSelectedSlot(characterId);
    }
}
