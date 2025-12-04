# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Unity 2022.3.62f2c1 project for PICO VR headsets (PCVR mode), focused on tracking and transmitting VR pose data to a remote server. The project implements real-time data streaming of head and controller tracking information, as well as bidirectional haptic feedback control.

## Key Technologies

- **Unity XR Interaction Toolkit** (v3.3.0) - Core XR input system
- **PICO Unity Integration SDK** (v3.3.2) - Local PICO SDK integration
- **Unity Input System** - Action-based input handling
- **OpenXR** (v1.14.3) - Cross-platform XR runtime

## Architecture Overview

### Data Flow Architecture

The project follows a **hub-and-spoke pattern** with three main components:

1. **DataTracking.cs** - Central data collection hub
   - Subscribes to Unity Input System actions for head and controller tracking
   - Aggregates position, rotation, velocity, angular velocity, and button states
   - Continuously sends JSON payloads to server via HTTPS POST to `/poseData`
   - Location: `Assets/Scripts/DataTracking/DataTracking.cs`

2. **HapticMessageReceiver.cs** - Bidirectional haptic feedback handler
   - Polls server at regular intervals (GET `/msg`)
   - Processes vibration commands with format: `{"id":"vibrate", "data":{"side":"left|right|both", "intensity":0-1, "duration":seconds}}`
   - Triggers PICO native haptic API and fallback PCVR-compatible haptic impulses
   - Location: `Assets/Scripts/DataTracking/HapticMessageReceiver.cs`

3. **UIController.cs** - Runtime configuration interface
   - Dynamically generates WorldSpace Canvas UI at runtime (no prefabs)
   - Provides input field for server URL configuration (format: `IP:PORT`)
   - Persists server URL to PlayerPrefs for session continuity
   - Supports XR ray interaction via TrackedDeviceGraphicRaycaster
   - Location: `Assets/Scripts/UI/UIController.cs`

### Data Structures (SendVRData.cs)

The VR data packet structure:
```csharp
SendVRData {
    string state, int battery,
    HeadInfo head { position, rotation, linearVelocity, angularVelocity },
    ControllerInfo left { position, rotation, velocities, button[7], axes[4] },
    ControllerInfo right { position, rotation, velocities, button[7], axes[4] },
    long timestamp
}
```

**Critical Details:**
- All velocities use `Vector4Data` (with `w=0` padding)
- Button arrays have 7 slots: index 1 = Grip, index 4 = A, index 5 = B
- Axes arrays are always `[0,0,0,0]` (placeholder for future joystick support)

### Communication Protocol

- **Outbound (VR → Server):** Continuous POST to `https://{serverBaseUrl}/poseData` with JSON payload
- **Inbound (Server → VR):** Polling GET to `https://{serverBaseUrl}/msg` (returns vibration commands)
- **SSL Handling:** Uses `CustomCertificateHandler` to bypass certificate validation (development only)

## Common Development Commands

### Building the Project

**Open in Unity Editor:**
```bash
"C:\Program Files\Unity\Hub\Editor\2022.3.62f2c1\Editor\Unity.exe" -projectPath "C:\work_project\pico_project\03_pico_pcvr"
```

**Build for PICO (Android):**
- File → Build Settings → Android
- Switch Platform (if needed)
- XR Plug-in Management: Ensure PICO XR is enabled
- Build And Run (requires PICO device connected via USB with PCVR mode enabled)

### Testing

**Test in Unity Editor (Play Mode):**
- Use XR Device Simulator or PICO LivePreview plugin
- Check Console for debug logs prefixed with emojis (🎮, 📨, ✅, ⚠️)

**Test Haptics without Server:**
- In HapticMessageReceiver.cs:72-76, uncomment fake data block to trigger test vibrations

**Manual Vibration Test:**
- Right-click HapticMessageReceiver component in Inspector
- Use Context Menu: "测试：震动右手柄" / "测试：震动左手柄" / "测试：震动双手柄"

### Server Integration

**Expected Server Endpoints:**
- `POST /poseData` - Receives VR tracking data (Content-Type: application/json)
- `GET /msg` - Returns pending messages (empty string or JSON with `id` and `data`)

**URL Configuration:**
- Runtime: Use in-VR UI input field
- Programmatically: Modify `UIController.serverBaseUrl` (automatically prepends `https://`)
- Persistent: Stored in PlayerPrefs key "ServerBaseUrl"

## Important Implementation Notes

### Input System Integration

- All tracking uses `InputActionReference` instead of direct device queries
- Actions must be **enabled in Awake()** and subscribed in OnEnable()
- Button state is tracked via `performed` and `canceled` callbacks
- Fallback to cached values when actions are disabled

### PICO-Specific Behaviors

- **Video See-Through:** Enabled in DataTracking.Awake() via `PXR_Manager.EnableVideoSeeThrough = true`
- **Haptic Frequency:** Fixed at 200Hz for vibrations (PICO SDK requirement)
- **PCVR Compatibility:** All haptic calls have fallback using Unity's standard `UnityEngine.XR.InputDevice.SendHapticImpulse()`

### UI System Quirks

- UI is **entirely code-generated** (no prefabs or scene Canvas)
- Canvas uses WorldSpace render mode with position calculated from main camera
- Requires EventSystem with StandaloneInputModule (auto-created if missing)
- TrackedDeviceGraphicRaycaster added via reflection to support XRI ray interactors

### Known Issues

- DataTracking.cs:292 has typo: `GetLetfRotation()` (should be `GetLeftRotation`)
- Update() sends data every frame despite commented-out interval check (lines 436-440)
- CustomCertificateHandler accepts all certificates (insecure for production)

## File Organization

```
Assets/Scripts/
├── DataTracking/
│   ├── DataTracking.cs          # Main tracking and network sender
│   ├── SendVRData.cs            # Data structure definitions
│   └── HapticMessageReceiver.cs # Haptic feedback receiver
└── UI/
    ├── UIController.cs           # Runtime UI generation + config
    └── UIControl.cs              # Legacy simple counter UI (not used in main flow)
```

## Debugging Tips

- **Enable verbose logging:** Set `HapticMessageReceiver.verboseLogging = true` in Inspector
- **Check network failures:** Look for "发送VR数据失败" error logs with response codes
- **Verify URL format:** Final URL should be `https://IP:PORT/poseData` or `https://IP:PORT/msg`
- **Button state debugging:** Use `DataTracking.TestGenerateJSON()` context menu when buttons pressed
