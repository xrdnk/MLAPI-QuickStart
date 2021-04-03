using UniRx;
using UnityEngine;

namespace MLAPIPractice.ChatMessage
{
    public class ChatMessagePresenter : MonoBehaviour
    {
        [SerializeField]
        private ChatMessageProvider _Provider = default;
        [SerializeField]
        private ChatMessageView _View = default;

        private void Awake()
        {
            _Provider.OnReceivedMessageAsObservable()
                .Subscribe(_View.DisplayTextMessage)
                .AddTo(this);

            _View.OnSendTextMessageAsObservable()
                .Subscribe(_Provider.SendMessageToServerToAllClients)
                .AddTo(this);
        }
    }
}