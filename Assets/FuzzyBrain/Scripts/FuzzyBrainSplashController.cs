using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace FuzzyBrain
{
    /// <summary>
    /// Displays the "Powered by FuzzyBrain" splash screen, then loads the next scene.
    /// Attach to the root GameObject in the FuzzyBrainSplash scene.
    /// Set nextSceneIndex to the build index of your first game scene (default 1).
    /// </summary>
    public class FuzzyBrainSplashController : MonoBehaviour
    {
        private const float DefaultHoldDuration = 2f;
        private const float DefaultFadeDuration = 0.8f;
        private const int   DefaultNextSceneIndex = 1;

        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float       holdDuration  = DefaultHoldDuration;
        [SerializeField] private float       fadeDuration  = DefaultFadeDuration;
        [SerializeField] private int         nextSceneIndex = DefaultNextSceneIndex;

        private void Start()
        {
            if (canvasGroup == null)
            {
                Debug.LogError("[FuzzyBrain] SplashController: CanvasGroup reference is missing.", this);
                return;
            }

            canvasGroup.alpha = 0f;
            StartCoroutine(PlaySplash());
        }

        private IEnumerator PlaySplash()
        {
            yield return Fade(0f, 1f, fadeDuration);
            yield return new WaitForSeconds(holdDuration);
            yield return Fade(1f, 0f, fadeDuration);
            LoadNextScene();
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            canvasGroup.alpha = from;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        /// <summary>Loads the target scene by build index. Logs an error if the index is out of range.</summary>
        private void LoadNextScene()
        {
            if (nextSceneIndex < 0 || nextSceneIndex >= SceneManager.sceneCountInBuildSettings)
            {
                Debug.LogError(
                    $"[FuzzyBrain] SplashController: nextSceneIndex ({nextSceneIndex}) is out of range. " +
                    $"Make sure your scenes are added to Build Settings.", this);
                return;
            }

            SceneManager.LoadScene(nextSceneIndex);
        }
    }
}
