using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using OpenAvatarKit.Infrastructure.Interface;

namespace OpenAvatarKit.Presentation.Controller
{
    /// <summary>
    /// 軽量版 SpeechSynthesizer
    /// - ITtsClientAdapter に依存（OpenAI / VoiceVox / Azure 等差し替え可）
    /// - AudioSource による再生
    /// - 割込・完了イベント付き
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("OpenAvatarKit/Audio/Simple Speech Synthesizer")]
    public sealed class SimpleSpeechSynthesizer : MonoBehaviour
    {
        [Header("Audio Output")]
        [SerializeField] private AudioSource audioSource;

        [Header("TTS Adapter")]
        public ITtsClientAdapter ttsClient;

        // イベント
        public event Action<string> OnSpeakStart;
        public event Action<string> OnSpeakComplete;
        public event Action<string> OnInterrupted;
        public event Action<Exception> OnError;

        // 再生制御
        private CancellationTokenSource _cts;

        private void Awake()
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// 指定テキストをTTS→AudioSource再生
        /// </summary>
        public async Task SpeakAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return;

            // 再生中なら割込
            if (audioSource.isPlaying)
                Interrupt();

            _cts = new CancellationTokenSource();
            OnSpeakStart?.Invoke(text);

            try
            {
                // 1. TTS生成
                var clip = await ttsClient.SynthesizeToClipAsync(text, ct: _cts.Token);
                if (_cts.IsCancellationRequested) return;

                // 2. AudioSource再生
                audioSource.clip = clip;
                audioSource.Play();

                // 3. 再生完了待機
                await WaitForEndAsync(audioSource, _cts.Token);
                if (!_cts.IsCancellationRequested)
                    OnSpeakComplete?.Invoke(text);
            }
            catch (OperationCanceledException)
            {
                OnInterrupted?.Invoke(text);
            }
            catch (Exception ex)
            {
                Debug.LogError($"SimpleSpeechSynthesizer Error: {ex}");
                OnError?.Invoke(ex);
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
            }
        }

        /// <summary>
        /// 現在の発話を中断
        /// </summary>
        public void Interrupt()
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                audioSource.Stop();
                OnInterrupted?.Invoke(audioSource.clip?.name ?? "(unknown)");
            }
        }

        /// <summary>
        /// 全停止（割込と同じだがイベント省略可）
        /// </summary>
        public void Stop(bool raiseEvent = false)
        {
            _cts?.Cancel();
            audioSource.Stop();
            if (raiseEvent)
                OnInterrupted?.Invoke(audioSource.clip?.name ?? "(unknown)");
        }

        private static async Task WaitForEndAsync(AudioSource source, CancellationToken ct)
        {
            while (source != null && source.isPlaying)
            {
                if (ct.IsCancellationRequested)
                    break;
                await Task.Yield();
            }
        }
    }
}
