using System;
using System.Drawing;

namespace GateHelper
{
    /// <summary>
    /// 비트 플립 특수 기믹 메타데이터 및 전술 가이드
    /// </summary>
    public static class BitFlipGimmickGuide
    {
        public static string GetTitle(BitNodeType type)
        {
            switch (type)
            {
                case BitNodeType.Diagonal: return "대각선 인버터 (X-Inverter)";
                case BitNodeType.Line: return "라인 레이저 (Laser Inverter)";
                case BitNodeType.Firewall: return "절연 방화벽 (Insulated Firewall)";
                case BitNodeType.Linked: return "양자 얽힘 노드 (Quantum Link)";
                case BitNodeType.Key: return "암호 해독 키 (Cipher Key)";
                case BitNodeType.Locked: return "잠금 보안 게이트 (Locked Gate)";
                default: return "표준 비트 노드 (Standard Bit)";
            }
        }

        public static string GetDescription(BitNodeType type)
        {
            switch (type)
            {
                case BitNodeType.Diagonal:
                    return "클릭 시 기본 십자(+)가 아닌 대각선(✕) 4방향의 인접 노드들을 반전시킵니다.";
                case BitNodeType.Line:
                    return "클릭 시 가로 1행(Row) 전체를 관통하여 라인의 모든 비트를 한 번에 동시 반전시킵니다.";
                case BitNodeType.Firewall:
                    return "외부 파동을 차단하는 절연 방화벽입니다. 인접 노드가 터져도 면역이며, 오직 직접 클릭해야만 반전됩니다.";
                case BitNodeType.Linked:
                    return "원거리의 동일 기호 노드와 양자 얽힘으로 연결되어 있어, 둘 중 하나를 누르면 파트너 노드도 함께 작동합니다.";
                case BitNodeType.Key:
                    return "이 노드를 활성화(1)하면 전장 내의 모든 [잠금 보안 게이트]를 즉시 강제 해제합니다.";
                case BitNodeType.Locked:
                    return "보안 게이트로 잠겨 있어 클릭하거나 반전시킬 수 없습니다. [암호 해독 키] 노드를 먼저 켜야 해제됩니다.";
                default:
                    return "자신과 상·하·좌·우 십자(+) 4방향의 인접 노드들을 동시에 반전시킵니다.";
            }
        }

        public static Color GetThemeColor(BitNodeType type)
        {
            switch (type)
            {
                case BitNodeType.Diagonal: return Color.FromArgb(255, 180, 50);    // Amber Gold
                case BitNodeType.Line: return Color.FromArgb(255, 230, 0);        // Electric Yellow
                case BitNodeType.Firewall: return Color.FromArgb(50, 220, 255);    // Cyber Cyan
                case BitNodeType.Linked: return Color.FromArgb(200, 80, 255);     // Neon Purple
                case BitNodeType.Key: return Color.FromArgb(255, 215, 0);         // Bright Gold
                case BitNodeType.Locked: return Color.FromArgb(255, 60, 90);       // Laser Red
                default: return Color.FromArgb(0, 245, 212);                       // Neon Emerald Teal
            }
        }

        public static string GetBadgeSymbol(BitNodeType type)
        {
            switch (type)
            {
                case BitNodeType.Diagonal: return "✕";
                case BitNodeType.Line: return "⚡";
                case BitNodeType.Firewall: return "🛡️";
                case BitNodeType.Linked: return "∞";
                case BitNodeType.Key: return "🔑";
                case BitNodeType.Locked: return "🔒";
                default: return "";
            }
        }
    }
}