namespace Osmalyzer;

public interface IChunkerItem
{
    (double x, double y) ChunkCoord { get; }
}