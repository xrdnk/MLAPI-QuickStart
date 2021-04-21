using UniRx;
using UnityEngine;

namespace MLAPIPractice.ChatMessage
{
    public class ChatMessagePresenter : MonoBehaviour
    {
        [SerializeField]
        private ChatMessageService service = default;
        [SerializeField]
        private ChatMessageView _View = default;

        private void Awake()
        {
            service.OnReceivedMessageAsObservable()
                .Subscribe(_View.DisplayTextMessage)
                .AddTo(this);

            _View.OnSendTextMessageAsObservable()
                .Subscribe(service.SendMessageToServerToAllClients)
                .AddTo(this);
        }
    }
}