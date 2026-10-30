using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RataBlanca.UI
{
    /// <summary>
    /// Cortina a pantalla completa para las transiciones entre escenas:
    /// al cargar una escena se desvanece, y antes de cambiar de escena se
    /// vuelve a cubrir la pantalla.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ScreenFader : MonoBehaviour
    {
        public float fadeInDuration = 0.6f;
        public float fadeOutDuration = 0.4f;

        CanvasGroup _group;
        bool _busy;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 1f;
            _group.blocksRaycasts = true;
        }

        IEnumerator Start()
        {
            yield return UITween.Fade(_group, 0f, fadeInDuration);
            _group.blocksRaycasts = false;
        }

        public void FadeOutAndLoad(string sceneName)
        {
            if (_busy) return;
            _busy = true;
            StartCoroutine(FadeOutRoutine(sceneName));
        }

        IEnumerator FadeOutRoutine(string sceneName)
        {
            _group.blocksRaycasts = true;
            yield return UITween.Fade(_group, 1f, fadeOutDuration);
            SceneManager.LoadScene(sceneName);
        }
    }
}
