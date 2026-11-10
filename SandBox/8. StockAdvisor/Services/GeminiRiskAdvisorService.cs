using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GateHelper.StockAdvisor.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GateHelper.StockAdvisor.Services
{
    public class DiagnosisResult
    {
        public bool IsSuccess { get; set; } = false;
        public string ErrorMessage { get; set; } = string.Empty;
        public MarketBriefing Briefing { get; set; } = new MarketBriefing();
        public List<StockRiskDiagnosis> ItemDiagnoses { get; set; } = new List<StockRiskDiagnosis>();
    }

    /// <summary>
    /// Gemini LLM 기반 주식 리스크 분석 및 3축 포지션 진단 서비스 (Prompt Engine)
    /// </summary>
    public static class GeminiRiskAdvisorService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(12)
        };

        private static readonly string[] ModelCandidates = new[]
        {
            "gemini-2.0-flash",
            "gemini-1.5-flash",
            "gemini-2.5-flash",
            "gemini-flash-latest",
            "gemini-pro-latest"
        };

        private static string _activeModel = "gemini-2.0-flash";

        public static string GetApiKey()
        {
            try
            {
                return Util_Option.LoadUserOptions()?.GeminiApiKey ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public static async Task<DiagnosisResult> DiagnosePortfolioAsync(List<StockHoldingItem> items, GlobalMarketSnapshot marketIntel = null)
        {
            var result = new DiagnosisResult();

            if (items == null || items.Count == 0)
            {
                result.ErrorMessage = "진단할 보유 종목이 없습니다. 종목을 먼저 등록해 주세요.";
                return result;
            }

            string apiKey = GetApiKey();
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                // API 키 미설정 시 휴리스틱 룰베이스 폴백 진단 수행
                return RunHeuristicFallback(items, "💡 [오프라인 자체 룰 진단 모드] Gemini API 키가 미등록되어 손익률 및 변동성 기반 규칙 엔진으로 분석을 완료했습니다. (우측 상단 [🔑 API 설정]에서 키를 등록하시면 실시간 글로벌 AI 진단이 활성화됩니다.)");
            }

            try
            {
                var sbPrompt = new StringBuilder();
                sbPrompt.AppendLine("당신은 글로벌 매크로 정책, 미국 수출 통제(BIS), 반도체/IT 실적 및 국내외 증시 리스크를 냉철하게 분석하는 최고 수준의 [AI 주식 리스크 수석 참모]입니다.");
                sbPrompt.AppendLine("투자자의 뇌동매매와 패닉셀을 방지하고, 명확한 근거와 정량 수치 기반으로 냉정하고 실행 가능한 포지션 조언을 제공하세요.");
                sbPrompt.AppendLine();

                if (marketIntel != null)
                {
                    sbPrompt.AppendLine("[실시간 글로벌 매크로 & 미국 반도체 지표]");
                    foreach (var idx in marketIntel.Indices)
                    {
                        sbPrompt.AppendLine($"- {idx.Name}({idx.Symbol}): {idx.ClosePrice} ({idx.ChangeRate:+0.00;-0.00}%)");
                    }
                    if (marketIntel.RecentPolicyHeadlines.Count > 0)
                    {
                        sbPrompt.AppendLine("- 최근 미 연방 관보(수출통제/규제): " + string.Join(" / ", marketIntel.RecentPolicyHeadlines));
                    }
                    sbPrompt.AppendLine();
                }

                sbPrompt.AppendLine("[현재 포트폴리오 종목 목록]");
                foreach (var item in items)
                {
                    sbPrompt.AppendLine($"- 종목: {item.StockName}({item.StockCode}), 보유수량: {item.Quantity:N0}주, 매수가: {item.BuyPrice:N0}원, 현재가: {item.CurrentPrice:N0}원, 수익률: {item.ProfitRate:F2}%, 포트폴리오 비중: {item.WeightPercent:F1}%, 메모: {item.Memo}");
                }

                sbPrompt.AppendLine();
                sbPrompt.AppendLine("다음 요구사항에 맞춰 **반드시 순수한 JSON 포맷**으로만 응답하세요(코드블록이나 마크다운 백틱 제외):");
                sbPrompt.AppendLine("※ 중요 서식 규칙: JSON 문자열 값 내부에는 절대 큰따옴표(\")를 넣지 마시고 작은따옴표(')나 괄호를 사용하세요. 문자열 내 줄바꿈을 넣지 마세요.");
                sbPrompt.AppendLine("{");
                sbPrompt.AppendLine("  \"overallRisk\": \"Safe\" | \"Caution\" | \"Panic\",");
                sbPrompt.AppendLine("  \"line1\": \"장전 3줄 핵심 요약 1행 (글로벌 증시 및 거시 정책 핵심)\",");
                sbPrompt.AppendLine("  \"line2\": \"장전 3줄 핵심 요약 2행 (국내 증시 및 주력 섹터 영향)\",");
                sbPrompt.AppendLine("  \"line3\": \"장전 3줄 핵심 요약 3행 (오늘의 포트폴리오 핵심 행동 지침)\",");
                sbPrompt.AppendLine("  \"diagnoses\": [");
                sbPrompt.AppendLine("    {");
                sbPrompt.AppendLine("      \"stockCode\": \"005930\",");
                sbPrompt.AppendLine("      \"reliability\": 1~5 (출처 신뢰도),");
                sbPrompt.AppendLine("      \"policyImpact\": -5~+5 (정책/실적 직결도: -5는 치명적 악재, +5는 강력 수혜),");
                sbPrompt.AppendLine("      \"group\": \"GroupA\"(반등시 손절/축소) | \"GroupB\"(관망/홀딩) | \"GroupC\"(분할매수 검토),");
                sbPrompt.AppendLine("      \"action\": \"구체적 실행 권고 (예: [익절] 비중 30% 축소 권고 / [관망] 관망 유지 / [매수] 분할 매수 검토)\",");
                sbPrompt.AppendLine("      \"targetCutPrice\": 권고 손절가 숫자,");
                sbPrompt.AppendLine("      \"reason\": \"진단 상세 사유 및 대응 논리 (작은따옴표 사용, 2~3문장)\"");
                sbPrompt.AppendLine("    }");
                sbPrompt.AppendLine("  ]");
                sbPrompt.AppendLine("}");

                string requestBody = JsonConvert.SerializeObject(new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = sbPrompt.ToString() }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.2,
                        maxOutputTokens = 2048,
                        responseMimeType = "application/json"
                    }
                });

                var modelsToTry = new List<string> { _activeModel };
                foreach (var m in ModelCandidates)
                {
                    if (!modelsToTry.Contains(m)) modelsToTry.Add(m);
                }

                string rawText = string.Empty;
                string lastErrorStatus = string.Empty;

                foreach (var modelName in modelsToTry)
                {
                    try
                    {
                        string apiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                        using (var content = new StringContent(requestBody, Encoding.UTF8, "application/json"))
                        {
                            var response = await _httpClient.PostAsync(apiUrl, content).ConfigureAwait(false);
                            if (!response.IsSuccessStatusCode)
                            {
                                int statusCode = (int)response.StatusCode;
                                lastErrorStatus = response.StatusCode.ToString();
                                LogManager.LogMessage($"[GeminiRiskAdvisor] 모델 '{modelName}' HTTP {statusCode} 발생. 다음 가용 모델로 우회합니다.", LogManager.Level.Warning);
                                continue;
                            }

                            string resJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            var resObj = JObject.Parse(resJson);
                            rawText = resObj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(rawText))
                            {
                                _activeModel = modelName; // 성공한 모델 캐싱
                                break;
                            }
                        }
                    }
                    catch (Exception loopEx)
                    {
                        LogManager.LogException(loopEx, LogManager.Level.Warning, $"[GeminiRiskAdvisor] 모델 '{modelName}' 예외");
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    return RunHeuristicFallback(items, $"Gemini API 가용 모델 호출 실패 ({lastErrorStatus}): 자체 규칙 진단으로 전환되었습니다.");
                }

                    // 순수 JSON 추출
                    rawText = rawText.Trim();
                    if (rawText.StartsWith("```json")) rawText = rawText.Substring(7);
                    if (rawText.StartsWith("```")) rawText = rawText.Substring(3);
                    if (rawText.EndsWith("```")) rawText = rawText.Substring(0, rawText.Length - 3);
                    rawText = rawText.Trim();

                    // 첫 번째 { 부터 마지막 } 까지 잘라내기
                    int startIdx = rawText.IndexOf('{');
                    int endIdx = rawText.LastIndexOf('}');
                    if (startIdx >= 0 && endIdx > startIdx)
                    {
                        rawText = rawText.Substring(startIdx, endIdx - startIdx + 1);
                    }

                    JObject parsed = null;
                    try
                    {
                        parsed = JObject.Parse(rawText);
                    }
                    catch (Exception jsonEx)
                    {
                        LogManager.LogException(jsonEx, LogManager.Level.Warning, "GeminiRiskAdvisorService.JObject.Parse fallback to Regex");
                        parsed = ExtractFieldsWithRegex(rawText);
                    }

                    result.Briefing = new MarketBriefing
                    {
                        Line1 = parsed["line1"]?.ToString() ?? "글로벌 매크로 리스크 모니터링 중입니다.",
                        Line2 = parsed["line2"]?.ToString() ?? "개별 종목별 정책 영향도를 점검하세요.",
                        Line3 = parsed["line3"]?.ToString() ?? "감정적 매매를 지양하고 권고 지침을 준수하세요.",
                        GeneratedAt = DateTime.Now
                    };

                    string riskStr = parsed["overallRisk"]?.ToString() ?? "Safe";
                    if (Enum.TryParse<OverallRiskLevel>(riskStr, true, out var rLevel))
                    {
                        result.Briefing.RiskLevel = rLevel;
                    }

                    var diagArray = parsed["diagnoses"] as JArray;
                    if (diagArray != null)
                    {
                        foreach (var token in diagArray)
                        {
                            string code = token["stockCode"]?.ToString() ?? string.Empty;
                            var matchedItem = items.Find(x => x.StockCode.TrimStart('0') == code.TrimStart('0'));
                            string name = matchedItem?.StockName ?? code;

                            int rel = token["reliability"]?.ToObject<int>() ?? 3;
                            int pol = token["policyImpact"]?.ToObject<int>() ?? 0;
                            string grpStr = token["group"]?.ToString() ?? "GroupB";
                            Enum.TryParse<PositionGroup>(grpStr, true, out var posGroup);

                            string act = token["action"]?.ToString() ?? "관망 유지";
                            decimal cut = token["targetCutPrice"]?.ToObject<decimal>() ?? 0m;
                            string rsn = token["reason"]?.ToString() ?? string.Empty;

                            result.ItemDiagnoses.Add(new StockRiskDiagnosis
                            {
                                StockCode = code,
                                StockName = name,
                                SourceReliability = Math.Max(1, Math.Min(5, rel)),
                                PolicyImpact = Math.Max(-5, Math.Min(5, pol)),
                                Group = posGroup,
                                RecommendedAction = act,
                                TargetCutPrice = cut,
                                ReasonSummary = rsn,
                                DiagnosedAt = DateTime.Now
                            });
                        }
                    }

                    result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                LogManager.LogException(ex, LogManager.Level.Warning, "GeminiRiskAdvisorService.DiagnosePortfolioAsync");
                return RunHeuristicFallback(items, $"AI 통신 예외 발생 ({ex.Message}): 자체 규칙 진단으로 전환되었습니다.");
            }

            return result;
        }

        private static JObject ExtractFieldsWithRegex(string text)
        {
            var obj = new JObject();

            var mRisk = Regex.Match(text, @"""overallRisk""\s*:\s*""?(\w+)""?", RegexOptions.IgnoreCase);
            obj["overallRisk"] = mRisk.Success ? mRisk.Groups[1].Value : "Caution";

            var mLine1 = Regex.Match(text, @"""line1""\s*:\s*""(.*?)(?<!\\)""\s*,", RegexOptions.Singleline);
            if (!mLine1.Success) mLine1 = Regex.Match(text, @"""line1""\s*:\s*""(.*?)""", RegexOptions.Singleline);
            obj["line1"] = mLine1.Success ? CleanJsonText(mLine1.Groups[1].Value) : "글로벌 매크로 리스크 모니터링 중입니다.";

            var mLine2 = Regex.Match(text, @"""line2""\s*:\s*""(.*?)(?<!\\)""\s*,", RegexOptions.Singleline);
            if (!mLine2.Success) mLine2 = Regex.Match(text, @"""line2""\s*:\s*""(.*?)""", RegexOptions.Singleline);
            obj["line2"] = mLine2.Success ? CleanJsonText(mLine2.Groups[1].Value) : "개별 종목별 정책 영향도를 점검하세요.";

            var mLine3 = Regex.Match(text, @"""line3""\s*:\s*""(.*?)(?<!\\)""\s*,", RegexOptions.Singleline);
            if (!mLine3.Success) mLine3 = Regex.Match(text, @"""line3""\s*:\s*""(.*?)""", RegexOptions.Singleline);
            obj["line3"] = mLine3.Success ? CleanJsonText(mLine3.Groups[1].Value) : "감정적 매매를 지양하고 권고 지침을 준수하세요.";

            var diagArray = new JArray();
            var itemMatches = Regex.Matches(text, @"\{[^{}]*""stockCode""[^{}]*\}", RegexOptions.Singleline);
            foreach (Match m in itemMatches)
            {
                string block = m.Value;
                var itemObj = new JObject();

                var mCode = Regex.Match(block, @"""stockCode""\s*:\s*""?(\w+)""?");
                itemObj["stockCode"] = mCode.Success ? mCode.Groups[1].Value : "";

                var mRel = Regex.Match(block, @"""reliability""\s*:\s*(\d+)");
                itemObj["reliability"] = mRel.Success ? int.Parse(mRel.Groups[1].Value) : 3;

                var mPol = Regex.Match(block, @"""policyImpact""\s*:\s*([+-]?\d+)");
                itemObj["policyImpact"] = mPol.Success ? int.Parse(mPol.Groups[1].Value) : 0;

                var mGrp = Regex.Match(block, @"""group""\s*:\s*""?(\w+)""?");
                itemObj["group"] = mGrp.Success ? mGrp.Groups[1].Value : "GroupB";

                var mAct = Regex.Match(block, @"""action""\s*:\s*""(.*?)""", RegexOptions.Singleline);
                itemObj["action"] = mAct.Success ? CleanJsonText(mAct.Groups[1].Value) : "관망 유지";

                var mCut = Regex.Match(block, @"""targetCutPrice""\s*:\s*([0-9.]+)");
                itemObj["targetCutPrice"] = mCut.Success ? decimal.Parse(mCut.Groups[1].Value) : 0m;

                var mRsn = Regex.Match(block, @"""reason""\s*:\s*""(.*?)""", RegexOptions.Singleline);
                itemObj["reason"] = mRsn.Success ? CleanJsonText(mRsn.Groups[1].Value) : "정상 진단 완료";

                diagArray.Add(itemObj);
            }
            obj["diagnoses"] = diagArray;
            return obj;
        }

        private static string CleanJsonText(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            return input.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'").Trim();
        }

        /// <summary>
        /// API 키가 없거나 통신 실패 시 즉각 가동되는 자체 룰베이스 휴리스틱 진단 엔진
        /// </summary>
        private static DiagnosisResult RunHeuristicFallback(List<StockHoldingItem> items, string note)
        {
            var res = new DiagnosisResult { IsSuccess = true, ErrorMessage = note };
            res.Briefing = new MarketBriefing
            {
                RiskLevel = OverallRiskLevel.Caution,
                Line1 = "◆ [자체 룰 엔진] 계좌 변동성 및 개별 손익률 기반으로 리스크를 분석했습니다.",
                Line2 = "• 손실폭이 크거나 비중이 과도한 종목은 반등 시 단계적 분할 손절을 권고합니다.",
                Line3 = "• Gemini API 키를 등록하시면 외신/수출통제 기반 초정밀 AI 참모 분석이 활성화됩니다.",
                GeneratedAt = DateTime.Now
            };

            foreach (var item in items)
            {
                var diag = new StockRiskDiagnosis
                {
                    StockCode = item.StockCode,
                    StockName = item.StockName,
                    DiagnosedAt = DateTime.Now
                };

                // 손익률 및 현재가 기준 자체 정량화
                decimal profitRate = item.ProfitRate;
                if (profitRate <= -15m)
                {
                    diag.SourceReliability = 4;
                    diag.PolicyImpact = -4;
                    diag.Group = PositionGroup.GroupA;
                    diag.RecommendedAction = "[경보] 반등 시 비중 40~50% 축소 손절 권고";
                    diag.TargetCutPrice = Math.Round(item.CurrentPrice * 0.95m, 0);
                    diag.ReasonSummary = $"누적 손실률이 {profitRate:F1}%에 달해 리스크 관리가 시급합니다. 추가 하락 시 손실 고착화 방지를 위해 반등 구간에서 단계적 비중 축소가 필요합니다.";
                }
                else if (profitRate <= -7m)
                {
                    diag.SourceReliability = 3;
                    diag.PolicyImpact = -2;
                    diag.Group = PositionGroup.GroupA;
                    diag.RecommendedAction = "[주의] 지지선 이탈 시 20% 분할 손절 준비";
                    diag.TargetCutPrice = Math.Round(item.CurrentPrice * 0.96m, 0);
                    diag.ReasonSummary = $"현재 손실률 {profitRate:F1}%로 경계 구간입니다. 전저점 지지 여부를 확인하고 이탈 시 기계적 손절 라인을 준수하세요.";
                }
                else if (profitRate >= 15m)
                {
                    diag.SourceReliability = 4;
                    diag.PolicyImpact = +3;
                    diag.Group = PositionGroup.GroupB;
                    diag.RecommendedAction = "[익절] 수익 실현(25%) 및 트레일링 스탑 적용";
                    diag.TargetCutPrice = Math.Round(item.CurrentPrice * 0.93m, 0);
                    diag.ReasonSummary = $"수익률 {profitRate:F1}%로 양호한 흐름입니다. 추세가 유지되는 동안 홀딩하되, 급격한 변동성에 대비해 일부 익절 후 트레일링 스탑을 권장합니다.";
                }
                else if (profitRate >= 0m)
                {
                    diag.SourceReliability = 3;
                    diag.PolicyImpact = +1;
                    diag.Group = PositionGroup.GroupB;
                    diag.RecommendedAction = "[관망] 관망 및 목표가 도달 시 분할 익절";
                    diag.TargetCutPrice = Math.Round(item.BuyPrice * 0.97m, 0);
                    diag.ReasonSummary = $"평단 부근에서 안정적인 흐름을 유지하고 있습니다. 급격한 추세 전환 전까지 관망하며 지지선을 관찰하세요.";
                }
                else
                {
                    diag.SourceReliability = 3;
                    diag.PolicyImpact = -1;
                    diag.Group = PositionGroup.GroupB;
                    diag.RecommendedAction = "[방어] 평단가 방어 관망 (추가 매수 보류)";
                    diag.TargetCutPrice = Math.Round(item.BuyPrice * 0.94m, 0);
                    diag.ReasonSummary = $"경미한 조정(-{Math.Abs(profitRate):F1}%) 구간입니다. 물타기는 자제하고 업종 섹터 수급 유입 여부를 점검하세요.";
                }

                res.ItemDiagnoses.Add(diag);
            }

            return res;
        }
    }
}
