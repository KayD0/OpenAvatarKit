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

        // Bootstrap から注入
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
                ui.AppendLog("(busy: 前リクエスト処理中)");
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
                // 言語はとりあえず日本語固定。後で自動判定 or UI 切替。
                var script = await runUseCase.ExecuteAsync(userText, Lang.Ja, ct);

                // ここではログ出力のみ（次フェーズで TTS/表情に接続）
                foreach (var utt in script.Utterances)
                {
                    ui.AppendLog($"AI: {utt.Text}");
                }
            }
            catch (System.OperationCanceledException)
            {
                ui.AppendLog("(キャンセル)");
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                ui.AppendLog($"(エラー) {ex.Message}");
            }
            finally
            {
                isBusy = false;
            }
        }
    }
}
