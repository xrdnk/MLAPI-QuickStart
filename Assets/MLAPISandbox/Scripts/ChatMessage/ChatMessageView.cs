using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace MLAPIPractice.ChatMessage
{
    public class ChatMessageView : MonoBehaviour
    {
        [SerializeField]
        private InputField inputFieldTextMessage;

        [SerializeField]
        private Button buttonTextMessage;

        public IObservable<string> OnSendTextMessageAsObservable() => _OnSendTextMessageSubject;
        private readonly Subject<string> _OnSendTextMessageSubject = new Subject<string>();

        private void Awake()
        {
            // テキストメッセージが入力されてない限り，ボタンを非活性にする
            inputFieldTextMessage.ObserveEveryValueChanged(field => field.text)
                .Subscribe(message => buttonTextMessage.interactable = !string.IsNullOrEmpty(message))
                .AddTo(this);

            buttonTextMessage.OnClickAsObservable()
                .Subscribe(_ => SendTextMessage(inputFieldTextMessage.text))
                .AddTo(this);
        }

        private void SendTextMessage(string message)
        {
            _OnSendTextMessageSubject.OnNext(message);
            inputFieldTextMessage.text = string.Empty;
        }

        public void DisplayTextMessage((ulong, string) messageTuple)
        {
            var (senderClientId, message) = messageTuple;
            Debug.Log($"SenderClientId: {senderClientId}, Message:[{message}]");
        }
    }
}