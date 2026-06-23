using BecomingLegend.Actors;
using UnityEngine;
using UnityEngine.UI;

namespace BecomingLegend.UI
{
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 offset = new Vector2(0, 0.8f);
        [SerializeField] private Vector2 barSize = new Vector2(0.5f, 0.06f);
        [SerializeField] private Color barColor = Color.red;

        private Image fill;
        private Actor actor;

        private void Awake()
        {
            actor = GetComponent<Actor>();
            CreateBar();
        }

        private void CreateBar()
        {
            var go = new GameObject("HealthBar", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);

            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = barSize;
            rect.anchoredPosition3D = offset;

            var bg = new GameObject("BG", typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgImage = bg.GetComponent<Image>();
            bgImage.color = Color.gray;
            bg.GetComponent<RectTransform>().sizeDelta = barSize;

            var fillGO = new GameObject("Fill", typeof(Image));
            fillGO.transform.SetParent(go.transform, false);
            fill = fillGO.GetComponent<Image>();
            fill.color = barColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.GetComponent<RectTransform>().sizeDelta = barSize;
        }

        private void LateUpdate()
        {
            if (fill == null || actor == null) return;
            bool visible = !actor.IsDead;
            if (fill.gameObject.activeSelf != visible)
                fill.gameObject.SetActive(visible);
            if (visible)
                fill.fillAmount = actor.CurrentHealth / actor.MaxHealth;
        }
    }
}
