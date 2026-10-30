using System;
using System.Collections.Generic;
using System.IO;
using RataBlanca.Audio;
using RataBlanca.Core;
using RataBlanca.Graficos;
using RataBlanca.Objeto;
using RataBlanca.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static RataBlanca.EditorTools.UIFactory;

namespace RataBlanca.EditorTools
{
    /// <summary>
    /// Menú "Rata Blanca" en la barra superior de Unity.
    /// Construye las escenas MenuPrincipal y Juego a partir de las medidas de
    /// Figma, configura los sprites y deja el proyecto listo para WebGL.
    ///
    /// Las escenas se regeneran cada vez que lo usas: no edites a mano lo que
    /// el constructor crea, cambia este script o los datos (TemaUI, prefab del
    /// AudioManager), que NO se sobrescriben.
    /// </summary>
    public static class RataBlancaBuilder
    {
        // Rutas
        const string ScenesFolder = "Assets/Scenes";
        const string MenuScenePath = ScenesFolder + "/" + SceneNames.Menu + ".unity";
        const string GameScenePath = ScenesFolder + "/" + SceneNames.Game + ".unity";
        const string SpritesFolder = "Assets/UI/Sprites";
        const string ImagesFolder = "Assets/Imagenes";
        const string PrefabsFolder = "Assets/Prefabs";
        const string AudioPrefabPath = PrefabsFolder + "/AudioManager.prefab";
        const string DataFolder = "Assets/Data";
        const string ThemePath = DataFolder + "/TemaUI.asset";
        const string NoiseShaderPath = "Assets/Shaders/FractalNoise.shader";
        const string ObjectSpritesFolder = SpritesFolder + "/Objeto";
        const string ObjectDataPath = DataFolder + "/ObjetoTurbante.asset";
        const string ChartSpritesFolder = SpritesFolder + "/Graficos";
        const string ChartsFolder = DataFolder + "/Graficos";
        const float BlockWidth = 584f;

        // Composición del turbante (Referencias/Exposion.png): 1849×1407.
        // Posiciones medidas sobre esa imagen (esquina superior izquierda).
        static readonly Vector2 CompositionSize = new Vector2(1849f, 1407f);
        static readonly Rect TurbanteRect = new Rect(335f, 131f, 1186f, 982f);
        static readonly Rect RubiRect = new Rect(812f, 469f, 178f, 221f);
        static readonly Rect TelaRect = new Rect(202f, 993f, 598f, 285f);
        static readonly Rect RubiZoomRect = new Rect(1189f, 919f, 352f, 349f);
        const float TurbanteGlowPad = 70f;
        const float RubiGlowPad = 60f;

        // Medidas de Figma
        const float RefWidth = 1440f;
        const float RefHeight = 1024f;
        const float SidebarWidth = 428f;

        // Recursos cargados durante la construcción
        static UITheme _theme;
        static Sprite _shadowOuter, _shadowInner, _chevron, _circle, _rounded;
        static Shader _noiseShader;
        static GameObject _audioPrefab;

        // ==================================================================
        // Menús
        // ==================================================================
        [MenuItem("Rata Blanca/Construir todo", priority = 0)]
        public static void BuildAll()
        {
            if (!CheckTextMeshPro()) return;
            if (!EditorUtility.DisplayDialog("Rata Blanca",
                    "Se van a (re)generar las escenas MenuPrincipal y Juego.\n\n" +
                    "Lo que hayas cambiado a mano dentro de esas escenas se pierde. " +
                    "El tema (TemaUI) y el prefab del AudioManager se conservan.",
                    "Construir", "Cancelar"))
                return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            bool ok = Step("la configuración del proyecto", ConfigureProject)
                      & Step("la escena MenuPrincipal", BuildMenuScene)
                      & Step("la escena Juego", BuildGameScene);
            SetBuildScenes();

            if (File.Exists(MenuScenePath)) EditorSceneManager.OpenScene(MenuScenePath);
            if (ok) Debug.Log("[Rata Blanca] Listo: escenas construidas. Abre MenuPrincipal y dale Play.");
        }

        [MenuItem("Rata Blanca/Construir solo la escena Juego", priority = 1)]
        public static void BuildGameOnly()
        {
            if (!CheckTextMeshPro()) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Las imágenes nuevas tienen que quedar como Sprite antes de construir.
            Step("la configuración de los sprites", ConfigureSprites);
            Step("la escena Juego", BuildGameScene);
            SetBuildScenes();
        }

        /// <summary>
        /// Ejecuta un paso del constructor. Si falla, muestra el error en una
        /// ventana (y en la consola) en vez de cortar todo en silencio.
        /// </summary>
        static bool Step(string what, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                string stack = e.StackTrace ?? "";
                var lines = stack.Split('\n');
                string shortStack = string.Join("\n", lines, 0, Mathf.Min(6, lines.Length));
                EditorUtility.DisplayDialog("Rata Blanca — error",
                    $"Falló {what}.\n\n{e.GetType().Name}: {e.Message}\n\n{shortStack}\n\n" +
                    "Mándale una captura de esta ventana a Claude.", "OK");
                return false;
            }
        }

        [MenuItem("Rata Blanca/Configurar proyecto (WebGL y sprites)", priority = 20)]
        public static void ConfigureProject()
        {
            EnsureFolder(ScenesFolder);
            EnsureFolder(PrefabsFolder);
            EnsureFolder(DataFolder);

            ConfigureWebGL();
            ConfigureSprites();
            AssetDatabase.SaveAssets();
            Debug.Log("[Rata Blanca] Proyecto configurado: WebGL (Gzip + Decompression Fallback) y sprites.");
        }

