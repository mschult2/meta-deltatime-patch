# Summary

This is an OpenXR feature that replaces `UnityEngine.Time.deltaTime` and `UnityEngine.Time.time`.  
This is necessary because the Unity Time APIs incorrectly return the PlayerLoop interval instead of the frame delivery interval on Meta Quest, specifically when using the "Unity OpenXR Meta" plugin.

# Instructions

1. Copy *unity/libopenxr_frame_stats.so* to the Android plugins folder in your Unity project.
2. Copy *unity/OpenXRFrameMetricsFeature.cs* into your Unity project.
3. In your Unity project, go to *Project Setting->XR Plug-in Management->OpenXR->All Features* and enable *Frame Stats*.

You can now invoke the APIs of the Frame Metrics OpenXR feature like so:

```
float deltaTime = OpenXRFrameMetricsFeature.FrameDeliveryIntervalSeconds;
```
