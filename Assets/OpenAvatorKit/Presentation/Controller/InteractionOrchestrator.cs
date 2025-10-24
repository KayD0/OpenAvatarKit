using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using OpenAvatarKid.Domain.Conversation;
using OpenAvatarKid.UseCases.Interactors;
using Cysharp.Threading.Tasks;

namespace OpenAvatarKid.Presentation.Controllers
{
    public sealed class InteractionOrchestrator : MonoBehaviour
    {
        [SerializeField] private SimpleMessageWindow ui; // 入出力を兼ねる

        // Bootstrap から注入
        private RunInteractionUseCase runUseCase;

        private CancellationTokenSource cts;
        private bool isBusy;

        public void Inject(RunInteractionUseCase useCase)
        {
            runUseCase = useCase;
            Debug.Log("[InteractionOrchestrator] Injected RunInteractionUseCase.");
        }

        private void OnEnable()
        {
            if (ui == null) Debug.LogError("[InteractionOrchestrator] ui(SimpleMessageWindow) is NOT assigned.", this);
            if (ui != null) ui.OnSubmit += HandleUserText;
        }

        private void OnDisable()
        {
            if (ui != null) ui.OnSubmit -= HandleUserText;
            cts?.Cancel();
            cts?.Dispose();
            cts = null;
        }

        private void HandleUserText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;

            if (runUseCase == null)
            {
                Debug.LogError("[InteractionOrchestrator] runUseCase is NULL. BootstrapからInjectしてください。", this);
                _ = ui?.ShowMessageAsync("（内部エラー：UseCase未注入）", CancellationToken.None);
                return;
            }

            if (isBusy)
            {
                _ = ui?.ShowMessageAsync("（前の処理中です…）", CancellationToken.None);
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
                // if (ui != null) await ui.ShowMessageAsync($"あなた：{userText}", ct);

                var script = await runUseCase.ExecuteAsync(userText, Lang.Ja, ct);

                if (script == null || script.Utterances == null || script.Utterances.Count == 0)
                {
                    if (ui != null) await ui.ShowMessageAsync("（応答が空でした）", ct);
                    return;
                }

                foreach (var utt in script.Utterances)
                {
                    ct.ThrowIfCancellationRequested();
                    var text = utt?.Text;
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    if (ui != null) await ui.ShowMessageAsync(text, ct);
                }
            }
            catch (System.OperationCanceledException)
            {
                if (ui != null) await ui.ShowMessageAsync("（キャンセルされました）", CancellationToken.None);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex);
                if (ui != null) await ui.ShowMessageAsync($"（エラー）{ex.Message}", CancellationToken.None);
            }
            finally
            {
                isBusy = false;
            }
        }
    }
}
