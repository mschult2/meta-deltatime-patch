using System;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif


/// <summary>
/// OpenXR feature that exposes the frame delivery interval. What Time.deltaTime is supposed to do, but is broken on some XR platforms.
/// Only supports Android.
/// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(
        UiName = FeatureName,
        Desc = "Exposes OpenXR frame delivery interval. What Time.deltaTime does on traditional platforms, but is broken on some XR platforms.",
        Company = "Niftysoft",
        BuildTargetGroups = new[] { BuildTargetGroup.Android },
        FeatureId = FeatureId,
        Priority = 900
    )]
#endif
public class OpenXRFrameMetricsFeature : OpenXRFeature
{
    public const string FeatureId = "com.niftysoft.openxr.feature.framestats";
    internal const string FeatureName = "Frame Stats";

#if UNITY_ANDROID && !UNITY_EDITOR
        // -------- Native plugin bindings --------

        // Called from HookGetInstanceProcAddr to let the native plugin intercept xrGetInstanceProcAddr.
        [DllImport("openxr_frame_stats", EntryPoint = "fm_hook_get_instance_proc_addr")]
        private static extern IntPtr Native_HookGetInstanceProcAddr(IntPtr original);

        // Last frame delivery interval, in seconds. Based on the last predicted display time.
        [DllImport("openxr_frame_stats", EntryPoint = "fm_get_last_frame_period_seconds")]
        private static extern double Native_GetLastFramePeriodSeconds();

        // Last cumulative time (multiple of frame delivery interval). The last predicted display time.
        [DllImport("openxr_frame_stats", EntryPoint = "fm_get_last_frame_time_seconds")]
        private static extern double Native_GetLastFrameTimeSeconds();
#endif

    private static OpenXRFrameMetricsFeature _instance;

    public static bool IsRunning => _instance != null && _instance.enabled;

    /// <summary>
    /// A drop-in replacement for <see cref="UnityEngine.Time.deltaTime" />.
    /// Measures the interval between the last two latched frames.
    /// </summary>
    public static float FrameDeliveryIntervalSeconds
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!IsRunning) return 0f;
                return (float)Native_GetLastFramePeriodSeconds();
#else
            return UnityEngine.Time.deltaTime;
#endif
        }
    }

    /// <summary>
    /// A drop-in replacement for <see cref="UnityEngine.Time.time" />.
    /// Current time, measured at latched frame intervals.
    /// </summary>
    public static float FrameDeliveryTimeSeconds
    {
        get
        {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!IsRunning) return 0f;
                return (float)Native_GetLastFrameTimeSeconds();
#else
            return UnityEngine.Time.time;
#endif
        }
    }

    /// <summary>
    /// FPS. Inverse of <see cref="FrameDeliveryIntervalSeconds" />. Useful for debugging.
    /// </summary>
    public static float FrameDeliveryRate => FrameDeliveryIntervalSeconds > 0f ? 1f / FrameDeliveryIntervalSeconds : 0f;

    protected override void OnEnable()
    {
        base.OnEnable();
        _instance = this;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (_instance == this)
            _instance = null;
    }

    // This is where Unity gives us the pointer to xrGetInstanceProcAddr. We hand it to native so it can wrap it and intercept xrWaitFrame.
    protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Native_HookGetInstanceProcAddr(func);
#else
        return func;
#endif
    }
}
