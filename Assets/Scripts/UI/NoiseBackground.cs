using UnityEngine;
using UnityEngine.UI;

namespace RataBlanca.UI
{
    /// <summary>
    /// Dibuja el fondo animado de ruido fractal (el mismo efecto de Figma).
    /// El shader se renderiza en una RenderTexture a menor resolución y se
    /// muestra en un RawImage. Así el costo en WebGL es bajo: el ruido es
    /// suave, por lo que dibujarlo a media resolución no se nota.
    /// Los parámetros usan las mismas unidades del panel de Figma.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class NoiseBackground : MonoBehaviour
    {
        [Header("Shader")]
        public Shader shader;

        [Header("Parámetros de Figma (Fractal noise)")]
        [Tooltip("Tamaño del frame de Figma. El ruido se calcula en estas unidades para que se vea igual a cualquier resolución.")]
        public Vector2 referenceSize = new Vector2(1440f, 1024f);
        [Range(0f, 100f)] public float centerXPercent = 4f;
        [Range(0f, 100f)] public float centerYPercent = 50f;
        [Range(1f, 100f)] public float radiusPercent = 52f;
        public float angleDegrees = 0f;
        [Range(0f, 100f)] public float tileSizeX = 73f;
        [Range(0f, 100f)] public float tileSizeY = 0f;
        [Range(0f, 200f)] public float turbulencePercent = 102f;
        [Range(0f, 100f)] public float sharpnessPercent = 0f;
        [Range(1f, 8f)] public float layers = 7.72f;
        [Range(0f, 100f)] public float layerGainPercent = 51f;
        [Range(1f, 100f)] public float layerScalePercent = 47f;
        [Range(0f, 100f)] public float layerFlowPercent = 7f;
        [Tooltip("Opacidad del ruido sobre el color base (en Figma: 26 %).")]
        [Range(0f, 100f)] public float opacityPercent = 26f;
        public Color baseColor = Color.white;

        [Header("Animación")]
        [Tooltip("Cuánto avanza la evolución por segundo. 1 = un ciclo completo (el ciclo se repite sin cortes).")]
        public float evolutionSpeed = 0.05f;
        [Tooltip("Evolución inicial (fase).")]
        public float evolutionOffset = 0f;

        [Header("Movimiento extra (todo en bucle)")]
        [Tooltip("Respiración: cuánto crece y se encoge el ruido (0.12 = ±12 %).")]
        [Range(0f, 0.5f)] public float breathAmount = 0.12f;
        [Tooltip("Segundos que tarda una respiración completa.")]
        public float breathPeriod = 10f;
        [Tooltip("Cuánto se desplaza el centro del ruido a los lados (% del ancho).")]
        [Range(0f, 20f)] public float driftPercent = 3f;
        public float driftPeriod = 17f;
        [Tooltip("Cuánto sube y baja el 'layer flow' (deformación de las capas), en %.")]
        [Range(0f, 20f)] public float flowWobblePercent = 4f;
        public float flowPeriod = 7f;

        [Header("Rendimiento")]
        [Tooltip("Resolución de la textura respecto a la pantalla. 0.5 = mitad.")]
        [Range(0.2f, 1f)] public float resolutionScale = 0.5f;
        [Tooltip("Veces por segundo que se redibuja el ruido.")]
        [Range(5f, 60f)] public float updatesPerSecond = 30f;

        RawImage _rawImage;
        Material _material;
        RenderTexture _rt;
        float _nextUpdate;

        static readonly int SizeId = Shader.PropertyToID("_Size");
        static readonly int CenterId = Shader.PropertyToID("_Center");
        static readonly int ScaleId = Shader.PropertyToID("_Scale");
        static readonly int AngleId = Shader.PropertyToID("_Angle");
        static readonly int TileId = Shader.PropertyToID("_TileSize");
        static readonly int TurbulenceId = Shader.PropertyToID("_Turbulence");
        static readonly int InterpolationId = Shader.PropertyToID("_Interpolation");
        static readonly int LayersId = Shader.PropertyToID("_Layers");
        static readonly int LayerGainId = Shader.PropertyToID("_LayerGain");
        static readonly int LayerScaleId = Shader.PropertyToID("_LayerScale");
        static readonly int FlowId = Shader.PropertyToID("_Flow");
        static readonly int EvolutionId = Shader.PropertyToID("_Evolution");
        static readonly int EvolutionLoopId = Shader.PropertyToID("_EvolutionLoop");
        static readonly int OpacityId = Shader.PropertyToID("_Opacity");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void OnEnable()
        {
            _rawImage = GetComponent<RawImage>();
            if (shader == null) shader = Shader.Find("RataBlanca/FractalNoise");
            if (shader == null || !shader.isSupported)
            {
                // Si el shader no está disponible, se deja el fondo liso.
                _rawImage.texture = null;
                _rawImage.color = baseColor;
                return;
            }
            _material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            _nextUpdate = 0f;
            Render();
        }

        void OnDisable()
        {
            if (_rawImage != null) _rawImage.texture = null;
            ReleaseTexture();
            if (_material != null) Destroy(_material);
            _material = null;
        }

        void Update()
        {
            if (_material == null) return;
            float now = Time.unscaledTime;
            if (now < _nextUpdate) return;
            _nextUpdate = now + 1f / Mathf.Max(1f, updatesPerSecond);
            Render();
        }

        void EnsureTexture()
        {
            int w = Mathf.Max(16, Mathf.RoundToInt(Screen.width * resolutionScale));
            int h = Mathf.Max(16, Mathf.RoundToInt(Screen.height * resolutionScale));
            if (_rt != null && _rt.width == w && _rt.height == h) return;

            ReleaseTexture();
            _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default)
            {
                name = "RuidoFondo",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                hideFlags = HideFlags.HideAndDontSave
            };
            _rt.Create();
            _rawImage.texture = _rt;
            _rawImage.color = Color.white;
        }

