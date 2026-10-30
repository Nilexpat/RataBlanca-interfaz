using System.Collections;
using RataBlanca.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Menú principal: Jugar / Nuevo juego / Salir.
    ///  - Jugar: entra a la bitácora. Si el tutorial nunca se ha visto, lo muestra.
    ///  - Nuevo juego: borra el progreso y entra con el tutorial.
    ///  - Salir: en WebGL (y en el editor, para poder grabarlo) muestra un
    ///    mensaje de cierre, porque Application.Quit() no funciona en el
    ///    navegador. En una versión de escritorio sí cierra la app.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public Button playButton;
        public Button newGameButton;
        public Button exitButton;

        [Header("Mensaje de cierre")]
        public CanvasGroup exitPanel;
        public Button exitPanelBackButton;

        [Header("Transición")]
        public ScreenFader fader;
        [Tooltip("Elementos que aparecen en secuencia al abrir el menú (título, botones...).")]
        public CanvasGroup[] introSequence;
        public float introDelay = 0.25f;
        public float introStep = 0.12f;
        public float introDuration = 0.5f;

        void Start()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
            if (exitButton != null) exitButton.onClick.AddListener(Exit);
            if (exitPanelBackButton != null) exitPanelBackButton.onClick.AddListener(HideExitPanel);

            if (exitPanel != null)
            {
                exitPanel.alpha = 0f;
                exitPanel.blocksRaycasts = false;
                exitPanel.interactable = false;
            }

            if (introSequence != null && introSequence.Length > 0)
                StartCoroutine(PlayIntro());
        }

        void Update()
        {
            if (exitPanel != null && exitPanel.blocksRaycasts &&
                Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                HideExitPanel();
        }

        IEnumerator PlayIntro()
        {
            foreach (var g in introSequence) if (g != null) g.alpha = 0f;
            yield return new WaitForSecondsRealtime(introDelay);
            foreach (var g in introSequence)
            {
                if (g == null) continue;
                StartCoroutine(UITween.Fade(g, 1f, introDuration));
                yield return new WaitForSecondsRealtime(introStep);
            }
        }

        public void Play()
        {
            GameProgress.ShowTutorialOnEnter = !GameProgress.TutorialSeen;
            Load(SceneNames.Game);
        }

        public void NewGame()
        {
            GameProgress.ResetProgress();
            GameProgress.ShowTutorialOnEnter = true;
            Load(SceneNames.Game);
        }

        public void Exit()
        {
#if UNITY_WEBGL || UNITY_EDITOR
            ShowExitPanel();
#else
            Application.Quit();
#endif
        }

        void Load(string scene)
        {
            if (fader != null) fader.FadeOutAndLoad(scene);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(scene);
        }

        void ShowExitPanel()
        {
            if (exitPanel == null) return;
            exitPanel.blocksRaycasts = true;
            exitPanel.interactable = true;
            StartCoroutine(UITween.Fade(exitPanel, 1f, 0.35f));
        }

        void HideExitPanel()
        {
            if (exitPanel == null) return;
            exitPanel.blocksRaycasts = false;
            exitPanel.interactable = false;
            StartCoroutine(UITween.Fade(exitPanel, 0f, 0.25f));
        }
    }
}
