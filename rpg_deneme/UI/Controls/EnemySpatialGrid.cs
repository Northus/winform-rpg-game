using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using rpg_deneme.Models;

namespace rpg_deneme.UI.Controls;

internal sealed class EnemySpatialGrid
{
 // Reuse dictionaries to avoid allocation every frame
    private readonly Dictionary<long, int> _cellStart = new();
    private readonly Dictionary<long, int> _cellCounts = new();
    private readonly Dictionary<long, int> _cellOffsets = new();
    private readonly List<int> _cellIndices = new(64);
    private float _cellSize;

    public void Build(IReadOnlyList<BattleEntity> enemies, float cellSize)
    {
        _cellSize = Math.Max(1f, cellSize);
        _cellStart.Clear();
 _cellCounts.Clear();
  _cellOffsets.Clear();
        _cellIndices.Clear();

        int count = enemies.Count;
        if (count == 0) return;

   // First pass: count entities per cell
        for (int i = 0; i < count; i++)
   {
     var e = enemies[i];
       if (e.CurrentHP <= 0) continue;
   long key = Key(Cell(e.X + e.Width / 2f), Cell(e.Y + e.Height / 2f));
    _cellCounts.TryGetValue(key, out int c);
        _cellCounts[key] = c + 1;
      }

        // Calculate start indices
     int totalEntries = 0;
        foreach (var kvp in _cellCounts)
        {
 _cellStart[kvp.Key] = totalEntries;
            _cellOffsets[kvp.Key] = 0;
        totalEntries += kvp.Value;
        }

        // Pre-allocate indices list
        for (int i = 0; i < totalEntries; i++)
            _cellIndices.Add(-1);

        // Second pass: fill indices
        for (int i = 0; i < count; i++)
    {
        var e = enemies[i];
            if (e.CurrentHP <= 0) continue;
  long key = Key(Cell(e.X + e.Width / 2f), Cell(e.Y + e.Height / 2f));
    int start = _cellStart[key];
            int offset = _cellOffsets[key];
            _cellIndices[start + offset] = i;
         _cellOffsets[key] = offset + 1;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ForEachNeighborIndex(float x, float y, Action<int> visitor)
    {
  int cx = Cell(x);
        int cy = Cell(y);

        for (int dy = -1; dy <= 1; dy++)
        {
            for (int dx = -1; dx <= 1; dx++)
      {
       long key = Key(cx + dx, cy + dy);
     if (!_cellStart.TryGetValue(key, out int start)) continue;
           if (!_cellCounts.TryGetValue(key, out int cellCount)) continue;

                int end = start + cellCount;
          for (int i = start; i < end; i++)
      {
           int idx = _cellIndices[i];
   if (idx >= 0) visitor(idx);
     }
 }
     }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int Cell(float v) => (int)MathF.Floor(v / _cellSize);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long Key(int x, int y) => ((long)x << 32) ^ (uint)y;
}
