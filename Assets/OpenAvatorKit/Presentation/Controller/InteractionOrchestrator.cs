using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using OpenAvatarKid.Domain.Conversation;
using OpenAvatarKid.UseCases.Interactors;

namespace OpenAvatarKid.Presentation.Controllers
{
    public sealed class InteractionOrchestrator : MonoBehaviour
    {
        [SerializeField] private SimpleChatUI ui;

        // Bootstrap ���璍��
        private RunInteractionUseCase runUseCase;

        private CancellationTokenSource cts;
        private bool isBusy;

        public void Inject(RunInteractionUseCase useCase)
        {
            runUseCase = useCase;
        }

        private void OnEnable()
        {
            if (ui != null) ui.OnSubmit += HandleUserText;
        }

        private void OnDisable()
        {
            if (ui != null) ui.OnSubmit -= HandleUserText;
            cts?.Cancel();
            cts?.Dispose();
        }

        private void HandleUserText(string text)
        {
            if (isBusy)
            {
                ui.AppendLog("(busy: �O���N�G�X�g������)");
                return;
            }
            cts?.Cancel();
            cts = new CancellationTokenSource();
            _ = RunFlowAsync(text, cts.Token);
        }

        private async Task RunFlowAsync(string userText, CancellationToken ct)
        {
            isBusy = true;
            try
            {
                // ����͂Ƃ肠�������{��Œ�B��Ŏ������� or UI �ؑցB
                var script = await runUseCase.ExecuteAsync(userText, Lang.Ja, ct);

                // �����ł̓��O�o�͂̂݁i���t�F�[�Y�� TTS/�\��ɐڑ��j
                foreach (var utt in script.Utterances)
                {
                    ui.AppendLog($"AI: {utt.Text}");
                }
            }
            catch (System.OperationCanceledException)
            {
                ui.AppendLog("(�L�����Z��)");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ui.AppendLog($"(�G���[) {ex.Message}");
            }
            finally
            {
                isBusy = false;
            }
        }
    }
}
