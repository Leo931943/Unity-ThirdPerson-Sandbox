using UnityEngine;

namespace ThirdPersonDemo
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("跟隨目標 (Target)")]
        [Tooltip("相機跟隨的角色 Transform")]
        public Transform target;

        [Tooltip("相機焦點高度偏移 (通常對準角色胸口或頭部)")]
        public Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);

        [Header("視角控制 (Look Settings)")]
        [Tooltip("滑鼠水平靈敏度")]
        public float mouseSensitivityX = 1.5f;

        [Tooltip("滑鼠垂直靈敏度")]
        public float mouseSensitivityY = 1.5f;

        [Tooltip("視角轉向平滑阻尼時間 (秒)")]
        public float rotationSmoothTime = 0.04f;

        [Tooltip("仰角最小值 (低角度仰視)")]
        public float minPitch = -20f;

        [Tooltip("仰角最大值 (俯視)")]
        public float maxPitch = 75f;

        [Header("距離與縮放 (Distance & Zoom)")]
        [Tooltip("預設相機與角色距離")]
        public float defaultDistance = 5.0f;

        [Tooltip("最近縮放距離")]
        public float minDistance = 1.2f;

        [Tooltip("最遠縮放距離")]
        public float maxDistance = 10.0f;

        [Tooltip("滾輪縮放速度")]
        public float zoomSpeed = 2.0f;

        [Tooltip("縮放平滑速度")]
        public float zoomSmoothSpeed = 10f;

        [Header("障礙物碰撞檢測 (Collision Avoidance)")]
        [Tooltip("開啟相機防穿牆檢測")]
        public bool enableCollisionAvoidance = true;

        [Tooltip("相機碰撞半徑")]
        public float cameraRadius = 0.25f;

        [Tooltip("碰撞遮罩層 (避免穿透牆壁或地板)")]
        public LayerMask obstacleLayers = ~0;

        // 旋轉內部數值
        private float yaw;
        private float pitch = 15f;
        private Vector3 currentRotationVelocity;
        private Vector3 currentEulerAngles;

        // 距離內部數值
        private float targetDistance;
        private float currentDistance;

        // 滑鼠游標狀態
        private bool isCursorLocked = true;

        private void Start()
        {
            targetDistance = defaultDistance;
            currentDistance = defaultDistance;

            if (target != null)
            {
                yaw = target.eulerAngles.y;
            }

            SetCursorState(true);
        }

        private void Update()
        {
            HandleCursorToggle();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            HandleRotationInput();
            HandleZoomInput();
            UpdateCameraTransform();
        }

        private void HandleCursorToggle()
        {
            if (PlayerInputBridge.GetCursorToggleDown())
            {
                SetCursorState(!isCursorLocked);
            }

            if (!isCursorLocked && PlayerInputBridge.GetMouseClickDown())
            {
                SetCursorState(true);
            }
        }

        public void SetCursorState(bool locked)
        {
            isCursorLocked = locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        private void HandleRotationInput()
        {
            if (!isCursorLocked) return;

            Vector2 lookInput = PlayerInputBridge.GetLookInput();

            yaw += lookInput.x * mouseSensitivityX;
            pitch -= lookInput.y * mouseSensitivityY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // 平滑角度過渡
            Vector3 targetEuler = new Vector3(pitch, yaw, 0f);
            currentEulerAngles = new Vector3(
                Mathf.SmoothDampAngle(currentEulerAngles.x, targetEuler.x, ref currentRotationVelocity.x, rotationSmoothTime),
                Mathf.SmoothDampAngle(currentEulerAngles.y, targetEuler.y, ref currentRotationVelocity.y, rotationSmoothTime),
                0f
            );
        }

        private void HandleZoomInput()
        {
            float zoom = PlayerInputBridge.GetZoomInput();
            if (Mathf.Abs(zoom) > 0.01f)
            {
                targetDistance = Mathf.Clamp(targetDistance - zoom * zoomSpeed, minDistance, maxDistance);
            }

            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * zoomSmoothSpeed);
        }

        private void UpdateCameraTransform()
        {
            Quaternion rotation = Quaternion.Euler(currentEulerAngles);
            Vector3 focusPoint = target.position + targetOffset;

            // 計算期望相機位置
            float desiredDistance = currentDistance;

            if (enableCollisionAvoidance)
            {
                Vector3 direction = (rotation * Vector3.back).normalized;

                // 使用球體射線檢測，避免相機穿入牆壁或地板
                if (Physics.SphereCast(focusPoint, cameraRadius, direction, out RaycastHit hit, currentDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
                {
                    // 稍微保留一點緩衝距離
                    desiredDistance = Mathf.Clamp(hit.distance - 0.05f, minDistance, currentDistance);
                }
            }

            Vector3 finalPosition = focusPoint - (rotation * Vector3.forward * desiredDistance);

            transform.rotation = rotation;
            transform.position = finalPosition;
        }
    }
}
