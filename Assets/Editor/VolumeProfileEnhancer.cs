using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class VolumeProfileEnhancer
{
    [InitializeOnLoadMethod]
    public static void EnhanceProfile()
    {
        // One-time execution flag to prevent infinite loops during domain reloads
        if (SessionState.GetBool("VolumeProfileEnhanced", false)) return;
        SessionState.SetBool("VolumeProfileEnhanced", true);

        string profilePath = "Assets/Settings/SampleSceneProfile.asset";
        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);

        if (profile == null)
        {
            Debug.LogError($"[VolumeProfileEnhancer] '{profilePath}' 경로에서 VolumeProfile을 찾을 수 없습니다. 경로를 확인해 주세요.");
            return;
        }

        // 1. Tonemapping (ACES for cinematic filmic look - prevents bright neon from burning to ugly white)
        if (!profile.TryGet(out Tonemapping tonemapping))
            tonemapping = profile.Add<Tonemapping>();
        tonemapping.active = true;
        tonemapping.mode.Override(TonemappingMode.ACES);

        // 2. Bloom (High quality glowing effect for the Neon Glass and Lights)
        if (!profile.TryGet(out Bloom bloom))
            bloom = profile.Add<Bloom>();
        bloom.active = true;
        bloom.intensity.Override(1.8f); // Strong enough to make the outline pop
        bloom.threshold.Override(0.85f);
        bloom.scatter.Override(0.7f);
        bloom.highQualityFiltering.Override(true);
        bloom.tint.Override(Color.white);

        // 3. Color Adjustments (Punchy contrast and vibrant saturation)
        if (!profile.TryGet(out ColorAdjustments colorAdj))
            colorAdj = profile.Add<ColorAdjustments>();
        colorAdj.active = true;
        colorAdj.postExposure.Override(0.15f); // Slight brightness lift
        colorAdj.contrast.Override(15f);      // Crisp contrast
        colorAdj.saturation.Override(8f);     // Rich, popping colors

        // 4. Vignette (Subtle dark frame to focus player's eyes to the center Claw Machine)
        if (!profile.TryGet(out Vignette vignette))
            vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(0.32f);
        vignette.smoothness.Override(0.45f);
        vignette.color.Override(new Color(0.1f, 0.0f, 0.05f)); // Deep dark plum tint instead of pure black

        // 5. Split Toning (Cinematic color grading matching the poster's aesthetic)
        if (!profile.TryGet(out SplitToning splitToning))
            splitToning = profile.Add<SplitToning>();
        splitToning.active = true;
        splitToning.shadows.Override(new Color(0.15f, 0.05f, 0.2f)); // Cool dark purple for shadows
        splitToning.highlights.Override(new Color(1.0f, 0.95f, 0.95f)); // Warm pinkish white for highlights
        splitToning.balance.Override(10f);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();

        Debug.Log("[VolumeProfileEnhancer] SampleSceneProfile에 고퀄리티(AAA급) 포스트 프로세싱 효과가 성공적으로 주입되었습니다! 씬 뷰를 확인해보세요.");
    }
}
