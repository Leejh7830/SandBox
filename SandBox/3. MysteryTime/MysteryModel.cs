using System;
using System.Collections.Generic;

namespace GateHelper.MysteryTime
{
    public enum MysteryAnswerType
    {
        Yes,            // 예
        No,             // 아니오
        Irrelevant,     // 상관없음 (사건과 무관/중요하지 않음)
        Ambiguous,      // 질문이 모호함 (구체적 문장으로 다시 작성 요청)
        SystemNotice    // 시스템 안내 / 오류
    }

    public class MysteryCase
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Difficulty { get; set; } = "보통"; // 쉬움, 보통, 어려움
        public string Category { get; set; } = "트릭"; // 트릭, 심리, 일상, 공장/설비
        public string Situation { get; set; } // 유저에게 보이는 수수께끼 현장 설명
        public string Truth { get; set; }     // AI만 알고 있는 사건의 정답 전말
        public string HintText { get; set; }  // 힌트 보기 시 텍스트 힌트
        public string MainImagePrompt { get; set; } // 메인 이미지 프롬프트 (영어)
        public string HintImagePrompt { get; set; } // 힌트 이미지 프롬프트 (영어)
        public List<string> Keywords { get; set; } = new List<string>(); // 정답 검증용 주요 키워드
        public bool IsGeneratedByAi { get; set; } = false;

        public MysteryCase()
        {
            Id = Guid.NewGuid().ToString("N");
        }
    }

    public class MysteryQuestion
    {
        public int QuestionNumber { get; set; }
        public string QuestionText { get; set; }
        public MysteryAnswerType AnswerType { get; set; }
        public string AnswerMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
