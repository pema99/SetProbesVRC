
using UdonSharp;
using UnityEngine;
using UnityEngine.Rendering;

// Just an example usage
public class SetProbesExample : UdonSharpBehaviour
{
    public ProbeGroupManager probeGroupManager;

    void Update()
    {
        if (probeGroupManager == null)
            probeGroupManager = GetComponent<ProbeGroupManager>();

        SphericalHarmonicsL2 day = new SphericalHarmonicsL2();
        day[0, 0] = 0.1273175f;
        day[0, 1] = 0.01357105f;
        day[0, 2] = -0.01026866f;
        day[0, 3] = 1.650752E-06f;
        day[0, 4] = 7.895586E-07f;
        day[0, 5] = -0.001595474f;
        day[0, 6] = 0.01324039f;
        day[0, 7] = -1.002063E-05f;
        day[0, 8] = 0.01943598f;
        day[1, 0] =  0.1341476f;
        day[1, 1] = 0.0475531f;
        day[1, 2] =  -0.01798804f;
        day[1, 3] =  3.290644E-06f;
        day[1, 4] = 1.588693E-06f;
        day[1, 5] = -0.003364967f;
        day[1, 6] = 0.01541936f;
        day[1, 7] = -1.171635E-05f; 
        day[1, 8] = 0.02150919f;
        day[2, 0] =  0.1210787f;
        day[2, 1] =  0.07749847f;
        day[2, 2] = -0.02556304f;
        day[2, 3] = 6.000433E-06f;
        day[2, 4] =  2.970611E-06f; 
        day[2, 5] = -0.007014005f;
        day[2, 6] = 0.01311036f;
        day[2, 7] =  -8.570518E-06f; 
        day[2, 8] =  0.01490471f; 

        SphericalHarmonicsL2 night = RenderSettings.ambientProbe;

        SphericalHarmonicsL2 target = day + (night + ((-1)*day)) * (Mathf.Sin(Time.time)*0.5f+0.5f); 

        SphericalHarmonicsL2[] sh = new SphericalHarmonicsL2[8];
        for (int j = 0; j < sh.Length; j++)
            sh[j] = target;
        probeGroupManager.SetSHCoefficients(sh);
    }
}
