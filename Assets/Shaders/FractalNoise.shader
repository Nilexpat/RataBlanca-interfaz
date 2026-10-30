// =============================================================================
// Rata Blanca — Fondo de ruido fractal
// -----------------------------------------------------------------------------
// Puerto a HLSL del efecto "Fractal noise" que se usa en Figma como relleno del
// fondo (autor original del efecto: Tim Murray-Browne). Se conserva la misma
// matemática (Perlin 3D periódico con derivadas, turbulencia, capas sumadas y
// "flow") para que el fondo se vea igual que en el diseño. La única diferencia
// es que en Figma el efecto es estático y aquí la "evolución" (eje Z del ruido)
// avanza con el tiempo, lo que produce la animación tipo radiografía/escáner.
//
// Solo se implementó el modo de ruido Perlin y la mezcla "Add", que son los que
// usa el diseño. Este shader no se pone en un Image: lo dibuja NoiseBackground
// dentro de una RenderTexture (Graphics.Blit) y la muestra en un RawImage.
// =============================================================================
Shader "RataBlanca/FractalNoise"
{
    Properties
    {
        _Size          ("Tamaño de referencia (px)", Vector) = (1440, 1024, 0, 0)
        _Center        ("Centro (px)", Vector)               = (57.6, 512, 0, 0)
        _Scale         ("Escala (px)", Float)                = 532.48
        _Angle         ("Ángulo (rad)", Float)               = 0
        _TileSize      ("Tamaño de tile X/Y", Vector)        = (73, 0, 0, 0)
        _Turbulence    ("Turbulencia (0-2)", Float)          = 1.02
        _Interpolation ("Suavidad (1 - sharpness)", Float)   = 1
        _Layers        ("Capas", Float)                      = 7.72
        _LayerGain     ("Ganancia por capa", Float)          = 0.51
        _LayerScale    ("Escala por capa", Float)            = 0.47
        _LayerRotation ("Rotación por capa (rad)", Float)    = 0.383972
        _Flow          ("Flow", Float)                       = 0.07
        _Evolution     ("Evolución (tiempo)", Float)         = 0
        _EvolutionLoop ("Ciclo de evolución", Float)         = 1
        _Opacity       ("Opacidad sobre el color base", Float) = 0.26
        _BaseColor     ("Color base (sRGB)", Color)          = (1, 1, 1, 1)
    }

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.5
            #include "UnityCG.cginc"

            float4 _Size;
            float4 _Center;
            float  _Scale;
            float  _Angle;
            float4 _TileSize;
            float  _Turbulence;
            float  _Interpolation;
            float  _Layers;
            float  _LayerGain;
            float  _LayerScale;
            float  _LayerRotation;
            float  _Flow;
            float  _Evolution;
            float  _EvolutionLoop;
            float  _Opacity;
            float4 _BaseColor;

            // ---------------------------------------------------------------
            // Utilidades
            // ---------------------------------------------------------------
            // Rotación igual a rot(t) * v del WGSL original (matriz por columnas).
            float2 Rotate(float2 v, float t)
            {
                float c = cos(t);
                float s = sin(t);
                return float2(c * v.x + s * v.y, -s * v.x + c * v.y);
            }

            // Número pseudoaleatorio en [0, 1) a partir de los bits del float.
            float Rand(float3 inF)
            {
                uint3 x = asuint(inF);
                const uint k = 1103515245u;
                x = ((x >> 8u) ^ x.yzx) * k;
                x = ((x >> 8u) ^ x.yzx) * k;
                x = ((x >> 8u) ^ x.yzx) * k;
                uint outI = x.x ^ x.y ^ x.z;
                uint outF = (127u << 23u) | (outI & ((1u << 23u) - 1u));
                return asfloat(outF) - 1.0;
            }

            // x módulo period en los ejes con period != 0; salida en [-p/2, p/2].
            float3 WrapToPeriod(float3 x, float3 period)
            {
                float3 hasPeriod = abs(sign(period));
                float3 m = x - floor(x / (period + 0.00001)) * period;
                float3 xMod = lerp(x, m, hasPeriod);
                return xMod - period * step(period * 0.5 + 1e-6, xMod);
            }

            float3 QuinticFade(float3 t)      { return t * t * t * (t * (t * 6.0 - 15.0) + 10.0); }
            float3 QuinticFadeDeriv(float3 t) { return 30.0 * t * t * (t * (t - 2.0) + 1.0); }

            // 16 direcciones de gradiente de Perlin (mismo orden que el original).
            static const float3 kGrad[16] =
            {
                float3( 1, 1, 0), float3(-1, 1, 0), float3( 1,-1, 0), float3(-1,-1, 0),
                float3( 1, 0, 1), float3(-1, 0, 1), float3( 1, 0,-1), float3(-1, 0,-1),
                float3( 0, 1, 1), float3( 0,-1, 1), float3( 0, 1,-1), float3( 0,-1,-1),
                float3( 1, 1, 0), float3(-1, 1, 0), float3( 0,-1, 1), float3( 0,-1,-1)
            };

            float3 GradientDir(float3 p)
            {
                int i = (int)min(floor(Rand(p) * 16.0), 15.0);
                return kGrad[i];
            }

            // ---------------------------------------------------------------
            // Perlin 3D periódico con derivada analítica
            //   devuelve xyz = derivada, w = valor del ruido
            // ---------------------------------------------------------------
            float4 Perlin(float3 P, float3 period, float seed)
            {
                float3 sp  = P + seed;
                float3 i0  = WrapToPeriod(floor(sp), period);
                float3 i1  = WrapToPeriod(i0 + 1.0, period);
                float3 f0  = frac(sp);
                float3 f1  = f0 - 1.0;

                float3 g000 = GradientDir(float3(i0.x, i0.y, i0.z));
                float3 g100 = GradientDir(float3(i1.x, i0.y, i0.z));
                float3 g010 = GradientDir(float3(i0.x, i1.y, i0.z));
                float3 g110 = GradientDir(float3(i1.x, i1.y, i0.z));
                float3 g001 = GradientDir(float3(i0.x, i0.y, i1.z));
                float3 g101 = GradientDir(float3(i1.x, i0.y, i1.z));
                float3 g011 = GradientDir(float3(i0.x, i1.y, i1.z));
                float3 g111 = GradientDir(float3(i1.x, i1.y, i1.z));

                float a = dot(g000, f0);
                float b = dot(g100, float3(f1.x, f0.y, f0.z));
                float c = dot(g010, float3(f0.x, f1.y, f0.z));
                float d = dot(g110, float3(f1.x, f1.y, f0.z));
                float e = dot(g001, float3(f0.x, f0.y, f1.z));
                float f = dot(g101, float3(f1.x, f0.y, f1.z));
                float g = dot(g011, float3(f0.x, f1.y, f1.z));
                float h = dot(g111, f1);

                float3 u  = QuinticFade(f0);
                float3 du = QuinticFadeDeriv(f0);

                // Interpolación trilineal del valor
                float v00 = lerp(a, b, u.x);
                float v01 = lerp(e, f, u.x);
                float v10 = lerp(c, d, u.x);
                float v11 = lerp(g, h, u.x);
                float noise = lerp(lerp(v00, v10, u.y), lerp(v01, v11, u.y), u.z);

                // Derivada del interpolado
                float k0 = b - a;
                float k1 = c - a;
                float k2 = e - a;
                float k3 = a + d - b - c;
                float k4 = a + f - b - e;
                float k5 = a + g - c - e;
                float k6 = b + c + e + h - a - d - f - g;
                float3 deriv = float3(
                    du.x * (k0 + u.y * k3 + u.z * k4 + u.y * u.z * k6),
                    du.y * (k1 + u.x * k3 + u.z * k5 + u.x * u.z * k6),
                    du.z * (k2 + u.x * k4 + u.y * k5 + u.x * u.y * k6));

                // Contribución de los gradientes interpolados
                float3 gg00 = lerp(g000, g100, u.x);
                float3 gg01 = lerp(g001, g101, u.x);
                float3 gg10 = lerp(g010, g110, u.x);
                float3 gg11 = lerp(g011, g111, u.x);
                deriv += lerp(lerp(gg00, gg10, u.y), lerp(gg01, gg11, u.y), u.z);

                return float4(deriv, noise);
            }

            // Afilado opcional (sharpness). Con los valores del diseño no actúa.
            float4 Sharpen(float4 dn, float smoothness)
            {
                float sharpness = saturate(1.0 - smoothness);
                if (sharpness <= 0.001) return dn;
                float exponent = lerp(1.0, 0.45, sharpness);
                float absN = abs(dn.w);
                float slope = exponent * pow(max(absN, 0.02), exponent - 1.0);
                dn.xyz *= min(slope, 4.0);
                dn.w = sign(dn.w) * pow(absN, exponent);
                return dn;
            }

            // Turbulencia: "dobla" el ruido alrededor del umbral t.
            float4 Turbulence(float4 dn, float turbulence)
            {
                float t = turbulence - 1.0;
                bool above = dn.w > t;
                float rangeBelow = -2.0 / (t + 1.0);
                float rangeAbove =  2.0 / (1.0 - t);
                dn.xyz *= above ? rangeAbove : rangeBelow;

                float mapped = above
                    ? -1.0 + (dn.w - t) * 2.0 / (1.0 - t)      // map(n, t, 1, -1, 1)
                    :  1.0 + (dn.w + 1.0) * -2.0 / (t + 1.0);  // map(n, -1, t, 1, -1)
                dn.w = mapped;

                // Corrección de ganancia del original para Perlin
                float fold = saturate(1.0 - abs(t));
                if (fold > 0.001)
                {
                    float gain = lerp(1.0, 1.25, fold);
                    float lift = lerp(0.0, 0.55, fold);
                    dn.w = dn.w * gain + lift;
                    dn.xyz *= gain;
                }
                return dn;
            }

            float FractalNoise(float3 pos, float3 period)
            {
                bool isTiled   = period.x != 0.0 || period.y != 0.0;
                bool isLooping = period.z != 0.0;
                float periodicScale = 1.0 / max(round(1.0 / _LayerScale), 1.0);
                float scaleXY = isTiled   ? periodicScale : _LayerScale;
                float scaleZ  = isLooping ? periodicScale : _LayerScale;
                float3 layerScale = float3(scaleXY, scaleXY, scaleZ);

                float  noise     = 0.0;
                float3 gradient  = 0.0;
                float  amplitude = 1.0;
                float  seed      = 0.0;
                int numLayers = (int)ceil(_Layers);

                [loop]
                for (int i = 0; i < 8; i++)
                {
                    if (i >= numLayers) break;
                    float layerFraction = min(1.0, _Layers - (float)i);

                    float4 dn = Sharpen(Perlin(pos, period, seed), _Interpolation);
                    if (_Turbulence > 0.001) dn = Turbulence(dn, _Turbulence);

                    if (i == 0)
                    {
                        noise = dn.w;
                        gradient = dn.xyz;
                    }
                    else
                    {
                        float amp = amplitude * layerFraction;
                        noise    += amp * dn.w;      // mezcla "Add"
                        gradient += amp * dn.xyz;
                    }

                    amplitude *= _LayerGain;
                    if (amplitude < 0.002) break;

                    if (_Flow > 0.001) pos += gradient * _Flow;
                    if (!isTiled) pos.xy = Rotate(pos.xy, _LayerRotation);

                    pos /= layerScale;
                    pos.z += 0.5 / (float)(i + 1);
                    period /= layerScale;
                    seed += 1.0;
                }
                return noise;
            }

            fixed4 frag(v2f_img i) : SV_Target
            {
                // En Figma el eje Y crece hacia abajo; se invierte para coincidir.
                float2 uv = float2(i.uv.x, 1.0 - i.uv.y);
                float2 localPos = uv * max(_Size.xy, 1.0);

                float2 rawXY = Rotate(localPos - _Center.xy, _Angle);
                float3 period = float3(max(_TileSize.xy, 0.0), _EvolutionLoop > 0.0 ? _EvolutionLoop : 0.0);
                float3 pos = float3(rawXY, _Evolution) / float3(_Scale, _Scale, 1.0);

                float n = FractalNoise(pos, period);
                float v = saturate(n * 0.5 + 0.5);

                // Se mezcla con el color base en espacio sRGB (como en Figma)...
                float3 baseColor = _BaseColor.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                baseColor = LinearToGammaSpace(baseColor); // Unity entrega los Color en lineal
                #endif
                float3 col = lerp(baseColor, float3(v, v, v), _Opacity);
                // ...y se convierte a lineal si el proyecto trabaja en Linear.
                #ifndef UNITY_COLORSPACE_GAMMA
                col = GammaToLinearSpace(col);
                #endif
                return fixed4(col, 1.0);
            }
            ENDCG
        }
    }
    Fallback Off
}
