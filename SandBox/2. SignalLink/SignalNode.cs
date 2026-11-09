using System;
using System.Drawing;

namespace GateHelper
{
    // 각 칸의 상태를 정의하는 열거형
    public enum NodeType
    {
        Empty,
        Terminal,    // 시작/끝 신호 단말
        Path,        // 연결된 선로
        Bridge,      // 십자 교차 점퍼
        Barrier      // 절연 방화벽
    }

    public class SignalNode
    {
        public Point Pos { get; set; }           // 좌표 (x, y)
        public NodeType Type { get; set; }       // 상태: 빈칸, 시작/끝점, 경로 등
        public int ColorID { get; set; }         // 신호 색상 번호 (0: 없음)
        public bool IsStealth { get; set; }       // 고난이도 스텔스/암기 모드 노드 여부
        public float ConnectPulse { get; set; }  // 연결 성공 시 스프링 펄스 애니메이션 (0.0f ~ 1.0f)

        public SignalNode(int x, int y, NodeType type = NodeType.Empty, int colorId = 0)
        {
            Pos = new Point(x, y);
            Type = type;
            ColorID = colorId;
            IsStealth = false;
            ConnectPulse = 0f;
        }

        public SignalNode Clone()
        {
            return new SignalNode(Pos.X, Pos.Y, Type, ColorID)
            {
                IsStealth = this.IsStealth,
                ConnectPulse = this.ConnectPulse
            };
        }
    }
}