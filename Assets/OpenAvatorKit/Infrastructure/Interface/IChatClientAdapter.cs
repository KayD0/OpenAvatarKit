using Cysharp.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;
using System.Threading;

namespace OpenAvatarKid.Infrastructure.Interface
{
    public interface IChatClientAdapter
    {
        /// <summary>
        /// ���[�U���͂����Ƃ� LLM �����{ Script ���擾
        /// </summary>
        UniTask<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct);
    }
}
