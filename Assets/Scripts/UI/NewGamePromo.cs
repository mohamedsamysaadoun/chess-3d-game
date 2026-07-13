// Reconstructed NewGamePromo - shows promotional content for new game modes
// Original: NewGamePromo (TypeDefIndex: 6224)

using UnityEngine;
using UnityEngine.UI;

namespace Chess3D.UI
{
    /// <summary>
    /// Shows promotional content for new game modes or features.
    /// </summary>
    public class NewGamePromo : MonoBehaviour
    {
        public GameObject promoPanel;
        public Image promoImage;
        public Button closeButton;
        public Button downloadButton;

        public string promoUrl = "market://details?id=com.eivaagames.RealChess3DFree";

        void Start()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(ClosePromo);
            if (downloadButton != null)
                downloadButton.onClick.AddListener(OpenStorePage);

            // Show promo only once
            if (PlayerPrefs.GetInt("promo_shown", 0) == 1)
            {
                gameObject.SetActive(false);
            }
        }

        public void ShowPromo()
        {
            if (promoPanel != null)
                promoPanel.SetActive(true);
            PlayerPrefs.SetInt("promo_shown", 1);
            PlayerPrefs.Save();
        }

        public void ClosePromo()
        {
            if (promoPanel != null)
                promoPanel.SetActive(false);
        }

        public void OpenStorePage()
        {
            Application.OpenURL(promoUrl);
        }
    }
}
