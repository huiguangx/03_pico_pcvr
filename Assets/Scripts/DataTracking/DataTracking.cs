using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using Unity.XR.PXR;

namespace DataTracking
{
    public class DataTracking : MonoBehaviour
    {
        [Header("XRI Default Input Actions 中的 Head Position/Rotation Action")]
        public InputActionReference deviceHeadPositionRef;
        public InputActionReference deviceHeadRotationRef;
        public InputActionReference deviceHeadVelocityRef;
        public InputActionReference deviceHeadAngularVelocityRef;

        [Header("Left Hand")]
        public InputActionReference leftPositionRef;
        public InputActionReference leftRotationRef;
        public InputActionReference leftVelocityRef;
        public InputActionReference leftAngularVelocityRef;
        public InputActionReference leftGripRef;

        [Header("Right Hand")]
        public InputActionReference rightPositionRef;
        public InputActionReference rightRotationRef;
        public InputActionReference rightVelocityRef;
        public InputActionReference rightAngularVelocityRef;
        public InputActionReference rightAButtonRef;
        public InputActionReference rightBButtonRef;
        public InputActionReference rightGripRef;

        // Head
        private Vector3 _headPosition = Vector3.zero;
        private Quaternion _headRotation = Quaternion.identity;
        private Vector3 _headVelocity = Vector3.zero;
        private Vector3 _headAngularVelocity = Vector3.zero;

        // Left Hand
        private Vector3 _leftPosition = Vector3.zero;
        private Quaternion _leftRotation = Quaternion.identity;
        private Vector3 _leftVelocity = Vector3.zero;
        private Vector3 _leftAngularVelocity = Vector3.zero;

        // Right Hand
        private Vector3 _rightPosition = Vector3.zero;
        private Quaternion _rightRotation = Quaternion.identity;
        private Vector3 _rightVelocity = Vector3.zero;
        private Vector3 _rightAngularVelocity = Vector3.zero;
        // Left Buttons: 7 个按钮状态（index 2 = Grip）
        private ButtonState[] _leftButtons;
        // Right Buttons: 7 个按钮状态（index 4 = A, index 5 = B）
        private ButtonState[] _rightButtons;

        [Header("Network Settings")]
        [Tooltip("服务器完整 URL (从 UIController 自动获取)")]
        [SerializeField]
        private string serverUrl = "https://localhost:5000/poseData"; // 仅显示，实际从 UIController 获取
        private float lastSendTime = 0f;
        public float sendInterval = 0.1f; // 发送间隔（秒）

        private UIController uiController;

        private void Awake()
            {
                PXR_Manager.EnableVideoSeeThrough = true;
                Debug.Log("✅ 11111" + JsonUtility.ToJson(PXR_Manager.EnableVideoSeeThrough));
                
                // 初始化按钮数组
                _leftButtons = new ButtonState[7];
                _rightButtons = new ButtonState[7];
                for (int i = 0; i < 7; i++)
                {
                    _leftButtons[i] = new ButtonState();
                    _rightButtons[i] = new ButtonState();
                }

                // Head
                EnableAction(deviceHeadPositionRef);
                EnableAction(deviceHeadRotationRef);
                EnableAction(deviceHeadVelocityRef);
                EnableAction(deviceHeadAngularVelocityRef);

                // Left
                EnableAction(leftPositionRef);
                EnableAction(leftRotationRef);
                EnableAction(leftVelocityRef);
                EnableAction(leftAngularVelocityRef);
                EnableAction(leftGripRef); // 👈

                // Right
                EnableAction(rightPositionRef);
                EnableAction(rightRotationRef);
                EnableAction(rightVelocityRef);
                EnableAction(rightAngularVelocityRef);
                EnableAction(rightAButtonRef);
                EnableAction(rightBButtonRef);
                EnableAction(rightGripRef); // 👈

                // 获取 UIController 引用
                uiController = UnityEngine.Object.FindObjectOfType<UIController>();
                if (uiController == null)
                {
                    Debug.LogWarning("⚠️ 未找到 UIController，将使用默认 serverUrl");
                }
            }

