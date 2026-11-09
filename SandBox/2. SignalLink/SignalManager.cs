using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace GateHelper
{
    /// <summary>
    /// 실행 취소(Undo)를 위한 시그널 링크 상태 스냅샷
    /// </summary>
    public class SignalSnapshot
    {
        public Dictionary<int, List<Point>> CompletedPaths { get; }
        public int MoveCount { get; }

        public SignalSnapshot(Dictionary<int, List<Point>> paths, int moveCount)
        {
            CompletedPaths = new Dictionary<int, List<Point>>();
            foreach (var kvp in paths)
            {
                CompletedPaths[kvp.Key] = new List<Point>(kvp.Value);
            }
            MoveCount = moveCount;
        }
    }

    /// <summary>
    /// 시그널 링크 핵심 퍼즐 엔진 및 고속 회로 생성기
    /// </summary>
    public class SignalManager
    {
        private static readonly Random _rand = new Random();

        public int GridSize { get; private set; } = 6;
        public string Difficulty { get; private set; } = "Normal";
        public bool IsStealthMode { get; private set; } = false; // 고난이도 암기 모드

        public SignalNode[,] Grid { get; private set; }
        public int PairCount { get; private set; }

        // 정답 경로 (AI 힌트 및 완성도 검증용)
        public Dictionary<int, List<Point>> SolutionPaths { get; private set; } = new Dictionary<int, List<Point>>();

        // 사용자가 완성한 경로 목록
        public Dictionary<int, List<Point>> CompletedPaths { get; private set; } = new Dictionary<int, List<Point>>();

        // Undo 스택
        public Stack<SignalSnapshot> UndoStack { get; } = new Stack<SignalSnapshot>();

        public int MoveCount { get; private set; } = 0;
        public int HintsUsed { get; private set; } = 0;
        public bool IsGameActive { get; private set; } = false;
        public bool IsVictory { get; private set; } = false;

        public SignalManager()
        {
            InitializeGrid("Normal");
        }

        public void InitializeGrid(string difficulty)
        {
            Difficulty = difficulty;
            UndoStack.Clear();
            CompletedPaths.Clear();
            SolutionPaths.Clear();
            MoveCount = 0;
            HintsUsed = 0;
            IsVictory = false;

            int size;
            int pairs;
            int minLen;
            int minDist;

            switch (difficulty)
            {
                case "Easy":
                    size = 5;
                    pairs = 3;
                    minLen = 4;
                    minDist = 2;
                    IsStealthMode = false;
                    break;
                case "Normal":
                    size = 6;
                    pairs = 4;
                    minLen = 4;
                    minDist = 3;
                    IsStealthMode = false;
                    break;
                case "Hard":
                    size = 8;
                    pairs = 6;
                    minLen = 5;
                    minDist = 3;
                    IsStealthMode = false;
                    break;
                case "Expert":
                    size = 10;
                    pairs = 7;
                    minLen = 6;
                    minDist = 4;
                    IsStealthMode = true; // 사용자가 즐기는 스텔스 암기 모드 전격 활성화
                    break;
                default:
                    size = 6;
                    pairs = 4;
                    minLen = 4;
                    minDist = 3;
                    IsStealthMode = false;
                    break;
            }

            GridSize = size;
            PairCount = pairs;

            // 1. 고품질 맵 절차적 생성 시도
            // Pass 1: 엄격한 최고 난이도 제약 (긴 경로, 넓은 단말 간 거리 보장)
            bool generated = false;
            for (int attempt = 0; attempt < 25; attempt++)
            {
                if (TryGenerateValidPerfectMap(size, pairs, minLen, minDist))
                {
                    generated = true;
                    break;
                }
            }

            // Pass 2: 여유 난이도 제약 (약간 완화된 기준, 절대 쉬운 1~2칸 퍼즐 불가)
            if (!generated)
            {
                int relaxedLen = Math.Max(3, minLen - 1);
                int relaxedDist = Math.Max(2, minDist - 1);
                for (int attempt = 0; attempt < 25; attempt++)
                {
                    if (TryGenerateValidPerfectMap(size, pairs, relaxedLen, relaxedDist))
                    {
                        generated = true;
                        break;
                    }
                }
            }

            // 2. 만약 시간 내 생성되지 않았을 경우, 검증된 예술적 미로 템플릿 로드 (D4 대칭변환 + 색상 셔플)
            if (!generated)
            {
                LoadVerifiedFallbackPuzzle(size, pairs);
            }

            // 3. 스텔스 모드 속성 적용
            if (IsStealthMode)
            {
                for (int y = 0; y < GridSize; y++)
                    for (int x = 0; x < GridSize; x++)
                        if (Grid[x, y].Type == NodeType.Terminal)
                            Grid[x, y].IsStealth = true;
            }

            IsGameActive = true;
        }

        // 구버전 호환용 오버로드
        public void InitializeGrid(int size, int pairCount)
        {
            string diff = size <= 5 ? "Easy" : (size <= 6 ? "Normal" : (size <= 8 ? "Hard" : "Expert"));
            InitializeGrid(diff);
        }

        /// <summary>
        /// 지능형 뱀형 확장(Warnsdorff) + 2중 빈칸 흡수 100% 충진 알고리즘
        /// </summary>
        private bool TryGenerateValidPerfectMap(int size, int pairCount, int minLen, int minDist)
        {
            Grid = new SignalNode[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    Grid[x, y] = new SignalNode(x, y);

            HashSet<Point> occupied = new HashSet<Point>();
            List<List<Point>> allPaths = new List<List<Point>>();

            // 1. 초기 씨앗 분산 배치 (시작 씨앗들이 서로 맞닿지 않도록 거리 보장)
            for (int i = 0; i < pairCount; i++)
            {
                Point start = new Point(0, 0);
                bool placed = false;
                for (int trySeed = 0; trySeed < 50; trySeed++)
                {
                    Point candidate = new Point(_rand.Next(size), _rand.Next(size));
                    if (occupied.Contains(candidate)) continue;

                    bool tooClose = false;
                    foreach (var p in occupied)
                    {
                        if (Math.Abs(p.X - candidate.X) + Math.Abs(p.Y - candidate.Y) < 2)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    if (!tooClose || trySeed > 25)
                    {
                        start = candidate;
                        placed = true;
                        break;
                    }
                }
                if (!placed) return false;

                allPaths.Add(new List<Point> { start });
                occupied.Add(start);
            }

            // 2. 물리적 뱀형 확장 (Snake Growth + Warnsdorff 지능형 이웃 선택)
            bool moved = true;
            while (moved)
            {
                moved = false;
                var indices = Enumerable.Range(0, pairCount).OrderBy(_ => _rand.Next()).ToList();
                foreach (int idx in indices)
                {
                    var path = allPaths[idx];
                    Point[] currentEnds = { path.First(), path.Last() };

                    foreach (var end in currentEnds)
                    {
                        var neighbors = GetEmptyNeighbors(end, occupied, size);
                        if (neighbors.Count > 0)
                        {
                            // Warnsdorff: 빈 이웃이 가장 적은 고립 위험 칸을 우선 점유
                            var best = neighbors
                                .Select(n => new { pt = n, freeCount = GetEmptyNeighbors(n, occupied, size).Count })
                                .OrderBy(x => x.freeCount)
                                .ThenBy(_ => _rand.Next())
                                .First().pt;

                            if (end == path.First()) path.Insert(0, best);
                            else path.Add(best);

                            occupied.Add(best);
                            moved = true;
                            break;
                        }
                    }
                }
            }

            // 3. 스마트 빈칸 흡수 우회 (끝점 확장 및 마디 삽입 2중 처리)
            if (occupied.Count < size * size)
            {
                if (!SmartRerouteHoles(allPaths, occupied, size)) return false;
            }

            // 4. 고난이도 품질 검증 (너무 짧은 경로 및 단말 간 맨해튼 거리 검사)
            foreach (var p in allPaths)
            {
                if (p.Count < minLen) return false;
                int dist = Math.Abs(p.First().X - p.Last().X) + Math.Abs(p.First().Y - p.Last().Y);
                if (dist < minDist) return false;
            }

            // 5. 경로 확정 및 터미널 배치
            SolutionPaths.Clear();
            for (int i = 0; i < pairCount; i++)
            {
                var p = allPaths[i];
                int colorId = i + 1;
                SolutionPaths[colorId] = new List<Point>(p);

                Grid[p[0].X, p[0].Y].Type = NodeType.Terminal;
                Grid[p[0].X, p[0].Y].ColorID = colorId;

                Grid[p.Last().X, p.Last().Y].Type = NodeType.Terminal;
                Grid[p.Last().X, p.Last().Y].ColorID = colorId;
            }

            return true;
        }

        private bool SmartRerouteHoles(List<List<Point>> allPaths, HashSet<Point> occupied, int size)
        {
            bool changed = true;
            int passes = 0;
            while (changed && passes++ < 25)
            {
                changed = false;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        Point hole = new Point(x, y);
                        if (occupied.Contains(hole)) continue;

                        var neighbors = GetAllNeighbors(hole, size);

                        // 1. 단말(끝점)에 인접한 경우 바로 이어붙이기
                        foreach (var path in allPaths)
                        {
                            if (neighbors.Contains(path.First()))
                            {
                                path.Insert(0, hole);
                                occupied.Add(hole);
                                changed = true;
                                goto NextHole;
                            }
                            if (neighbors.Contains(path.Last()))
                            {
                                path.Add(hole);
                                occupied.Add(hole);
                                changed = true;
                                goto NextHole;
                            }
                        }

                        // 2. 연속된 두 노드 사이에 끼워넣기
                        foreach (var path in allPaths)
                        {
                            for (int i = 0; i < path.Count - 1; i++)
                            {
                                if (neighbors.Contains(path[i]) && neighbors.Contains(path[i + 1]))
                                {
                                    path.Insert(i + 1, hole);
                                    occupied.Add(hole);
                                    changed = true;
                                    goto NextHole;
                                }
                            }
                        }
                    NextHole:;
                    }
                }
            }
            return occupied.Count == size * size;
        }

        /// <summary>
        /// 대형 맵 등에서 절차 생성이 지연될 때 D4 대칭 변환(회전 4방향 x 좌우 반전) 및 색상 셔플을 적용하는 고난이도 미로 템플릿 로더
        /// </summary>
        private void LoadVerifiedFallbackPuzzle(int size, int pairs)
        {
            Grid = new SignalNode[size, size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    Grid[x, y] = new SignalNode(x, y);

            SolutionPaths.Clear();

            Point[][] rawTemplate = GetMasterpieceTemplate(size);
            int rot = _rand.Next(4); // 0, 90, 180, 270도 회전
            bool flip = _rand.Next(2) == 0; // 좌우 반전

            var colorIds = Enumerable.Range(1, rawTemplate.Length).OrderBy(_ => _rand.Next()).ToList();

            for (int i = 0; i < rawTemplate.Length; i++)
            {
                int colorId = colorIds[i];
                var rawPath = rawTemplate[i];
                var transformed = new List<Point>();

                foreach (var pt in rawPath)
                {
                    int x = pt.X;
                    int y = pt.Y;

                    if (flip) x = size - 1 - x;

                    int tx = x, ty = y;
                    if (rot == 1) { tx = size - 1 - y; ty = x; }
                    else if (rot == 2) { tx = size - 1 - x; ty = size - 1 - y; }
                    else if (rot == 3) { tx = y; ty = size - 1 - x; }

                    transformed.Add(new Point(tx, ty));
                }

                if (_rand.Next(2) == 0) transformed.Reverse();

                SolutionPaths[colorId] = transformed;

                Grid[transformed[0].X, transformed[0].Y].Type = NodeType.Terminal;
                Grid[transformed[0].X, transformed[0].Y].ColorID = colorId;

                Grid[transformed.Last().X, transformed.Last().Y].Type = NodeType.Terminal;
                Grid[transformed.Last().X, transformed.Last().Y].ColorID = colorId;
            }
        }

        private Point[][] GetMasterpieceTemplate(int size)
        {
            if (size <= 5)
            {
                return new Point[][]
                {
                    new Point[] { new Point(3, 1), new Point(3, 2), new Point(2, 2), new Point(2, 1), new Point(1, 1), new Point(1, 2), new Point(0, 2), new Point(0, 3) },
                    new Point[] { new Point(3, 3), new Point(4, 3), new Point(4, 4), new Point(3, 4), new Point(2, 4), new Point(2, 3), new Point(1, 3), new Point(1, 4), new Point(0, 4) },
                    new Point[] { new Point(4, 2), new Point(4, 1), new Point(4, 0), new Point(3, 0), new Point(2, 0), new Point(1, 0), new Point(0, 0), new Point(0, 1) }
                };
            }
            if (size <= 6)
            {
                return new Point[][]
                {
                    new Point[] { new Point(4, 3), new Point(4, 4), new Point(3, 4), new Point(3, 3), new Point(3, 2), new Point(3, 1), new Point(2, 1), new Point(2, 2) },
                    new Point[] { new Point(5, 3), new Point(5, 4), new Point(5, 5), new Point(4, 5), new Point(3, 5), new Point(2, 5), new Point(1, 5), new Point(0, 5), new Point(0, 4), new Point(0, 3) },
                    new Point[] { new Point(1, 4), new Point(2, 4), new Point(2, 3), new Point(1, 3), new Point(1, 2), new Point(0, 2), new Point(0, 1), new Point(0, 0), new Point(1, 0), new Point(1, 1) },
                    new Point[] { new Point(2, 0), new Point(3, 0), new Point(4, 0), new Point(5, 0), new Point(5, 1), new Point(4, 1), new Point(4, 2), new Point(5, 2) }
                };
            }
            if (size <= 8)
            {
                return new Point[][]
                {
                    new Point[] { new Point(4, 6), new Point(4, 5), new Point(5, 5), new Point(5, 4), new Point(4, 4), new Point(3, 4), new Point(3, 3), new Point(4, 3), new Point(5, 3) },
                    new Point[] { new Point(7, 1), new Point(6, 1), new Point(5, 1), new Point(5, 2), new Point(4, 2), new Point(3, 2), new Point(2, 2), new Point(2, 1), new Point(1, 1), new Point(1, 2), new Point(0, 2) },
                    new Point[] { new Point(1, 5), new Point(1, 6), new Point(2, 6), new Point(2, 5), new Point(3, 5), new Point(3, 6), new Point(3, 7), new Point(4, 7), new Point(5, 7), new Point(5, 6) },
                    new Point[] { new Point(7, 0), new Point(6, 0), new Point(5, 0), new Point(4, 0), new Point(4, 1), new Point(3, 1), new Point(3, 0), new Point(2, 0), new Point(1, 0), new Point(0, 0), new Point(0, 1) },
                    new Point[] { new Point(2, 7), new Point(1, 7), new Point(0, 7), new Point(0, 6), new Point(0, 5), new Point(0, 4), new Point(0, 3), new Point(1, 3), new Point(2, 3), new Point(2, 4), new Point(1, 4) },
                    new Point[] { new Point(6, 2), new Point(7, 2), new Point(7, 3), new Point(6, 3), new Point(6, 4), new Point(7, 4), new Point(7, 5), new Point(6, 5), new Point(6, 6), new Point(7, 6), new Point(7, 7), new Point(6, 7) }
                };
            }

            // 10x10 Expert Masterpiece Labyrinth
            return new Point[][]
            {
                new Point[] { new Point(8, 1), new Point(9, 1), new Point(9, 0), new Point(8, 0), new Point(7, 0), new Point(7, 1), new Point(7, 2), new Point(7, 3), new Point(7, 4), new Point(7, 5) },
                new Point[] { new Point(4, 7), new Point(4, 6), new Point(4, 5), new Point(4, 4), new Point(5, 4), new Point(5, 5), new Point(6, 5), new Point(6, 4), new Point(6, 3), new Point(6, 2), new Point(6, 1), new Point(6, 0), new Point(5, 0), new Point(5, 1), new Point(5, 2), new Point(5, 3), new Point(4, 3), new Point(4, 2), new Point(4, 1), new Point(4, 0), new Point(3, 0), new Point(3, 1), new Point(2, 1), new Point(2, 0), new Point(1, 0), new Point(1, 1), new Point(0, 1), new Point(0, 0) },
                new Point[] { new Point(3, 7), new Point(3, 6), new Point(3, 5), new Point(3, 4), new Point(3, 3), new Point(3, 2), new Point(2, 2), new Point(1, 2), new Point(0, 2), new Point(0, 3), new Point(1, 3), new Point(1, 4), new Point(2, 4), new Point(2, 3) },
                new Point[] { new Point(5, 9), new Point(5, 8), new Point(5, 7), new Point(5, 6), new Point(6, 6), new Point(6, 7), new Point(6, 8), new Point(6, 9), new Point(7, 9), new Point(7, 8), new Point(7, 7), new Point(7, 6) },
                new Point[] { new Point(9, 8), new Point(9, 9), new Point(8, 9), new Point(8, 8), new Point(8, 7), new Point(9, 7), new Point(9, 6), new Point(8, 6), new Point(8, 5), new Point(9, 5), new Point(9, 4), new Point(8, 4), new Point(8, 3), new Point(9, 3), new Point(9, 2), new Point(8, 2) },
                new Point[] { new Point(1, 7), new Point(2, 7), new Point(2, 6), new Point(2, 5), new Point(1, 5), new Point(0, 5), new Point(0, 4) },
                new Point[] { new Point(4, 9), new Point(4, 8), new Point(3, 8), new Point(3, 9), new Point(2, 9), new Point(2, 8), new Point(1, 8), new Point(1, 9), new Point(0, 9), new Point(0, 8), new Point(0, 7), new Point(0, 6), new Point(1, 6) }
            };
        }

        private List<Point> GetEmptyNeighbors(Point p, HashSet<Point> occupied, int size)
        {
            var res = new List<Point>();
            Point[] dirs = { new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) };
            foreach (var d in dirs)
            {
                Point n = new Point(p.X + d.X, p.Y + d.Y);
                if (n.X >= 0 && n.X < size && n.Y >= 0 && n.Y < size && !occupied.Contains(n))
                    res.Add(n);
            }
            return res;
        }

        private List<Point> GetAllNeighbors(Point p, int size)
        {
            var res = new List<Point>();
            Point[] dirs = { new Point(0, 1), new Point(0, -1), new Point(1, 0), new Point(-1, 0) };
            foreach (var d in dirs)
            {
                Point n = new Point(p.X + d.X, p.Y + d.Y);
                if (n.X >= 0 && n.X < size && n.Y >= 0 && n.Y < size)
                    res.Add(n);
            }
            return res;
        }

        // ==========================================
        // 인게임 조작 및 상태 관리
        // ==========================================

        /// <summary>
        /// 신호 경로 연결 확정
        /// </summary>
        public bool CommitPath(int colorId, List<Point> path)
        {
            if (path == null || path.Count < 2) return false;

            Point start = path[0];
            Point end = path[path.Count - 1];

            var startNode = Grid[start.X, start.Y];
            var endNode = Grid[end.X, end.Y];

            // 시작점과 끝점이 같은 색상의 단말 노드이고 서로 다른 좌표여야 함
            if (startNode.Type == NodeType.Terminal && endNode.Type == NodeType.Terminal &&
                startNode.ColorID == colorId && endNode.ColorID == colorId && start != end)
            {
                // 스냅샷 저장 (Undo 지원)
                UndoStack.Push(new SignalSnapshot(CompletedPaths, MoveCount));
                MoveCount++;

                CompletedPaths[colorId] = new List<Point>(path);

                // 연결 성공 펄스 부여
                startNode.ConnectPulse = 1.0f;
                endNode.ConnectPulse = 1.0f;

                CheckVictory();
                return true;
            }

            return false;
        }

        /// <summary>
        /// 단일 신호선 연결 해제
        /// </summary>
        public void RemovePath(int colorId)
        {
            if (CompletedPaths.ContainsKey(colorId))
            {
                UndoStack.Push(new SignalSnapshot(CompletedPaths, MoveCount));
                CompletedPaths.Remove(colorId);
            }
        }

        /// <summary>
        /// 1수 되돌리기 (Undo)
        /// </summary>
        public bool Undo()
        {
            if (UndoStack.Count == 0 || !IsGameActive || IsVictory) return false;

            var snapshot = UndoStack.Pop();
            CompletedPaths.Clear();
            foreach (var kvp in snapshot.CompletedPaths)
            {
                CompletedPaths[kvp.Key] = new List<Point>(kvp.Value);
            }
            MoveCount = snapshot.MoveCount;

            return true;
        }

        /// <summary>
        /// 승리 여부 검증 (모든 쌍 연결 + 전 구역 100% 충진)
        /// </summary>
        public bool CheckVictory()
        {
            if (CompletedPaths.Count != PairCount) return false;

            int filledCount = 0;
            foreach (var path in CompletedPaths.Values)
                filledCount += path.Count;

            if (filledCount == GridSize * GridSize)
            {
                IsVictory = true;
                IsGameActive = false;

                try
                {
                    GateHelper.Dashboard.DashboardManager.RecordSignalLinkWin();
                }
                catch { }

                return true;
            }

            return false;
        }

        /// <summary>
        /// AI 힌트 요청 (아직 연결되지 않은 1개 색상의 최적 경로 반환)
        /// </summary>
        public int? RequestAiHint(out List<Point> hintPath)
        {
            hintPath = null;
            if (!IsGameActive || IsVictory) return null;

            // 아직 연결되지 않은 색상 우선 선택
            var unconnected = SolutionPaths.Keys.Where(k => !CompletedPaths.ContainsKey(k)).ToList();
            if (unconnected.Count == 0)
            {
                // 모두 연결되어 있으나 오답 경로인 경우 첫 번째 경로 제공
                unconnected = SolutionPaths.Keys.ToList();
            }

            if (unconnected.Count > 0)
            {
                int colorId = unconnected[_rand.Next(unconnected.Count)];
                hintPath = SolutionPaths[colorId];
                HintsUsed++;
                return colorId;
            }

            return null;
        }

        /// <summary>
        /// 3-Star 평가 산출 (이동 수, 힌트 수 기반)
        /// </summary>
        public int GetStarRating()
        {
            if (HintsUsed == 0 && MoveCount <= PairCount + 1) return 3;
            if (HintsUsed <= 1 && MoveCount <= PairCount + 4) return 2;
            return 1;
        }

        /// <summary>
        /// 현재 보드 채움율 (0% ~ 100%)
        /// </summary>
        public int GetCoveragePercentage()
        {
            int filled = 0;
            foreach (var p in CompletedPaths.Values) filled += p.Count;
            return (filled * 100) / (GridSize * GridSize);
        }
    }
}