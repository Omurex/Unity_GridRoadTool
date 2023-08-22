using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Flags]
/// <summary>
/// Enum representing how the road point is connected to other road points
/// </summary>
public enum RoadConnectionType
{
    NONE = 0,
    NORTH = 1,
    SOUTH = 2,
    WEST = 4,
    EAST = 8
}


/// <summary>
/// A set of functions relating to RoadConnectionType
/// </summary>
public static class RoadConnectionTypeFunctions
{
    /// <summary>
    /// Gets opposite representation of conType; fails if conType has more than one flag set
    /// </summary>
    /// <param name="conType"></param>
    /// <returns></returns>
    public static RoadConnectionType GetOppositeConnectionType(this RoadConnectionType conType)
    {
        switch(conType)
        {
            case RoadConnectionType.NORTH: return RoadConnectionType.SOUTH;
            case RoadConnectionType.SOUTH: return RoadConnectionType.NORTH;

            case RoadConnectionType.WEST: return RoadConnectionType.EAST;
            case RoadConnectionType.EAST: return RoadConnectionType.WEST;

            default: return RoadConnectionType.NONE;
        }
    }


    /// <summary>
    /// Get all the directions that this RoadConnectionType has set to true
    /// </summary>
    /// <param name="conType"></param>
    /// <returns>List of directions conType represents</returns>
    public static List<Vector2Int> GetDirections(this RoadConnectionType conType)
    {
        List<Vector2Int> directions = new List<Vector2Int>(1);

        if(conType.HasFlag(RoadConnectionType.NORTH)) directions.Add(Vector2Int.up);
        if(conType.HasFlag(RoadConnectionType.SOUTH)) directions.Add(Vector2Int.down);
        if(conType.HasFlag(RoadConnectionType.WEST)) directions.Add(Vector2Int.left);
        if(conType.HasFlag(RoadConnectionType.EAST)) directions.Add(Vector2Int.right);

        if(directions.Count <= 0) directions.Add(Vector2Int.zero);

        return directions;
    }


    /// <summary>
    /// Converts direction to RoadConnectionType; dir must be normalized
    /// </summary>
    /// <param name="dir">Normalized direction (magnitude cannot be more than 1)</param>
    /// <returns></returns>
    public static RoadConnectionType GetRoadConnectionTypeFromDirection(this Vector2Int dir)
    {
        if(dir == Vector2Int.up) return RoadConnectionType.NORTH;
        if(dir == Vector2Int.down) return RoadConnectionType.SOUTH;
        if(dir == Vector2Int.left) return RoadConnectionType.WEST;
        if(dir == Vector2Int.right) return RoadConnectionType.EAST;
        else return RoadConnectionType.NONE;
    }


    /// <summary>
    /// Gets flag with every road connection set to true
    /// </summary>
    /// <returns></returns>
    public static RoadConnectionType GetEverythingFlag()
    {
        return RoadConnectionType.NORTH | RoadConnectionType.SOUTH | 
                RoadConnectionType.WEST | RoadConnectionType.EAST;
    }


    /// <summary>
    /// Because Unity autmatically fills entire byte with 1s when every flag is selected,
    /// this turns the RoadConnectionType enum into an arbitrary negative number. This function
    /// turns it back to how it should be
    /// </summary>
    /// <param name="conType">RoadConnectionType enum to try fixing</param>
    /// <returns>Fixed RoadConnectionType enum (won't do anything if already correct)</returns>
    public static void RemoveLeadingOnes(this ref RoadConnectionType conType)
    {
        conType = (RoadConnectionType) ((byte) conType & (byte) GetEverythingFlag());
    }
}
