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
    /// 2�g�Œ�iUser / Assistant�j�Ƀ��b�Z�[�W��\������V���v��UI�B
    /// - ���͑��M�� UserSlot ���X�V���AOnSubmit �ŊO���ɒʒm
    /// - �����͊O������ SetAssistantText / SetAssistantTextAsync ���Ă�
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

        [SerializeField] private bool autoHide = false; // �g��Ȃ��Ȃ� false �̂܂�
        private string currentMessageId;

        public event Action<string> OnSubmit;

        private void Awake()
        {
            // �Q�ƃ`�F�b�N
            if (userText == null) Debug.LogError("[TwoSlot] userText is NOT assigned.", this);
            if (assistantText == null) Debug.LogError("[TwoSlot] assistantText is NOT assigned.", this);
            if (inputField == null) Debug.LogWarning("[TwoSlot] inputField is NOT assigned.", this);
            if (sendButton == null) Debug.LogWarning("[TwoSlot] sendButton is NOT assigned.", this);

            // ���M�n���h��
            if (sendButton != null) sendButton.onClick.AddListener(SubmitInternal);

            // Enter���M�i1�s�j
            if (inputField != null)
            {
                inputField.lineType = TMP_InputField.LineType.SingleLine;
                inputField.onEndEdit.AddListener(v =>
                {
                    if (!string.IsNullOrWhiteSpace(v)) SubmitInternal();
                });
            }
        }

        // ==== ���M ====
        private void SubmitInternal()
        {
            var text = inputField != null ? inputField.text?.Trim() : null;
            if (string.IsNullOrEmpty(text)) return;

            // 1) ���[�U�[�g���X�V
            _ = SetUserTextAsync(text, animateUser, CancellationToken.None);

            // 2) �O���֒ʒm�iLLM�Ăяo���Ȃǁj
            OnSubmit?.Invoke(text);

            // 3) ���͗��N���A
            if (inputField != null)
            {
                inputField.text = string.Empty;
                inputField.ActivateInputField();
            }
        }

        // ==== MessageWindowBase �݊��̍Œ���� ====
        public override void Show(string prompt = null)
        {
            SetActive(true);
            if (!string.IsNullOrEmpty(prompt))
                userText.text = prompt; // �����͕K�v�Ȃ烆�[�U�[���Ɂi�܂��͗����N���A�Ɂj
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
            // �݊��F�f�t�H���g�� assistant ���ɏo��
            await SetAssistantTextAsync(message, animateAssistant, token);
        }

        // ==== ���JAPI�F�g���Ƃ̍X�V ====

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

        // ==== ���ʁF��������\�� ====
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

    // �����݊��F�X�g���[���^���K�v�Ȃ�Aassistant���ɗ������ފ֐���ǉ����Ă��������B
    // ��F
    // public async UniTask SetAssistantStreamAsync(IAsyncEnumerable<string> chunks, CancellationToken ct) { ... }
}
