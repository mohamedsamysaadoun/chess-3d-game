// Reconstructed UI animation scripts
// Original: UITabAnimate (TypeDefIndex: 6200), UIBobAnim (6198), UIPingPongAnim (6199)

using UnityEngine;

namespace Chess3D.UI
{
    /// <summary>
    /// Tab animation - animates UI tab transitions.
    /// </summary>
    public class UITabAnimate : MonoBehaviour
    {
        public RectTransform tabPanel;
        public float animationSpeed = 5f;
        public Vector2 hiddenPosition;
        public Vector2 visiblePosition;

        private bool isVisible = false;
        private Vector2 targetPosition;

        void Update()
        {
            if (tabPanel == null) return;

            tabPanel.anchoredPosition = Vector2.Lerp(
                tabPanel.anchoredPosition,
                targetPosition,
                Time.deltaTime * animationSpeed
            );
        }

        public void ToggleTab()
        {
            isVisible = !isVisible;
            targetPosition = isVisible ? visiblePosition : hiddenPosition;
        }

        public void ShowTab()
        {
            isVisible = true;
            targetPosition = visiblePosition;
        }

        public void HideTab()
        {
            isVisible = false;
            targetPosition = hiddenPosition;
        }
    }

    /// <summary>
    /// Bobbing animation - makes UI elements gently bob up and down.
    /// </summary>
    public class UIBobAnim : MonoBehaviour
    {
        public float bobHeight = 5f;
        public float bobSpeed = 2f;
        private Vector3 startPos;

        void Start()
        {
            startPos = transform.localPosition;
        }

        void Update()
        {
            float yOffset = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = startPos + new Vector3(0, yOffset, 0);
        }
    }

    /// <summary>
    /// Ping-pong animation - moves element back and forth between two positions.
    /// </summary>
    public class UIPingPongAnim : MonoBehaviour
    {
        public Vector3 startPos;
        public Vector3 endPos;
        public float speed = 1f;

        void Update()
        {
            float t = Mathf.PingPong(Time.time * speed, 1f);
            transform.localPosition = Vector3.Lerp(startPos, endPos, t);
        }
    }

    /// <summary>
    /// Blinking text animation.
    /// </summary>
    public class UIBlinkingText : MonoBehaviour
    {
        public UnityEngine.UI.Text text;
        public float blinkSpeed = 1f;

        void Update()
        {
            if (text == null) return;
            float alpha = (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f;
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        public void ShowBlinkingText(string val)
        {
            if (text != null)
            {
                text.text = val;
                gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// Toggle that activates on enable.
    /// </summary>
    public class UIToggleOnEnabled : MonoBehaviour
    {
        public GameObject[] objectsToToggle;

        void OnEnable()
        {
            foreach (var obj in objectsToToggle)
            {
                if (obj != null)
                    obj.SetActive(!obj.activeSelf);
            }
        }
    }
}
