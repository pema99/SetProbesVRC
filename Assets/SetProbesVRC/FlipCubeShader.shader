Shader "Pema99/FlipCubeShader"
{
    Properties
    {
        _SHAr("SHAr", Vector) = (0, 0, 0, 0)
        _SHAg("SHAg", Vector) = (0, 0, 0, 0)
        _SHAb("SHAb", Vector) = (0, 0, 0, 0)
        _SHBr("SHBr", Vector) = (0, 0, 0, 0)
        _SHBg("SHBg", Vector) = (0, 0, 0, 0)
        _SHBb("SHBb", Vector) = (0, 0, 0, 0)
        _SHC("SHC", Vector) = (0, 0, 0, 0)
    }
    SubShader
    {
        Tags { "DisableBatching"="True"}

        Pass
        {
            Name "EMPTY"
            CGPROGRAM
            #pragma vertex empty
            #pragma fragment empty
            void empty() {};
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            ENDCG
        }

        Pass
        {
            Name "META"
            Tags { "LightMode"="Meta" }
            Cull Off
            CGPROGRAM
            #include "UnityStandardMeta.cginc"

            float4 _SHAr;
            float4 _SHAg;
            float4 _SHAb;
            float4 _SHBr;
            float4 _SHBg;
            float4 _SHBb;
            float4 _SHC;

            float3 SHEvalLinearL0L1_ (float4 normal)
            {
                float3 x;
                x.r = dot(_SHAr,normal);
                x.g = dot(_SHAg,normal);
                x.b = dot(_SHAb,normal);

                return x;
            }

            float3 SHEvalLinearL2_ (float4 normal)
            {
                float3 x1, x2;
                float4 vB = normal.xyzz * normal.yzzx;
                x1.r = dot(_SHBr,vB);
                x1.g = dot(_SHBg,vB);
                x1.b = dot(_SHBb,vB);
                half vC = normal.x*normal.x - normal.y*normal.y;
                x2 = _SHC.rgb * vC;
                return x1 + x2;
            }

            // normal should be normalized, w=1.0
            float3 ShadeSH9_ (float4 normal)
            {
                float3 res = SHEvalLinearL0L1_ (normal);
                res += SHEvalLinearL2_ (normal);
                #ifdef UNITY_COLORSPACE_GAMMA
                res = LinearToGammaSpace (res);
                #endif
                return res;
            }

            struct v2f_meta2
            {
                float4 pos      : SV_POSITION;
                float4 uv       : TEXCOORD0;
                #ifdef EDITOR_VISUALIZATION
                float2 vizUV        : TEXCOORD1;
                float4 lightCoord   : TEXCOORD2;
                #endif
                float3 normal       : NORMAL;
            };

            v2f_meta2 vert_meta2 (VertexInput v)
            {
                v2f_meta2 o;
                o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
                o.uv = TexCoords(v);
                o.normal = -UnityObjectToWorldNormal(v.normal);
                #ifdef EDITOR_VISUALIZATION
                o.vizUV = 0;
                o.lightCoord = 0;
                if (unity_VisualizationMode == EDITORVIZ_TEXTURE)
                    o.vizUV = UnityMetaVizUV(unity_EditorViz_UVIndex, v.uv0.xy, v.uv1.xy, v.uv2.xy, unity_EditorViz_Texture_ST);
                else if (unity_VisualizationMode == EDITORVIZ_SHOWLIGHTMASK)
                {
                    o.vizUV = v.uv1.xy * unity_LightmapST.xy + unity_LightmapST.zw;
                    o.lightCoord = mul(unity_EditorViz_WorldToLight, mul(unity_ObjectToWorld, float4(v.vertex.xyz, 1)));
                }
                #endif
                return o;
            }

            float4 frag_meta2 (v2f_meta2 i): SV_Target
            {
                UnityMetaInput o = (UnityMetaInput)0;
                o.Albedo = 0;
                o.Emission = ShadeSH9_(float4(normalize(i.normal), 1)).xyz;
                return UnityMetaFragment(o);
            }

            #pragma vertex vert_meta2
            #pragma fragment frag_meta2
            #pragma shader_feature _EMISSION
            #pragma shader_feature _METALLICGLOSSMAP
            #pragma shader_feature ___ _DETAIL_MULX2
            ENDCG
        }
    }
}