        private void OnEnable()
        {
            // Head
            SubscribeVector3(deviceHeadPositionRef, v => _headPosition = v);
            SubscribeQuaternion(deviceHeadRotationRef, q => _headRotation = q);
            SubscribeVector3(deviceHeadVelocityRef, v => _headVelocity = v);
            SubscribeVector3(deviceHeadAngularVelocityRef, v => _headAngularVelocity = v);

            // Left
            SubscribeVector3(leftPositionRef, v => _leftPosition = v);
            SubscribeQuaternion(leftRotationRef, q => _leftRotation = q);
            SubscribeVector3(leftVelocityRef, v => _leftVelocity = v);
            SubscribeVector3(leftAngularVelocityRef, v => _leftAngularVelocity = v);

            // Right
            SubscribeVector3(rightPositionRef, v => _rightPosition = v);
            SubscribeQuaternion(rightRotationRef, q => _rightRotation = q);
            SubscribeVector3(rightVelocityRef, v => _rightVelocity = v);
            SubscribeVector3(rightAngularVelocityRef, v => _rightAngularVelocity = v);

            // Right A Button → index 4
            if (rightAButtonRef != null)
            {
                var action = rightAButtonRef.action;
                action.performed += _ => {
                    _rightButtons[4].pressed = true;
                    _rightButtons[4].value = 1f;
                };
                action.canceled += _ => {
                    _rightButtons[4].pressed = false;
                    _rightButtons[4].value = 0f;
                };
            }

            // Right B Button → index 5
            if (rightBButtonRef != null)
            {
                var action = rightBButtonRef.action;
                action.performed += ctx => {
                    _rightButtons[5].pressed = true;
                    _rightButtons[5].value = 1f;

                    Debug.Log("🎮 B键按下！");

                    // 简单直接的震动
                    PXR_Input.SendHapticImpulse(
                        PXR_Input.VibrateType.RightController,
                        0.8f,   // 强度
                        300,    // 时长 ms
                        200     // 频率 Hz
                    );

                    // PCVR 兼容震动
                    TriggerHapticForPCVR(ctx);
                };
                action.canceled += _ => {
                    _rightButtons[5].pressed = false;
                    _rightButtons[5].value = 0f;
                };
            }
            // Left Grip → index 2
            if (leftGripRef != null)
            {
                var action = leftGripRef.action;
                action.performed += _ => {
                    _leftButtons[1].pressed = true;
                    _leftButtons[1].value = 1f;
                };
                action.canceled += _ => {
                    _leftButtons[1].pressed = false;
                    _leftButtons[1].value = 0f;
                };
            }

            // Right Grip → index 2
            if (rightGripRef != null)
            {
                var action = rightGripRef.action;
                action.performed += _ => {
                    _rightButtons[1].pressed = true;
                    _rightButtons[1].value = 1f;
                };
                action.canceled += _ => {
                    _rightButtons[1].pressed = false;
                    _rightButtons[1].value = 0f;
                };
            }
        }

        private void OnDisable()
        {
            // Head
            DisableAction(deviceHeadPositionRef);
            DisableAction(deviceHeadRotationRef);
            DisableAction(deviceHeadVelocityRef);
            DisableAction(deviceHeadAngularVelocityRef);

            // Left
            DisableAction(leftPositionRef);
            DisableAction(leftRotationRef);
            DisableAction(leftVelocityRef);
            DisableAction(leftAngularVelocityRef);

            // Right
            DisableAction(rightPositionRef);
            DisableAction(rightRotationRef);
            DisableAction(rightVelocityRef);
            DisableAction(rightAngularVelocityRef);

            DisableAction(rightAButtonRef);
            DisableAction(rightBButtonRef);

            DisableAction(leftGripRef);
            DisableAction(rightGripRef);
        }

        // --- Helper Methods ---
        private void EnableAction(InputActionReference actionRef)
        {
            actionRef?.action?.Enable();
        }

        private void DisableAction(InputActionReference actionRef)
        {
            actionRef?.action?.Disable();
        }

        private void SubscribeVector3(InputActionReference actionRef, System.Action<Vector3> callback)
        {
            if (actionRef != null)
                actionRef.action.performed += ctx => callback(ctx.ReadValue<Vector3>());
        }

        private void SubscribeQuaternion(InputActionReference actionRef, System.Action<Quaternion> callback)
        {
            if (actionRef != null)
                actionRef.action.performed += ctx => callback(ctx.ReadValue<Quaternion>());
        }

        /// <summary>
        /// PCVR 模式震动支持
        /// </summary>
        private void TriggerHapticForPCVR(InputAction.CallbackContext ctx)
        {
            try
            {
                // 使用 Unity XR 标准 API（PCVR 兼容）
                var xrDevices = new System.Collections.Generic.List<UnityEngine.XR.InputDevice>();
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                    UnityEngine.XR.InputDeviceCharacteristics.Controller |
                    UnityEngine.XR.InputDeviceCharacteristics.Right,
                    xrDevices
                );

                foreach (var device in xrDevices)
                {
                    if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                    {
                        device.SendHapticImpulse(0, 0.8f, 0.3f);
                        Debug.Log($"✅ PCVR 震动发送到: {device.name}");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"⚠️ PCVR 震动失败: {e.Message}");
            }
        }

