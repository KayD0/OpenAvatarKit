using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using OpenAvatarKid.Presentation.Controllers.Base;
using TMPro;

namespace OpenAvatarKid.Presentation.Controllers
{
    /// <summary>
    /// 2枠固定（User / Assistant）にメッセージを表示するシンプルUI。
    /// - 入力送信で UserSlot を更新し、OnSubmit で外部に通知
    /// - 応答は外部から SetAssistantText / SetAssistantTextAsync を呼ぶ
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("OpenAvatarKid/UI/Simple Two-Slot Message Window")]
    public class SimpleMessageWindow : MessageWindowBase
    {
        [Header("Output Slots (TMP_Text)")]
        [SerializeField] private TMP_Text userText;        // ChatArea/UserSlot/Text
        [SerializeField] private TMP_Text assistantText;   // ChatArea/AssistantSlot/Text

        [Header("Input (optional)")]
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private Button sendButton;

        [Header("Typing Animation")]
        public bool animateUser = false;
        public bool animateAssistant = true;
        public float charIntervalSec = 0.03f;
        public float preGapSec = 0.10f;
        public float postGapSec = 0.70f;

        [SerializeField] private bool autoHide = false; // 使わないなら false のまま
        private string currentMessageId;

        public event Action<string> OnSubmit;

        private void Awake()
        {
            // 参照チェック
            if (userText == null) Debug.LogError("[TwoSlot] userText is NOT assigned.", this);
            if (assistantText == null) Debug.LogError("[TwoSlot] assistantText is NOT assigned.", this);
            if (inputField == null) Debug.LogWarning("[TwoSlot] inputField is NOT assigned.", this);
            if (sendButton == null) Debug.LogWarning("[TwoSlot] sendButton is NOT assigned.", this);

            // 送信ハンドラ
            if (sendButton != null) sendButton.onClick.AddListener(SubmitInternal);

            // Enter送信（1行）
            if (inputField != null)
            {
                inputField.lineType = TMP_InputField.LineType.SingleLine;
                inputField.onEndEdit.AddListener(v =>
                {
                    if (!string.IsNullOrWhiteSpace(v)) SubmitInternal();
                });
            }
        }

        // ==== 送信 ====
        private void SubmitInternal()
        {
            var text = inputField != null ? inputField.text?.Trim() : null;
            if (string.IsNullOrEmpty(text)) return;

            // 1) ユーザー枠を更新
            _ = SetUserTextAsync(text, animateUser, CancellationToken.None);

            // 2) 外部へ通知（LLM呼び出しなど）
            OnSubmit?.Invoke(text);

            // 3) 入力欄クリア
            if (inputField != null)
            {
                inputField.text = string.Empty;
                inputField.ActivateInputField();
            }
        }

        // ==== MessageWindowBase 互換の最低実装 ====
        public override void Show(string prompt = null)
        {
            SetActive(true);
            if (!string.IsNullOrEmpty(prompt))
                userText.text = prompt; // ここは必要ならユーザー側に（または両方クリアに）
        }

        public override void Hide()
        {
            try
            {
                SetActive(false);
                if (userText) userText.text = string.Empty;
                if (assistantText) assistantText.text = string.Empty;
            }
            catch (MissingReferenceException) { }
        }

        public override async UniTask ShowMessageAsync(string message, CancellationToken token)
        {
            // 互換：デフォルトは assistant 側に出す
            await SetAssistantTextAsync(message, animateAssistant, token);
        }

        // ==== 公開API：枠ごとの更新 ====

        public void SetUserText(string text) =>
            userText.text = text ?? string.Empty;

        public void SetAssistantText(string text) =>
            assistantText.text = text ?? string.Empty;

        public async UniTask SetUserTextAsync(string text, bool animate, CancellationToken ct)
        {
            await SetSlotAsync(userText, text, animate, ct);
        }

        public async UniTask SetAssistantTextAsync(string text, bool animate, CancellationToken ct)
        {
            await SetSlotAsync(assistantText, text, animate, ct);
        }

        // ==== 共通：文字送り表示 ====
        private async UniTask SetSlotAsync(TMP_Text target, string message, bool animate, CancellationToken ct)
        {
            if (target == null) return;

            SetActive(true);
            var id = Guid.NewGuid().ToString();
            currentMessageId = id;

            try
            {
                if (string.IsNullOrEmpty(message))
                {
                    target.text = string.Empty;
                    return;
                }

                await UniTask.Delay((int)(preGapSec * 1000), cancellationToken: ct);

                if (!animate)
                {
                    target.text = message;
                }
                else
                {
                    target.text = string.Empty;
                    for (int i = 0; i < message.Length; i++)
                    {
                        if (ct.IsCancellationRequested) return;
                        target.text = message.Substring(0, i + 1);
                        await UniTask.Delay((int)(charIntervalSec * 1000), cancellationToken: ct);
                    }
                }

                await UniTask.Delay((int)(postGapSec * 1000), cancellationToken: ct);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogWarning($"[TwoSlot] Error: {ex.Message}\n{ex.StackTrace}", this);
            }
            finally
            {
                if (currentMessageId == id && autoHide) Hide();
            }
        }

        private void SetActive(bool value) => gameObject.SetActive(value);
    }

    // 既存互換：ストリーム型が必要なら、assistant側に流し込む関数を追加してください。
    // 例：
    // public async UniTask SetAssistantStreamAsync(IAsyncEnumerable<string> chunks, CancellationToken ct) { ... }
}
