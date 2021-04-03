using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using MLAPI;
using MLAPI.Messaging;
using MLAPI.Serialization.Pooled;
using UniRx;
using UnityEngine;

namespace MLAPIPractice.ChatMessage
{
    public static class CMMConst
    {
        public static readonly string CLIENT_TO_SERVER = "CLIENT_TO_SERVER";
        public static readonly string SERVER_TO_ALL_CLIENTS = "SERVER_TO_ALL_CLIENTS";
    }

    public class ChatMessageService : MonoBehaviour
    {
        public IObservable<(ulong senderId, string message)> OnReceivedMessageAsObservable() => _onReceivedMessageSubject;
        private readonly Subject<(ulong senderId, string message)> _onReceivedMessageSubject = new Subject<(ulong senderId, string message)>();

        private async void Awake()
        {
            await UniTask.WaitUntil(() => NetworkManager.Singleton.IsListening);

            if (NetworkManager.Singleton.IsServer)
            {
                // 事前にクライアント側からのストリームを受け取る処理を登録する
                CustomMessagingManager.RegisterNamedMessageHandler(CMMConst.CLIENT_TO_SERVER,
                    MessageHandler_Server_ReceiveAndSendMessageToAllClients);
                Debug.Log("U R SERVER. <color=red>YOU CANNOT RECEIVE MESSAGES.</color>");
            }

            if (NetworkManager.Singleton.IsClient)
            {
                // 事前にサーバー側からのストリームを受け取る処理を登録する
                CustomMessagingManager.RegisterNamedMessageHandler(CMMConst.SERVER_TO_ALL_CLIENTS,
                    MessageHandler_Client_ReceiveMessage);
                Debug.Log("U R CLIENT.");
            }
        }

        private void OnDestroy()
        {
            CustomMessagingManager.UnregisterNamedMessageHandler(CMMConst.CLIENT_TO_SERVER);
            CustomMessagingManager.UnregisterNamedMessageHandler(CMMConst.SERVER_TO_ALL_CLIENTS);
        }

        /// <summary>
        /// チャットメッセージ送信処理
        /// </summary>
        /// <param name="message">テキストメッセージの内容</param>
        public void SendMessageToServerToAllClients(string message)
        {
            // ① まず最初に，クライアント側がサーバー側に向けて送る
            // PooledNetworkBuffer Stream の生成(送る側なので生成する)
            using var outputStream = PooledNetworkBuffer.Get();
            // PooledNetworkBuffer Stream から Writer を生成
            using var writer = PooledNetworkWriter.Get(outputStream);
            // テキストメッセージを PooledNetworkBuffer Streamに書きだすイメージ
            writer.WriteStringPacked(message);

            if (NetworkManager.Singleton.IsListening)
            {
                // サーバーのクライアントIDを取得し，ストリームにデータを乗せて送る
                var serverClientId = NetworkManager.Singleton.ServerClientId;
                CustomMessagingManager.SendNamedMessage(CMMConst.CLIENT_TO_SERVER, serverClientId, outputStream);
            }
            else
            {
                Debug.LogError("Cannot send message because network manager is not listening.");
            }
        }

        /// <summary>
        /// クライアント側から送られてきたテキストメッセージ入りのストリームを読み込み，
        /// さらに，そのストリームを全クライアントへ送り返す．
        /// </summary>
        /// <param name="senderClientId">送信者側のクライアントID(ここでは SendMessageToServerToAllClients を実行したクライアントIDになる)</param>
        /// <param name="inputStream">PooledNetworkBuffer Stream</param>
        private void MessageHandler_Server_ReceiveAndSendMessageToAllClients(ulong senderClientId, Stream inputStream)
        {
            // ② 次に，サーバー側がクライアント側から送られてきたメッセージを受け取って読み込む
            using var reader = PooledNetworkReader.Get(inputStream);
            var message = reader.ReadStringPacked();

            // ③ 今度は，送られてきたメッセージをサーバー側が全クライアントに向けて送り返す
            using var outputStream = PooledNetworkBuffer.Get();
            using var writer = PooledNetworkWriter.Get(outputStream);
            // 送信したクライアントのIDも乗せる
            writer.WriteUInt64Packed(senderClientId);
            writer.WriteStringPacked(message);

            // 全クライアントのIDリストを取得し，ストリームにデータを乗せて送る
            var targetClientIds =
                NetworkManager.Singleton.ConnectedClientsList.Select(client => client.ClientId).ToList();
            CustomMessagingManager.SendNamedMessage(CMMConst.SERVER_TO_ALL_CLIENTS, targetClientIds, outputStream);
        }

        /// <summary>
        /// サーバー側から送られてきたテキストメッセージ入りのストリームを読み込む．
        /// </summary>
        /// <param name="senderServerId">送信者側のクライアントID(このまま利用するとサーバーのIDになるのでここでは使わない)</param>
        /// <param name="inputStream">PooledNetworkBuffer Stream</param>
        private void MessageHandler_Client_ReceiveMessage(ulong senderServerId, Stream inputStream)
        {
            // ④ 最後に，サーバー側から送られてきたストリームから送り手側のIDとメッセージ内容を読み取る
            using var reader = PooledNetworkReader.Get(inputStream);
            var senderClientId = reader.ReadUInt64Packed();
            var message = reader.ReadStringPacked();
            _onReceivedMessageSubject.OnNext((senderClientId, message));
        }
    }
}