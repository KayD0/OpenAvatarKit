using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace OpenAvatarKit.Infrastructure.Interface
{
    public interface ITtsClientAdapter
    {
        Task<AudioClip> SynthesizeToClipAsync(
            string text, OpenAvatarKit.Infrastructure.TTS.OpenAiTtsClientAdapter.TtsSynthesisOptions options = null,
            CancellationToken ct = default);

        Task<byte[]> SynthesizeToBytesAsync(
            string text, OpenAvatarKit.Infrastructure.TTS.OpenAiTtsClientAdapter.TtsSynthesisOptions options = null,
            CancellationToken ct = default);
    }
}
