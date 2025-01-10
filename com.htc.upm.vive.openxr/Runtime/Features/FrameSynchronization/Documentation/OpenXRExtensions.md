# 12.1. XR_HTC_frame_synchronization
## Overview
Traditional, runtime will use the latest frame which will cost jitter. With Frame Synchronization, the render frame will not be discarded for smooth gameplay experience.
However, if the GPU cannot consistently finish rendering on time (rendering more than one vsync at a time), jitter will still occur. Therefore, reducing GPU load is key to smooth gameplay.
## Name String
    XR_HTC_frame_synchronization
## Revision
    1
## New Enum Constants
[XrStructureType](https://registry.khronos.org/OpenXR/specs/1.1/html/xrspec.html#XrStructureType) enumeration is extended with:
- XR_TYPE_FRAME_SYNCHRONIZATION_SESSION_BEGIN_INFO_HTC
## New Enums
- XrFrameSynchronizationModeHTC
## New Structures
- XrFrameSynchronizationSessionBeginInfoHTC

## VIVE Plugin

Enable "VIVE XR Frame Synchronization" in "Project Settings > XR Plugin-in Management > OpenXR > Android Tab > OpenXR Feature Groups" to use the frame synchronization provided by VIVE OpenXR plugin.
