using System;

namespace OpenAvatarKid.Domain.Conversation
{
    /// <summary>
    /// �A�o�^�[��1���b�f�[�^
    /// </summary>
    [Serializable]
    public sealed class Utterance
    {
        public string Text { get; }
        public string FaceExpression { get; }
        public string BodyExpression { get; }
        public float EmotionLevel { get; }

        public Utterance(string text, string faceExpression, string bodyExpression, float emotionLevel)
        {
            Text = text ?? string.Empty;
            FaceExpression = faceExpression ?? "neutral";
            BodyExpression = bodyExpression ?? "idle";
            EmotionLevel = Math.Clamp(emotionLevel, 0f, 1f);
        }

        public override string ToString()
            => $"[Utterance] {Text} (Face={FaceExpression}, Body={BodyExpression}, Emotion={EmotionLevel:0.0})";
    }
}
