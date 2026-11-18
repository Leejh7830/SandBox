using System;
using System.Collections.Generic;

namespace GateHelper.EcoSim.Services
{
    /// <summary>
    /// O(N^2) 근접 탐색을 방지하기 위한 Zero-GC 공간 분할 해시 그리드.
    /// </summary>
    public sealed class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly int _cols;
        private readonly int _rows;
        private readonly List<int>[] _cells;

        public SpatialGrid(float worldWidth, float worldHeight, float cellSize)
        {
            _cellSize = Math.Max(16f, cellSize);
            _cols = Math.Max(1, (int)Math.Ceiling(worldWidth / _cellSize));
            _rows = Math.Max(1, (int)Math.Ceiling(worldHeight / _cellSize));
            _cells = new List<int>[_cols * _rows];
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i] = new List<int>(8);
            }
        }

        public void Clear()
        {
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].Clear();
            }
        }

        public void Insert(int index, float x, float y)
        {
            int cx = ClampCol((int)(x / _cellSize));
            int cy = ClampRow((int)(y / _cellSize));
            _cells[cy * _cols + cx].Add(index);
        }

        /// <summary>
        /// 주어진 반경 내의 모든 후보 인덱스를 콜백 없이 버퍼에 채웁니다 (GC 0 Byte).
        /// </summary>
        public void QueryRadius(float x, float y, float radius, List<int> resultsBuffer)
        {
            resultsBuffer.Clear();
            int minX = ClampCol((int)((x - radius) / _cellSize));
            int maxX = ClampCol((int)((x + radius) / _cellSize));
            int minY = ClampRow((int)((y - radius) / _cellSize));
            int maxY = ClampRow((int)((y + radius) / _cellSize));

            for (int cy = minY; cy <= maxY; cy++)
            {
                int rowOffset = cy * _cols;
                for (int cx = minX; cx <= maxX; cx++)
                {
                    var cell = _cells[rowOffset + cx];
                    for (int k = 0; k < cell.Count; k++)
                    {
                        resultsBuffer.Add(cell[k]);
                    }
                }
            }
        }

        private int ClampCol(int c) => c < 0 ? 0 : (c >= _cols ? _cols - 1 : c);
        private int ClampRow(int r) => r < 0 ? 0 : (r >= _rows ? _rows - 1 : r);
    }
}
