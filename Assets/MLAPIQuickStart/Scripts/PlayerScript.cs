using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// NetworkBehaviour 継承に必要
using MLAPI;
// RPCに必要
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

        [SerializeField, Tooltip("プレイヤーの武器リスト")]
        private List<GameObject> playerWeapons;

        /// <summary>
        /// プレイヤーの色設定に利用するマテリアルのクローン
        /// </summary>
        private Material _playerMaterialClone;
        /// <summary>
        /// カメラのTransform
        /// </summary>
        private Transform _cameraTransform;

        private SceneScript _sceneScript;

        /// <summary>
        /// 現在の武器インデックス
        /// </summary>
        private int _currentWeaponIndex;

        /// <summary>
        /// 現在の武器
        /// </summary>
        private Weapon _activeWeapon;

        /// <summary>
        /// 武器の攻撃間隔
        /// </summary>
        private float _weaponCooldownTime;

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
        /// <summary>
        /// 武器番号の同期変数
        /// </summary>
        private readonly NetworkVariable<int> _networkWeaponIndex = new NetworkVariable<int>
            (new NetworkVariableSettings {WritePermission = NetworkVariablePermission.OwnerOnly});

        private void Awake()
        {
            // フック関数の設定
            _networkPlayerName.OnValueChanged += OnNameChanged;
            _networkPlayerColor.OnValueChanged += OnColorChanged;
            _networkWeaponIndex.OnValueChanged += OnWeaponChanged;

            // カメラのTransformのキャッシュ
            _cameraTransform = Camera.main.transform;

            _sceneScript = GameObject.Find("SceneReference").GetComponent<SceneReference>().SceneScript;

            // 武器は最初全て非表示にする
            foreach (var weapon in playerWeapons.Where(weapon => weapon != null))
            {
                weapon.SetActive(false);
            }

            // 現在の武器の設定と銃弾の弾数を表示する
            if (_currentWeaponIndex < playerWeapons.Count && playerWeapons[_currentWeaponIndex] != null)
            {
                _activeWeapon = playerWeapons[_currentWeaponIndex].GetComponent<Weapon>();
                _sceneScript.DisplayAmmo(_activeWeapon.weaponAmmo);
            }
        }

        private void Start()
        {
            // ローカルプレイヤーの場合，カメラを一人称視点に設定し，プレイヤー名のテキストを画面下に表示する
            if (IsOwner)
            {
                _sceneScript.PlayerScript = this;

                var thisTransform = transform;
                // thisTransform.position = new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5));
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

            // 右クリックで武器変更処理を走らせる
            if (Input.GetButtonDown("Fire2"))
            {
                // 解釈が誤りそうなので修正
                // var nextIndex = _currentWeaponIndex++ % playerWeapons.Count;
                _currentWeaponIndex++;
                if (_currentWeaponIndex > playerWeapons.Count)
                {
                    _currentWeaponIndex = 0;
                }

                // ChangeActiveWeaponServerRpc(nextIndex);
                ChangeActiveWeaponServerRpc(_currentWeaponIndex);
            }

            // 左クリックで発射処理を走らせる
            if (Input.GetButtonDown("Fire1"))
            {
                if (_activeWeapon && Time.time > _weaponCooldownTime && _activeWeapon.weaponAmmo > 0)
                {
                    _weaponCooldownTime = Time.time + _activeWeapon.weaponCooldown;
                    _activeWeapon.weaponAmmo--;
                    _sceneScript.DisplayAmmo(_activeWeapon.weaponAmmo);
                    FireWeaponServerRpc();
                }
            }
        }

        private void OnDestroy()
        {
            _networkPlayerName.OnValueChanged -= OnNameChanged;
            _networkPlayerColor.OnValueChanged -= OnColorChanged;
        }

        /// <summary>
        /// テキストチャットを送信する
        /// </summary>
        /// <param name="serverRpcParams">ServerRpcParams</param>
        [ServerRpc(RequireOwnership = true)]
        public void SubmitMessageServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (_sceneScript != null)
            {
                _sceneScript.SetMessage($"SenderId:{serverRpcParams.Receive.SenderClientId}[{_networkPlayerName.Value} says hello {Random.Range(10, 99)}]");
            }
        }

        /// <summary>
        /// プレイヤー名を設定する
        /// </summary>
        /// <param name="playerName">playerName</param>
        /// <param name="serverRpcParams">ServerRpcParams</param>
        [ServerRpc(RequireOwnership = true)]
        private void SubmitPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
            => _networkPlayerName.Value = playerName;

        /// <summary>
        /// プレイヤー色を設定する
        /// </summary>
        /// <param name="playerColor">playerColor</param>
        /// <param name="serverRpcParams">ServerRpcParams</param>
        [ServerRpc(RequireOwnership = true)]
        private void SubmitPlayerColorServerRpc(Color playerColor, ServerRpcParams serverRpcParams = default)
            => _networkPlayerColor.Value = playerColor;

        /// <summary>
        /// 入室時メッセージを表示する
        /// </summary>
        /// <param name="serverRpcParams">ServerRpcParams</param>
        [ServerRpc(RequireOwnership = true)]
        private void SubmitJoinedMessageServerRpc(ServerRpcParams serverRpcParams = default)
        {
            if (_sceneScript != null)
            {
                _sceneScript.SetMessage($"{_networkPlayerName.Value} joined");
            }
        }

        /// <summary>
        /// 武器インデックスを設定する
        /// </summary>
        /// <param name="index">武器インデックス</param>
        /// <param name="serverRpcParams">ServerRpcParams</param>
        [ServerRpc(RequireOwnership = true)]
        private void ChangeActiveWeaponServerRpc(int index, ServerRpcParams serverRpcParams = default) => _networkWeaponIndex.Value = index;

        /// <summary>
        /// サーバーへ発射処理を走らせる
        /// </summary>
        /// <param name="serverRpcParams">ServerRpcParams</param>
        [ServerRpc(RequireOwnership = true)]
        private void FireWeaponServerRpc(ServerRpcParams serverRpcParams = default)
            => FireWeaponClientRpc(
                // new ClientRpcParams { Send = new ClientRpcSendParams
                //     { TargetClientIds =
                //         NetworkManager.Singleton.ConnectedClientsList
                //         .Where(c => c.ClientId != OwnerClientId)
                //         .Select(c => c.ClientId).ToArray() }}
                );

        /// <summary>
        /// 全クライアントへ発射処理を送る
        /// </summary>
        /// <param name="clientRpcParams">ClientRpcParams</param>
        [ClientRpc]
        private void FireWeaponClientRpc(ClientRpcParams clientRpcParams = default)
        {
            var bullet = Instantiate(_activeWeapon.weaponBullet, _activeWeapon.weaponFirePosition.position,
                _activeWeapon.weaponFirePosition.rotation);
            bullet.GetComponent<Rigidbody>().velocity = bullet.transform.forward * _activeWeapon.weaponSpeed;
            if (bullet)
            {
                Destroy(bullet, _activeWeapon.weaponLife);
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
        /// 武器インデックスが変更された時に呼ばれるフック関数
        /// </summary>
        /// <param name="oldIndex">前のインデックス</param>
        /// <param name="newIndex">現在のインデックス</param>
        private void OnWeaponChanged(int oldIndex, int newIndex)
        {
            if (0 < oldIndex && oldIndex < playerWeapons.Count && playerWeapons[oldIndex] != null)
            {
                playerWeapons[oldIndex].SetActive(false);
            }

            if (0 < newIndex && newIndex < playerWeapons.Count && playerWeapons[newIndex] != null)
            {
                playerWeapons[newIndex].SetActive(true);
                _activeWeapon = playerWeapons[_networkWeaponIndex.Value].GetComponent<Weapon>();
                if (IsLocalPlayer)
                {
                    _sceneScript.DisplayAmmo(_activeWeapon.weaponAmmo);
                }
            }
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