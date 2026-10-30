using System;
using System.Collections.Generic;
using RataBlanca.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Navegación de la interfaz principal:
    ///  - clic en una opción de la barra lateral → abre esa sección;
    ///  - Escape o el botón "Volver" → cierra la sección (o la vista interna
    ///    que tenga abierta) y vuelve a la pantalla de inicio;
    ///  - "Salir del juego" → vuelve al menú principal.
    /// En WebGL a pantalla completa Escape saca de la pantalla completa, por
    /// eso el botón Volver siempre está visible cuando hay una sección abierta.
    /// </summary>
    public class NavigationManager : MonoBehaviour
    {
        public static NavigationManager Instance { get; private set; }

        [Header("Secciones y opciones de la barra")]
        public List<Section> sections = new List<Section>();
        public List<SidebarLink> links = new List<SidebarLink>();

        [Header("Inicio y botones")]
        [Tooltip("Lo que se ve cuando no hay ninguna sección abierta.")]
        public CanvasGroup homePanel;
        [Tooltip("Botón Volver (visible solo con una sección abierta).")]
        public CanvasGroup backButtonGroup;
        public Button backButton;
        public Button exitButton;
        public SidebarController sidebar;
        public ScreenFader fader;

        [Header("Animación")]
        public float fadeDuration = 0.25f;

        /// <summary>Se dispara al abrir una sección (el tutorial lo escucha).</summary>
        public event Action<SectionId> SectionOpened;
        /// <summary>Se dispara al volver a la pantalla de inicio.</summary>
        public event Action ReturnedHome;

        Section _current;
        Coroutine _homeFade;
        Coroutine _backFade;

        public Section Current => _current;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void Start()
        {
            foreach (var s in sections)
                if (s != null) s.HideImmediate();

            foreach (var link in links)
            {
                if (link == null) continue;
                SectionId target = link.target;
                link.Button.onClick.AddListener(() => Open(target));
            }

            if (backButton != null) backButton.onClick.AddListener(GoBack);
            if (exitButton != null) exitButton.onClick.AddListener(ExitToMenu);

            SetGroupVisible(homePanel, true, false);
            SetGroupVisible(backButtonGroup, false, false);
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                GoBack();
        }

        public void Open(SectionId id)
        {
            Section target = sections.Find(s => s != null && s.id == id);
            if (target == null)
            {
                Debug.LogWarning($"[Navegación] No hay una sección con id {id}.");
                return;
            }
            if (target == _current) return;

            if (_current != null) _current.Close();
            _current = target;
            _current.Open();

            foreach (var link in links)
                if (link != null && link.hover != null) link.hover.Selected = link.target == id;

            FadeHome(false);
            FadeBack(true);
            SectionOpened?.Invoke(id);
        }

        /// <summary>Escape o botón Volver.</summary>
        public void GoBack()
        {
            if (_current == null) return;
            if (_current.HandleBack()) return;   // la sección cerró algo interno
            CloseCurrent();
        }

        public void CloseCurrent()
        {
            if (_current == null) return;
            _current.Close();
            _current = null;

            foreach (var link in links)
                if (link != null && link.hover != null) link.hover.Selected = false;

            FadeHome(true);
            FadeBack(false);
            ReturnedHome?.Invoke();
        }

        public void ExitToMenu()
        {
            if (fader != null) fader.FadeOutAndLoad(SceneNames.Menu);
            else UnityEngine.SceneManagement.SceneManager.LoadScene(SceneNames.Menu);
        }

        // ------------------------------------------------------------------
        void FadeHome(bool visible)
        {
            if (homePanel == null) return;
            if (_homeFade != null) StopCoroutine(_homeFade);
            _homeFade = StartCoroutine(FadeGroup(homePanel, visible));
        }

        void FadeBack(bool visible)
        {
            if (backButtonGroup == null) return;
            if (_backFade != null) StopCoroutine(_backFade);
            _backFade = StartCoroutine(FadeGroup(backButtonGroup, visible));
        }

        System.Collections.IEnumerator FadeGroup(CanvasGroup group, bool visible)
        {
            group.blocksRaycasts = visible;
            group.interactable = visible;
            yield return UITween.Fade(group, visible ? 1f : 0f, fadeDuration);
        }

        static void SetGroupVisible(CanvasGroup group, bool visible, bool animate)
        {
            if (group == null) return;
            group.alpha = visible ? 1f : 0f;
            group.blocksRaycasts = visible;
            group.interactable = visible;
        }
    }
}
