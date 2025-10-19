using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace OpenAvatarKid.Presentation.Controllers
{
    public sealed class SimpleChatUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;
        [SerializeField] private TMP_Text logText;

        public event Action<string> OnSubmit;

        private void Awake()
        {
            sendButton.onClick.AddListener(Submit);
            inputField.onSubmit.AddListener(_ => Submit());
        }

        private void Submit()
        {
            var text = inputField.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            AppendLog($"> User: {text}");
            OnSubmit?.Invoke(text);
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }

        public void AppendLog(string line)
        {
            if (logText == null) return;
            logText.text += (logText.text.Length > 0 ? "\n" : "") + line;
        }
    }
}
