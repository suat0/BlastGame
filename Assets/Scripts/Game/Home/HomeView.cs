using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // The home screen. Shows where the campaign stands and starts the next level.
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private LevelRequest request;

        [SerializeField] private Button levelButton;
        [SerializeField] private TMP_Text levelLabel;

        private void OnEnable() => levelButton.onClick.AddListener(HandleLevelClicked);

        private void OnDisable() => levelButton.onClick.RemoveListener(HandleLevelClicked);

        private void Start() => levelLabel.SetText("Level {0:0}", PlayerProgress.LevelIndex + 1);

        private void HandleLevelClicked()
        {
            int index = PlayerProgress.LevelIndex;

            request.Set(catalog.At(index), index);
            App.Instance.Transition.LoadScene(Scenes.Level);
        }
    }
}