        // --- Getters (fallback to cached values if action disabled) ---
        public Vector3 GetHeadPosition() =>
            IsActionEnabled(deviceHeadPositionRef) ? deviceHeadPositionRef.action.ReadValue<Vector3>() : _headPosition;

        public Quaternion GetHeadRotation() =>
            IsActionEnabled(deviceHeadRotationRef) ? deviceHeadRotationRef.action.ReadValue<Quaternion>() : _headRotation;

        public Vector3 GetHeadVelocity() =>
            IsActionEnabled(deviceHeadVelocityRef) ? deviceHeadVelocityRef.action.ReadValue<Vector3>() : _headVelocity;

        public Vector3 GetHeadAngularVelocity() =>
            IsActionEnabled(deviceHeadAngularVelocityRef) ? deviceHeadAngularVelocityRef.action.ReadValue<Vector3>() : _headAngularVelocity;

        public Vector3 GetLeftPosition() =>
            IsActionEnabled(leftPositionRef) ? leftPositionRef.action.ReadValue<Vector3>() : _leftPosition;

        public Quaternion GetLetfRotation() =>
            IsActionEnabled(leftRotationRef) ? leftRotationRef.action.ReadValue<Quaternion>() : _leftRotation;

        public Vector3 GetLeftVelocity() =>
            IsActionEnabled(leftVelocityRef) ? leftVelocityRef.action.ReadValue<Vector3>() : _leftVelocity;

        public Vector3 GetLeftAngularVelocity() =>
            IsActionEnabled(leftAngularVelocityRef) ? leftAngularVelocityRef.action.ReadValue<Vector3>() : _leftAngularVelocity;

        public Vector3 GetRightPosition() =>
            IsActionEnabled(rightPositionRef) ? rightPositionRef.action.ReadValue<Vector3>() : _rightPosition;

        public Quaternion GetRightRotation() =>
            IsActionEnabled(rightRotationRef) ? rightRotationRef.action.ReadValue<Quaternion>() : _rightRotation;

        public Vector3 GetRightVelocity() =>
            IsActionEnabled(rightVelocityRef) ? rightVelocityRef.action.ReadValue<Vector3>() : _rightVelocity;

        public Vector3 GetRightAngularVelocity() =>
            IsActionEnabled(rightAngularVelocityRef) ? rightAngularVelocityRef.action.ReadValue<Vector3>() : _rightAngularVelocity;

        private bool IsActionEnabled(InputActionReference actionRef) =>
            actionRef?.action?.enabled == true;

        private void SendVRDataToServer()
        {
            var data = new SendVRData();

            // Head
            data.head.position = new Vector3Data(GetHeadPosition());
            data.head.rotation = new QuaternionData(GetHeadRotation());
            data.head.linearVelocity = new Vector4Data(GetHeadVelocity());      // ✅ Vector4Data
            data.head.angularVelocity = new Vector4Data(GetHeadAngularVelocity()); // ✅

            // Left
            data.left.position = new Vector3Data(GetLeftPosition());
            data.left.rotation = new QuaternionData(GetLetfRotation());
            data.left.linearVelocity = new Vector4Data(GetLeftVelocity());       // ✅
            data.left.angularVelocity = new Vector4Data(GetLeftAngularVelocity()); // ✅
            // left.button 保持默认（全 false）
            // left.axes 已在构造函数中初始化为 [0,0,0,0]

            // Right
            data.right.position = new Vector3Data(GetRightPosition());
            data.right.rotation = new QuaternionData(GetRightRotation());
            data.right.linearVelocity = new Vector4Data(GetRightVelocity());     // ✅
            data.right.angularVelocity = new Vector4Data(GetRightAngularVelocity()); // ✅

            // 深拷贝按钮状态
            // Left buttons
            data.left.button = new ButtonState[_leftButtons.Length];
            for (int i = 0; i < _leftButtons.Length; i++)
            {
                var src = _leftButtons[i];
                data.left.button[i] = new ButtonState
                {
                    value = src.value,
                    pressed = src.pressed,
                    touched = src.touched
                };
            }

            // Right buttons
            data.right.button = new ButtonState[_rightButtons.Length];
            for (int i = 0; i < _rightButtons.Length; i++)
            {
                var src = _rightButtons[i];
                data.right.button[i] = new ButtonState
                {
                    value = src.value,
                    pressed = src.pressed,
                    touched = src.touched
                };
            }

            // axes 不需要赋值，默认就是 [0,0,0,0]

            data.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            string json = JsonUtility.ToJson(data, true);
       
            // 发送到服务器
            StartCoroutine(PostDataToServer(json));
        }

