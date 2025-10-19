namespace OpenAvatarKid.Domain.Conversation
{
    public sealed class Utterance
    {
        public string Text { get; }

        public Utterance(string text)
        {
            Text = text ?? string.Empty;
        }
    }
}
