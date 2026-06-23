using TMPro;
using UnityEngine;

namespace BecomingLegend.UI
{
    [RequireComponent(typeof(TextMeshPro))]
    public class DamageNumber : MonoBehaviour
    {
        private TextMeshPro label;
        private Vector3 velocity;
        private float fadeTimer;
        private float fadeDuration;
        private System.Action<DamageNumber> onFinished;

        private void Awake()
        {
            label = GetComponent<TextMeshPro>();
            if (label == null)
            {
                label = gameObject.AddComponent<TextMeshPro>();
            }
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 3.5f;
        }

        public void Show(string text, Color color, Vector3 worldPos, float duration, System.Action<DamageNumber> finished)
        {
            gameObject.SetActive(true);
            transform.position = worldPos;
            label.text = text;
            label.color = color;
            fadeDuration = duration;
            fadeTimer = duration;
            velocity = new Vector3(Random.Range(-0.3f, 0.3f), 1.5f, 0);
            onFinished = finished;
        }

        private void Update()
        {
            if (label == null) return;

            transform.position += velocity * Time.deltaTime;
            velocity.y -= 2f * Time.deltaTime;

            fadeTimer -= Time.deltaTime;
            float t = fadeTimer / fadeDuration;
            Color c = label.color;
            label.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(t));
            transform.localScale = Vector3.one * (1f + (1f - t) * 0.15f);

            if (fadeTimer <= 0f)
            {
                gameObject.SetActive(false);
                onFinished?.Invoke(this);
            }
        }
    }
}
