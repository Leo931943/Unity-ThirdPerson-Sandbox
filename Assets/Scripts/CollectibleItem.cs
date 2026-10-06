using UnityEngine;

namespace ThirdPersonDemo
{
    public class CollectibleItem : MonoBehaviour
    {
        [Header("旋轉與浮動效果")]
        public float rotateSpeed = 90f;
        public float floatAmplitude = 0.25f;
        public float floatFrequency = 2.5f;

        private Vector3 startPosition;
        private bool isCollected = false;

        private void Start()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            if (isCollected) return;

            // 自轉
            transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

            // 上下浮動
            float newY = startPosition.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
            transform.position = new Vector3(startPosition.x, newY, startPosition.z);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (isCollected) return;

            // 檢測是否被玩家碰觸
            if (other.GetComponent<ThirdPersonController>() != null || other.CompareTag("Player"))
            {
                isCollected = true;
                SimpleHUD.AddScore(1);

                // 觸發拾取效果 (縮小後銷毀)
                StartCoroutine(PickupEffectRoutine());
            }
        }

        private System.Collections.IEnumerator PickupEffectRoutine()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 initScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initScale, Vector3.zero, elapsed / duration);
                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
