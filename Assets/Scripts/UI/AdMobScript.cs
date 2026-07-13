// Reconstructed AdMobScript - handles ad display and management
// MODIFIED: All ad methods are patched to be no-ops (ads removed)

using UnityEngine;

namespace Chess3D.UI
{
    /// <summary>
    /// Ad management script.
    /// NOTE: In the original game, this class manages AdMob interstitial and rewarded ads.
    /// In this reconstruction, all ad functions are stubbed out to remove ads.
    /// </summary>
    public class AdMobScript : MonoBehaviour
    {
        public static AdMobScript Instance { get; private set; }

        // IAP product ID for removing ads (from decompiled code)
        public const string IAP_ID_REMOVE_ADS = "real_chess_3d_remove_ads";

        // URLs from decompiled code
        public const string URL_GAME = "market://details?id=com.eivaagames.RealChess3DFree";
        public const string URL_EG_WEBSITE = "https://www.eivaagames.com";
        public const string URL_PRIVACY_POLICY = "https://www.eivaagames.com/company/privacy-policy";
        public const string URL_TWITTER = "https://twitter.com/eivaagames";
        public const string URL_YOUTUBE = "https://www.youtube.com/eivaagames";
        public const string URL_FB_PAGE = "fb://page/269426263878";
        public const string URL_GMG_STORE = "market://search?q=pub:EivaaGames";

        // High score submission URL (vulnerable - no auth!)
        public const string PHP_SUBMIT_URL = "https://www.eivaagames.com/games/highscores/submit-score.php";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize AdMob. Original RVA: 0xEC9BCC
        /// MODIFIED: Patched to do nothing (ad removal).
        /// </summary>
        private void AdMobInit()
        {
            // Original code:
            //   MobileAds.Initialize(callback);
            //   RequestConfiguration.Builder()...
            //   AMReqInters();
            //
            // Patched: Just return immediately
            Debug.Log("[AdMobScript] AdMobInit skipped (ads removed)");
        }

        /// <summary>
        /// Request an interstitial ad. Original RVA: 0xEC9C54
        /// MODIFIED: Patched to do nothing.
        /// </summary>
        private void AMReqInters()
        {
            // Original code:
            //   InterstitialAd.Load(adUnitId, request, callback);
            //
            // Patched: Just return
            Debug.Log("[AdMobScript] AMReqInters skipped (ads removed)");
        }

        /// <summary>
        /// Called when a game completes. Original RVA: 0xEC15CC
        /// MODIFIED: Patched to do nothing (no end-game ad).
        /// </summary>
        private void AMOnGameCompleteEv()
        {
            // Original code:
            //   if (interstitial.IsLoaded()) interstitial.Show();
            //
            // Patched: Just return
            Debug.Log("[AdMobScript] AMOnGameCompleteEv skipped (ads removed)");
        }

        /// <summary>
        /// Called when a game starts. Original RVA: 0xEB6CB0
        /// MODIFIED: Patched to do nothing (no start-game ad).
        /// </summary>
        private void AdMobOnGameStart()
        {
            // Original code:
            //   if (interstitial.IsLoaded()) interstitial.Show();
            //
            // Patched: Just return
            Debug.Log("[AdMobScript] AdMobOnGameStart skipped (ads removed)");
        }
    }
}
