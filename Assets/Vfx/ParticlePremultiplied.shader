// One material for every particle in the game. The texture is premultiplied, which lets a single
// blend mode draw two kinds of pixel: where the texture's alpha is zero but its colour is not, the
// pixel adds light (glows, sparkles); where alpha is one, it covers like any opaque shape (splinters,
// confetti). One material is one batch, where an additive and an alpha-blended material would be two.
//
// The vertex colour tints and fades: rgb multiplies the colour, alpha scales the whole pixel - both
// its coverage and the light it adds - so a fading particle fades whichever kind it is.
Shader "Blast/Particle Premultiplied"
{
    Properties
    {
        _MainTex ("Sheet", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "IgnoreProjector" = "True" "PreviewType" = "Plane" }

        Blend One OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 t = tex2D(_MainTex, i.uv);

                fixed4 c;
                c.rgb = t.rgb * i.color.rgb * i.color.a;
                c.a = t.a * i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
