﻿namespace DiffPatchFixed.Data;

public class ChunkRangeInfo
{
    public ChunkRangeInfo(ChunkRange originalRange, ChunkRange newRange)
    {
        OriginalRange = originalRange;
        NewRange = newRange;
    }

    public ChunkRange OriginalRange { get; }

    public ChunkRange NewRange { get; }
}
