using PrimeTween;
using TMPro;
using UnityEngine;

namespace BlastGame.Game.UI
{
    // The coin pill at the top of the home screen. After a win the coins earned fly into it from the
    // middle of the screen and the count climbs as each one lands - the moment the reward is paid,
    // rather than a number that was simply already higher.
    public sealed class CoinCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;
        [SerializeField] private RectTransform icon;

        [Tooltip("Preplaced and hidden; the flight reuses them. More coins than this earned still fly " +
                 "as this many - the count is split between them.")]
        [SerializeField] private RectTransform[] flyers;

        [SerializeField] private float flightDuration = 0.7f;
        [SerializeField] private float stagger = 0.07f;

        private int shown;
        private int target;
        private int perCoin;
        private int landed;
        private int flying;

        private void Awake()
        {
            foreach (RectTransform flyer in flyers) flyer.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            foreach (RectTransform flyer in flyers) Tween.StopAll(flyer);
            Tween.StopAll(icon);
        }

        // Shows the saved total, minus a reward still to be flown in.
        public void Begin(int total, int pendingReward)
        {
            target = total;
            shown = total - pendingReward;
            label.SetText("{0:0}", shown);

            if (pendingReward > 0) Fly(pendingReward);
        }

        private void Fly(int reward)
        {
            flying = Mathf.Min(flyers.Length, reward);
            perCoin = reward / flying;
            landed = 0;

            Vector3 start = ((RectTransform)transform.root).position;   // the middle of the screen
            Vector3 end = icon.position;

            for (int i = 0; i < flying; i++)
            {
                RectTransform flyer = flyers[i];
                flyer.gameObject.SetActive(true);
                flyer.position = start + (Vector3)(Random.insideUnitCircle * 120f);
                flyer.localScale = Vector3.one;

                float delay = 0.35f + i * stagger;

                // X eases in and Y eases out, so the straight line between them bends into an arc.
                Tween.PositionX(flyer, end.x, flightDuration, Ease.InQuad, startDelay: delay);
                Tween.PositionY(flyer, end.y, flightDuration, Ease.OutQuad, startDelay: delay)
                     .OnComplete(this, self => self.Land());
                Tween.Scale(flyer, 0.6f, flightDuration, Ease.InQuad, startDelay: delay);
            }
        }

        // Each landing adds its share; the last one adds whatever the split left over.
        private void Land()
        {
            flyers[landed].gameObject.SetActive(false);
            landed++;

            shown = landed == flying ? target : shown + perCoin;
            label.SetText("{0:0}", shown);

            Tween.StopAll(icon);
            icon.localScale = Vector3.one;
            Tween.PunchScale(icon, new Vector3(0.35f, 0.35f, 0f), 0.2f, frequency: 6f);
        }
    }
}
