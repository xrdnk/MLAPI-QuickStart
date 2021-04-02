using MLAPI;
using UnityEngine;

namespace MLAPIPractice
{
    // Network Update Loop を利用する場合は INetworkUpdateSystem を実装する
    public class NetworkUpdateSystemTest : NetworkBehaviour, INetworkUpdateSystem
    {
        private void OnEnable()
        {
            // Update Stages の順番は以下の通り．
            // 追加したい Stage を指定する時は this.RegisterNetworkUpdate() を最初に指定する
            this.RegisterNetworkUpdate(NetworkUpdateStage.Initialization);
            // ちなみに上のコードはこちらと同じ
            // NetworkUpdateLoop.RegisterNetworkUpdate(this, NetworkUpdateStage.Initialization);

            this.RegisterNetworkUpdate(NetworkUpdateStage.EarlyUpdate);
            this.RegisterNetworkUpdate(NetworkUpdateStage.FixedUpdate);
            this.RegisterNetworkUpdate(NetworkUpdateStage.PreUpdate);
            this.RegisterNetworkUpdate(NetworkUpdateStage.Update); // 引数なしでも同様
            this.RegisterNetworkUpdate(NetworkUpdateStage.PreLateUpdate);
            this.RegisterNetworkUpdate(NetworkUpdateStage.PostLateUpdate);

            // 全ての Update States を指定したい場合は以下で宣言してもよい
            // this.RegisterAllNetworkUpdates();
            // こちらでもよい
            // NetworkUpdateLoop.RegisterAllNetworkUpdates(this);
        }

        /// <summary>
        /// RegisterNetworkUpdate に指定した Stages が Resolve される
        /// </summary>
        /// <param name="updateStage">updateStage</param>
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            Debug.Log($"{nameof(NetworkUpdateSystemTest)}.{nameof(NetworkUpdate)}({updateStage})");
        }

        private void Update()
        {
            Debug.Log($"{nameof(NetworkUpdateSystemTest)}.{nameof(Update)}()");
        }

        private void FixedUpdate()
        {
            Debug.Log($"{nameof(NetworkUpdateSystemTest)}.{nameof(FixedUpdate)}()");
        }

        private void LateUpdate()
        {
            Debug.Log($"{nameof(NetworkUpdateSystemTest)}.{nameof(LateUpdate)}()");
        }

        private void OnDisable()
        {
            // Dispose する時は以下のように宣言する
            this.UnregisterAllNetworkUpdates();
            // こちらでもよい
            // NetworkUpdateLoop.UnregisterAllNetworkUpdates(this);
        }
    }
}