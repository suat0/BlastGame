using System;
using BlastGame.Core;
using BlastGame.Game.UI;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game
{
    // Runs a level from the goal banner to the way out, as a set of states. This is the explicit
    // state machine the README named with its threshold - "the second screen" - and that screen has
    // now arrived: a home scene, a start popup, pause, confirm, win and lose.
    //
    // Owns the references every state needs and forwards events to whichever state is current. The
    // states are created once, here, and reused: changing state allocates nothing.
    public sealed class LevelFlow : MonoBehaviour
    {
        [Header("Level")]
        [SerializeField] private GameController controller;
        [SerializeField] private BoardView boardView;
        [SerializeField] private InputHandler input;

        [Header("UI")]
        [SerializeField] private Button settingsButton;
        [SerializeField] private IntroBanner introBanner;
        [SerializeField] private LevelStartPopup startPopup;
        [SerializeField] private SettingsPopup pausePopup;
        [SerializeField] private ConfirmPopup confirmExitPopup;
        [SerializeField] private WinPopup winPopup;
        [SerializeField] private LosePopup losePopup;

        [Header("Timing")]
        [Tooltip("Seconds the goal banner holds before play starts.")]
        [SerializeField] private float introDuration = 1.2f;

        [Tooltip("Seconds without a move before the largest group starts to pulse.")]
        [SerializeField] private float hintDelay = 5f;

        [Tooltip("Seconds between the board settling and the end card, so the last blast is seen " +
                 "landing before anything covers it.")]
        [SerializeField] private float outcomeBeat = 0.4f;

        [Header("Reward")]
        [SerializeField] private int coinsPerWin = 20;
        [SerializeField] private int coinsPerMoveLeft = 5;

        private LevelState current;

        public GameController Controller => controller;
        public BoardView BoardView => boardView;
        public IntroBanner IntroBanner => introBanner;
        public LevelStartPopup StartPopup => startPopup;
        public SettingsPopup PausePopup => pausePopup;
        public ConfirmPopup ConfirmExitPopup => confirmExitPopup;
        public WinPopup WinPopup => winPopup;
        public LosePopup LosePopup => losePopup;

        public float IntroDuration => introDuration;
        public float OutcomeBeat => outcomeBeat;
        public float HintDelay => hintDelay;

        public BriefingState Briefing { get; private set; }
        public IntroState Intro { get; private set; }
        public PlayingState Playing { get; private set; }
        public SettlingState Settling { get; private set; }
        public PausedState Paused { get; private set; }
        public ConfirmExitState ConfirmExit { get; private set; }
        public WonState Won { get; private set; }
        public LostState Lost { get; private set; }

        public LevelState Current => current;

        private void Awake()
        {
            Briefing = new BriefingState(this);
            Intro = new IntroState(this);
            Playing = new PlayingState(this);
            Settling = new SettlingState(this);
            Paused = new PausedState(this);
            ConfirmExit = new ConfirmExitState(this);
            Won = new WonState(this);
            Lost = new LostState(this);

            // Nothing is open when a level scene opens; the first state decides what appears.
            startPopup.HideImmediately();
            pausePopup.HideImmediately();
            confirmExitPopup.HideImmediately();
            winPopup.HideImmediately();
            losePopup.HideImmediately();

            // Off until a state turns it on. The board draws on its first frame; play does not start
            // until the banner has gone.
            input.enabled = false;
        }

        // Named methods, never lambdas, so every one of these can be removed again.
        private void OnEnable()
        {
            controller.OnStatusChanged += HandleStatusChanged;
            controller.OnTapRejected += HandleTapRejected;

            settingsButton.onClick.AddListener(HandleSettingsClicked);

            startPopup.PlayButton.onClick.AddListener(HandleStartPlayClicked);
            startPopup.CloseButton.onClick.AddListener(HandleStartCloseClicked);
            pausePopup.CloseButton.onClick.AddListener(HandleResumeClicked);
            pausePopup.ActionButton.onClick.AddListener(HandleLeaveClicked);
            confirmExitPopup.ConfirmButton.onClick.AddListener(HandleConfirmLeaveClicked);
            confirmExitPopup.CancelButton.onClick.AddListener(HandleConfirmStayClicked);
            winPopup.ContinueButton.onClick.AddListener(HandleContinueClicked);
            losePopup.RetryButton.onClick.AddListener(HandleRetryClicked);
            losePopup.HomeButton.onClick.AddListener(HandleLoseHomeClicked);
        }

        private void OnDisable()
        {
            controller.OnStatusChanged -= HandleStatusChanged;
            controller.OnTapRejected -= HandleTapRejected;

            settingsButton.onClick.RemoveListener(HandleSettingsClicked);

            startPopup.PlayButton.onClick.RemoveListener(HandleStartPlayClicked);
            startPopup.CloseButton.onClick.RemoveListener(HandleStartCloseClicked);
            pausePopup.CloseButton.onClick.RemoveListener(HandleResumeClicked);
            pausePopup.ActionButton.onClick.RemoveListener(HandleLeaveClicked);
            confirmExitPopup.ConfirmButton.onClick.RemoveListener(HandleConfirmLeaveClicked);
            confirmExitPopup.CancelButton.onClick.RemoveListener(HandleConfirmStayClicked);
            winPopup.ContinueButton.onClick.RemoveListener(HandleContinueClicked);
            losePopup.RetryButton.onClick.RemoveListener(HandleRetryClicked);
            losePopup.HomeButton.onClick.RemoveListener(HandleLoseHomeClicked);
        }

        private void Update()
        {
            // The level starts on its first frame rather than on the board's ready event: the intro
            // drops the drawn blocks, and nothing orders this listener after the view's. By the first
            // Update every Start has run, so the board is built and drawn.
            if (current == null)
            {
                if (controller.Session == null) return;
                ChangeState(Intro);
            }

            if (Input.GetKeyDown(KeyCode.Escape)) current.OnBack();

            current.Tick(Time.unscaledDeltaTime);
        }

        public void ChangeState(LevelState next)
        {
            if (next == null) throw new ArgumentNullException(nameof(next));

            current?.Exit();
            current = next;
            current.Enter();
        }

        public void SetInputEnabled(bool enabled) => input.enabled = enabled;

        // Only a campaign level moves the campaign on; a debug level opened straight in this scene
        // pays out nothing and saves nothing.
        public int CompleteLevel()
        {
            GameSession session = controller.Session;
            int coins = coinsPerWin + coinsPerMoveLeft * Math.Max(session.MovesLeft, 0);

            if (controller.IsCampaign) PlayerProgress.CompleteLevel(coins);

            return coins;
        }

        public void RestartLevel() => controller.Restart();

        public void GoHome() => App.Instance.Transition.LoadScene(Scenes.Home);

        // One-based for display; zero for a debug level.
        public int LevelNumber => controller.IsCampaign ? controller.CampaignIndex + 1 : 0;

        private void HandleStatusChanged() => current?.OnStatusChanged(controller.Session.State);

        private void HandleTapRejected(int cell) => current?.OnTapRejected();

        private void HandleSettingsClicked() => current?.OnSettingsPressed();

        private void HandleStartPlayClicked() => Briefing.Play();
        private void HandleStartCloseClicked() => Briefing.Leave();
        private void HandleResumeClicked() => Paused.Resume();
        private void HandleLeaveClicked() => Paused.AskToLeave();
        private void HandleConfirmLeaveClicked() => ConfirmExit.Leave();
        private void HandleConfirmStayClicked() => ConfirmExit.Stay();
        private void HandleContinueClicked() => Won.Continue();
        private void HandleRetryClicked() => Lost.Retry();
        private void HandleLoseHomeClicked() => Lost.Leave();
    }
}
