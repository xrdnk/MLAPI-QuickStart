using MLAPI;
using MLAPI.NetworkVariable;
using UnityEngine;
using UnityEngine.UI;

namespace MLAPIQuickStart
{
    public class SceneScript : NetworkBehaviour
    {
        [SerializeField, Tooltip("メッセージ表示用のテキスト")]
        private Text textMessage;
        [SerializeField, Tooltip("メッセージ送信用のボタン")]
        private Button buttonSendMessage;
        [SerializeField, Tooltip("銃弾の弾数表示用のテキスト")]
        private Text textAmmo;

        public PlayerScript PlayerScript { set => _playerScript = value; }
        private PlayerScript _playerScript;

        public SceneReference SceneReference { set => _sceneReference = value; }
        private SceneReference _sceneReference;

        /// <summary>
        /// メッセージの同期変数
        /// </summary>
        private readonly NetworkVariable<string> _networkMessage = new NetworkVariable<string>
            (new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly});

        private void Awake()
        {
            // フック関数の設定
            _networkMessage.OnValueChanged += OnMessageChanged;

            buttonSendMessage.onClick.AddListener(SendMessage);
        }

        private void OnDestroy()
        {
            buttonSendMessage.onClick.RemoveListener(SendMessage);
        }

        /// <summary>
        /// メッセージの設定
        /// </summary>
        /// <param name="message">テキストメッセージ</param>
        public void SetMessage(string message) => _networkMessage.Value = message;

        /// <summary>
        /// 銃弾の弾数を表示する
        /// </summary>
        /// <param name="value"></param>
        public void DisplayAmmo(int value) => textAmmo.text = $"Ammo: {value}";

        /// <summary>
        /// メッセージ内容が変更された時に呼ばれるフック関数
        /// </summary>
        /// <param name="oldMessage">旧メッセージ</param>
        /// <param name="newMessage">新メッセージ</param>
        private void OnMessageChanged(string oldMessage, string newMessage) => textMessage.text = newMessage;

        /// <summary>
        /// メッセージの送信
        /// </summary>
        private void SendMessage()
        {
            if (_playerScript != null)
            {
                _playerScript.SubmitMessageServerRpc();
            }
        }
    }
}