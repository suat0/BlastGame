using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // The home screen, laid out after Match Villains': the level button, the start popup it opens,
    // the coin pill and the settings. Everything here either works or answers with "unlocks at
    // level X"; the parts that answer are LockedFeature components and need nothing from this class.
    //
    // No state machine, unlike the level: the only states are "a popup is open" and "none is", and
    // the popups' own IsOpen already says which.
    public sealed class HomeView : MonoBehaviour
    {
        [SerializeField] private LevelCatalog catalog;
        [SerializeField] private LevelRequest request;

        [Header("Level")]
        [SerializeField] private Button levelButton;
        [SerializeField] private TMP_Text levelLabel;
        [SerializeField] private GameObject hardTag;
        [SerializeField] private LevelStartPopup startPopup;

        [Header("Top bar")]
        [SerializeField] private CoinCounter coins;
        [SerializeField] private Button settingsButton;
        [SerializeField] private SettingsPopup settingsPopup;
        [SerializeField] private ConfirmPopup confirmPopup;

        private void Awake()
        {
            startPopup.HideImmediately();
            settingsPopup.HideImmediately();
            confirmPopup.HideImmediately();
        }

        private void OnEnable()
        {
            levelButton.onClick.AddListener(HandleLevelClicked);
            startPopup.PlayButton.onClick.AddListener(HandlePlayClicked);
            startPopup.CloseButton.onClick.AddListener(HandleStartCloseClicked);
            settingsButton.onClick.AddListener(HandleSettingsClicked);
            settingsPopup.CloseButton.onClick.AddListener(HandleSettingsCloseClicked);
            settingsPopup.ActionButton.onClick.AddListener(HandleResetClicked);
            confirmPopup.ConfirmButton.onClick.AddListener(HandleResetConfirmed);
            confirmPopup.CancelButton.onClick.AddListener(HandleResetCancelled);
        }

        private void OnDisable()
        {
            levelButton.onClick.RemoveListener(HandleLevelClicked);
            startPopup.PlayButton.onClick.RemoveListener(HandlePlayClicked);
            startPopup.CloseButton.onClick.RemoveListener(HandleStartCloseClicked);
            settingsButton.onClick.RemoveListener(HandleSettingsClicked);
            settingsPopup.CloseButton.onClick.RemoveListener(HandleSettingsCloseClicked);
            settingsPopup.ActionButton.onClick.RemoveListener(HandleResetClicked);
            confirmPopup.ConfirmButton.onClick.RemoveListener(HandleResetConfirmed);
            confirmPopup.CancelButton.onClick.RemoveListener(HandleResetCancelled);
        }

        private void OnDestroy() => Tween.StopAll(levelButton.transform);

        private void Start()
        {
            int pending = PlayerProgress.TakePendingCoinReward();

            ShowCampaign();
            coins.Begin(PlayerProgress.Coins, pending);

            // Back from a win: the level number just went up, so the button says so.
            if (pending > 0) Tween.PunchScale(levelButton.transform, new Vector3(0.18f, 0.18f, 0f), 0.4f, frequency: 5f, startDelay: 0.3f);
        }

        // Escape on a keyboard, back on Android: closes whatever is open, top first.
        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (confirmPopup.IsOpen) HandleResetCancelled();
            else if (settingsPopup.IsOpen) settingsPopup.Hide();
            else if (startPopup.IsOpen) startPopup.Hide();
        }

        private void ShowCampaign()
        {
            int index = PlayerProgress.LevelIndex;

            levelLabel.SetText("Level {0:0}", index + 1);
            hardTag.SetActive(catalog.At(index).IsHard);
        }

        private void HandleLevelClicked()
        {
            int index = PlayerProgress.LevelIndex;
            LevelConfig level = catalog.At(index);

            // What Core will place, not what the level asks for: generation keeps the top row free of
            // Boxes, so a request above that is clamped. The campaign never asks for that many, but the
            // popup should not be able to promise a goal the board cannot hold.
            int boxes = Mathf.Min(level.BoxCount, (level.Rows - 1) * level.Cols);

            startPopup.Show(index + 1, level.IsHard, boxes, boxes > 0 ? level.MoveLimit : 0);
        }

        private void HandlePlayClicked()
        {
            if (!startPopup.IsOpen) return;

            int index = PlayerProgress.LevelIndex;

            request.Set(catalog.At(index), index);
            App.Instance.Transition.LoadScene(Scenes.Level);
        }

        private void HandleStartCloseClicked() => startPopup.Hide();

        private void HandleSettingsClicked() => settingsPopup.Show();

        private void HandleSettingsCloseClicked() => settingsPopup.Hide();

        private void HandleResetClicked()
        {
            settingsPopup.Hide();
            confirmPopup.Show("Reset progress?", "Back to Level 1 with no coins.", "Reset");
        }

        private void HandleResetConfirmed()
        {
            if (!confirmPopup.IsOpen) return;

            PlayerProgress.ResetAll();
            confirmPopup.Hide();

            ShowCampaign();
            coins.Begin(PlayerProgress.Coins, 0);
        }

        private void HandleResetCancelled()
        {
            confirmPopup.Hide();
            settingsPopup.Show();
        }
    }
}
