using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BlastGame.Game.UI
{
    // A button for a feature this build does not have: the shop, the team, an event. It answers a tap
    // the way Match Villains answers one for a feature not yet unlocked - a wiggle and a note saying
    // when it opens - so nothing on the home screen is a dead tap.
    [RequireComponent(typeof(Button))]
    public sealed class LockedFeature : MonoBehaviour
    {
        [SerializeField] private string featureName = "Feature";
        [SerializeField] private int unlockLevel = 10;
        [SerializeField] private Toast toast;

        private Button button;
        private string message;

        private void Awake()
        {
            button = GetComponent<Button>();

            // Built once: the message never changes, and the home screen is not a place where a few
            // bytes matter, but there is no reason to build it on every tap either.
            message = $"{featureName} unlocks at Level {unlockLevel}";
        }

        private void OnEnable() => button.onClick.AddListener(HandleClicked);

        private void OnDisable() => button.onClick.RemoveListener(HandleClicked);

        private void OnDestroy() => Tween.StopAll(transform);

        private void HandleClicked()
        {
            Tween.StopAll(transform);
            transform.localRotation = Quaternion.identity;

            Tween.ShakeLocalRotation(transform, new Vector3(0f, 0f, 12f), 0.35f, frequency: 14f, useUnscaledTime: true);
            toast.Show(message);
            Sfx.Play(SfxId.Locked);
        }
    }
}
