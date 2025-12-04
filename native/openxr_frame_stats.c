#define XR_USE_PLATFORM_ANDROID
#include <jni.h>
#include <openxr/openxr.h>
#include <openxr/openxr_platform.h>

#include <string.h>

// ---- Export macro ----
#ifdef _WIN32
#define FM_EXPORT __declspec(dllexport)
#else
#define FM_EXPORT __attribute__((visibility("default")))
#endif

// ---- Global state ----

// The real xrGetInstanceProcAddr provided by the runtime.
static PFN_xrGetInstanceProcAddr g_nextGetInstanceProcAddr = NULL;

// The real xrWaitFrame we will call after intercepting.
static PFN_xrWaitFrame g_real_xrWaitFrame = NULL;

// Last predicted display time and period (OpenXR timebase: nanoseconds).
static XrTime  g_prevPredictedDisplayTime = 0;
static double  g_lastFramePeriodSeconds = 0.0;
static double  g_lastPredictedDisplayTimeSec = 0.0;

// Convert OpenXR time (nanoseconds) to seconds.
static inline double XrTimeToSeconds(XrTime t)
{
    return (double)t / 1e9;
}

// ---- Our xrWaitFrame wrapper ----

static XrResult XRAPI_PTR Fm_xrWaitFrame(XrSession session, const XrFrameWaitInfo* frameWaitInfo, XrFrameState* frameState)
{
    if (!g_real_xrWaitFrame)
    {
        // Should not happen; if it does, just bail.
        return XR_ERROR_FUNCTION_UNSUPPORTED;
    }

    XrResult r = g_real_xrWaitFrame(session, frameWaitInfo, frameState);

    if (r == XR_SUCCESS && frameState != NULL)
    {
        XrTime t = frameState->predictedDisplayTime;

        if (g_prevPredictedDisplayTime != 0)
        {
            XrTime delta = t - g_prevPredictedDisplayTime;
            // Ignore negative / zero deltas; those would be bogus.
            if (delta > 0)
            {
                g_lastFramePeriodSeconds = XrTimeToSeconds(delta);
            }
        }

        g_prevPredictedDisplayTime = t;
        g_lastPredictedDisplayTimeSec = XrTimeToSeconds(t);
    }

    return r;
}

// ---- Our xrGetInstanceProcAddr wrapper ----
static XrResult XRAPI_PTR Fm_xrGetInstanceProcAddr(XrInstance instance, const char* name, PFN_xrVoidFunction* function)
{
    if (!g_nextGetInstanceProcAddr)
        return XR_ERROR_FUNCTION_UNSUPPORTED;

    XrResult r = g_nextGetInstanceProcAddr(instance, name, function);
    if (r != XR_SUCCESS || !function || !*function)
        return r;

    // We only care about xrWaitFrame; everything else is left as provided.
    if (strcmp(name, "xrWaitFrame") == 0)
    {
        g_real_xrWaitFrame = (PFN_xrWaitFrame)(*function);
        *function = (PFN_xrVoidFunction)Fm_xrWaitFrame;
    }

    return XR_SUCCESS;
}

// ---- Exports used by C# ----

// Called by HookGetInstanceProcAddr(C#) with the runtime's original xrGetInstanceProcAddr.
FM_EXPORT void* fm_hook_get_instance_proc_addr(void* original)
{
    g_nextGetInstanceProcAddr = (PFN_xrGetInstanceProcAddr)original;

    if (!g_nextGetInstanceProcAddr)
        return original; // nothing we can do

    // Return our wrapper so Unity/OpenXR will call us instead.
    return (void*)Fm_xrGetInstanceProcAddr;
}

// Return last measured frame period (delta between predictedDisplayTime calls) in seconds.
FM_EXPORT double fm_get_last_frame_period_seconds()
{
    return g_lastFramePeriodSeconds;
}

// Return last predicted display time in seconds (OpenXR's monotonic timebase).
FM_EXPORT double fm_get_last_frame_time_seconds()
{
    return g_lastPredictedDisplayTimeSec;
}
