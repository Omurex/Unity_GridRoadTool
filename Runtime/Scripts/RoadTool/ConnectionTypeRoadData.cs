
/// <summary>
/// Container for data about what road piece should be spawned and at what rotation
/// </summary>
namespace GridRoadTool
{
    [System.Serializable]
    public class RoadPieceSpawnData
    {
        public RoadPiece roadPiecePrefab;
        public RotationType rotationType;
    }
}
