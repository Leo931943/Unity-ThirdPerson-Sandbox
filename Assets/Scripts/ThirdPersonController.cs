using UnityEngine;

namespace ThirdPersonDemo
{
    [RequireComponent(typeof(CharacterController))]
    public class ThirdPersonController : MonoBehaviour
    {
        [Header("移動設定 (Movement Settings)")]
        [Tooltip("走路速度 (公尺/秒)")]
        public float walkSpeed = 4.5f;

        [Tooltip("奔跑速度 (公尺/秒)")]
        public float sprintSpeed = 8.5f;

        [Tooltip("加速度平滑度")]
        public float acceleration = 10f;

        [Tooltip("角色轉向平滑時間 (秒)")]
        public float rotationSmoothTime = 0.1f;

        [Header("跳躍與重力 (Jump & Gravity)")]
        [Tooltip("跳躍高度 (公尺)")]
        public float jumpHeight = 2.6f;

        [Tooltip("重力加速度")]
        public float gravity = -22f;

        [Tooltip("落空邊緣寬限時間 (Coyote Time)")]
        public float coyoteTime = 0.15f;

        [Tooltip("跳躍輸入緩衝時間 (Jump Buffer)")]
        public float jumpBufferTime = 0.15f;

        [Header("重生與邊界 (Respawn)")]
        [Tooltip("掉落低於此高度時自動重置位置")]
        public float fallThreshold = -15f;
        public Vector3 spawnPosition = new Vector3(0f, 1.5f, 0f);

        [Header("相機參考 (Camera Reference)")]
        public Transform cameraTransform;

        // 單例快取：確保場景中僅有一個主角，避免多出重複角色一起移動
        public static ThirdPersonController Instance { get; private set; }

        // 組件快取
        private CharacterController controller;

        // 內部狀態
        private float currentSpeed;
        private float verticalVelocity;
        private float turnSmoothVelocity;
        private float coyoteCounter;
        private float jumpBufferCounter;
        private bool isGrounded;

        // 公開屬性供 UI / HUD 讀取
        public float Speed => currentSpeed;
        public bool Grounded => isGrounded;
        public bool IsSprinting { get; private set; }

        private void Awake()
        {
            // 防呆防重複：若場景中已有主角控制器，銷毀多餘的副本
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning($"[ThirdPersonController] 檢測到場景中已存在玩家角色，自動銷毀重複的物件 '{gameObject.name}'！");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            controller = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            spawnPosition = transform.position;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            CheckGroundStatus();
            HandleInput();
            HandleMovement();
            HandleJumpAndGravity();
            CheckBoundary();
        }

        private void CheckGroundStatus()
        {
            isGrounded = controller.isGrounded;

            if (isGrounded)
            {
                coyoteCounter = coyoteTime;

                // 稍微向下壓，防止走下坡階梯時懸空
                if (verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
            }
            else
            {
                coyoteCounter -= Time.deltaTime;
            }
        }

        private void HandleInput()
        {
            if (PlayerInputBridge.GetJumpDown())
            {
                jumpBufferCounter = jumpBufferTime;
            }
            else
            {
                jumpBufferCounter -= Time.deltaTime;
            }

            IsSprinting = PlayerInputBridge.GetSprintHeld();
        }

        private void HandleMovement()
        {
            Vector2 input = PlayerInputBridge.GetMoveInput();
            Vector3 direction = new Vector3(input.x, 0f, input.y);

            float targetSpeed = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                targetSpeed = IsSprinting ? sprintSpeed : walkSpeed;

                // 依據相機方向計算移動方位
                float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                if (cameraTransform != null)
                {
                    targetAngle += cameraTransform.eulerAngles.y;
                }

                // 平滑轉向
                float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, rotationSmoothTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);

                // 計算移動前進向量
                Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
                currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);

                controller.Move(moveDir.normalized * (currentSpeed * Time.deltaTime));
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime * acceleration);
            }
        }

        private void HandleJumpAndGravity()
        {
            // 跳躍判定 (Coyote Time + Jump Buffer)
            if (jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
            }

            // 重力加速
            verticalVelocity += gravity * Time.deltaTime;

            // 限制最大下墜速度
            if (verticalVelocity < -35f)
            {
                verticalVelocity = -35f;
            }

            // 垂直位移套用
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        private void CheckBoundary()
        {
            if (transform.position.y < fallThreshold)
            {
                Respawn();
            }
        }

        public void Respawn()
        {
            controller.enabled = false;
            transform.position = spawnPosition;
            verticalVelocity = 0f;
            currentSpeed = 0f;
            controller.enabled = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            // 雙向觸發收集水晶，確保玩家碰觸時百分之百能吃到水晶
            CollectibleItem item = other.GetComponent<CollectibleItem>();
            if (item == null) item = other.GetComponentInParent<CollectibleItem>();
            if (item != null)
            {
                item.Collect();
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // 若 CharacterController 物理推進時撞擊到水晶亦能收集
            if (hit.collider != null)
            {
                CollectibleItem item = hit.collider.GetComponent<CollectibleItem>();
                if (item == null) item = hit.collider.GetComponentInParent<CollectibleItem>();
                if (item != null)
                {
                    item.Collect();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
