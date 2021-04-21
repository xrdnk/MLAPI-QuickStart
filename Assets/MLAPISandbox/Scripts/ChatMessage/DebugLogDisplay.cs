using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace MLAPIPractice.ChatMessage
{
    public class DebugLogDisplay : MonoBehaviour
    {
        [SerializeField]
        private int maxLogCount = 20;
        [SerializeField]
        private Rect logArea = new Rect(300, 10, 400, 400);

        private readonly Queue<string> _logMessages = new Queue<string>();
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private void Start()
        {
            Application.logMessageReceived += LogReceived;
        }

        private void LogReceived(string text, string stackTrace, LogType type)
        {
            _logMessages.Enqueue(text);

            while(_logMessages.Count > maxLogCount)
            {
                _logMessages.Dequeue();
            }
        }

        private void OnGUI()
        {
            _stringBuilder.Length = 0;
            GUI.skin.label.fontSize = 30;

            foreach (var message in _logMessages)
            {
                _stringBuilder.Append(message).Append(System.Environment.NewLine);
            }

            GUI.Label(logArea, _stringBuilder.ToString());
        }
    }
}