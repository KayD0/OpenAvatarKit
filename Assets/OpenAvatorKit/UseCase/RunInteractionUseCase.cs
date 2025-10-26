using System.Threading;
using System.Threading.Tasks;
using OpenAvatarKit.Domain.Conversation;
using OpenAvatarKit.Domain.Services;
using OpenAvatarKit.Infrastructure.Interface;

namespace OpenAvatarKit.UseCases.Interactors
{
    public sealed class RunInteractionUseCase
    {
        private readonly IChatClientAdapter chat;

        public RunInteractionUseCase(IChatClientAdapter chat)
        {
            this.chat = chat;
        }

        public async Task<ConversationScript> ExecuteAsync(string userText, Lang lang, CancellationToken ct)
        {
            var script = await chat.GetScriptAsync(userText, lang, ct);
            return ScriptValidator.Clamp(script, maxCharsPerUtterance: 200);
        }
    }
}
