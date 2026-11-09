using System;
using System.Collections.Generic;
using System.Linq;

namespace GateHelper.MysteryTime
{
    public class MysteryCaseRepository
    {
        private readonly List<MysteryCase> _presetCases;

        public MysteryCaseRepository()
        {
            _presetCases = LoadPresetCases();
        }

        public List<MysteryCase> GetAllCases()
        {
            return _presetCases.ToList();
        }

        public void AddCase(MysteryCase newCase)
        {
            if (newCase != null)
            {
                _presetCases.Insert(0, newCase);
            }
        }

        public MysteryCase GetCaseById(string id)
        {
            return _presetCases.FirstOrDefault(c => c.Id == id);
        }

        public MysteryCase GetRandomCase()
        {
            var rand = new Random();
            int index = rand.Next(_presetCases.Count);
            return _presetCases[index];
        }

        private List<MysteryCase> LoadPresetCases()
        {
            return new List<MysteryCase>
            {
                new MysteryCase
                {
                    Id = "CASE_001",
                    Title = "밀실의 물웅덩이와 로프",
                    Difficulty = "쉬움",
                    Category = "밀실 트릭",
                    Situation = "닫힌 텅 빈 방 안에서 남자가 목을 매고 숨진 채 발견되었다. 발밑에는 아무런 의자나 발판도 없었고 오직 흥건한 물웅덩이만 고여 있었다. 방은 안에서 고정되어 있었는데 남자는 어떻게 목을 매었을까?",
                    Truth = "남자는 커다란 얼음 덩어리를 발판 삼아 올라간 뒤 목에 로프를 걸었다. 시간이 지나면서 얼음이 완전히 녹아 바닥에 물웅덩이로 남았고 남자는 허공에 기게 되었다.",
                    HintText = "바닥의 물웅덩이는 외부에서 들어온 것이 아니라, 사건 발생 당시에는 고체 상태의 형태였습니다.",
                    MainImagePrompt = "A dark room with a rope hanging from ceiling, a large puddle of water on the wooden floor, detective investigation illustration, noir style",
                    HintImagePrompt = "Close up of a melting block of ice on the floor dripping water, detective clue style",
                    Keywords = new List<string> { "얼음", "녹아", "녹았", "고체", "발판" }
                },
                new MysteryCase
                {
                    Id = "CASE_002",
                    Title = "비 오는 날 서재의 시신",
                    Difficulty = "보통",
                    Category = "상황 추리",
                    Situation = "폭우가 쏟아지던 밤, 남자가 자신의 서재에서 차가운 시신으로 발견되었다. 창문과 문은 안에서 잠겨 있었고 빗물 한 방울 들어오지 않았다. 하지만 남자의 옷은 온통 흠뻑 젖어 있었다. 무슨 일이 있었던 걸까?",
                    Truth = "남자는 비가 내리기 시작할 때 야외에서 빗물을 맞아 옷이 흠뻑 젖은 채 서재로 들어왔고, 들어온 직후 갑작스러운 심장마비로 사망했다. 타살이나 외부 침입은 없었다.",
                    HintText = "남자가 젖은 원인은 서재 내부가 아니라, 서재로 들어오기 직전 야외 상황과 관련이 있습니다.",
                    MainImagePrompt = "A cozy study room with bookshelf, a man lying on floor with wet clothes, rain falling outside window, mystery illustration",
                    HintImagePrompt = "Close up of wet suit jacket and raindrops on window glass, detective style",
                    Keywords = new List<string> { "밖", "야외", "심장마비", "들어오", "자연사", "병사" }
                },
                new MysteryCase
                {
                    Id = "CASE_003",
                    Title = "소리가 사라진 알람 시계",
                    Difficulty = "쉬움",
                    Category = "일상 미스테리",
                    Situation = "한 엔지니어가 매일 아침 7시마다 크게 울리던 알람 소리를 듣지 못해 중요 점검 시간에 지각했다. 알람 시계는 정시를 가리키며 정상 작동 중이었고 배터리도 충분했다. 남자는 시계에 손 하나 대지 않았다. 어떻게 알람을 듣지 못한 걸까?",
                    Truth = "남자는 어젯밤 야간 설비 소음을 막기 위해 사내 보급용 강력 소음 차단 귀마개를 끼고 잤기 때문에 알람 소리를 전혀 듣지 못한 것이었다.",
                    HintText = "알람 시계의 고장이 아니라, 남자의 신체 상태 및 착용한 소지품에 원인이 있습니다.",
                    MainImagePrompt = "An alarm clock ringing on nightstand next to bed, man sleeping deeply, warm morning sunlight, concept art",
                    HintImagePrompt = "Close up of foam earplugs resting on a wooden table, macro photo style",
                    Keywords = new List<string> { "귀마개", "이어플러그", "소음", "귀" }
                },
                new MysteryCase
                {
                    Id = "CASE_004",
                    Title = "독이 든 두 잔의 주스",
                    Difficulty = "보통",
                    Category = "트릭",
                    Situation = "두 사람이 식당에서 동일한 주전자에서 따른 차가운 주스 두 잔을 주문했다. 한 사람은 목이 말라 빠르게 한 잔을 모두 마셨고 살았다. 다른 한 사람은 천천히 마셨는데 얼마 후 중독으로 사망했다. 어찌된 일일까?",
                    Truth = "독은 주스 액체가 아니라 주스 속에 들어있던 얼음 속에 들어있었다. 첫 번째 사람은 얼음이 녹기 전에 마셔서 살았고, 두 번째 사람은 천천히 마시는 동안 얼음이 녹아 독이 쏟아져 사망했다.",
                    HintText = "독은 주스 액체 자체가 아니라, 주스를 차갑게 유지해주던 내용물 속에 있었습니다.",
                    MainImagePrompt = "Two glasses of iced juice on restaurant table, one empty and one half full, mysterious lighting",
                    HintImagePrompt = "Close up of ice cubes inside a glass of juice melting, detailed illustration",
                    Keywords = new List<string> { "얼음", "녹아", "녹았", "녹으면서" }
                },
                new MysteryCase
                {
                    Id = "CASE_005",
                    Title = "스마트 보안문의 비밀",
                    Difficulty = "어려움",
                    Category = "설비/보안",
                    Situation = "사내 보안 구역 스마트 출입문이 밤사이에 출입 태그(NFC) 기록 없이 열렸다. 문에는 물리적 파손이나 네트워크 오류 기록이 전혀 없었다. 어떻게 문이 열린 것일까?",
                    Truth = "청소 직원이 바닥을 청소하던 중 강력 자석이 달린 청소도구를 문 안쪽 감지 센서에 가까이 대어 문 안쪽 센서의 자성 반응으로 센서를 작동시켜 문을 열었던 것이었다.",
                    HintText = "디지털 인가 기록이 아니라, 문 내부 감지 센서에 가해진 물리적 반응 때문입니다.",
                    MainImagePrompt = "A high-tech smart security door in a modern factory hallway, green status light, sleek design",
                    HintImagePrompt = "Close up of a magnet tool near an electronic sensor panel, tech diagram style",
                    Keywords = new List<string> { "자석", "자성", "센서", "청소" }
                }
            };
        }
    }
}
