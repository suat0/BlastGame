using System;
using TMPro;
using UnityEngine;

namespace BlastGame.Game.UI
{
    // The countdown under an event icon. There is no event behind it; the time left is the position
    // within a repeating period of real time, so it keeps counting down across sessions and restarts
    // when it runs out, the way a weekly event would.
    public sealed class EventTimer : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        [Tooltip("Length of one run of the event, in hours.")]
        [SerializeField] private float periodHours = 72f;

        [Tooltip("Shifts this event's cycle so two icons never show the same time.")]
        [SerializeField] private float offsetHours;

        private long lastShownMinute = -1;

        private void Update()
        {
            double periodSeconds = periodHours * 3600.0;
            double now = (DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds + offsetHours * 3600.0;
            double remaining = periodSeconds - now % periodSeconds;

            // The label shows minutes at the finest, so it is rewritten at most once a minute.
            long minute = (long)(remaining / 60.0);
            if (minute == lastShownMinute) return;
            lastShownMinute = minute;

            long days = minute / (24 * 60);
            long hours = minute / 60 % 24;
            long minutes = minute % 60;

            if (days > 0) label.SetText("{0:0}d {1:0}h", days, hours);
            else label.SetText("{0:0}h {1:0}m", hours, minutes);
        }
    }
}
