using System.Text;
using MLAPI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UniRx;

namespace MLAPIQuickStart.View
{
    public class ConnectionView : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputFieldPassword;
        [SerializeField] private Button buttonHost;
        [SerializeField] private Button buttonClient;
        [SerializeField] private Button buttonLeave;

        private void Start()
        {
            NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;

            buttonHost.OnClickAsObservable()
                .Subscribe(_ => Host())
                .AddTo(this);
            buttonClient.OnClickAsObservable()
                .Subscribe(_ => Client())
                .AddTo(this);
            buttonLeave.OnClickAsObservable()
                .Subscribe(_ => Leave())
                .AddTo(this);

            buttonLeave.interactable = false;

            inputFieldPassword.ObserveEveryValueChanged(inputField => inputField.text)
                .Subscribe(text =>
                {
                    buttonHost.interactable = !string.IsNullOrEmpty(text);
                    buttonClient.interactable = !string.IsNullOrEmpty(text);
                })
                .AddTo(this);
        }

        private void OnDestroy()
        {
            if (NetworkManager.Singleton == null) { return; }

            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        private void Host()
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += ApprovalCheck;
            NetworkManager.Singleton.StartHost(new Vector3(-2f, 0f, 0f), Quaternion.Euler(0f, 135f, 0f));
        }

        private void Client()
        {
            // Set password ready to send to the server to validate
            NetworkManager.Singleton.NetworkConfig.ConnectionData = Encoding.ASCII.GetBytes(inputFieldPassword.text);
            NetworkManager.Singleton.StartClient();
        }

        private void Leave()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                NetworkManager.Singleton.StopHost();
                NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;
            }
            else if (NetworkManager.Singleton.IsClient)
            {
                NetworkManager.Singleton.StopClient();
            }
        }

        private void HandleServerStarted()
        {
            if (NetworkManager.Singleton.IsHost)
            {
                HandleClientConnected(NetworkManager.Singleton.ServerClientId);
            }
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                inputFieldPassword.interactable = false;
                buttonLeave.interactable = true;
            }
        }

        private void HandleClientDisconnect(ulong clientId)
        {
            if (clientId == NetworkManager.Singleton.LocalClientId)
            {
                inputFieldPassword.interactable = true;
                buttonLeave.interactable = false;
            }
        }

        /// <summary>
        /// 接続チェック用のコールバック
        /// </summary>
        /// <param name="connectionData">ClientがServerに送信する任意のカスタムデータ</param>
        /// <param name="clientId">ClientId</param>
        /// <param name="callback">デリゲート</param>
        private void ApprovalCheck(byte[] connectionData, ulong clientId, NetworkManager.ConnectionApprovedDelegate callback)
        {
            var password = Encoding.ASCII.GetString(connectionData);

            var approveConnection = password.Equals(inputFieldPassword.text);

            var spawnPos = Vector3.zero;
            var spawnRot = Quaternion.identity;

            switch (NetworkManager.Singleton.ConnectedClients.Count)
            {
                case 1:
                    spawnPos = new Vector3(0f, 0f, 0f);
                    spawnRot = Quaternion.Euler(0f, 180f, 0f);
                    break;
                case 2:
                    spawnPos = new Vector3(2f, 0f, 0f);
                    spawnRot = Quaternion.Euler(0f, 225, 0f);
                    break;
            }

            // 第一引数はデリゲートを通してプレイヤーオブジェクトを生成するか否か．個人でカスタマイズしたい場合は false にする．
            // 第二引数はどのプレハブを生成するか（ハッシュ値で）．"Default Player Prefab" にチェックが入っている場合は null にする．
            // 第三引数は接続の許可が得られたか否か．大方，パスワードが合っているかどうかの判定をここに入れる．脳死で通したい場合は true にする．
            // 第四引数，第五引数は生成時にプレイヤーに渡すPositionとRotation．nullable なので デフォルトを渡したい場合は null にする．
            callback(true, null, approveConnection, spawnPos, spawnRot);
        }
    }
}