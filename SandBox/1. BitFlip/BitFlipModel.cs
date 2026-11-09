using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GateHelper
{
    /// <summary>
    /// 비트 노드의 특수 기믹 타입
    /// </summary>
    public enum BitNodeType
    {
        Normal,     // 기본 십자(+) 반전 (자신 + 상하좌우)
        Diagonal,   // 대각선(✕) 반전 (자신 + 대각선 4방향)
        Line,       // 라인 레이저(⚡) 반전 (가로 1행 전체 관통 반전)
        Firewall,   // 절연 방화벽(🛡️) 노드 (인접 타일의 반전 파동에 면역, 오직 직접 클릭으로만 반전)
        Linked,     // 양자 얽힘(∞) 노드 (동일 LinkId를 가진 노드와 동시 반전)
        Locked,     // 잠금 게이트(🔒) 노드 (키 노드가 활성화되기 전까지 반전 불가)
        Key         // 암호 키(🔑) 노드 (활성화되면 맵의 잠금 노드를 즉시 해제)
    }

    /// <summary>
    /// 개별 비트 노드 데이터
    /// </summary>
    public class BitNode
    {
        public int X { get; set; }
        public int Y { get; set; }
        public bool State { get; set; }           // true = ON (1), false = OFF (0)
        public BitNodeType Type { get; set; }
        public int LinkId { get; set; } = -1;     // Linked 노드용 매칭 ID
        public bool IsLocked { get; set; } = false; // Locked 노드용 잠금 상태

        // 애니메이션 상태
        public float FlipProgress { get; set; } = 1.0f; // 0.0 ~ 1.0 (1.0 = 애니메이션 완료)
        public bool PrevState { get; set; } = false;
        public float PulseScale { get; set; } = 1.0f;

        public BitNode(int x, int y, bool state, BitNodeType type = BitNodeType.Normal)
        {
            X = x;
            Y = y;
            State = state;
            PrevState = state;
            Type = type;
        }

        public BitNode Clone()
        {
            return new BitNode(X, Y, State, Type)
            {
                LinkId = this.LinkId,
                IsLocked = this.IsLocked,
                PrevState = this.PrevState,
                FlipProgress = this.FlipProgress,
                PulseScale = this.PulseScale
            };
        }
    }

    /// <summary>
    /// 실행 취소(Undo)용 스냅샷
    /// </summary>
    public class BitFlipSnapshot
    {
        public bool[,] States { get; set; }
        public bool[,] Locks { get; set; }
        public int MoveCount { get; set; }

        public BitFlipSnapshot(int size, BitNode[,] grid, int moveCount)
        {
            MoveCount = moveCount;
            States = new bool[size, size];
            Locks = new bool[size, size];

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    States[c, r] = grid[c, r].State;
                    Locks[c, r] = grid[c, r].IsLocked;
                }
            }
        }
    }

    /// <summary>
    /// 비주얼 파동 및 이펙트 데이터
    /// </summary>
    public class ShockwaveEffect
    {
        public PointF Center { get; set; }
        public float Radius { get; set; }
        public float MaxRadius { get; set; }
        public Color WaveColor { get; set; }
        public float Alpha { get; set; } = 1.0f;
    }

    public class SparkParticle
    {
        public PointF Position;
        public PointF Velocity;
        public Color ParticleColor;
        public float Life = 1.0f;
        public float Size = 3.0f;
    }

    public class FloatingNotification
    {
        public PointF Position;
        public string Text;
        public Color TextColor;
        public float Life = 1.0f;
        public float Alpha = 1.0f;
    }

    /// <summary>
    /// 비트 플립 코어 퍼즐 엔진 및 GF(2) 가우스 소거법 AI 풀이기
    /// </summary>
    public class BitFlipModel
    {
        private static readonly Random _rand = new Random();

        public int GridSize { get; private set; } = 4;
        public string Difficulty { get; private set; } = "Normal";
        public BitNode[,] Grid { get; private set; }

        public Stack<BitFlipSnapshot> UndoStack { get; } = new Stack<BitFlipSnapshot>();
        public int MoveCount { get; private set; } = 0;
        public int ParMoves { get; private set; } = 0;
        public int HintsUsed { get; private set; } = 0;
        public bool IsGameActive { get; private set; } = false;
        public bool IsVictory { get; private set; } = false;

        public Point? CurrentHintNode { get; private set; } = null;

        public BitFlipModel()
        {
            InitializeGame("Normal");
        }

        public void InitializeGame(string difficulty)
        {
            Difficulty = difficulty;
            switch (difficulty)
            {
                case "Easy":
                    GridSize = 3;
                    break;
                case "Normal":
                    GridSize = 4;
                    break;
                case "Hard":
                    GridSize = 5;
                    break;
                case "Expert":
                    GridSize = 6;
                    break;
                default:
                    GridSize = 4;
                    Difficulty = "Normal";
                    break;
            }

            GenerateSolvablePuzzle();
        }

        /// <summary>
        /// 100% 해결 보장되는 고품질 퍼즐 생성기
        /// </summary>
        public void GenerateSolvablePuzzle()
        {
            Grid = new BitNode[GridSize, GridSize];
            UndoStack.Clear();
            MoveCount = 0;
            HintsUsed = 0;
            IsVictory = false;
            CurrentHintNode = null;

            // 1. 모든 노드를 ON(1) 상태로 초기화
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    Grid[c, r] = new BitNode(c, r, true, BitNodeType.Normal);
                }
            }

            // 2. 난이도별 특수 기믹 배치
            ApplyGimmickLayout();

            // 3. 역산 셔플 (Solvable Guarantee)
            // 완료 상태에서 유효한 클릭을 N회 무작위 시뮬레이션하여 100% 풀 수 있는 퍼즐 생성
            int shuffleClicks = GetShuffleClickCount(Difficulty);
            List<Point> allCoords = new List<Point>();
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (Grid[c, r].Type != BitNodeType.Locked)
                        allCoords.Add(new Point(c, r));
                }
            }

            int minOffCount;
            int minPar;
            switch (Difficulty)
            {
                case "Easy": minOffCount = 3; minPar = 2; break;
                case "Normal": minOffCount = 5; minPar = 3; break;
                case "Hard": minOffCount = 8; minPar = 5; break;
                case "Expert": minOffCount = 12; minPar = 7; break;
                default: minOffCount = 5; minPar = 3; break;
            }

            // 무작위 셔플 실행 (최소 난이도 조건 만족 보장)
            for (int attempt = 0; attempt < 50; attempt++)
            {
                // 초기화
                for (int r = 0; r < GridSize; r++)
                    for (int c = 0; c < GridSize; c++)
                    {
                        Grid[c, r].State = true;
                        Grid[c, r].PrevState = true;
                        Grid[c, r].FlipProgress = 1.0f;
                        if (Grid[c, r].Type == BitNodeType.Locked)
                            Grid[c, r].IsLocked = true;
                    }

                var shuffledList = allCoords.OrderBy(x => _rand.Next()).Take(shuffleClicks).ToList();
                foreach (var pt in shuffledList)
                {
                    SimulateClick(pt.X, pt.Y);
                }

                int offCount = 0;
                for (int r = 0; r < GridSize; r++)
                    for (int c = 0; c < GridSize; c++)
                        if (!Grid[c, r].State) offCount++;

                if (offCount >= minOffCount)
                {
                    var solution = SolveGF2();
                    if (solution != null && solution.Count >= minPar)
                    {
                        ParMoves = solution.Count;
                        break;
                    }
                }
            }

            // 4. Par (목표 최소 수) 산출 및 보정
            if (ParMoves == 0)
            {
                var finalSol = SolveGF2();
                if (finalSol != null && finalSol.Count > 0)
                    ParMoves = finalSol.Count;
                else
                    ParMoves = Math.Max(minPar, shuffleClicks - 1);
            }

            IsGameActive = true;
        }

        private int GetShuffleClickCount(string diff)
        {
            switch (diff)
            {
                case "Easy": return 3;
                case "Normal": return 5;
                case "Hard": return 8;
                case "Expert": return 13;
                default: return 5;
            }
        }

        private void ApplyGimmickLayout()
        {
            if (Difficulty == "Easy")
            {
                // 순수 클래식 퍼즐
                return;
            }

            if (Difficulty == "Normal")
            {
                // 대각선 노드 1~2개 배치
                Grid[1, 1].Type = BitNodeType.Diagonal;

                if (GridSize >= 4)
                {
                    Grid[GridSize - 2, GridSize - 2].Type = BitNodeType.Diagonal;
                }
            }
            else if (Difficulty == "Hard")
            {
                // 대각선 2개 + 절연 방화벽 2개 + 라인 빔 1개
                Grid[1, 1].Type = BitNodeType.Diagonal;
                Grid[GridSize - 2, GridSize - 2].Type = BitNodeType.Diagonal;

                Grid[0, GridSize - 1].Type = BitNodeType.Firewall;
                Grid[GridSize - 1, 0].Type = BitNodeType.Firewall;

                // 중앙 라인 빔
                Grid[GridSize / 2, GridSize / 2].Type = BitNodeType.Line;
            }
            else if (Difficulty == "Expert")
            {
                // 양자 얽힘 노드 1쌍 + 방화벽 2개 + 라인 빔 1개 + 키/잠금 노드 1쌍
                // 1) 양자 얽힘
                Grid[0, 1].Type = BitNodeType.Linked;
                Grid[0, 1].LinkId = 1;
                Grid[GridSize - 1, GridSize - 2].Type = BitNodeType.Linked;
                Grid[GridSize - 1, GridSize - 2].LinkId = 1;

                // 2) 절연 방화벽
                Grid[1, 1].Type = BitNodeType.Firewall;
                Grid[GridSize - 2, GridSize - 2].Type = BitNodeType.Firewall;

                // 3) 라인 빔
                Grid[GridSize / 2, GridSize / 2].Type = BitNodeType.Line;

                // 4) 키 & 잠금
                Grid[0, GridSize - 1].Type = BitNodeType.Key;
                Grid[GridSize - 1, 0].Type = BitNodeType.Locked;
                Grid[GridSize - 1, 0].IsLocked = true;
            }
        }

        /// <summary>
        /// 특정 노드 클릭 시 영향을 받는 대상 노드들의 목록 반환 (호버 하이라이트, 실제 반전, 행렬 A 구성 시 100% 동일 사용)
        /// </summary>
        public List<BitNode> GetAffectedNodes(int x, int y)
        {
            var result = new HashSet<BitNode>();
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return result.ToList();

            var center = Grid[x, y];
            if (center.Type == BitNodeType.Locked && center.IsLocked) return result.ToList();

            // 클릭한 자기 자신은 언제나 포함
            result.Add(center);

            // 타입별 파동 전파 대상
            if (center.Type == BitNodeType.Line)
            {
                // 라인 빔: 가로 1행 전체를 반전
                for (int c = 0; c < GridSize; c++)
                {
                    result.Add(Grid[c, y]);
                }
            }
            else if (center.Type == BitNodeType.Diagonal)
            {
                // 대각선 4방향
                int[] dx = { -1, 1, -1, 1 };
                int[] dy = { -1, -1, 1, 1 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    if (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize)
                        result.Add(Grid[nx, ny]);
                }
            }
            else
            {
                // 표준 십자 4방향 (Normal, Firewall, Linked, Key 등)
                int[] dx = { 0, 0, -1, 1 };
                int[] dy = { -1, 1, 0, 0 };
                for (int i = 0; i < 4; i++)
                {
                    int nx = x + dx[i];
                    int ny = y + dy[i];
                    if (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize)
                        result.Add(Grid[nx, ny]);
                }
            }

            // 양자 얽힘 (Linked) 노드인 경우 연동된 파트너 노드도 함께 작동
            if (center.Type == BitNodeType.Linked && center.LinkId != -1)
            {
                for (int r = 0; r < GridSize; r++)
                {
                    for (int c = 0; c < GridSize; c++)
                    {
                        if ((c != x || r != y) && Grid[c, r].Type == BitNodeType.Linked && Grid[c, r].LinkId == center.LinkId)
                        {
                            result.Add(Grid[c, r]);
                            int[] dx = { 0, 0, -1, 1 };
                            int[] dy = { -1, 1, 0, 0 };
                            for (int i = 0; i < 4; i++)
                            {
                                int nx = c + dx[i];
                                int ny = r + dy[i];
                                if (nx >= 0 && nx < GridSize && ny >= 0 && ny < GridSize)
                                    result.Add(Grid[nx, ny]);
                            }
                        }
                    }
                }
            }

            // [면역 규칙 적용]
            // 다른 노드로부터 오는 파동일 때:
            // 1. Firewall 노드는 인접 파동에 면역 (자신을 직접 누른 경우가 아니면 제외)
            // 2. Locked 노드는 잠금 상태일 때 인접 파동에 면역
            var filtered = new List<BitNode>();
            foreach (var node in result)
            {
                if (node.X == x && node.Y == y)
                {
                    // 직접 클릭한 노드는 항상 포함
                    filtered.Add(node);
                }
                else
                {
                    // 절연 방화벽 노드는 stray wave에 면역
                    if (node.Type == BitNodeType.Firewall) continue;

                    // 잠긴 노드는 게이트 해제 전까지 면역
                    if (node.Type == BitNodeType.Locked && node.IsLocked) continue;

                    filtered.Add(node);
                }
            }

            return filtered;
        }

        /// <summary>
        /// 실제 유저 클릭 처리
        /// </summary>
        public bool ClickNode(int x, int y, out List<BitNode> toggledNodes, out bool unlockedGate)
        {
            toggledNodes = new List<BitNode>();
            unlockedGate = false;

            if (!IsGameActive || IsVictory) return false;
            if (x < 0 || x >= GridSize || y < 0 || y >= GridSize) return false;

            var target = Grid[x, y];
            if (target.Type == BitNodeType.Locked && target.IsLocked)
            {
                // 잠긴 노드는 클릭 불가
                return false;
            }

            // 스냅샷 저장 (Undo용)
            UndoStack.Push(new BitFlipSnapshot(GridSize, Grid, MoveCount));
            MoveCount++;
            CurrentHintNode = null;

            var affected = GetAffectedNodes(x, y);

            foreach (var node in affected)
            {
                // 일반 상태 반전
                node.PrevState = node.State;
                node.State = !node.State;
                node.FlipProgress = 0.0f;
                node.PulseScale = 1.3f;
                toggledNodes.Add(node);
            }

            // 키 노드 활성화 여부 검사 -> 잠금 노드 해제
            if (target.Type == BitNodeType.Key && target.State)
            {
                for (int r = 0; r < GridSize; r++)
                {
                    for (int c = 0; c < GridSize; c++)
                    {
                        if (Grid[c, r].Type == BitNodeType.Locked && Grid[c, r].IsLocked)
                        {
                            Grid[c, r].IsLocked = false;
                            Grid[c, r].PulseScale = 1.4f;
                            unlockedGate = true;
                        }
                    }
                }
            }

            // 승리 체크
            CheckVictory();

            return true;
        }

        private void SimulateClick(int x, int y)
        {
            var affected = GetAffectedNodes(x, y);
            foreach (var node in affected)
            {
                node.State = !node.State;
            }

            if (Grid[x, y].Type == BitNodeType.Key && Grid[x, y].State)
            {
                for (int r = 0; r < GridSize; r++)
                    for (int c = 0; c < GridSize; c++)
                        if (Grid[c, r].Type == BitNodeType.Locked)
                            Grid[c, r].IsLocked = false;
            }
        }

        /// <summary>
        /// 실행 취소 (Undo)
        /// </summary>
        public bool Undo()
        {
            if (UndoStack.Count == 0 || !IsGameActive || IsVictory) return false;

            var snap = UndoStack.Pop();
            MoveCount = snap.MoveCount;
            CurrentHintNode = null;

            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    Grid[c, r].State = snap.States[c, r];
                    Grid[c, r].IsLocked = snap.Locks[c, r];
                    Grid[c, r].FlipProgress = 1.0f;
                    Grid[c, r].PulseScale = 1.0f;
                }
            }

            return true;
        }

        private void CheckVictory()
        {
            bool allSolved = true;
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    if (!Grid[c, r].State)
                    {
                        allSolved = false;
                        break;
                    }
                }
                if (!allSolved) break;
            }

            if (allSolved)
            {
                IsVictory = true;
                IsGameActive = false;

                try
                {
                    var dashType = Type.GetType("GateHelper.Dashboard.DashboardManager, GateHelper") 
                                   ?? Type.GetType("GateHelper.Dashboard.DashboardManager");
                    dashType?.GetMethod("RecordBitFlipWin", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?.Invoke(null, null);
                }
                catch { }
            }
        }

        /// <summary>
        /// GF(2) 가우스 소거법 AI 풀이기 (최적 클릭 수열 반환)
        /// </summary>
        public List<Point> SolveGF2()
        {
            int n = GridSize * GridSize;
            bool[,] matrix = new bool[n, n + 1];

            // 1. 행렬 A 구성: j번 노드를 눌렀을 때 i번 노드가 반전되는가?
            for (int r = 0; r < GridSize; r++)
            {
                for (int c = 0; c < GridSize; c++)
                {
                    int j = r * GridSize + c;
                    var affected = GetAffectedNodes(c, r);
                    foreach (var aff in affected)
                    {
                        int i = aff.Y * GridSize + aff.X;
                        matrix[i, j] = true;
                    }

                    // 목표 벡터: 현재 꺼져있는(false) 노드를 켜기(true) 위해 1을 만듦
                    int rowIdx = r * GridSize + c;
                    matrix[rowIdx, n] = !Grid[c, r].State;
                }
            }

            // 2. 가우스 소거법 (Forward Elimination)
            int pivot = 0;
            int[] pivotCols = new int[n];
            for (int i = 0; i < n; i++) pivotCols[i] = -1;

            for (int col = 0; col < n && pivot < n; col++)
            {
                int sel = pivot;
                while (sel < n && !matrix[sel, col]) sel++;
                if (sel == n) continue;

                // 행 스왑
                for (int k = col; k <= n; k++)
                {
                    bool tmp = matrix[sel, k];
                    matrix[sel, k] = matrix[pivot, k];
                    matrix[pivot, k] = tmp;
                }

                pivotCols[pivot] = col;

                // 타 행 XOR 소거
                for (int r = 0; r < n; r++)
                {
                    if (r != pivot && matrix[r, col])
                    {
                        for (int k = col; k <= n; k++)
                            matrix[r, k] ^= matrix[pivot, k];
                    }
                }

                pivot++;
            }

            // 3. 무모순(Solvability) 검증
            for (int r = pivot; r < n; r++)
            {
                if (matrix[r, n]) return null; // 0 = 1 (해 없음)
            }

            // 4. 해 도출
            bool[] solutionVector = new bool[n];
            for (int p = 0; p < pivot; p++)
            {
                int c = pivotCols[p];
                if (c != -1)
                {
                    solutionVector[c] = matrix[p, n];
                }
            }

            var result = new List<Point>();
            for (int i = 0; i < n; i++)
            {
                if (solutionVector[i])
                {
                    result.Add(new Point(i % GridSize, i / GridSize));
                }
            }

            return result;
        }

        /// <summary>
        /// AI 딥 스캔 힌트 요청 (최적의 다음 클릭 노드 제시)
        /// </summary>
        public Point? RequestAiHint()
        {
            if (!IsGameActive || IsVictory) return null;

            var solution = SolveGF2();
            if (solution != null && solution.Count > 0)
            {
                // 현재 누를 수 있는 유효한 후보 선정
                var validHints = solution.Where(pt => !Grid[pt.X, pt.Y].IsLocked).ToList();
                if (validHints.Count > 0)
                {
                    CurrentHintNode = validHints[0];
                    HintsUsed++;
                    return CurrentHintNode;
                }
            }

            return null;
        }

        /// <summary>
        /// 3-Star 평가 산출
        /// </summary>
        public int GetStarRating()
        {
            if (MoveCount <= ParMoves) return 3;
            if (MoveCount <= ParMoves + 3) return 2;
            return 1;
        }
    }
}
