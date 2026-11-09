using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace GateHelper.MysteryTime
{
    public class MysteryAiEngine
    {
        private readonly string _apiKey;

        public MysteryAiEngine(string apiKey = "")
        {
            _apiKey = apiKey;
        }

        public bool IsApiKeyConfigured => !string.IsNullOrWhiteSpace(_apiKey);
        public bool IsOnlineVerified { get; private set; } = false;
        private DateTime _lastVerifiedTime = DateTime.MinValue;

        /// <summary>
        /// 백그라운드에서 실제 구글 Gemini 서버와 핑 통신을 테스트하여 연결 성공 여부를 판정
        /// </summary>
        public async Task<(bool success, string errorMessage)> ValidateConnectionAsync(bool forceCheck = false)
        {
            if (!IsApiKeyConfigured)
            {
                IsOnlineVerified = false;
                return (false, "API Key가 설정되어 있지 않습니다.");
            }

            // 최근 3분 이내에 이미 온라인 검증이 성공했다면 불필요한 핑 호출을 생략하여 쿼터(RPM) 절약
            if (!forceCheck && IsOnlineVerified && (DateTime.Now - _lastVerifiedTime).TotalMinutes < 3)
            {
                return (true, string.Empty);
            }

            try
            {
                // 가벼운 ping 테스트 요청 (1단어 응답 유도)
                string pingResponse = await CallGeminiApiAsync("You are a connection test responder. Reply only with OK.", "PING");
                if (!string.IsNullOrWhiteSpace(pingResponse))
                {
                    IsOnlineVerified = true;
                    _lastVerifiedTime = DateTime.Now;
                    return (true, string.Empty);
                }

                IsOnlineVerified = false;
                return (false, "응답 내용이 비어있습니다.");
            }
            catch (WebException webEx)
            {
                IsOnlineVerified = false;
                string errorMsg = webEx.Message;
                if (webEx.Response is HttpWebResponse httpResp)
                {
                    int statusCode = (int)httpResp.StatusCode;
                    if (statusCode == 429)
                    {
                        errorMsg = "무료 API 할당량(분당 요청 한도)에 도달했습니다. 약 30초~1분 후 다시 시도해 주세요. (HTTP 429)";
                    }
                    else
                    {
                        errorMsg += $" (HTTP {statusCode})";
                    }
                }
                return (false, errorMsg);
            }
            catch (Exception ex)
            {
                IsOnlineVerified = false;
                return (false, ex.Message);
            }
        }
        public async Task<MysteryQuestion> EvaluateQuestionAsync(MysteryCase currentCase, string userQuestion, int questionNumber)
        {
            var result = new MysteryQuestion
            {
                QuestionNumber = questionNumber,
                QuestionText = userQuestion
            };

            if (currentCase == null || string.IsNullOrWhiteSpace(userQuestion))
            {
                result.AnswerType = MysteryAnswerType.SystemNotice;
                result.AnswerMessage = "질문 내용이 비어있습니다.";
                return result;
            }

            if (!IsApiKeyConfigured)
            {
                // API 키가 없을 때의 로컬 키워드 간이 처리 fallback
                return EvaluateQuestionLocally(currentCase, userQuestion, questionNumber);
            }

            try
            {
                string difficultyPolicy = "";
                if (currentCase.Difficulty == "어려움")
                {
                    difficultyPolicy = @"
[난이도: 어려움 모드 규칙 - 절대 개입 금지 및 엄격한 단답형]
- AI의 사견이나 힌트, 감정 표현, 방향성 가이드를 일절 금지한다.
- 1. 질문이 맞으면 오직: '예.'
- 2. 질문이 틀리면 오직: '아니오.'
- 3. 사건과 무관하면 오직: '사건과 관련 없습니다.'
- 4. 질문이 모호하면 오직: '질문이 모호합니다.'";
                }
                else if (currentCase.Difficulty == "보통")
                {
                    difficultyPolicy = @"
[난이도: 보통 모드 규칙 - 절제된 피드백]
- 기본적으로 단답을 유지하되, 결정적 단서에 직접 닿았을 때만 가벼운 피드백을 제공한다.
- 1. 질문이 맞고 핵심에 근접했을 때: '예! (중요한 단서입니다)'
- 2. 질문이 맞을 때: '예! (맞습니다)'
- 3. 질문이 틀렸을 때: '아니오! (아닙니다)'
- 4. 맞는 부분과 틀린 부분이 섞여있을 때: '반은 맞고 반은 틀립니다. 나누어 질문해 주세요.'
- 5. 사건과 무관할 때: '사건과 관련 없습니다.'
- 6. 질문이 모호할 때: '질문이 모호합니다. 더 구체적인 질문으로 작성해 주세요.'";
                }
                else // 쉬움
                {
                    difficultyPolicy = @"
[난이도: 쉬움 모드 규칙 - 적극적인 온도감 및 지능형 방향 피드백 제공]
- 플레이어가 추리의 재미와 몰입감을 느끼도록 친절한 게임마스터로서 방향성과 온도감 피드백을 괄호나 문장으로 제공해라. 단, 정답을 직접 스포일러하지 마라.
- 1. 질문이 맞고 결정적 핵심 트릭에 다가갔을 때: '예! (🎯 결정적인 핵심에 매우 근접했습니다!)'
- 2. 질문이 맞지만 보조 단서일 때: '예! (맞습니다)'
- 3. 질문이 틀렸지만 발상의 방향성이 좋을 때: '아니오, 하지만 그 방향의 추리는 아주 훌륭합니다.'
- 4. 질문이 틀렸고 엉뚱할 때: '아니오! (아닙니다)'
- 5. 질문에 맞는 내용과 틀린 내용이 섞여있을 때: '반은 맞고 반은 틀립니다. 두 가지 내용을 나누어서 질문해 보세요.'
- 6. 사건과 무관할 때: '사건과 관련 없습니다. 현장 공간과 남아있는 물건에 집중해 보세요.'
- 7. 질문이 모호할 때: '질문이 모호합니다. 조금 더 구체적으로 물어보세요.'";
                }

                string systemInstruction = $@"너는 추리 게임 '미스터리 타임'의 AI 진행자(Game Master)다.
사건 제목: {currentCase.Title}
난이도: {currentCase.Difficulty}
사건 현장 설명: {currentCase.Situation}
비밀 진실(정답): {currentCase.Truth}

{difficultyPolicy}

주의사항: 절대로 정답인 비밀 진실을 직접 발설하거나 노골적으로 힌트를 주지 마라. 답변은 1~2문장 이내로 명확하게 작성해라.";

                string prompt = $"유저 질문: {userQuestion}";
                string aiResponse = await CallGeminiApiAsync(systemInstruction, prompt);

                aiResponse = aiResponse.Trim();

                if (aiResponse.Contains("모호합니다") || aiResponse.Contains("모호"))
                {
                    result.AnswerType = MysteryAnswerType.Ambiguous;
                    result.AnswerMessage = aiResponse;
                }
                else if (aiResponse.StartsWith("예") || aiResponse.Contains("예!"))
                {
                    result.AnswerType = MysteryAnswerType.Yes;
                    result.AnswerMessage = aiResponse;
                }
                else if (aiResponse.StartsWith("아니오") || aiResponse.Contains("아니오!"))
                {
                    result.AnswerType = MysteryAnswerType.No;
                    result.AnswerMessage = aiResponse;
                }
                else if (aiResponse.Contains("반은 맞고"))
                {
                    result.AnswerType = MysteryAnswerType.Ambiguous;
                    result.AnswerMessage = aiResponse;
                }
                else
                {
                    result.AnswerType = MysteryAnswerType.Irrelevant;
                    result.AnswerMessage = aiResponse;
                }
            }
            catch (Exception ex)
            {
                IsOnlineVerified = false;
                LogManager.LogException(ex, LogManager.Level.Warning, "EvaluateQuestionAsync AI Call Failed. Fallback to Local.");
                var localResult = EvaluateQuestionLocally(currentCase, userQuestion, questionNumber);
                localResult.AnswerMessage = $"[사내망/네트워크 차단으로 로컬 매칭됨]\n{localResult.AnswerMessage}";
                return localResult;
            }

            return result;
        }

        /// <summary>
        /// 유저의 최종 정답 제출 검증
        /// </summary>
        public async Task<(bool isCorrect, string feedback)> EvaluateAnswerAsync(MysteryCase currentCase, string userSolution)
        {
            if (currentCase == null || string.IsNullOrWhiteSpace(userSolution))
            {
                return (false, "제출된 정답이 비어있습니다.");
            }

            if (!IsApiKeyConfigured)
            {
                // 로컬 키워드 매칭 fallback
                int matchCount = 0;
                foreach (var kw in currentCase.Keywords)
                {
                    if (userSolution.Contains(kw)) matchCount++;
                }

                bool localCorrect = matchCount >= Math.Min(1, currentCase.Keywords.Count);
                string localMsg = localCorrect 
                    ? $"[정답 축하합니다!]\n사건의 비밀 전말: {currentCase.Truth}"
                    : $"[아쉽지만 오답입니다.]\n사건의 비밀 전말: {currentCase.Truth}";

                return (localCorrect, localMsg);
            }

            try
            {
                string systemInstruction = $@"너는 추리 게임 '미스터리 타임'의 정답 검수자다.
사건 비밀 진실: {currentCase.Truth}

규칙:
유저가 작성한 사건 전말 답안이 비밀 진실의 핵심 내용(원인, 트릭, 결과)을 정확히 파악했는지 검증해라.
첫 줄에는 오직 [정답] 또는 [오답] 을 적고, 두 번째 줄부터 친절한 총평과 전체 사건의 비밀 전말 해설을 적어라.";

                string prompt = $"유저가 작성한 정답: {userSolution}";
                string aiResponse = await CallGeminiApiAsync(systemInstruction, prompt);

                bool isCorrect = aiResponse.Contains("[정답]");
                return (isCorrect, aiResponse);
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "EvaluateAnswerAsync AI Failed");
                return (false, $"[검증 중 오류 발생]\n비밀 전말: {currentCase.Truth}");
            }
        }

        /// <summary>
        /// Gemini AI를 통해 논리적 개연성을 갖춘 새로운 바다거북 스프 사건을 실시간 무한 생성
        /// </summary>
        public async Task<MysteryCase> GenerateNewCaseAsync(string difficulty, string category = "")
        {
            if (!IsApiKeyConfigured)
            {
                throw new InvalidOperationException("API Key가 설정되어 있지 않아 사건을 생성할 수 없습니다.");
            }

            string targetDiff = string.IsNullOrWhiteSpace(difficulty) || difficulty == "전체 난이도" ? "보통" : difficulty;
            string targetCat = string.IsNullOrWhiteSpace(category) ? "상황 추리" : category;

            string difficultyGuide = "";
            if (targetDiff == "쉬움")
            {
                difficultyGuide = @"- [쉬움 난이도 가이드]:
  * 고정관념을 살짝 비트는 직관적인 트릭 (예: 직업적 특수성, 신체적 반응(딸꾹질/재채기/잠꼬대), 시각장애, 착시, 동물/아이의 시점 등).
  * 상황 텍스트(situation) 안에 진실로 도달할 수 있는 '기이한 행동'이나 '언어적 단서'가 반드시 은연중에 드러나야 함.
  * 플레이어가 5~10번 내외의 상식적인 질문으로도 실마리를 좁힐 수 있어야 함.";
            }
            else if (targetDiff == "어려움")
            {
                difficultyGuide = @"- [어려움 난이도 가이드]:
  * 플레이어의 강력한 선입견(시간대, 성별, 생사 여부, 장소 등)을 교묘하게 유도하는 고도의 트릭.
  * 정답을 알고 나면 상황 설명의 모든 단어가 완벽한 복선이었음을 깨닫게 되는 정교한 구성.";
            }
            else
            {
                difficultyGuide = @"- [보통 난이도 가이드]:
  * 물리적 환경, 시간차, 도구의 독창적 활용 등 논리적 추론이 명쾌한 트릭.
  * 진실을 들었을 때 '아! 그래서 그랬구나!' 하고 무릎을 치게 만드는 탄탄한 개연성.";
            }

            string systemInstruction = $@"너는 세계적인 수평사고 추리 퍼즐(바다거북 스프 / Lateral Thinking Puzzle)의 거장이다.
풀었을 때 감탄이 나오는 명작 사건을 1개 창작해라.

[★ 절대 준수해야 할 창작 철칙]
1. '아하! 모먼트(Aha! Moment)' 필수:
   - 해답을 들었을 때 플레이어가 '아! 그래서 그랬구나!' 하고 감탄해야 한다.
   - 해답을 들었을 때 '겨우 그것 때문에? 말이 돼?'라는 실소나 반발심이 나오는 억지 콩트는 절대로 금지한다.
2. 뜬금없는 제3의 요소 금지 (복선과 개연성 필수):
   - 문제 상황에 일절 암시나 복선이 없는데, 정답에 난데없이 '파리', '바퀴벌레', '외계인', '복권' 같은 무작위 대상이 튀어나와 맞춰야 하는 '억지 찍기' 수수께끼는 엄격히 금지한다.
   - 등장인물의 동기와 행동은 현실적 상식과 인간 심리에 부합해야 한다. (예: 손님이 파리 몇 마리 쫓았다고 식당 주인이 감격해 밥값을 전액 면제한다는 식의 황당한 비약은 절대 금지)
3. 상황 설명(situation)의 구조:
   - 2~3문장으로 간결하되, 겉보기에는 상식적으로 모순되거나 기이해 보이는 장면을 연출해라.
   - 단, 그 모순은 숨겨진 진실(진짜 이유/상황)을 알게 되면 100% 완벽하게 상식적으로 해명되어야 한다.

{difficultyGuide}

반드시 아래 JSON 형식으로만 응답해라. 마크다운 백틱 없이 순수 JSON 문자열만 출력해라.

{{
  ""title"": ""사건 제목"",
  ""difficulty"": ""{targetDiff}"",
  ""category"": ""{targetCat}"",
  ""situation"": ""플레이어에게 공개할 기이하고 미스테리한 현장 상황 설명 (2~3문장)"",
  ""truth"": ""사건의 비밀 전말과 숨겨진 진실 (듣는 순간 감탄이 나오는 명쾌한 논리적 정답)"",
  ""hintText"": ""플레이어가 막혔을 때 제공할 결정적 힌트"",
  ""keywords"": [""핵심단어1"", ""핵심단어2"", ""핵심단어3""]
}}";

            string prompt = $"난이도 '{targetDiff}'에 맞고, 듣는 순간 '아하!' 하고 감탄이 나오는 완벽한 개연성의 바다거북 스프 사건을 1개 창작해줘.";
            string jsonResponse = await CallGeminiApiAsync(systemInstruction, prompt);

            jsonResponse = jsonResponse.Trim();
            if (jsonResponse.StartsWith("```json"))
            {
                jsonResponse = jsonResponse.Substring(7);
            }
            else if (jsonResponse.StartsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(3);
            }

            if (jsonResponse.EndsWith("```"))
            {
                jsonResponse = jsonResponse.Substring(0, jsonResponse.Length - 3);
            }
            jsonResponse = jsonResponse.Trim();

            var obj = JObject.Parse(jsonResponse);

            var newCase = new MysteryCase
            {
                Id = "AI_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"),
                Title = obj["title"]?.ToString() ?? "미지의 사건",
                Difficulty = obj["difficulty"]?.ToString() ?? targetDiff,
                Category = obj["category"]?.ToString() ?? targetCat,
                Situation = obj["situation"]?.ToString() ?? "기이한 사건이 발생했습니다.",
                Truth = obj["truth"]?.ToString() ?? "사건의 진실입니다.",
                HintText = obj["hintText"]?.ToString() ?? "상황을 다시 꼼꼼히 살펴보세요.",
                IsGeneratedByAi = true
            };

            if (obj["keywords"] is JArray kwArray)
            {
                foreach (var kw in kwArray)
                {
                    if (kw != null) newCase.Keywords.Add(kw.ToString());
                }
            }

            return newCase;
        }

        private MysteryQuestion EvaluateQuestionLocally(MysteryCase currentCase, string userQuestion, int questionNumber)
        {
            var q = new MysteryQuestion
            {
                QuestionNumber = questionNumber,
                QuestionText = userQuestion
            };

            bool match = false;
            foreach (var kw in currentCase.Keywords)
            {
                if (userQuestion.Contains(kw))
                {
                    match = true;
                    break;
                }
            }

            if (match)
            {
                q.AnswerType = MysteryAnswerType.Yes;
                q.AnswerMessage = "예! (핵심 관련 내용입니다)";
            }
            else
            {
                q.AnswerType = MysteryAnswerType.Irrelevant;
                q.AnswerMessage = "사건과 크게 관련이 없거나 중요하지 않습니다.";
            }

            return q;
        }

        private static readonly string[] ModelCandidates = new[]
        {
            "gemini-2.5-flash",
            "gemini-flash-latest",
            "gemini-3.5-flash-lite",
            "gemini-3.5-flash",
            "gemini-pro-latest"
        };

        private string _activeModel = "gemini-2.5-flash";

        private async Task<string> CallGeminiApiAsync(string systemInstruction, string userPrompt)
        {
            Exception lastException = null;

            var modelsToTry = new List<string> { _activeModel };
            foreach (var m in ModelCandidates)
            {
                if (!modelsToTry.Contains(m)) modelsToTry.Add(m);
            }

            foreach (var modelName in modelsToTry)
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={_apiKey}";

                    string jsonBody = new JObject
                    {
                        ["contents"] = new JArray
                        {
                            new JObject
                            {
                                ["role"] = "user",
                                ["parts"] = new JArray
                                {
                                    new JObject { ["text"] = systemInstruction + "\n\n" + userPrompt }
                                }
                            }
                        }
                    }.ToString(Newtonsoft.Json.Formatting.None);

                    byte[] bytes = Encoding.UTF8.GetBytes(jsonBody);

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.ContentLength = bytes.Length;
                    request.Timeout = 12000;

                    using (Stream requestStream = await request.GetRequestStreamAsync())
                    {
                        await requestStream.WriteAsync(bytes, 0, bytes.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                    using (Stream responseStream = response.GetResponseStream())
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.UTF8))
                    {
                        string jsonResponse = await reader.ReadToEndAsync();
                        try
                        {
                            var jsonObj = JObject.Parse(jsonResponse);
                            var textToken = jsonObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"];
                            if (textToken != null)
                            {
                                string text = textToken.ToString().Trim();
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    _activeModel = modelName; // 성공한 모델 캐싱
                                    return text;
                                }
                            }
                        }
                        catch (Exception parseEx)
                        {
                            LogManager.LogException(parseEx, LogManager.Level.Warning, "Gemini API Response JSON Parse Failed");
                        }

                        return string.Empty;
                    }
                }
                catch (WebException webEx)
                {
                    lastException = webEx;
                    if (webEx.Response is HttpWebResponse httpResp)
                    {
                        int statusCode = (int)httpResp.StatusCode;
                        if (statusCode == 404 || statusCode == 429)
                        {
                            // 404(모델 없음) 또는 429(해당 모델 분당 할당량 초과)
                            // -> 다음 가용 모델로 자동 우회(Fallback) 시도
                            LogManager.LogMessage($"[Gemini] 모델 '{modelName}'에서 HTTP {statusCode} 발생. 다른 모델로 우회합니다.", LogManager.Level.Warning);
                            await Task.Delay(600); // 가벼운 지연 후 다른 모델 시도
                            continue;
                        }
                    }
                    throw; // 다른 에러(인증 실패 400/403 등)는 상위로 던짐
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    throw;
                }
            }

            if (lastException != null) throw lastException;
            return string.Empty;
        }
    }
}
