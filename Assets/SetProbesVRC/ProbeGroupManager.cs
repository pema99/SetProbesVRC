
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;
using VRC.SDKBase;
using VRC.Udon;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UnityEditor;
using System.Linq;
#endif

[RequireComponent(typeof(LightProbeGroup))]
public class ProbeGroupManager : UdonSharpBehaviour
{
    [Header("WARNING! DO NOT TOUCH THESE SETTINGS! INTERNAL USE ONLY!")]
    public int probeCount;
    public SphericalHarmonicsL2[] lastSH;
    public Vector4[][] lastShaderCoeffs;
    public MeshRenderer[] probes;
 
    public void SetSHCoefficients(SphericalHarmonicsL2[] sh)
    {
        lastSH = sh;
        if (lastShaderCoeffs == null || lastShaderCoeffs.Length < sh.Length)
        {
            lastShaderCoeffs = new Vector4[sh.Length][];
            for (int i = 0; i < lastShaderCoeffs.Length; i++)
                lastShaderCoeffs[i] = new Vector4[7];
        }
        SHArrayToShaderCoefficientArrays(sh, ref lastShaderCoeffs);
        UploadShaderCoeffs();
    }

    public SphericalHarmonicsL2[] GetSHCoefficients()
    {
        if (lastSH == null)
        {
            lastSH = new SphericalHarmonicsL2[probeCount];
        }

        return lastSH;
    }

    public void SetSHCoefficientsSubset(SphericalHarmonicsL2[] sh, int offset)
    {
        if (lastSH == null || lastSH.Length < sh.Length + offset)
        {
            lastSH = new SphericalHarmonicsL2[sh.Length + offset];
            for (int i = 0; i < lastSH.Length; i++)
                lastSH[i] = new SphericalHarmonicsL2();
        }
        if (lastShaderCoeffs == null || lastShaderCoeffs.Length < sh.Length + offset)
        {
            lastShaderCoeffs = new Vector4[sh.Length + offset][];
            for (int i = 0; i < lastShaderCoeffs.Length; i++)
                lastShaderCoeffs[i] = new Vector4[7];
        }
        System.Array.Copy(sh, 0, lastSH, offset, sh.Length);
        SHArrayToShaderCoefficientArrays(lastSH, ref lastShaderCoeffs);
        UploadShaderCoeffs();
    }

    public SphericalHarmonicsL2[] GetSHCoefficientsSubset(int offset, int length)
    {
        if (lastSH == null)
        {
            lastSH = new SphericalHarmonicsL2[probeCount];
        }

        var newArray = new SphericalHarmonicsL2[length];
        if (offset + length <= lastSH.Length)
            System.Array.Copy(lastSH, offset, newArray, 0, length);
        return newArray;
    }

    // outCoeffs must be length == sh length, with 7 in each staggered
    private void SHArrayToShaderCoefficientArrays(SphericalHarmonicsL2[] sh, ref Vector4[][] outCoeffs)
    {
        for (int i = 0; i < sh.Length; i++)
        {
            SHToShaderCoefficients(ref sh[i], ref outCoeffs[i]);
        }
    }

    // outCoeffs must be size 7
    private void SHToShaderCoefficients(ref SphericalHarmonicsL2 sh, ref Vector4[] outCoeffs)
    {
        // outCoeffs will have this order:
        // [0] = unity_SHAr
        // [1] = unity_SHAg
        // [2] = unity_SHAb
        // [3] = unity_SHBr
        // [4] = unity_SHBg
        // [5] = unity_SHBb
        // [6] = unity_SHC

        for (int i = 0; i < 3; i++)
        {
            outCoeffs[i] = new Vector4(
                sh[i, 3],
                sh[i, 1],
                sh[i, 2],
                sh[i, 0] - sh[i, 6]
            );

            outCoeffs[i + 3] = new Vector4(
                sh[i, 4],
                sh[i, 5],
                sh[i, 6] * 3.0f,
                sh[i, 7]
            );
        }

        outCoeffs[6] = new Vector4(
            sh[0, 8],
            sh[1, 8],
            sh[2, 8],
            1.0f
        );
    }

