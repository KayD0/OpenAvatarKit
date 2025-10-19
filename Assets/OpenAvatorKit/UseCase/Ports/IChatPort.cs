using System.Threading;
using System.Threading.Tasks;
using OpenAvatarKid.Domain.Conversation;

namespace OpenAvatarKid.UseCases.Ports
{
    public interface IChatPort
    {
        /// <summary>
        /// ���[�U���͂����Ƃ� LLM �����{ Script ���擾
        /// </summary>
        Task<ConversationScript> GetScriptAsync(string userText, Lang lang, CancellationToken ct);
    }
}
