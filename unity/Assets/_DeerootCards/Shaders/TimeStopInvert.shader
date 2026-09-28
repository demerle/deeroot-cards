// Time Stop — negative monochrome post-processing effect, background-gated.
// Active pixels (players, bullets, particles, bright FX) become their own
// opposite in grayscale: bright -> dark, dark -> bright, all color removed.
// DARK pixels are LEFT AS-IS: the game itself paints every map scenery
// sprite a constant near-black (Map.Start recolors everything to
// 11/51 = 0.216 gray), so gating on luminance keeps the backdrop and walls
// visually unchanged while only gameplay content negates — the dark
// background must never invert, it whites out and blinds the screen.
// Applied via OnRenderImage (Graphics.Blit) by TimeStopCameraFX.
Shader "Hidden/Deeroot/TimeStopInvertMono"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GateLo ("Scenery luma (at or below = untouched)", Range(0, 1)) = 0.24
        _GateHi ("Gameplay luma (at or above = fully inverted)", Range(0, 1)) = 0.45
    }
    SubShader
    {
        Tags { "Queue" = "Overlay" }
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _GateLo;
            float _GateHi;

            fixed4 frag (v2f_img i) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, i.uv);
                fixed g = dot(c.rgb, fixed3(0.299, 0.587, 0.114));
                // soft ramp between the scenery and gameplay luminance bands:
                // nothing below _GateLo is touched, everything above _GateHi
                // fully negates, smoothstep avoids a hard aliased edge band.
                fixed mask = smoothstep(_GateLo, _GateHi, g);
                fixed3 inv = 1.0 - g;
                return fixed4(lerp(c.rgb, inv, mask), c.a);
            }
            ENDCG
        }
    }
}
