using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JosephLyons.Core.Extensions;
using AYellowpaper.SerializedCollections;


#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace GridRoadTool
{
    /// <summary>
    /// Handles manipulation and management of road points
    /// </summary>
    [ExecuteAlways]
    public class RoadGrid : MonoBehaviour
    {
        public const float ROAD_ELEVATION_FROM_POINT = .1f;


        [SerializeField] RoadPoint roadPointPrefab;

        // Pass in a RoadConnectionType, get back data on road that should be spawned
        [SerializedDictionary("ConnectionType", "Road Spawn Data")]
        [SerializeField] SerializedDictionary<RoadConnectionType, RoadPieceSpawnData> connectionTypeToRoad;


        [SerializeField] RoadPoint[,] grid = new RoadPoint[0, 0]; // Grid that holds road points


        [SerializeField, HideInInspector] Terrain terrain; // Terrain this grid is attached to
        [SerializeField, HideInInspector] Vector2 intervalBetweenPoints; // Distance between points on x and y axis
        [SerializeField, HideInInspector] Vector3 scaleToApplyToRoads; // Scale to apply to spawned roads

        [SerializeField, HideInInspector] Vector2Int gridSize = Vector2Int.zero;

        [SerializeField, HideInInspector] bool initialized = false;


        public RoadPoint[,] GetGrid() { return grid; }

        /// <summary>
        /// Gets RoadPieceSpawnData from passed in RoadConnectionType
        /// </summary>
        /// <param name="connectionType"></param>
        /// <returns></returns>
        public RoadPieceSpawnData GetRoadPieceSpawnData(RoadConnectionType connectionType)
        { 
            // Because Unity serializes the 'Everything' flag as all ones, it becomes -1. This statement converts it
            if(connectionType == RoadConnectionTypeFunctions.GetEverythingFlag())
            {
                unchecked // Stops compiler from throwing error due to invalid cast
                {
                    connectionType = (RoadConnectionType) (-1);
                }
            }
            else if(connectionType != (RoadConnectionType) (-1)) // Unity might add leading ones to connectionType, get rid of them
            {
                connectionType.RemoveLeadingOnes();
            }

            return connectionTypeToRoad[connectionType]; 
        }

        public Vector3 GetScaleToApplyToRoads() { return scaleToApplyToRoads; }
        public Vector2 GetIntervalBetweenPoints() { return intervalBetweenPoints; }

        public RoadPoint GetRoadPoint(Vector2Int pos) { return GetRoadPoint(pos.x, pos.y); }

        public RoadPoint GetRoadPoint(int x, int y)
        {
            // Make passed in point inside of grid limits
            if(!IsPointInGrid(new Vector2Int(x, y))) return null;

            return grid[y, x];
        }


        public RoadPoint GetClosestRoadPointToWorldPosition(Vector3 worldPos) 
        { 
            return GetClosestRoadPointToWorldPosition(new Vector2(worldPos.x, worldPos.z)); 
        }


        public RoadPoint GetClosestRoadPointToWorldPosition(Vector2 worldPos)
        {
            if(terrain == null || grid == null || grid.GetLength(0) <= 0 || grid.GetLength(1) <= 0) return null;

            worldPos -= new Vector2(terrain.transform.position.x, terrain.transform.position.z);

            Vector2 gridSpacePos = new Vector2(worldPos.x / intervalBetweenPoints.x, worldPos.y / intervalBetweenPoints.y);
            Vector2Int gridPoint = new Vector2Int(Mathf.RoundToInt(gridSpacePos.x), Mathf.RoundToInt(gridSpacePos.y));

            if(!IsPointInGrid(gridPoint))
                return null;

            return grid[gridPoint.y, gridPoint.x];
        }


        /// <summary>
        /// Finds the closest axis aligned road point to the passed in point
        /// </summary>
        /// <param name="axisOrigin">Point in grid that we should use x and y to base axes off of</param>
        /// <param name="point">Point we want to clamp to axis</param>
        /// <returns></returns>
        public RoadPoint GetClosestAxisAlignedPointToRoadPoint(Vector2Int axisOrigin, Vector2Int point)
        {
            if(Mathf.Abs(point.x - axisOrigin.x) >= Mathf.Abs(point.y - axisOrigin.y))
            {
                point.y = axisOrigin.y;
            }
            else
            {
                point.x = axisOrigin.x;
            }

            return GetRoadPoint(point);
        }


        public bool IsInitialized() { return initialized; }


        public bool IsPointInGrid(Vector2Int gridPoint)
        {
            return gridPoint.x >= 0 && gridPoint.x < grid.GetLength(1) && 
                gridPoint.y >= 0 && gridPoint.y < grid.GetLength(0);
        }


        /// <summary>
        /// If we're in editor mode and enable is called, populate grid with children since grid probably reset at some point
        /// </summary>
        void OnEnable()
        {
            #if UNITY_EDITOR
            if(PrefabStageUtility.GetCurrentPrefabStage() != null || Application.isPlaying) return; // Not in prefab editing mode currently
            
            PopulateGridWithChildren();
            #endif

        }


        void Update()
        {
            if(transform.hasChanged) // Ignore grid transform changes
            {
                transform.localPosition = Vector3.zero;
                transform.rotation = Quaternion.identity;
                transform.localScale = Vector3.one;

                transform.hasChanged = false;
            }
            
            if(terrain != null && terrain.transform.hasChanged) // Cancel terrain transform changes
            {
                transform.rotation = Quaternion.identity;

                terrain.transform.rotation = Quaternion.identity;
                terrain.transform.localScale = Vector3.one;

                terrain.transform.hasChanged = false;
            }
        }


        /// <summary>
        /// Fill grid with already existing children road points
        /// </summary>
        void PopulateGridWithChildren()
        {
            // If grid is already populated, don't repopulate
            if(grid != null && (grid.GetLength(0) != 0 || grid.GetLength(1) != 0)) return;

            grid = new RoadPoint[gridSize.y, gridSize.x];

            var children = GetComponentsInChildren<RoadPoint>();

            foreach(RoadPoint point in children) // Loop through and fill grid with existing RoadPoints
            {
                grid[point.GetPos().y, point.GetPos().x] = point;
            }
        }


        public void Init(Terrain _terrain, Vector2 _intervalBetweenPoints, Vector3 _scaleToApplyToRoads)
        {
            ClearExistingRoadPoints();

            terrain = _terrain;
            intervalBetweenPoints = _intervalBetweenPoints;
            scaleToApplyToRoads = _scaleToApplyToRoads;

            gridSize = new Vector2Int(Mathf.FloorToInt(terrain.terrainData.size.x / intervalBetweenPoints.x) + 1, 
                Mathf.FloorToInt(terrain.terrainData.size.z / intervalBetweenPoints.y) + 1);

            grid = new RoadPoint[gridSize.y, gridSize.x];

            for(int r = 0; r < gridSize.y; r++)
            {
                for(int c = 0; c < gridSize.x; c++)
                {
                    Vector3 newPointPosition = terrain.transform.position;
                    newPointPosition += new Vector3(intervalBetweenPoints.x * c, 0, intervalBetweenPoints.y * r);

                    RoadPoint newPoint = Instantiate(roadPointPrefab, newPointPosition, Quaternion.identity);
                    newPoint.transform.parent = transform;
                    
                    newPoint.Init(new Vector2Int(c, r));

                    grid[r, c] = newPoint;

                    EditorExtensions.MarkDirty(newPoint);
                }
            }

            initialized = true;

            EditorExtensions.MarkDirty(this);
        }


        [ContextMenu("CLEAR POINTS")]
        /// <summary>
        /// Deletes all road point and bridge children
        /// </summary>
        void ClearExistingRoadPoints()
        {
            var roadPointChildren = GetComponentsInChildren<RoadPoint>();

            foreach(var child in roadPointChildren)
            {
                #if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
                #else
                Destroy(child.gameObject);
                #endif
            }

            var roadConnectorChildren = GetComponentsInChildren<RoadPointBridge>();

            foreach(var child in roadConnectorChildren)
            {
                #if UNITY_EDITOR
                DestroyImmediate(child.gameObject);
                #else
                Destroy(child.gameObject);
                #endif
            }

            grid = new RoadPoint[0, 0];

            EditorExtensions.MarkDirty(this);
        }
    }
}