    private void UploadShaderCoeffs()
    {
        if (probes == null || probes.Length < probeCount)
        {
            var meta = transform.Find("ProbeGroupMetaData");
            if (meta == null)
                return;
            probes = meta.GetComponentsInChildren<MeshRenderer>();
        }

        for (int i = 0; i < probes.Length; i++)
        {
            var mat = probes[i].material;
            mat.SetVector("_SHAr", lastShaderCoeffs[i][0]);
            mat.SetVector("_SHAg", lastShaderCoeffs[i][1]);
            mat.SetVector("_SHAb", lastShaderCoeffs[i][2]);
            mat.SetVector("_SHBr", lastShaderCoeffs[i][3]);
            mat.SetVector("_SHBg", lastShaderCoeffs[i][4]);
            mat.SetVector("_SHBb", lastShaderCoeffs[i][5]);
            mat.SetVector("_SHC", lastShaderCoeffs[i][6]);

            probes[i].UpdateGIMaterials();
        }
    }
}

#if !COMPILER_UDONSHARP && UNITY_EDITOR
[CustomEditor(typeof(ProbeGroupManager))]
public class ProbeGroupManagerEditor : UnityEditor.Editor
{
    public int minimumResolution = 10;

    public override void OnInspectorGUI()
    {        
        if (Lightmapping.bakedGI)
        {
            GUILayout.Label("Due to a bug, realtime GI and baked GI do not work together when using probe lighting.");
            if (GUILayout.Button("Disabled baked GI"))
                Lightmapping.bakedGI = false;
        }

        var man = target as ProbeGroupManager;
        var lp = man.gameObject.GetComponent<LightProbeGroup>();
        string buttonText = "Reinitialize component (Press me after editing the scene)";
        bool needsInit = man.transform.Find("ProbeGroupMetaData") == null;
        if (needsInit)
        {
            GUILayout.Label("This component has not yet been initialized. Press the button below to do so.");
            buttonText = "Initialize component";
        }

        minimumResolution = EditorGUILayout.IntField("Minimum resolution", minimumResolution);

        if (GUILayout.Button(buttonText))
        {
            // Disable auto mode
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;

            // Make sure meta object is attached
            var go = lp.gameObject;
            var meta = go.transform.Find("ProbeGroupMetaData");
            GameObject metaGo;
            if (meta == null)
            {
                metaGo = new GameObject("ProbeGroupMetaData");
                metaGo.transform.SetParent(go.transform);
            }
            else
            {
                metaGo = meta.gameObject;
            }

            // Delete all children of meta object
            foreach (Transform child in metaGo.transform.Cast<Transform>().ToArray())
            {
                GameObject.DestroyImmediate(child.gameObject);
            }

            // Add probe cubes
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SetProbesVRC/FlipCube.fbx");
            var mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SetProbesVRC/FlipCubeMat.mat");
            foreach (var pos in lp.probePositions)
            {
                var probeCubeGo = new GameObject("ProbeCube");
                probeCubeGo.transform.SetParent(metaGo.transform);
                probeCubeGo.transform.position = go.transform.TransformPoint(pos);
                probeCubeGo.isStatic = true;

                var mf = probeCubeGo.AddComponent<MeshFilter>();
                mf.sharedMesh = mesh;

                var mr = probeCubeGo.AddComponent<MeshRenderer>();
                mr.material = mat;
                mr.receiveShadows = false;
                mr.shadowCastingMode = ShadowCastingMode.Off;
                mr.receiveGI = ReceiveGI.LightProbes;
                mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
                mr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }

            // Hide cruft
            SceneVisibilityManager.instance.Hide(metaGo, true);

            // Add data to meta object
            var udon = go.GetOrAddComponent<ProbeGroupManager>();
            udon.probeCount = lp.probePositions.Length;
            udon.lastSH = new SphericalHarmonicsL2[udon.probeCount];
            udon.lastShaderCoeffs = new Vector4[udon.probeCount][];
            for (int i = 0; i < udon.lastShaderCoeffs.Length; i++)
                udon.lastShaderCoeffs[i] = new Vector4[7];
            udon.probes = metaGo.GetComponentsInChildren<MeshRenderer>();

            // TODO: Set lighting settings, do bake
            Lightmapping.realtimeGI = true;
            Lightmapping.indirectOutputScale = Mathf.Max(Lightmapping.indirectOutputScale, minimumResolution);
            Lightmapping.BakeAsync();
        }
        if (!needsInit)
        {
            if (GUILayout.Button("Reset / Deinitialize component (Use me to fix corrupt state)"))
            {
                GameObject.DestroyImmediate(lp.gameObject.transform.Find("ProbeGroupMetaData").gameObject);
            }
        }
    }
}
#endif