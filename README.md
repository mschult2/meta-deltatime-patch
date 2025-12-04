# Summary

This is an OpenXR feature that replaces `UnityEngine.Time.deltaTime` and `UnityEngine.Time.time`.

This is necessary because the Unity Time APIs incorrectly return the frame production interval instead of the frame delivery interval on Meta Quest, specifically when using the "Unity OpenXR Meta" plugin.  
https://unity.com/blog/engine-platform/fixing-time-deltatime-in-unity-2020-2-for-smoother-gameplay

OVRPlugin has a frame metrics API, but it doesn't work when using the "Unity OpenXR Meta" plugin.

# Instructions

1. Copy *unity/libopenxr_frame_stats.so* to the Android plugins folder in your Unity project.
2. Copy *unity/OpenXRFrameMetricsFeature.cs* into your Unity project.
3. In your Unity project, go to *Project Setting->XR Plug-in Management->OpenXR->All Features* and enable *Frame Stats*.

You can now invoke the APIs of the Frame Metrics OpenXR feature like so:

```
float deltaTime = OpenXRFrameMetricsFeature.FrameDeliveryIntervalSeconds;
```

# Example

If the display refresh rate and target frame rate is 60, but your app is struggling at an average frame rate of 45 FPS, `Time.deltaTime` is typically:

```
0.017 seconds
0.033 seconds
0.017 seconds
0.033 seconds
```
This is the frame delivery interval. Aka the display frame rate; a multiple of vsync; when a new frame is actually latched and presented to the user.


But on Quest, `Time.deltaTime` is:

```
0.022 seconds
0.022 seconds
0.022 seconds
```

This is the frame production interval. Aka the app frame rate; the PlayerLoop interval; when Unity finishes producing a frame and moves on to the next one.