        private IEnumerator PostDataToServer(string jsonData)
        {
            
            // 从 UIController 获取基础地址并拼接完整 URL
            string url = serverUrl; // 默认值
            if (uiController != null)
            {
                url = "https://" + uiController.serverBaseUrl + "/poseData";
            }
            // 检查URL是否有效
            if (string.IsNullOrEmpty(url))
            {
                Debug.LogError("服务器URL为空");
                yield break;
            }

            var request = new UnityEngine.Networking.UnityWebRequest(url, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // 忽略SSL证书错误（仅用于开发环境）
            request.certificateHandler = new CustomCertificateHandler();
            request.disposeCertificateHandlerOnDispose = true;

            // Debug.Log("正在发送请求到: " + url);

            yield return request.SendWebRequest();

            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.LogError("发送VR数据失败. 错误信息1: " + request.error +
                              "\n响应代码: " + request.responseCode +
                              "\nURL: " + url);
            }
            else
            {
                Debug.Log("成功发送VR数据到服务器. 响应代码: " + '-' + url + '-' + request.responseCode);
            }

            request.Dispose();
        }

        void Update()
        {
            // 更新 Inspector 显示的 URL（从 UIController 同步）
            if (uiController != null)
            {
                serverUrl = "https://" + uiController.serverBaseUrl + "/poseData";
            }

            // 可选：每帧更新缓存（确保最新值）
            if (IsActionEnabled(deviceHeadPositionRef))
                _headPosition = deviceHeadPositionRef.action.ReadValue<Vector3>();
            if (IsActionEnabled(deviceHeadRotationRef))
                _headRotation = deviceHeadRotationRef.action.ReadValue<Quaternion>();

            // 直接在Update中发送数据
            // if (Time.time - lastSendTime >= sendInterval)
            // {
                SendVRDataToServer();
                lastSendTime = Time.time;
            // }
        }

        [ContextMenu("Test Generate JSON")]
        void TestGenerateJSON()
            {
                bool anyPressed = 
                _rightButtons[4].pressed || _rightButtons[5].pressed || _rightButtons[1].pressed ||
                _leftButtons[1].pressed;

                if (anyPressed)
                {
                    var data = new SendVRData();

                    // Head
                    data.head.position = new Vector3Data(GetHeadPosition());
                    data.head.rotation = new QuaternionData(GetHeadRotation());
                    data.head.linearVelocity = new Vector4Data(GetHeadVelocity());      // ✅ Vector4Data
                    data.head.angularVelocity = new Vector4Data(GetHeadAngularVelocity()); // ✅

                    // Left
                    data.left.position = new Vector3Data(GetLeftPosition());
                    data.left.rotation = new QuaternionData(GetLetfRotation());
                    data.left.linearVelocity = new Vector4Data(GetLeftVelocity());       // ✅
                    data.left.angularVelocity = new Vector4Data(GetLeftAngularVelocity()); // ✅
                    // left.button 保持默认（全 false）
                    // left.axes 已在构造函数中初始化为 [0,0,0,0]

                    // Right
                    data.right.position = new Vector3Data(GetRightPosition());
                    data.right.rotation = new QuaternionData(GetRightRotation());
                    data.right.linearVelocity = new Vector4Data(GetRightVelocity());     // ✅
                    data.right.angularVelocity = new Vector4Data(GetRightAngularVelocity()); // ✅

                    // 深拷贝按钮状态
                    // Left buttons
                    data.left.button = new ButtonState[_leftButtons.Length];
                    for (int i = 0; i < _leftButtons.Length; i++)
                    {
                        var src = _leftButtons[i];
                        data.left.button[i] = new ButtonState
                        {
                            value = src.value,
                            pressed = src.pressed,
                            touched = src.touched
                        };
                    }

                    // Right buttons
                    data.right.button = new ButtonState[_rightButtons.Length];
                    for (int i = 0; i < _rightButtons.Length; i++)
                    {
                        var src = _rightButtons[i];
                        data.right.button[i] = new ButtonState
                        {
                            value = src.value,
                            pressed = src.pressed,
                            touched = src.touched
                        };
                    }

                    // axes 不需要赋值，默认就是 [0,0,0,0]

                    data.timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

                    string json = JsonUtility.ToJson(data, true);
                    Debug.Log("✅ A/B 按下中 - VR 数据:\n" + json);
                }
            }
    }

    // 自定义证书处理程序，用于忽略SSL证书错误
    public class CustomCertificateHandler : UnityEngine.Networking.CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // 在开发环境中忽略证书验证
            // 注意：在生产环境中不应忽略证书验证
            return true;
        }
    }
}