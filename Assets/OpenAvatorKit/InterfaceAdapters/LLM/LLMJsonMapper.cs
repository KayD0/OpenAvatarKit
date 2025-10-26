using System;
using System.Collections.Generic;
using OpenAvatarKid.Domain.Conversation;

namespace OpenAvatarKid.InterfaceAdapters.LLM
{
    /// <summary>
    /// LLM��DTO��Domain���f���iConversationScript/Utterance�j�փ}�b�s���O�B
    /// �Enull���S
    /// �E�f�t�H���g�⊮�ibetweenPauseSec=1.2f�A�\��/���[�V����/���x�j
    /// �E�����̐��K���i���������ȂǍŏ����j
    /// </summary>
    public static class LLMJsonMapper
    {
        // �݌v��̌Œ����l
        private const float DefaultBetweenPauseSec = 1.2f;
        private const string DefaultFace = "neutral";
        private const string DefaultBody = "idle";
        private const float DefaultEmotion = 0.3f;

        /// <summary>
        /// �����̊O������ Lang ��D��iSTT��UI�ݒ�j�BDTO.lang �͕⏕�I�ɂ̂ݎg�p�������ꍇ�͂�����𐄏��B
        /// </summary>
        public static ConversationScript ToDomain(LLMScriptDto dto, Lang lang)
        {
            if (dto == null)
            {
                return Fallback(lang, "(LLM�G���[: ��̃��X�|���X)");
            }

            // timing.betweenPauseSec ��⊮�i���w��/0�ȉ��� 1.2f ���̗p�j
            var pause = (dto.timing != null && dto.timing.betweenPauseSec > 0f)
                ? dto.timing.betweenPauseSec
                : DefaultBetweenPauseSec;

            var utterances = new List<Utterance>();

            if (dto.utterances != null && dto.utterances.Count > 0)
            {
                foreach (var u in dto.utterances)
                {
                    if (u == null) continue;

                    // �l�̕⊮�Ɛ��K��
                    var text = u.text ?? string.Empty;
                    var face = string.IsNullOrWhiteSpace(u.faceExpression) ? DefaultFace : u.faceExpression.Trim();
                    var body = string.IsNullOrWhiteSpace(u.bodyExpression) ? DefaultBody : u.bodyExpression.Trim();
                    var emo = Clamp01(u.emotionLevel ?? DefaultEmotion);

                    // Domain Utterance ��
                    utterances.Add(new Utterance(
                        text: text,
                        faceExpression: face,
                        bodyExpression: body,
                        emotionLevel: emo));
                }
            }

            // ���b��0���Ȃ�t�H�[���o�b�N��1����������
            if (utterances.Count == 0)
            {
                utterances.Add(new Utterance(
                    text: "(�����Ȃ�)",
                    faceExpression: DefaultFace,
                    bodyExpression: DefaultBody,
                    emotionLevel: 0.0f));
            }

            return new ConversationScript(lang, pause, utterances.AsReadOnly());
        }

        /// <summary>
        /// DTO����lang���g���� Lang �����߂����ꍇ�̃I�[�o�[���[�h�B
        /// �i�����R�[�h�݊��̂��ߎc�u�B�ʏ�͊O������ł��g�����Ƃ𐄏��j
        /// </summary>
        public static ConversationScript ToDomain(LLMScriptDto dto)
        {
            var lang = ToLang(dto?.lang);
            return ToDomain(dto, lang);
        }

        private static Lang ToLang(string code)
        {
            switch (code?.Trim().ToLowerInvariant())
            {
                case "en": return Lang.En;
                case "ja":
                default: return Lang.Ja;
            }
        }

        private static float Clamp01(float v) => Math.Clamp(v, 0f, 1f);

        private static ConversationScript Fallback(Lang lang, string message)
        {
            return new ConversationScript(
                lang,
                DefaultBetweenPauseSec,
                new[]
                {
                    new Utterance(message, DefaultFace, DefaultBody, 0.0f)
                });
        }
    }
}
