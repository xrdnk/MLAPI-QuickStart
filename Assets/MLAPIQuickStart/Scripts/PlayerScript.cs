using UnityEngine;
// NetworkBehaviour 継承に必要
using MLAPI;
using MLAPI.Messaging;
// NetworkVariable 利用に必要
using MLAPI.NetworkVariable;
using Random = UnityEngine.Random;

namespace MLAPIQuickStart
{
    // MonoBehaviour ではなく NetworkBehaviour を継承する
    public class PlayerScript : NetworkBehaviour
    {
        [SerializeField, Tooltip("プレイヤー名のテキスト")]
        private TextMesh textPlayerName;

        [SerializeField,Tooltip("プレイヤー情報")]
        private GameObject floatingInfo;

        /// <summary>
        /// プレイヤーの色設定に利用するマテリアルのクローン
        /// </summary>
        private Material _playerMaterialClone;
        /// <summary>
        /// カメラのTransform
        /// </summary>
        private Transform _cameraTransform;

        private SceneScript _sceneScript;

        #region Constants
        private static readonly string HORIZONTAL = "Horizontal";
        private static readonly string VERTICAL = "Vertical";
        private static readonly float MOVEX_COEFFICIENT = 110.0f;
        private static readonly float MOVEZ_COEFFICIENT = 4.0f;
        #endregion

        /// <summary>
        /// プレイヤー名の同期変数
        /// </summary>
        private readonly NetworkVariable<string> _networkPlayerName = new NetworkVariable<string>
            (new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly});
        /// <summary>
        /// プレイヤー色の同期変数
        /// </summary>
        private readonly NetworkVariable<Color> _networkPlayerColor = new NetworkVariable<Color>
            (new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly}, Color.white);

        private void Awake()
        {
            // フック関数の設定
            _networkPlayerName.OnValueChanged += OnNameChanged;
            _networkPlayerColor.OnValueChanged += OnColorChanged;

            // カメラのTransformのキャッシュ
            _cameraTransform = Camera.main.transform;

            _sceneScript = FindObjectOfType<SceneScript>();
        }

        private void Start()
        {
            // ローカルプレイヤーの場合，カメラを一人称視点に設定し，プレイヤー名のテキストを画面下に表示する
            if (IsOwner)
            {
                _sceneScript.PlayerScript = this;

                var thisTransform = transform;
                thisTransform.position = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
                _cameraTransform.transform.SetParent(thisTransform);
                _cameraTransform.localPosition = new Vector3(0, 0, 0);

                floatingInfo.transform.localPosition = new Vector3(0, -0.3f, 0.6f);
                floatingInfo.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);

                var playerName = "Player" + Random.Range(100, 999);
                var color = new Color(Random.Range(0f, 1f), Random.Range(0f, 1f), Random.Range(0f, 1f));

                SubmitPlayerNameServerRpc(playerName);
                SubmitPlayerColorServerRpc(color);
                SubmitJoinedMessageServerRpc();
            }
        }

        private void Update()
        {
            // ローカルプレイヤーではない場合，プレイヤー名のテキストをカメラに向けさせる
            if (!IsLocalPlayer)
            {
                floatingInfo.transform.LookAt(_cameraTransform);
                return;
            }

            //　ローカルプレイヤーの場合，移動・回転処理を実行する
            Move();
        }

        private void OnDestroy()
        {
            _networkPlayerName.OnValueChanged -= OnNameChanged;
            _networkPlayerColor.OnValueChanged -= OnColorChanged;
        }

        /// <summary>
        /// テキストチャットを送信する
        /// </summary>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = true)]
        public void SubmitMessageServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (_sceneScript != null)
            {
                _sceneScript.SetMessage($"{_networkPlayerName.Value} says hello {Random.Range(10, 99)}");
            }
        }

        /// <summary>
        /// プレイヤー名を設定する
        /// </summary>
        /// <param name="playerName">playerName</param>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = true)]
        private void SubmitPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
            => _networkPlayerName.Value = playerName;

        /// <summary>
        /// プレイヤー色を設定する
        /// </summary>
        /// <param name="playerColor">playerColor</param>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = true)]
        private void SubmitPlayerColorServerRpc(Color playerColor, ServerRpcParams serverRpcParams = default)
            => _networkPlayerColor.Value = playerColor;

        /// <summary>
        /// 入室時メッセージを表示する
        /// </summary>
        /// <param name="serverRpcParams"></param>
        [ServerRpc(RequireOwnership = true)]
        private void SubmitJoinedMessageServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (_sceneScript != null)
            {
                _sceneScript.SetMessage($"{_networkPlayerName.Value} joined");
            }
        }

        /// <summary>
        /// プレイヤー名が変更された時に呼ばれるフック関数
        /// </summary>
        /// <param name="oldName">前のプレイヤー名</param>
        /// <param name="newName">現在のプレイヤー名</param>
        private void OnNameChanged(string oldName, string newName)
            => textPlayerName.text = newName;

        /// <summary>
        /// プレイヤー色が変更された時に呼ばれるフック関数
        /// </summary>
        /// <param name="oldColor">前のプレイヤー色</param>
        /// <param name="newColor">現在のプレイヤー色</param>
        private void OnColorChanged(Color oldColor, Color newColor)
        {
            textPlayerName.color = newColor;
            _playerMaterialClone = new Material(GetComponent<Renderer>().material) {color = newColor};
            GetComponent<Renderer>().material = _playerMaterialClone;
        }

        /// <summary>
        /// プレイヤーの移動・回転処理
        /// </summary>
        private void Move()
        {
            var moveX = Input.GetAxis(HORIZONTAL) * Time.deltaTime * MOVEX_COEFFICIENT;
            var moveZ = Input.GetAxis(VERTICAL) * Time.deltaTime * MOVEZ_COEFFICIENT;

            transform.Rotate(0, moveX, 0);
            transform.Translate(0, 0, moveZ);
        }
    }
}