        void ReleaseTexture()
        {
            if (_rt == null) return;
            _rt.Release();
            Destroy(_rt);
            _rt = null;
        }

        /// <summary>Onda seno entre -1 y 1 con el período dado (en segundos).</summary>
        static float Wave(float t, float period) =>
            period <= 0f ? 0f : Mathf.Sin(t * Mathf.PI * 2f / period);

        void Render()
        {
            EnsureTexture();

            // Conversión de los valores de Figma a los uniforms del shader
            // (igual que hace la función render() del efecto original).
            float w = referenceSize.x, h = referenceSize.y;
            float t = Time.unscaledTime;
            float breath = 1f + breathAmount * Wave(t, breathPeriod);
            float drift = driftPercent * Wave(t, driftPeriod);
            float flow = layerFlowPercent + flowWobblePercent * Wave(t, flowPeriod);

            _material.SetVector(SizeId, new Vector4(w, h, 0, 0));
            _material.SetVector(CenterId, new Vector4((centerXPercent + drift) / 100f * w, centerYPercent / 100f * h, 0, 0));
            _material.SetFloat(ScaleId, radiusPercent / 100f * Mathf.Min(w, h) * breath);
            _material.SetFloat(AngleId, angleDegrees * Mathf.Deg2Rad);
            _material.SetVector(TileId, new Vector4(tileSizeX, tileSizeY, 0, 0));
            _material.SetFloat(TurbulenceId, turbulencePercent / 100f);
            _material.SetFloat(InterpolationId, 1f - sharpnessPercent / 100f);
            _material.SetFloat(LayersId, layers);
            _material.SetFloat(LayerGainId, layerGainPercent / 100f);
            _material.SetFloat(LayerScaleId, Mathf.Max(0.001f, layerScalePercent / 100f));
            _material.SetFloat(FlowId, Mathf.Max(0f, flow) / 100f);
            _material.SetFloat(EvolutionLoopId, 1f);
            _material.SetFloat(OpacityId, opacityPercent / 100f);
            _material.SetColor(BaseColorId, baseColor);

            float evolution = Mathf.Repeat(evolutionOffset + t * evolutionSpeed, 1f);
            _material.SetFloat(EvolutionId, evolution);

            Graphics.Blit(null, _rt, _material);
        }
    }
}
