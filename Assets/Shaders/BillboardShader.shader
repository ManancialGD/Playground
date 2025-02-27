Shader "Custom/BillboardShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
            
                // Get the object's world position (ignoring rotation)
                float3 world_pos = unity_ObjectToWorld._m03_m13_m23;
            
                // Get the camera's right and up vectors (for billboard alignment)
                float3 right = UNITY_MATRIX_V[0].xyz; // Camera right vector
                float3 up = UNITY_MATRIX_V[1].xyz;    // Camera up vector
            
                // Reconstruct the quad's position in world space
                float3 local_offset = v.vertex.x * right + v.vertex.y * up;
                float3 billboard_pos = world_pos + local_offset;
            
                // Transform to clip space
                float4 view_pos = mul(UNITY_MATRIX_V, float4(billboard_pos, 1.0));
                float4 clip_pos = mul(UNITY_MATRIX_P, view_pos);
            
                o.vertex = clip_pos;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
            
                UNITY_TRANSFER_FOG(o, o.vertex);
                
                return o;
            }
            

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
