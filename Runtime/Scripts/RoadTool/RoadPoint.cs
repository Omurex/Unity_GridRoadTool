using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using JosephLyons.Core.Extensions;


namespace GridRoadTool
{
    /// <summary>
    /// Point on RoadGrid that represents a possible inflection point where the road can change direction
    /// </summary>
    public class RoadPoint : MonoBehaviour
    {
        const float DEFAULT_ROAD_ELEVATION_FROM_POINT = .1f; // How far the physical road model is from the road point


        /// <summary>
        /// First Parameter = Previous RoadConnectionType, Second Parameter = this
        /// </summary>
        public UnityEvent<RoadPoint, RoadConnectionType> OnConnectionTypeChanged;


        // How the road at this point should connect to the rest of the network
        [SerializeField, HideInInspector] RoadConnectionType connectionType = RoadConnectionType.NONE;

        // Grid position
        [SerializeField, HideInInspector] Vector2Int pos;

        // Physical road model that this point has spawned
        [SerializeField, HideInInspector] RoadPiece spawnedRoadPiece;

        // All the bridges attached to 
        [SerializeField, HideInInspector] List<RoadPointBridge> bridges = new List<RoadPointBridge>();


        public Vector2Int GetPos() { return pos; }
        public void SetPos(Vector2Int _pos) { pos = _pos; EditorExtensions.MarkDirty(this); }

        public RoadPiece GetRoadPiece() { return spawnedRoadPiece; }

        public void AddRoadPointBridge(RoadPointBridge connector) { bridges.Add(connector); EditorExtensions.MarkDirty(this); }

        public RoadConnectionType GetConnectionType() { return connectionType; }

        /// <returns>True if connection type changed, false if connection type stays the same</returns>
        public bool SetConnectionType(RoadConnectionType _connectionType, RoadGrid grid, 
            float roadElevationFromPoint = DEFAULT_ROAD_ELEVATION_FROM_POINT)
        { 
            if(connectionType == _connectionType) return false; // If we're trying to change to what this is already set as, don't do anything

            RoadConnectionType previousConnectionType = connectionType;

            connectionType = _connectionType;

            if(spawnedRoadPiece != null) // Get rid of whatever road piece we currently have attached to this point
            {
                DestroyImmediate(spawnedRoadPiece.gameObject);
            }

            RoadPieceSpawnData data = grid.GetRoadPieceSpawnData(connectionType);

            if(data.roadPiecePrefab != null) // If there is a road piece prefab associated with our new connection type, spawn it
            {
                SpawnRoadPart(data.roadPiecePrefab, roadElevationFromPoint, data.rotationType, grid.GetScaleToApplyToRoads());
            }

            OnConnectionTypeChanged?.Invoke(this, previousConnectionType);

            EditorExtensions.MarkDirty(this);

            return true;
        }


        /// <summary>
        /// Initialize road point
        /// </summary>
        /// <param name="_pos">Position in road grid</param>
        /// <param name="roadPartPrefab"></param>
        /// <param name="rotationType"></param>
        /// <param name="roadPartScale"></param>
        /// <param name="roadElevationFromPoint"></param>
        public void Init(Vector2Int _pos, RoadPiece roadPartPrefab = null, RotationType rotationType = RotationType.NO_ROTATION, 
            Vector3? roadPartScale = null, float roadElevationFromPoint = DEFAULT_ROAD_ELEVATION_FROM_POINT)
        {
            SetPos(_pos);
            SpawnRoadPart(roadPartPrefab, roadElevationFromPoint, rotationType, roadPartScale);
        }


        /// <summary>
        /// Spawn passed in road part prefab
        /// </summary>
        /// <param name="roadPartPrefab">Prefab to spawn instance of</param>
        /// <param name="elevationFromPoint">How far elevated above road point it should be</param>
        /// <param name="rotType">How the road part should be rotated</param>
        /// <param name="scale">Scale of spawned road part</param>
        public void SpawnRoadPart(RoadPiece roadPartPrefab, float elevationFromPoint = DEFAULT_ROAD_ELEVATION_FROM_POINT, 
            RotationType rotType = RotationType.NO_ROTATION, Vector3? scale = null)
        {   
            if(scale.HasValue == false) // Default to (1, 1, 1) if scale not provided
            {
                scale = Vector3.one;
            }
            
            if(spawnedRoadPiece) // Destroy current spawned road piece if it exists
            {
                Destroy(spawnedRoadPiece);
            }

            // Return if there's nothing to spawn
            if(roadPartPrefab == null) return;

            spawnedRoadPiece = Instantiate(roadPartPrefab);

            spawnedRoadPiece.transform.parent = transform;
            spawnedRoadPiece.transform.localScale = scale.Value;
            spawnedRoadPiece.transform.localPosition = Vector3.up * DEFAULT_ROAD_ELEVATION_FROM_POINT;
            spawnedRoadPiece.transform.rotation = Quaternion.identity;

            spawnedRoadPiece.SetRotation(rotType);

            EditorExtensions.MarkDirty(this);
        }


        /// <summary>
        /// Update all bridges this road point is in charge of. In charge of only up and right bridges
        /// </summary>
        public void UpdateBridges()
        {
            // Keep record of which bridges to delete from bridges list
            Stack<int> bridgeIndexesToDelete = new Stack<int>();

            // Loop through bridges and either update them or delete them if they're not valid anymore
            for(int i = 0; i < bridges.Count; i++)
            {
                RoadPointBridge bridge = bridges[i];
                RoadConnectionType conType = bridge.GetConnectionType();

                if(connectionType.HasFlag(conType)) // If this road point still has conType, refresh bridge
                {
                    bridge.RefreshConnection();
                }
                else // If road point doesn't still have conType, delete bridge
                {
                    DestroyImmediate(bridge.gameObject);
                    bridgeIndexesToDelete.Push(i);
                }
            }

            while(bridgeIndexesToDelete.Count > 0) // Get rid of destroyed bridges from bridges list
            {
                bridges.RemoveAt(bridgeIndexesToDelete.Pop());
            }

            EditorExtensions.MarkDirty(this);
        }
    }
}