        // ==================================================================
        // Configuración del proyecto
        // ==================================================================
        static void ConfigureWebGL()
        {
            // GitHub Pages no envía los encabezados de compresión: con
            // "Decompression Fallback" el navegador descomprime por su cuenta.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.productName = "Rata Blanca";
            PlayerSettings.defaultWebScreenWidth = (int)RefWidth;
            PlayerSettings.defaultWebScreenHeight = (int)RefHeight;
            PlayerSettings.runInBackground = true;
        }

        static void ConfigureSprites()
        {
            AssetDatabase.Refresh();

            // Sprites generados para la interfaz
            SetSprite(SpritesFolder + "/SombraExterna.png", Vector4.zero);
            SetSprite(SpritesFolder + "/SombraInterna.png", new Vector4(96, 96, 96, 96));
            SetSprite(SpritesFolder + "/Chevron.png", Vector4.zero);
            SetSprite(SpritesFolder + "/Circulo.png", Vector4.zero);
            SetSprite(SpritesFolder + "/Redondeado16.png", new Vector4(16, 16, 16, 16));
            SetSprite(SpritesFolder + "/Brillo.png", Vector4.zero);
            SetSprite(ObjectSpritesFolder + "/BrilloTurbante.png", Vector4.zero);
            SetSprite(ObjectSpritesFolder + "/BrilloRubi.png", Vector4.zero);
            SetSprite(ObjectSpritesFolder + "/Anillo.png", Vector4.zero);
            foreach (var cell in new[] { "CeldaTitulo", "CeldaIcono", "CuerpoBloque", "BarraInferior" })
                SetSprite(ChartSpritesFolder + "/" + cell + ".png", new Vector4(20, 20, 20, 20));
            SetSprite(ChartSpritesFolder + "/Tarjeta.png", new Vector4(10, 10, 10, 10));
            foreach (var ic in new[] { "IconoTorta", "IconoBarrasH", "IconoBarrasV", "IconoLineas" })
                SetSprite(ChartSpritesFolder + "/" + ic + ".png", Vector4.zero);

            // Imágenes del turbante: Sprite (2D and UI) con transparencia.
            // "Read/Write" activado para que el clic solo cuente sobre la
            // parte visible de la imagen (no en el hueco del turbante).
            if (AssetDatabase.IsValidFolder(ImagesFolder))
            {
                foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ImagesFolder }))
                    SetSprite(AssetDatabase.GUIDToAssetPath(guid), Vector4.zero, true);
            }
        }

        static void SetSprite(string path, Vector4 border, bool readable = false)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            bool changed = importer.textureType != TextureImporterType.Sprite
                           || importer.spriteBorder != border
                           || importer.mipmapEnabled
                           || !importer.alphaIsTransparency
                           || importer.isReadable != readable;
            if (!changed) return;

            importer.isReadable = readable;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.spriteBorder = border;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        static bool CheckTextMeshPro()
        {
            if (TMP_Settings.instance != null) return true;
            EditorUtility.DisplayDialog("Rata Blanca",
                "Falta importar los recursos esenciales de TextMeshPro.\n\n" +
                "Ve a Window > TextMeshPro > Import TMP Essential Resources, " +
                "dale Import y luego vuelve a usar Rata Blanca > Construir todo.",
                "Entendido");
            return false;
        }

        // ==================================================================
        // Recursos compartidos
        // ==================================================================
        static void LoadResources()
        {
            _theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (_theme == null)
            {
                _theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(_theme, ThemePath);
            }

            DefaultFont = TMP_Settings.defaultFontAsset;

            _shadowOuter = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + "/SombraExterna.png");
            _shadowInner = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + "/SombraInterna.png");
            _chevron = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + "/Chevron.png");
            _circle = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + "/Circulo.png");
            _rounded = AssetDatabase.LoadAssetAtPath<Sprite>(SpritesFolder + "/Redondeado16.png");
            _noiseShader = AssetDatabase.LoadAssetAtPath<Shader>(NoiseShaderPath);

            _audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AudioPrefabPath);
            if (_audioPrefab == null)
            {
                var go = new GameObject("AudioManager");
                go.AddComponent<AudioManager>();
                _audioPrefab = PrefabUtility.SaveAsPrefabAsset(go, AudioPrefabPath);
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        static TMP_FontAsset TitleFont => _theme.titleFont;
        static TMP_FontAsset BodyFont => _theme.bodyFont;

        // ==================================================================
        // Partes comunes de las dos escenas
        // ==================================================================
        static Canvas CreateSceneBase(string canvasName)
        {
            // Cámara (URP necesita al menos una para mostrar la pantalla)
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = _theme.background;
            cam.orthographic = true;
            camGo.AddComponent<AudioListener>();

            // EventSystem con el Input System nuevo
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            var module = esGo.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();

            // AudioManager (prefab compartido)
            PrefabUtility.InstantiatePrefab(_audioPrefab);

            // Canvas adaptable: escala con la ventana usando la resolución de Figma
            var canvasGo = new GameObject(canvasName, typeof(RectTransform));
            canvasGo.layer = LayerMask.NameToLayer("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Fondo animado (shader de Figma)
            var bg = Stretch(MakeRect("Fondo", canvasGo.transform));
            var raw = bg.gameObject.AddComponent<RawImage>();
            raw.color = _theme.background;
            raw.raycastTarget = false;
            var noise = bg.gameObject.AddComponent<NoiseBackground>();
            noise.shader = _noiseShader;
            noise.referenceSize = new Vector2(RefWidth, RefHeight);
            noise.baseColor = _theme.background;

            return canvas;
        }

        static ScreenFader CreateFader(Transform canvas)
        {
            var rt = Stretch(MakeRect("Fundido", canvas));
            MakeImage(rt, _theme.fade, null, true);
            MakeGroup(rt.gameObject);
            return rt.gameObject.AddComponent<ScreenFader>();
        }

        static void SaveScene(UnityEngine.SceneManagement.Scene scene, string path)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, path);
        }

        // ==================================================================
        // Escena: Menú principal (frame "Desktop - 5")
        // ==================================================================
        static void BuildMenuScene()
        {
            LoadResources();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = CreateSceneBase("Interfaz");
            var root = canvas.transform;

            var intro = new List<CanvasGroup>();

            // Título "RATA BLANCA" — Kiwi Maru 96, negro. En Figma: x 355, y 358, 730×139
            var titleRt = TopCenter(MakeRect("Titulo", root), 0f, 358f, 1000f, 139f);
            MakeText(titleRt, "RATA BLANCA", TitleFont, 96f, _theme.titleText, TextAlignmentOptions.Center);
            intro.Add(MakeGroup(titleRt.gameObject));

            // Opciones (Inter 32). Figma: Jugar y=600, Salir y=675.
            // "Nuevo juego" no está en Figma: va entre las dos y Salir baja 75 px.
            Button play = MenuButton(root, "Jugar", 600f, _theme.menuPlay, intro);
            Button newGame = MenuButton(root, "Nuevo juego", 675f, _theme.menuItem, intro);
            Button exit = MenuButton(root, "Salir", 750f, _theme.menuItem, intro);

            // Mensaje de cierre (WebGL no puede cerrar la pestaña)
            var exitPanel = Stretch(MakeRect("MensajeSalida", root));
            MakeImage(exitPanel, new Color(1f, 1f, 1f, 0.94f), null, true);
            var exitGroup = MakeGroup(exitPanel.gameObject);
            var thanks = Center(MakeRect("Gracias", exitPanel), 0f, 60f, 1100f, 80f);
            MakeText(thanks, "Gracias por visitar la bitácora.", TitleFont, 48f, _theme.menuItemActive, TextAlignmentOptions.Center);
            var hint = Center(MakeRect("Indicacion", exitPanel), 0f, -10f, 1100f, 40f);
            MakeText(hint, "Ya puedes cerrar esta pestaña.", BodyFont, 26f, _theme.menuItem, TextAlignmentOptions.Center);
            var backRt = Center(MakeRect("BotonVolver", exitPanel), 0f, -100f, 300f, 56f);
            var exitBack = TextButton(backRt, "Volver al menú", BodyFont, 30f, _theme.menuItem, _theme.menuItemActive,
                _theme.menuItemActive, TextAlignmentOptions.Center, 0f, 0f, out _);

            var fader = CreateFader(root);

            var menu = canvas.gameObject.AddComponent<MainMenu>();
            menu.playButton = play;
            menu.newGameButton = newGame;
            menu.exitButton = exit;
            menu.exitPanel = exitGroup;
            menu.exitPanelBackButton = exitBack;
            menu.fader = fader;
            menu.introSequence = intro.ToArray();

            SaveScene(scene, MenuScenePath);
        }

        static Button MenuButton(Transform root, string label, float figmaY, Color normal, List<CanvasGroup> intro)
        {
            // Rect de 360×56 centrado; el texto (alto 39 en Figma) queda centrado en él.
            var rt = TopCenter(MakeRect("Boton" + label.Replace(" ", ""), root), 0f, figmaY - 8.5f, 360f, 56f);
            var button = TextButton(rt, label, BodyFont, 32f, normal, _theme.menuItemActive, _theme.menuItemActive,
                TextAlignmentOptions.Center, 0f, 0f, out _);
            intro.Add(MakeGroup(rt.gameObject));
            return button;
        }

        // ==================================================================
        // Escena: Juego (frames "Desktop - 2/4/6/7/11")
        // ==================================================================
        static void BuildGameScene()
        {
            LoadResources();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var canvas = CreateSceneBase("Interfaz");
            var root = canvas.transform;

            // ---------------- Área de contenido ----------------
            // Estirada a toda la pantalla; su borde izquierdo lo mueve la barra.
            var content = Stretch(MakeRect("Contenido", root), SidebarWidth, 0f, 0f, 0f);

            // Pantalla de inicio (sin sección abierta)
            var home = Stretch(MakeRect("Inicio", content));
            var homeGroup = MakeGroup(home.gameObject);
            var homeTitle = Center(MakeRect("Titulo", home), 0f, 30f, 900f, 90f);
            MakeText(homeTitle, "Bitácora del laboratorio", TitleFont, 56f, _theme.darkText, TextAlignmentOptions.Center);
            var homeHint = Center(MakeRect("Indicacion", home), 0f, -40f, 900f, 40f);
            MakeText(homeHint, "Elige una sección en la barra lateral.", BodyFont, 26f, _theme.menuItem, TextAlignmentOptions.Center);

            // Secciones
            var sections = new List<Section>
            {
                PlaceholderSection(content, SectionId.Mapa, "Mapa", "El mapa de Rat City llegará pronto."),
                BuildMisteriosSection(content),
                PlaceholderSection(content, SectionId.Personajes, "Personajes", "Aquí irá el carrusel de personajes."),
                BuildObjectSection(content),
                SettingsSection(content)
            };

            // Botón Volver (arriba a la derecha del contenido)
            var backRt = Anchor(MakeRect("BotonVolver", content), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-48f, -40f), new Vector2(190f, 56f));
            var backGroup = MakeGroup(backRt.gameObject);
            var backButton = TextButton(backRt, "Volver", BodyFont, 30f, _theme.menuItem, _theme.menuItemActive,
                _theme.menuItemActive, TextAlignmentOptions.MidlineLeft, 52f, -6f, out var backHover);
            var backIcon = Anchor(MakeRect("Flecha", backHover.label.transform), new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-30f, 0f), new Vector2(24f, 15f));
            MakeImage(backIcon, _theme.menuItem, _chevron);
            backIcon.localEulerAngles = new Vector3(0f, 0f, -90f);

            // ---------------- Barra lateral ----------------
            var sidebar = MakeRect("BarraLateral", root);
            sidebar.anchorMin = new Vector2(0f, 0f);
            sidebar.anchorMax = new Vector2(0f, 1f);
            sidebar.pivot = new Vector2(0f, 0.5f);
            sidebar.sizeDelta = new Vector2(SidebarWidth, 0f);
            sidebar.anchoredPosition = Vector2.zero;

            // Sombra externa: x 9, desenfoque 49.8, negro 25 %
            var outer = MakeRect("SombraExterna", sidebar);
            outer.anchorMin = new Vector2(1f, 0f);
            outer.anchorMax = new Vector2(1f, 1f);
            outer.pivot = new Vector2(0f, 0.5f);
            outer.sizeDelta = new Vector2(96f, 0f);
            outer.anchoredPosition = Vector2.zero;
            MakeImage(outer, Color.white, _shadowOuter);

            // Vidrio: rgba(84, 84, 84, 0.33)
            MakeImage(Stretch(MakeRect("Vidrio", sidebar)), _theme.sidebarGlass, null, true);

            // Sombra interna: desenfoque 44, negro 25 %
            MakeImage(Stretch(MakeRect("SombraInterna", sidebar)), Color.white, _shadowInner);

            // Opciones (Inter 32, x 74). Figma: Mapa 196, Misterios 270,
            // Personajes 344, Objeto 418, Configuración 492.
            var links = new List<SidebarLink>
            {
                SidebarOption(sidebar, "Mapa", 196f, SectionId.Mapa),
                SidebarOption(sidebar, "Misterios", 270f, SectionId.Misterios),
                SidebarOption(sidebar, "Personajes", 344f, SectionId.Personajes),
                SidebarOption(sidebar, "Objeto", 418f, SectionId.Objeto),
                SidebarOption(sidebar, "Configuración", 492f, SectionId.Configuracion)
            };

            // "Salir del Juego" abajo (Figma y 951 → 34 px desde abajo)
            var exitRt = Anchor(MakeRect("BotonSalir", sidebar), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 25.5f), new Vector2(330f, 56f));
            var exitButton = TextButton(exitRt, "Salir del Juego", BodyFont, 32f, _theme.menuItem, _theme.menuItemActive,
                _theme.menuItemActive, TextAlignmentOptions.Center, 0f, 0f, out _);

            // Pestaña para expandir/contraer la barra (sobresale a la derecha)
            var tab = Anchor(MakeRect("BotonBarra", sidebar), new Vector2(1f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, -32f), new Vector2(44f, 64f));
            var tabImg = MakeImage(tab, _theme.sidebarGlass, _rounded, true);
            tabImg.pixelsPerUnitMultiplier = 1.6f;
            var tabButton = tab.gameObject.AddComponent<Button>();
            tabButton.targetGraphic = tabImg;
            var tabNav = tabButton.navigation;
            tabNav.mode = Navigation.Mode.None;
            tabButton.navigation = tabNav;
            tab.gameObject.AddComponent<UISound>().playClick = false;   // la barra ya suena al expandir/contraer
            var tabIcon = Center(MakeRect("Flecha", tab), 0f, 0f, 20f, 13f);
            MakeImage(tabIcon, _theme.menuItemActive, _chevron);

            var sidebarController = sidebar.gameObject.AddComponent<SidebarController>();
            sidebarController.sidebar = sidebar;
            sidebarController.content = content;
            sidebarController.toggleIcon = tabIcon;
            sidebarController.expandedWidth = SidebarWidth;
            Persist(tabButton.onClick, sidebarController.Toggle);

            // ---------------- Fundido y navegación ----------------
            var fader = CreateFader(root);

            var nav = canvas.gameObject.AddComponent<NavigationManager>();
            nav.sections = sections;
            nav.links = links;
            nav.homePanel = homeGroup;
            nav.backButtonGroup = backGroup;
            nav.backButton = backButton;
            nav.exitButton = exitButton;
            nav.sidebar = sidebarController;
            nav.fader = fader;

            SaveScene(scene, GameScenePath);
        }

        static SidebarLink SidebarOption(RectTransform sidebar, string label, float figmaY, SectionId target)
        {
            // Rect de 330×56 que empieza en x 48; el texto queda en x 74 como en Figma.
            var rt = TopLeft(MakeRect("Opcion" + label, sidebar), 48f, figmaY - 8.5f, 330f, 56f);
            TextButton(rt, label, BodyFont, 32f, _theme.menuItem, _theme.menuItemActive, _theme.menuItemActive,
                TextAlignmentOptions.MidlineLeft, 26f, 10f, out var hover);
            var link = rt.gameObject.AddComponent<SidebarLink>();
            link.target = target;
            link.hover = hover;
            return link;
        }

        static Section NewSection(RectTransform content, SectionId id, string title)
        {
            var rt = Stretch(MakeRect("Seccion" + id, content));
            MakeGroup(rt.gameObject);
            var section = rt.gameObject.AddComponent<Section>();
            section.id = id;

            var titleRt = TopLeft(MakeRect("Titulo", rt), 72f, 56f, 700f, 70f);
            MakeText(titleRt, title, TitleFont, 48f, _theme.darkText, TextAlignmentOptions.MidlineLeft);
            return section;
        }

        static Section PlaceholderSection(RectTransform content, SectionId id, string title, string message)
        {
            var section = NewSection(content, id, title);
            var msg = Center(MakeRect("Mensaje", section.transform as RectTransform), 0f, 0f, 800f, 60f);
            MakeText(msg, message, BodyFont, 26f, _theme.menuItem, TextAlignmentOptions.Center);
            return section;
        }

        static Section SettingsSection(RectTransform content)
        {
            var section = NewSection(content, SectionId.Configuracion, "Configuración");
            var rt = (RectTransform)section.transform;

            var block = Center(MakeRect("Volumenes", rt), 0f, 0f, 760f, 300f);
            var subtitle = TopLeft(MakeRect("Subtitulo", block), 0f, 0f, 760f, 40f);
            MakeText(subtitle, "Volumen", BodyFont, 26f, _theme.menuItem, TextAlignmentOptions.MidlineLeft);

            var panel = section.gameObject.AddComponent<SettingsPanel>();
            panel.generalSlider = VolumeRow(block, "General", 70f, out panel.generalValue);
            panel.feedbackSlider = VolumeRow(block, "Feedback", 150f, out panel.feedbackValue);
            panel.ambientSlider = VolumeRow(block, "Ambiente", 230f, out panel.ambientValue);
            return section;
        }

        static Slider VolumeRow(RectTransform parent, string label, float y, out TMP_Text valueText)
        {
            var row = TopLeft(MakeRect("Fila" + label, parent), 0f, y, 760f, 56f);
            var labelRt = TopLeft(MakeRect("Etiqueta", row), 0f, 0f, 220f, 56f);
            MakeText(labelRt, label, BodyFont, 32f, _theme.menuItemActive, TextAlignmentOptions.MidlineLeft);

            var sliderRt = TopLeft(MakeRect("Slider", row), 230f, 14f, 420f, 28f);
            var slider = MakeSlider(sliderRt, _theme.panelGray, _theme.menuItem, _theme.menuItemActive, _rounded, _circle);

            var valueRt = TopLeft(MakeRect("Valor", row), 670f, 0f, 90f, 56f);
            valueText = MakeText(valueRt, "80 %", BodyFont, 26f, _theme.menuItem, TextAlignmentOptions.MidlineRight);
            return slider;
        }

        // ==================================================================
        // Sección Misterios: bloques desplegables con gráficos
        // ==================================================================
        static Section BuildMisteriosSection(RectTransform content)
        {
            EnsureFolder(ChartsFolder);
            var rt = Stretch(MakeRect("Seccion" + SectionId.Misterios, content));
            MakeGroup(rt.gameObject);
            var section = rt.gameObject.AddComponent<MisteriosSection>();
            section.id = SectionId.Misterios;

            // Lista con desplazamiento (rueda del mouse o arrastrar)
            var scrollRt = Stretch(MakeRect("Lista", rt));
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch(MakeRect("Vista", scrollRt));
            MakeImage(viewport, new Color(1f, 1f, 1f, 0f), null, true);
            viewport.gameObject.AddComponent<RectMask2D>();

            var list = MakeRect("Bloques", viewport);
            list.anchorMin = new Vector2(0f, 1f);
            list.anchorMax = new Vector2(1f, 1f);
            list.pivot = new Vector2(0.5f, 1f);
            list.anchoredPosition = Vector2.zero;
            list.sizeDelta = Vector2.zero;
            var vlg = list.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 32f;
            vlg.padding = new RectOffset(0, 0, 110, 60);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = false;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = false;
            vlg.childForceExpandHeight = false;
            var fitter = list.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = list;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            scroll.inertia = true;

            section.template = BuildBlockTemplate(list);
            section.listContent = list;
            section.scroll = scroll;
            section.charts = LoadOrCreateCharts();
            return section;
        }

        static AccordionBlock BuildBlockTemplate(RectTransform list)
        {
            Sprite cellTitle = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/CeldaTitulo.png");
            Sprite cellIcon = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/CeldaIcono.png");
            Sprite bodySprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/CuerpoBloque.png");
            Sprite footerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/BarraInferior.png");
            Sprite cardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/Tarjeta.png");
            Sprite iconPie = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/IconoTorta.png");
            Sprite iconH = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/IconoBarrasH.png");
            Sprite iconV = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/IconoBarrasV.png");
            Sprite iconL = AssetDatabase.LoadAssetAtPath<Sprite>(ChartSpritesFolder + "/IconoLineas.png");
            RequireSprites(("Graficos/CeldaTitulo", cellTitle), ("Graficos/CeldaIcono", cellIcon), ("Graficos/CuerpoBloque", bodySprite),
                ("Graficos/BarraInferior", footerSprite), ("Graficos/Tarjeta", cardSprite), ("Graficos/IconoTorta", iconPie),
                ("Graficos/IconoBarrasH", iconH), ("Graficos/IconoBarrasV", iconV), ("Graficos/IconoLineas", iconL));

            // Raíz del bloque: 584 de ancho (Figma); el alto lo maneja el bloque.
            var root = MakeRect("BloquePlantilla", list);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 1f);
            root.pivot = new Vector2(0.5f, 1f);
            root.sizeDelta = new Vector2(BlockWidth, 146f);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 146f;
            var block = root.gameObject.AddComponent<AccordionBlock>();

            // Cuerpo (va antes para quedar detrás de la cabecera y la barra)
            var body = MakeRect("Cuerpo", root);
            body.anchorMin = new Vector2(0f, 1f);
            body.anchorMax = new Vector2(1f, 1f);
            body.pivot = new Vector2(0.5f, 1f);
            body.sizeDelta = new Vector2(0f, 0f);
            MakeImage(body, Color.white, bodySprite);
            body.gameObject.AddComponent<RectMask2D>();

            var card = MakeRect("Grafico", body);
            MakeImage(card, Color.white, cardSprite);
            var chart = card.gameObject.AddComponent<ChartView>();
            chart.font = _theme.chartTextFont;
            chart.textColor = _theme.darkText;
            chart.dotSprite = _circle;

            // Cabecera: celda del título + celda del ícono
            var header = MakeRect("Cabecera", root);
            header.anchorMin = new Vector2(0f, 1f);
            header.anchorMax = new Vector2(1f, 1f);
            header.pivot = new Vector2(0.5f, 1f);
            header.anchoredPosition = Vector2.zero;
            header.sizeDelta = new Vector2(0f, 109f);

            var titleCell = MakeRect("CeldaTitulo", header);
            titleCell.anchorMin = new Vector2(0f, 0f);
            titleCell.anchorMax = new Vector2(0f, 1f);
            titleCell.pivot = new Vector2(0f, 0.5f);
            titleCell.anchoredPosition = Vector2.zero;
            titleCell.sizeDelta = new Vector2(474f, 0f);
            var titleImg = MakeImage(titleCell, Color.white, cellTitle, true);
            var titleText = MakeText(Stretch(MakeRect("Titulo", titleCell), 12f, 12f, 0f, 0f), "Título", _theme.chartFont, 32f,
                Color.black, TextAlignmentOptions.Center, true);

            var iconCell = MakeRect("CeldaIcono", header);
            iconCell.anchorMin = new Vector2(1f, 0f);
            iconCell.anchorMax = new Vector2(1f, 1f);
            iconCell.pivot = new Vector2(1f, 0.5f);
            iconCell.anchoredPosition = Vector2.zero;
            iconCell.sizeDelta = new Vector2(112f, 0f);
            MakeImage(iconCell, Color.white, cellIcon, true);
            var icon = MakeImage(Center(MakeRect("Icono", iconCell), 0f, 0f, 66f, 66f), _theme.accentRed, iconV);

            var headerButton = header.gameObject.AddComponent<Button>();
            headerButton.targetGraphic = titleImg;
            var colors = headerButton.colors;
            colors.highlightedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.86f, 0.86f, 0.86f, 1f);
            colors.selectedColor = Color.white;
            headerButton.colors = colors;
            var nav = headerButton.navigation; nav.mode = Navigation.Mode.None; headerButton.navigation = nav;
            header.gameObject.AddComponent<UISound>().playClick = false;

            // Barra inferior con la flecha
            var footer = MakeRect("BarraInferior", root);
            footer.anchorMin = new Vector2(0f, 1f);
            footer.anchorMax = new Vector2(1f, 1f);
            footer.pivot = new Vector2(0.5f, 1f);
            footer.sizeDelta = new Vector2(0f, 38f);
            var footerImg = MakeImage(footer, Color.white, footerSprite, true);
            var chevron = Center(MakeRect("Flecha", footer), 0f, 0f, 23f, 14f);
            MakeImage(chevron, Color.white, _chevron);
            var footerButton = footer.gameObject.AddComponent<Button>();
            footerButton.targetGraphic = footerImg;
            footerButton.colors = colors;
            var nav2 = footerButton.navigation; nav2.mode = Navigation.Mode.None; footerButton.navigation = nav2;
            footer.gameObject.AddComponent<UISound>().playClick = false;

            block.header = header;
            block.body = body;
            block.card = card;
            block.footer = footer;
            block.chevron = chevron;
            block.title = titleText;
            block.icon = icon;
            block.chart = chart;
            block.headerButton = headerButton;
            block.footerButton = footerButton;
            block.layout = layout;
            block.iconPie = iconPie;
            block.iconHorizontalBars = iconH;
            block.iconVerticalBars = iconV;
            block.iconLines = iconL;
            return block;
        }

        // ------------------------------------------------------------------
        // Datos de las 5 gráficas, tomados del archivo de Illustrator
        // (Referencias/Rata Blanca graficos.ai). Solo se crean si no existen:
        // lo que edites en los archivos no se sobrescribe.
        // ------------------------------------------------------------------
        static readonly Color Rojo = new Color32(0xFF, 0x17, 0x00, 0xFF);
        static readonly Color Vino = new Color32(0x4F, 0x06, 0x06, 0xFF);
        static readonly Color Coral = new Color32(0xFF, 0x54, 0x4D, 0xFF);
        static readonly Color Ladrillo = new Color32(0x9B, 0x2D, 0x29, 0xFF);
        static readonly Color Salmon = new Color32(0xFF, 0x8E, 0x85, 0xFF);
        static readonly Color LineaRoja = new Color32(0xFF, 0x00, 0x00, 0xFF);
        static readonly Color LineaAmbar = new Color32(0xFF, 0xAF, 0x00, 0xFF);

        static List<ChartData> LoadOrCreateCharts()
        {
            return new List<ChartData>
            {
                Chart("1_TiposDeRatas", c =>
                {
                    c.type = ChartType.Torta;
                    c.title = "Tipos de Ratas en la Bio-jaula";
                    c.entries = new List<ChartEntry>
                    {
                        new ChartEntry("Procreadoras", 30f, Salmon),
                        new ChartEntry("Alcohólicas", 56.8f, Coral),
                        new ChartEntry("Ancianas", 24.3f, Ladrillo),
                        new ChartEntry("Obreras", 11.1f, Vino),
                        new ChartEntry("Sapiens", 1f, Rojo),
                    };
                    c.valueSuffix = "%";
                    c.pieLabels = PieLabelMode.Valor;
                    c.startAngle = 274.2f;
                    c.innerRadius = 0.46f;
                    c.bodyHeight = 360f;
                }),
                Chart("2_IQComparativo", c =>
                {
                    c.type = ChartType.BarrasHorizontales;
                    c.title = "IQ comparativo de los personajes del mundo";
                    c.entries = new List<ChartEntry>
                    {
                        new ChartEntry("BIO-JAULA", 120f, Rojo),
                        new ChartEntry("HUMANO", 92f, Vino),
                        new ChartEntry("RATA BLANCA", 84f, Coral),
                        new ChartEntry("ANTONELLA", 84f, Ladrillo),
                        new ChartEntry("GORDO JOHN", 68f, Salmon),
                    };
                    c.showLegend = false;
                    c.bodyHeight = 330f;
                }),
                Chart("3_DistribucionDelTiempo", c =>
                {
                    c.type = ChartType.BarrasVerticales;
                    c.title = "Promedio de Como las Ratas Distribuyen su Tiempo";
                    c.entries = new List<ChartEntry>
                    {
                        new ChartEntry("Dormir", 8f, Salmon),
                        new ChartEntry("Comer", 1.5f, Vino),
                        new ChartEntry("Trabajar", 8f, Coral),
                        new ChartEntry("Ocio", 3f, Ladrillo),
                        new ChartEntry("Hobbies", 4f, Rojo),
                    };
                    c.yAxisLabel = "Horas";
                    c.showLegend = false;
                    c.bodyHeight = 360f;
                }),
                Chart("4_InformacionMensual", c =>
                {
                    c.type = ChartType.Lineas;
                    c.title = "Cantidad de información que recibe la Bio-Jaula mensualmente";
                    c.series = new List<ChartSeries>
                    {
                        new ChartSeries("Información recolectada por Bio-Jaula", LineaRoja,
                            P(1, 10), P(2, 25), P(3, 25), P(4, 30), P(6, 40), P(7, 45), P(8, 60), P(9, 80),
                            P(10, 100), P(11, 150), P(12, 200), P(13, 220), P(14, 260), P(15, 300)),
                        new ChartSeries("Información extraída de estudios externos", LineaAmbar,
                            P(1, 20), P(2, 25), P(3, 25), P(4, 25), P(5, 25), P(7, 30), P(8, 30), P(9, 25),
                            P(10, 25), P(11, 40), P(12, 30), P(13, 30), P(14, 25), P(15, 30)),
                    };
                    c.xAxisLabel = "Meses";
                    c.yAxisLabel = "Progreso de inteligencia (Gigabyte)";
                    c.xMin = 0f; c.xMax = 15f; c.xStep = 1f;
                    c.yMax = 300f; c.yStep = 100f;
                    c.axisColor = new Color32(0x79, 0x2B, 0x2B, 0xFF);
                    c.duration = 1.8f;
                    c.bodyHeight = 400f;
                }),
                Chart("5_EdadDeJubilacion", c =>
                {
                    c.type = ChartType.Lineas;
                    c.title = "Edad Promedio en la que se “Jubilan” las Ratas de Laboratorio";
                    c.series = new List<ChartSeries>
                    {
                        new ChartSeries("Ratas pensionadas", LineaRoja,
                            P(1, 0), P(2, 0), P(3, 0), P(4, 0), P(5, 1), P(6, 1), P(7, 1), P(8, 2), P(9, 3),
                            P(10, 4), P(11, 8), P(12, 10), P(13, 14), P(14, 13), P(15, 9), P(16, 7), P(17, 7),
                            P(18, 5), P(19, 4), P(20, 2), P(21, 1), P(22, 0)),
                    };
                    c.xAxisLabel = "Edad (meses)";
                    c.yAxisLabel = "Número de ratas pensionadas";
                    c.xMin = 0f; c.xMax = 24f; c.xStep = 2f;
                    c.yMax = 15f; c.yStep = 5f;
                    c.axisColor = new Color32(0xFF, 0x73, 0x73, 0xFF);
                    c.showLegend = false;
                    c.duration = 1.8f;
                    c.bodyHeight = 400f;
                }),
            };
        }

        static Vector2 P(float x, float y) => new Vector2(x, y);

        static ChartData Chart(string fileName, Action<ChartData> fill)
        {
            string path = ChartsFolder + "/" + fileName + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<ChartData>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<ChartData>();
            fill(data);
            AssetDatabase.CreateAsset(data, path);
            AssetDatabase.SaveAssets();
            return data;
        }

        // ==================================================================
        // Sección Objeto: despiece del turbante de Madame White
        // ==================================================================
        static Section BuildObjectSection(RectTransform content)
        {
            var data = AssetDatabase.LoadAssetAtPath<ExplodedViewData>(ObjectDataPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<ExplodedViewData>();
                AssetDatabase.CreateAsset(data, ObjectDataPath);
                AssetDatabase.SaveAssets();
            }

            var rt = Stretch(MakeRect("Seccion" + SectionId.Objeto, content));
            MakeGroup(rt.gameObject);
            var section = rt.gameObject.AddComponent<ObjectSection>();
            section.id = SectionId.Objeto;
            section.data = data;

            // Escenario: el espacio libre bajo el título; la composición se
            // escala para caber entera (se adapta a la ventana y a la barra).
            var stage = Stretch(MakeRect("Escenario", rt), 40f, 40f, 150f, 60f);
            var comp = Center(MakeRect("Composicion", stage), 0f, 0f, CompositionSize.x, CompositionSize.y);
            comp.gameObject.AddComponent<FitToParent>().designSize = CompositionSize;

            Sprite turbante = AssetDatabase.LoadAssetAtPath<Sprite>(ImagesFolder + "/Turbante.png");
            Sprite rubi = AssetDatabase.LoadAssetAtPath<Sprite>(ImagesFolder + "/Rubi.png");
            Sprite tela = AssetDatabase.LoadAssetAtPath<Sprite>(ImagesFolder + "/Tela.png");
            Sprite rubiZoom = AssetDatabase.LoadAssetAtPath<Sprite>(ImagesFolder + "/RubiZoom.png");
            Sprite glowTurbante = AssetDatabase.LoadAssetAtPath<Sprite>(ObjectSpritesFolder + "/BrilloTurbante.png");
            Sprite glowRubi = AssetDatabase.LoadAssetAtPath<Sprite>(ObjectSpritesFolder + "/BrilloRubi.png");
            Sprite ring = AssetDatabase.LoadAssetAtPath<Sprite>(ObjectSpritesFolder + "/Anillo.png");
            RequireSprites(("Imagenes/Turbante", turbante), ("Imagenes/Rubi", rubi), ("Imagenes/Tela", tela),
                ("Imagenes/RubiZoom", rubiZoom), ("UI/Sprites/Objeto/BrilloTurbante", glowTurbante),
                ("UI/Sprites/Objeto/BrilloRubi", glowRubi), ("UI/Sprites/Objeto/Anillo", ring));

            // Turbante con su brillo detrás
            var glowT = TopLeft(MakeRect("BrilloTurbante", comp), TurbanteRect.x - TurbanteGlowPad, TurbanteRect.y - TurbanteGlowPad,
                TurbanteRect.width + TurbanteGlowPad * 2f, TurbanteRect.height + TurbanteGlowPad * 2f);
            section.clothGlow = MakeImage(glowT, new Color(1f, 1f, 1f, 0f), glowTurbante);
            var turbanteRt = TopLeft(MakeRect("Turbante", comp), TurbanteRect.x, TurbanteRect.y, TurbanteRect.width, TurbanteRect.height);
            MakeImage(turbanteRt, Color.white, turbante, true);
            section.clothZone = turbanteRt.gameObject.AddComponent<HotZone>();

            // Rubí con su brillo
            var glowR = TopLeft(MakeRect("BrilloRubi", comp), RubiRect.x - RubiGlowPad, RubiRect.y - RubiGlowPad,
                RubiRect.width + RubiGlowPad * 2f, RubiRect.height + RubiGlowPad * 2f);
            section.rubyGlow = MakeImage(glowR, new Color(1f, 1f, 1f, 0f), glowRubi);
            var rubiRt = TopLeft(MakeRect("Rubi", comp), RubiRect.x, RubiRect.y, RubiRect.width, RubiRect.height);
            MakeImage(rubiRt, Color.white, rubi, true);
            section.rubyZone = rubiRt.gameObject.AddComponent<HotZone>();

            // Lupa: anillo alrededor del rubí + línea punteada hasta el círculo
            var ringRt = MakeRect("Anillo", comp);
            section.ring = MakeImage(ringRt, new Color(1f, 1f, 1f, 0f), ring);
            section.magnifierLine = Stretch(MakeRect("LineaLupa", comp)).gameObject.AddComponent<ArrowGraphic>();

            // Vistas que aparecen (escalan desde su centro)
            section.clothView = PieceView(comp, "VistaTela", TelaRect, tela, out section.clothPieceZone);
            section.zoomView = PieceView(comp, "VistaRubi", RubiZoomRect, rubiZoom, out section.zoomPieceZone);

            // Flechas y textos (encima de todo)
            for (int i = 0; i < data.clothCallouts.Count; i++)
                CreateCallout(comp, "Tela" + (i + 1), data.clothCallouts[i].text, data, section.clothArrows, section.clothLabels);
            for (int i = 0; i < data.rubyCallouts.Count; i++)
                CreateCallout(comp, "Rubi" + (i + 1), data.rubyCallouts[i].text, data, section.rubyArrows, section.rubyLabels);

            // Título, subtítulo e indicación
            var titleRt = TopLeft(MakeRect("Titulo", rt), 72f, 56f, 700f, 70f);
            MakeText(titleRt, "Objeto", TitleFont, 48f, _theme.darkText, TextAlignmentOptions.MidlineLeft);
            var subRt = TopLeft(MakeRect("Subtitulo", rt), 72f, 122f, 900f, 36f);
            MakeText(subRt, "El turbante de Madame White", BodyFont, 26f, _theme.menuItem, TextAlignmentOptions.MidlineLeft);
            var hintRt = Anchor(MakeRect("Indicacion", rt), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(72f, 28f), new Vector2(900f, 36f));
            MakeText(hintRt, "Haz clic en lo que brilla para ver de qué está hecho.", BodyFont, 22f, _theme.menuItem, TextAlignmentOptions.MidlineLeft);

            return section;
        }

        /// <summary>Si alguna imagen no quedó como Sprite, detiene la construcción con un mensaje claro.</summary>
        static void RequireSprites(params (string name, Sprite sprite)[] sprites)
        {
            var missing = new List<string>();
            foreach (var item in sprites) if (item.sprite == null) missing.Add(item.name);
            if (missing.Count > 0)
                throw new InvalidOperationException(
                    "Estas imágenes no están importadas como Sprite (2D and UI): " + string.Join(", ", missing) +
                    ". Usa Rata Blanca > Configurar proyecto y vuelve a construir.");
        }

        static CanvasGroup PieceView(RectTransform comp, string name, Rect r, Sprite sprite, out HotZone zone)
        {
            // Pivote al centro para que la animación de escala salga desde ahí.
            var view = Anchor(MakeRect(name, comp), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(r.x + r.width * 0.5f, -(r.y + r.height * 0.5f)), new Vector2(r.width, r.height));
            var group = MakeGroup(view.gameObject);
            var img = Stretch(MakeRect("Imagen", view));
            MakeImage(img, Color.white, sprite, true);
            zone = img.gameObject.AddComponent<HotZone>();
            return group;
        }

        static void CreateCallout(RectTransform comp, string suffix, string text, ExplodedViewData data,
            List<ArrowGraphic> arrows, List<TMP_Text> labels)
        {
            var arrow = Stretch(MakeRect("Flecha" + suffix, comp)).gameObject.AddComponent<ArrowGraphic>();
            arrow.color = data.arrowColor;
            arrows.Add(arrow);

            var labelRt = TopLeft(MakeRect("Texto" + suffix, comp), 0f, 0f, 300f, 100f);
            var label = MakeText(labelRt, text, BodyFont, data.textSize, data.textColor, TextAlignmentOptions.MidlineLeft, true);
            labels.Add(label);
        }

        // ==================================================================
        static void SetBuildScenes()
        {
            // Solo se registran las escenas que existen (Menú primero).
            var list = new List<EditorBuildSettingsScene>();
            if (File.Exists(MenuScenePath)) list.Add(new EditorBuildSettingsScene(MenuScenePath, true));
            if (File.Exists(GameScenePath)) list.Add(new EditorBuildSettingsScene(GameScenePath, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
