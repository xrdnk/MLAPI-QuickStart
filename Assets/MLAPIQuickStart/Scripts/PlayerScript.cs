using UnityEngine;
// NetworkBehaviour 継承に必要
using MLAPI;

namespace MLAPIQuickStart
{
    // MonoBehaviour ではなく NetworkBehaviour を継承する
    public class PlayerScript : NetworkBehaviour
    {
        private Transform _cameraTransform;
        private static readonly string HORIZONTAL = "Horizontal";
        private static readonly string VERTICAL = "Vertical";

        /// <summary>
        /// MLAPI の Setup が完了後に呼ばれるコールバック
        /// </summary>
        public override void NetworkStart()
        {
            transform.position = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
            _cameraTransform = Camera.main.transform;
            _cameraTransform.transform.SetParent(transform);
            _cameraTransform.localPosition = new Vector3(0, 0, 0);
        }

        private void Update()
        {
            // ローカルプレイヤーではない場合，以下の処理を走らせない
            if (!IsLocalPlayer)
            {
                return;
            }

            var moveX = Input.GetAxis(HORIZONTAL) * Time.deltaTime * 110.0f;
            var moveZ = Input.GetAxis(VERTICAL) * Time.deltaTime * 4f;

            transform.Rotate(0, moveX, 0);
            transform.Translate(0, 0, moveZ);
        }
    }
}