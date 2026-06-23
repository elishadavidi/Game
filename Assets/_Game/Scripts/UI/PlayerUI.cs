using BecomingLegend.Actors;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BecomingLegend.UI
{
    public class PlayerUI : MonoBehaviour
    {
        [Header("Bars")]
        [SerializeField] private Image healthBar;
        [SerializeField] private Image mpBar;
        [SerializeField] private Image xpBar;

        [Header("Text")]
        [SerializeField] private TMP_Text levelText;

        [Header("Smoothing")]
        [SerializeField] private float smoothTime = 0.1f;

        private PlayerActor player;
        private float healthVelocity;
        private float mpVelocity;
        private float xpVelocity;

        private void Start()
        {
            TryResolvePlayer();
        }

        private void TryResolvePlayer()
        {
            if (player == null)
                player = PlayerActor.Instance;
        }

        private void Update()
        {
            TryResolvePlayer();
            if (player == null) return;

            UpdateBars();
            UpdateText();
        }

        private void UpdateBars()
        {
            if (healthBar != null)
                healthBar.fillAmount = Mathf.SmoothDamp(healthBar.fillAmount,
                    player.CurrentHealth / player.MaxHealth, ref healthVelocity, smoothTime);

            if (mpBar != null)
                mpBar.fillAmount = Mathf.SmoothDamp(mpBar.fillAmount,
                    player.CurrentMP / player.MaxMP, ref mpVelocity, smoothTime);

            if (xpBar != null)
            {
                float target = player.XPToNextLevel > 0
                    ? (float)player.CurrentXP / player.XPToNextLevel : 0f;
                xpBar.fillAmount = Mathf.SmoothDamp(xpBar.fillAmount,
                    target, ref xpVelocity, smoothTime * 1.5f);
            }
        }

        private void UpdateText()
        {
            if (levelText != null)
                levelText.text = $"Lv. {player.Level}";
        }
    }
}
