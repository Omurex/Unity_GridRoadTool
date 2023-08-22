using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JosephLyons.Core.Extensions;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace GridRoadTool
{
    /// <summary>
    /// Handles intermediary road models placed between road points to close gaps and make it look like one continuous road
    /// </summary>
    [ExecuteAlways]
    public class RoadPointBridge : MonoBehaviour
    {
        const float DEFAULT_ROAD_ELEVATION_FROM_START_ROAD_PIECE = -.0025f;
        const int MAX_CONNECTING_ROADS_TO_PLACE = 400;


        [SerializeField] RoadPieceSpawnData horizontalRoadPrefab; // Road piece used to bridge horizontally
        [SerializeField] RoadPieceSpawnData verticalRoadPrefab; // Road piece used to bridge horizontally

        [SerializeField, HideInInspector] RoadConnectionType connectionType;

        [SerializeField, HideInInspector] RoadPoint startRoadPoint;
        [SerializeField, HideInInspector] RoadPoint endRoadPoint;

        [SerializeField, HideInInspector] Vector3 roadScale;
        [SerializeField, HideInInspector] float elevationFromConnector;


        public Vector2Int GetStartPos() { return startRoadPoint.GetPos(); }
        public RoadConnectionType GetConnectionType() { return connectionType; }


        public void RefreshConnection()
        {
            CreateConnection(connectionType, startRoadPoint, endRoadPoint, roadScale, elevationFromConnector);
        }


        public void CreateConnection(RoadConnectionType _connectionType, RoadPoint _startRoadPoint, 
            RoadPoint _endRoadPoint, Vector3 _roadScale, float _elevationFromConnector = DEFAULT_ROAD_ELEVATION_FROM_START_ROAD_PIECE)
        {
            ClearExistingConnections();

            if(_startRoadPoint == null || _endRoadPoint == null || _connectionType == RoadConnectionType.NONE)
            {
                Debug.LogError("CreateConnection - Invalid call");
                return;
            }

            connectionType = _connectionType;
            startRoadPoint = _startRoadPoint;
            endRoadPoint = _endRoadPoint;
            roadScale = _roadScale;
            elevationFromConnector = _elevationFromConnector;
    
            RoadPieceSpawnData pieceData;

            if(connectionType.HasFlag(RoadConnectionType.NORTH) || connectionType.HasFlag(RoadConnectionType.SOUTH))
            {
                pieceData = verticalRoadPrefab;
            }
            else
            {
                pieceData = horizontalRoadPrefab;
            }

            SpawnRoadConnections(pieceData, roadScale, elevationFromConnector);
        }


        /// <summary>
        /// Spawns road connections between start road point and end road point
        /// </summary>
        /// <param name="connectingPieceData">Data for horizontal or vertical piece used for connection</param>
        /// <param name="newRoadScale">Scale to apply to bridges</param>
        /// <param name="elevationFromBridgeObject">Elevation from this</param>
        void SpawnRoadConnections(RoadPieceSpawnData connectingPieceData, Vector3 newRoadScale, float elevationFromBridgeObject)
        {
            RoadPiece startPiece = startRoadPoint.GetRoadPiece();
            RoadPiece endPiece = endRoadPoint.GetRoadPiece();

            if (startPiece == null || endPiece == null) return; // Can't spawn road connections between non-existant pieces

            Vector3 startPos = startPiece.transform.position;
            Vector3 endPos = endPiece.transform.position;

            var startBounds = startPiece.GetRotatedBounds();
            var endBounds = endPiece.GetRotatedBounds();

            Vector2Int dir2D = connectionType.GetDirections()[0];

            UnityEngine.Assertions.Assert.IsTrue(connectionType.GetDirections().Count == 1); // connectionType should only have one flag set

            RoadPiece connectingPiecePrefab = connectingPieceData.roadPiecePrefab;
            Vector2 connectingPieceSize = CalculateConnectingPieceSize(connectingPieceData, newRoadScale);

            float startOffset;
            float endOffset;
            float sizeInDir;

            switch(connectionType)
            {
                case RoadConnectionType.NORTH:
                {
                    startOffset = startBounds.uB.y;
                    endOffset = endBounds.lB.y;
                    sizeInDir = connectingPieceSize.y;
                    break;
                }

                case RoadConnectionType.SOUTH:
                {
                    startOffset = startBounds.lB.y;
                    endOffset = endBounds.uB.y;
                    sizeInDir = connectingPieceSize.y;
                    break;
                }

                case RoadConnectionType.EAST:
                {
                    startOffset = startBounds.uB.x;
                    endOffset = endBounds.lB.x;
                    sizeInDir = connectingPieceSize.x;
                    break;
                }

                case RoadConnectionType.WEST:
                {
                    startOffset = startBounds.lB.x;
                    endOffset = endBounds.uB.x;
                    sizeInDir = connectingPieceSize.x;
                    break;
                }

                default:
                {
                    throw new System.Exception("RoadConnectionType not valid!");
                }
            }
            
            float dist = (endPiece.transform.position - startPiece.transform.position).magnitude;
            startOffset = Mathf.Abs(startOffset);
            endOffset = Mathf.Abs(endOffset);

            dist -= startOffset + endOffset;

            int numToPlace = Mathf.CeilToInt(dist / sizeInDir);

            UnityEngine.Assertions.Assert.IsTrue(numToPlace <= MAX_CONNECTING_ROADS_TO_PLACE);

            Vector3 dir = new Vector3(dir2D.x, 0, dir2D.y);

            RoadPiece lastPiecePlaced;

            for(int i = 0; i < numToPlace; i++)
            {
                Vector3 pos = startPos + (dir * startOffset) + (dir * (sizeInDir / 2));
                pos += (dir * sizeInDir) * i;
                
                pos.y = startPiece.transform.position.y + DEFAULT_ROAD_ELEVATION_FROM_START_ROAD_PIECE;

                lastPiecePlaced = Instantiate(connectingPiecePrefab, pos, Quaternion.identity, transform);
                lastPiecePlaced.SetRotation(connectingPieceData.rotationType);
                lastPiecePlaced.transform.localScale = newRoadScale;
            }

            EditorExtensions.MarkDirty(this);
        }


        /// <summary>
        /// Assumes rotation of piece is 0 and bound transforms are properly set as lower and upper bound
        /// </summary>
        /// <param name="piece"></param>
        /// <returns></returns>
        Vector2 CalculateConnectingPieceSize(RoadPieceSpawnData connectionData, Vector3 scale)
        {
            RoadPiece piece = connectionData.roadPiecePrefab;
            RotationType rotationType = connectionData.rotationType;

            var boundTransforms = piece.GetBoundTransforms();

            Vector3 ubVector = Vector3.Scale(boundTransforms.uB.position - piece.transform.position, scale);
            Vector3 lbVector = Vector3.Scale(boundTransforms.lB.position - piece.transform.position, scale);

            if(rotationType == RotationType.ONE_CLOCKWISE || rotationType == RotationType.THREE_CLOCKWISE)
            {
                Vector3 temp = ubVector;
                ubVector = lbVector;
                lbVector = temp;
            }

            return new Vector2(Mathf.Abs(ubVector.x - lbVector.x), Mathf.Abs(ubVector.z - lbVector.z));
        }


        /// <summary>
        /// Delete all road piece children
        /// </summary>
        public void ClearExistingConnections()
        {
            RoadPiece[] pieces = GetComponentsInChildren<RoadPiece>();

            foreach(RoadPiece piece in pieces)
            {
                DestroyImmediate(piece.gameObject);
            }

            EditorExtensions.MarkDirty(this);
        }
    }
}