using Cysharp.Threading.Tasks;
using OpenAvatarKit.Domain.Conversation;
using OpenAvatarKit.UseCases.Interactors;
using System.Threading;
using UnityEngine;

namespace OpenAvatarKit.Presentation.Controller
{
    public sealed class InteractionOrchestrator : MonoBehaviour
    {
        [Header("UI (入出力)")]
        [SerializeField] private SimpleMessageWindow ui; // 入出力を兼ねる

        [Header("Audio (TTS出力)")]
        [SerializeField] private SimpleSpeechSynthesizer synth; // ← 追加

        [Header("Timing")]
        [SerializeField, Range(0f, 2f)] private float betweenPauseSec = 0.3f; // 発話間の無音

        // Bootstrap から注入
        private RunInteractionUseCase runUseCase;

        private CancellationTokenSource cts;
        private bool isBusy;

        public void Inject(RunInteractionUseCase useCase, SimpleSpeechSynthesizer speechSynth = null)
        {
            runUseCase = useCase;
            if (speechSynth != null) synth = speechSynth;
            Debug.Log("[InteractionOrchestrator] Injected RunInteractionUseCase / Synth.");
        }

        private void OnEnable()
        {
            if (ui == null) Debug.LogError("[InteractionOrchestrator] ui(SimpleMessageWindow) is NOT assigned.", this);
            if (synth == null) Debug.LogWarning("[InteractionOrchestrator] synth(SimpleSpeechSynthesizer) is NOT assigned.", this);

            if (ui != null) ui.OnSubmit += HandleUserText;
        }

        private void OnDisable()
        {
            if (ui != null) ui.OnSubmit -= HandleUserText;

            // 実行中処理のクリーンアップ
            try { synth?.Interrupt(); } catch { /* ignore */ }

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

            // 進行中のTTS/フローを中断
            try { synth?.Interrupt(); } catch { /* ignore */ }

            cts?.Cancel();
            cts = new CancellationTokenSource();

            _ = RunFlowAsync(text, cts.Token);
        }

        private async UniTask RunFlowAsync(string userText, CancellationToken ct)
        {
            isBusy = true;
            try
            {
                // 入力表示（必要なら）
                // if (ui != null) await ui.ShowMessageAsync($"あなた：{userText}", ct);

                // 1) LLM台本を取得
                var script = await runUseCase.ExecuteAsync(userText, Lang.Ja, ct);

                if (script == null || script.Utterances == null || script.Utterances.Count == 0)
                {
                    if (ui != null) await ui.ShowMessageAsync("（応答が空でした）", ct);
                    return;
                }

                // 2) 台本の各Utteranceを順番に UI表示 → TTS再生
                foreach (var utt in script.Utterances)
                {
                    ct.ThrowIfCancellationRequested();

                    var text = utt?.Text;
                    if (string.IsNullOrWhiteSpace(text)) continue;

                    // UI表示
                    if (ui != null) await ui.ShowMessageAsync(text, ct);

                    // TTS（割込対応 / キャンセル伝播）
                    if (synth != null)
                    {
                        // 注意: synth内部はAudioSource.Play()で非同期再生 -> 完了までawait
                        await synth.SpeakAsync(text); // ← ここでキャンセル/割込は伝播
                    }

                    // 発話間の間合い（任意）
                    if (betweenPauseSec > 0f)
                    {
                        await UniTask.Delay((int)(betweenPauseSec * 1000f), cancellationToken: ct);
                    }
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
