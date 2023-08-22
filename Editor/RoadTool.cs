#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using JosephLyons.Core.Extensions;


namespace GridRoadTool
{
    /// <summary>
    /// Tool used for placing roads on a grid
    /// </summary>
    public class RoadTool : EditorWindow
    {
        static readonly Color HIGHLIGHTED_POINT_COLOR = new Color(1, .92f, .016f, .25f); // Color of sphere showing which road point the user is hovering over
        const float HIGHLIGHTED_POINT_RADIUS = 1; // How big the sphere for the highlighted point is

        static readonly Color SELECTED_POINT_COLOR = new Color(0, .15f, 1, .5f); // Color of sphere showing which point the user has selected
        const float SELECTED_POINT_RADIUS = 2; // How big the sphere for the selected point is

        static readonly Color DRAG_END_POINT_COLOR = new Color(.5f, 0, 1, .5f);
        const float DRAG_END_POINT_RADIUS = 2; // How big the sphere for the drag end point is

        static readonly (Color fill, Color outline) DRAG_RECTANGLE_COLORS = 
            (new Color(.5f, .5f, .5f, .5f), Color.black);


        const string ROAD_GRID_PREFAB_PATH = "RoadTool/RoadGrid"; // Path of RoadGrid prefab to spawn
        const string ROAD_POINT_BRIDGE_PREFAB_PATH = "RoadTool/RoadPointBridge"; // Path of RoadPointBridge prefab to spawn between road points

        const float MIN_INTERVAL_BETWEEN_POINTS = .25f; // Tool throws error if user tries to initialize the interval with any components less than this value
        const float MIN_ROAD_SCALE = .01f; // Tool throws error if user tries to initialize the road scale with any components less than this value

        const int MAX_DRAG_LENGTH = 1000; // Max number of tiles a drag creation / destruction can be

        /// <summary>
        /// Function for having a change to a road point "cascade", or apply to other points in the row / column
        /// </summary>
        /// <param name="startPoint">Point we're currently applying the changes to</param>
        /// <param name="pointConnectionType">New connection type we're cascading with and applying to the point</param>
        /// <param name="shouldEndHere">Function may have special behavior if it is the last point to get the cascade functionality</param>
        /// <returns></returns>
        delegate void ConnectionCascadeFunction(RoadPoint startPoint, RoadConnectionType pointConnectionType, bool shouldEndHere);

        /// <summary>
        /// Function that determines if we should end our drag selection at the current target point
        /// </summary>
        /// <returns>True if we should end drag, false if otherwise</returns>
        delegate bool DragSelectionEndCheck();


        #region Prefabs
        static RoadGrid roadGridPrefab;
        static RoadPointBridge roadPointBridgePrefab;
        #endregion


        static RoadGrid roadGrid; // Current RoadGrid we have selected


        // RoadPoint the user is currently hovering over with the mouse
        static RoadPoint _highlightedRoadPoint;
        static RoadPoint HighlightedRoadPoint
        {
            get { return _highlightedRoadPoint; }
            set
            {
                if(value != HighlightedRoadPoint) repaintQueued = true;

                _highlightedRoadPoint = value;
            }
        }

        // RoadPoint the user currently has selected to modify
        static RoadPoint _selectedRoadPoint;
        static RoadPoint SelectedRoadPoint
        {
            get { return _selectedRoadPoint; }
            set
            {
                if(value != SelectedRoadPoint) repaintQueued = true;

                _selectedRoadPoint = value;
            }
        }

        // RoadPoint the user currently is dragging to
        static RoadPoint _dragEndRoadPoint;
        static RoadPoint DragEndRoadPoint
        {
            get { return _dragEndRoadPoint; }
            set
            {
                if(value != DragEndRoadPoint) repaintQueued = true;

                _dragEndRoadPoint = value;
            }
        }

        // Is user dragging to draw road
        static bool _isDragging = false;
        static bool IsDragging
        {
            get { return _isDragging; }
            set
            {
                if(value == IsDragging) return;

                repaintQueued = true;
                _isDragging = value;

                if(IsDragging == false)
                {
                    OnDragEnd();
                }
            }
        }


        static bool repaintQueued = true; // If true, will repaint 


        #region RoadGrid Initialization Data
        static Terrain terrain = null; // Terrain we have selected
        static Vector2 intervalBetweenRoadPoints = new Vector2(5, 5); // Distance between road points
        static Vector3 roadScale = new Vector3(.18f, .18f, .18f); // Scale to apply to road pieces
        #endregion


        [MenuItem("Tools/Road Generation Tool")]
        public static void ShowWindow()
        {
            LoadPrefabs();
            GetWindow(typeof(RoadTool));
        }


        /// <summary>
        /// Loads prefabs from resources if they aren't already loaded
        /// </summary>
        static void LoadPrefabs()
        {
            if(roadGridPrefab == null)
                roadGridPrefab = (Resources.Load(ROAD_GRID_PREFAB_PATH, typeof(GameObject)) as GameObject).GetComponent<RoadGrid>();
            
            if(roadPointBridgePrefab == null)
                roadPointBridgePrefab = (Resources.Load(ROAD_POINT_BRIDGE_PREFAB_PATH, typeof(GameObject)) as GameObject).GetComponent<RoadPointBridge>();
        }


        // Repaint whenever inspector changes so we reflect changes in editor in tool
        void OnInspectorUpdate() 
        {
            Repaint();
        }


        // Display tool options
        void OnGUI()
        {
            DrawContents();
        }


        // Make sure initialization steps are done when we re-enter focus
        void OnFocus()
        {
            LoadPrefabs(); // Make sure prefabs are loaded properly

            // Add callbacks
            SceneView.beforeSceneGui += OnBeforeSceneGUI;
            SceneView.duringSceneGui += OnDuringSceneGUI;
        }


        // Make sure we aren't calling things we shouldn't when we aren't in focus
        void OnLostFocus() 
        {
            // Remove callbacks if they are attached
            SceneView.duringSceneGui -= OnDuringSceneGUI;
            SceneView.beforeSceneGui -= OnBeforeSceneGUI;
        }


        void OnBeforeSceneGUI(SceneView sceneView)
        {
            if(!EditorWindow.HasOpenInstances<RoadTool>()) return; // If tool not open, return

            // True if the user did something and we want to cancel Unity's built in functionality from triggering (selecting objects, etc.)
            bool useEvent = false;

            if(roadGrid == null) // Try to get or create road grid if we don't have one cached
            {
                useEvent = useEvent || TryGetRoadGridOnTerrain();
            }
            else
            {
                if(IsDragging)
                {
                    UpdateDragEndPoint();
                }
                else
                {
                    UpdateHighlightedRoadPointToNearestRoadPointToMouse();
                    useEvent = useEvent || TrySelectPoint();
                }            
            }

            // If user performed an action supported by the tool, cancel whatever Unity was going to do with its bult-in functionality
            if(useEvent)
            {
                Selection.activeGameObject = null;
                Event.current.Use();
            }

            // Resets highlight road point or drag end road point if either isn't in use
            if(IsDragging)
            {
                HighlightedRoadPoint = null;
            }
            else
            {
                DragEndRoadPoint = null;
            }

            UpdateIsDragging();
        }


        void OnDuringSceneGUI(SceneView sceneView)
        {
            // Don't run if roadGrid isn't cached or we have no open windows
            if(roadGrid == null) return;
            if(!EditorWindow.HasOpenInstances<RoadTool>()) return;

            DrawHandles();
            RepaintSceneIfQueued();
        }


        /// <summary>
        /// Updates where drag end point is, stops if drag selection would be invalid
        /// </summary>
        void UpdateDragEndPoint()
        {
            if(SelectedRoadPoint == null) return; // Can't drag if we don't have a selected road point

            RoadPoint nearestMousePoint = DetectNearestRoadPointToMouse();

            // Clamps nearest mouse point to closest point that is on the same x or y axis as our selected point
            // Do this since we can only drag in a straight line on the x or y axis
            RoadPoint axisAlignedPoint = GetAxisAlignedPointFromSelectedPoint(nearestMousePoint);

            if(DragEndRoadPoint == axisAlignedPoint || axisAlignedPoint == null) return; // Don't bother doing calculations if 
                                                                                            // we've already done them for this point

            // Get direction from selection to axis aligned point
            Vector2Int dir = axisAlignedPoint.GetPos() - SelectedRoadPoint.GetPos();
            int length = dir.GetLengthOfAxisAlignedVec();

            // No dir means no selection
            if(length <= 0)
            {
                DragEndRoadPoint = null;
                return;
            }

            dir /= length; // Normalize dir

            RoadConnectionType conType = dir.GetRoadConnectionTypeFromDirection(); // Converts dir to RoadConnectionType
            RoadConnectionType oppConType = conType.GetOppositeConnectionType(); // Opposite to conType
            RoadConnectionType combinedConType = conType | oppConType; // Has conType and oppConType flags set

            // Target is which road point we're currently looking at when determining if drag should stop
            RoadPoint target = SelectedRoadPoint;

            int pointsChecked = 0; // Number of points we've checked when looking to see if drag should stop


            // Functions used to check if selection should stop
            bool ShouldEndAddRoadSelection()
            {
                // End when road point already has connection type we are trying to add
                return (target.GetConnectionType() & combinedConType) != 0;
            }

            bool ShouldEndRemoveRoadSelection()
            {
                // End when road point doesn't have the connection type we are trying to remove
                return !target.GetConnectionType().HasFlag(conType) || !target.GetConnectionType().HasFlag(oppConType);
            }


            DragSelectionEndCheck shouldEnd; // Function to call to determine if selection should end

            if(SelectedRoadPoint.GetConnectionType().HasFlag(conType) == false) // Adding road
            {
                shouldEnd = ShouldEndAddRoadSelection;
            }
            else // Removing road
            {
                shouldEnd = ShouldEndRemoveRoadSelection;
            }


            target = roadGrid.GetRoadPoint(target.GetPos() + dir); // Advance target in direction

            // Loop through each point starting at SelectedRoadPoint until axisAlignedPoint
            // If we hit a point that isn't valid for the drag selection, end the drag early at that point
            while(target != axisAlignedPoint && pointsChecked <= MAX_DRAG_LENGTH) // Try to draw selection to axis aligned point, up to limit
            {
                if(shouldEnd.Invoke()) // If we should end selection, break out of loop
                {
                    break;
                }
                else
                {
                    target = roadGrid.GetRoadPoint(target.GetPos() + dir); // Advance target in direction
                }

                pointsChecked++;
            }

            if(target) DragEndRoadPoint = target; // If target exists, set end point to it
        }


        /// <summary>
        /// Checks if mouse state has changed. If it has, will change IsDragging accordingly
        /// </summary>
        void UpdateIsDragging()
        {
            if(Event.current.isMouse == false || Event.current.button != 0) return;

            if(Event.current.rawType == EventType.MouseDrag)
            {
                IsDragging = true;
            }
            else if(Event.current.rawType == EventType.MouseUp)
            {
                IsDragging = false;
            }
        }


        /// <summary>
        /// When drag ends, try to either add / remove the roads depending on the context
        /// </summary>
        static void OnDragEnd()
        {
            // Don't do anything if SelectedRoadPoint and DragEndRoadPoint are the same, or if either are null
            if(SelectedRoadPoint == null || DragEndRoadPoint == null || SelectedRoadPoint == DragEndRoadPoint) return;

            // Get direction from SelectedRoadPoint to DragEndRoadPoint
            Vector2Int dir = DragEndRoadPoint.GetPos() - SelectedRoadPoint.GetPos();
            int length = dir.GetLengthOfAxisAlignedVec();
            dir /= length; // Normalize dir

            RoadConnectionType dirConType = dir.GetRoadConnectionTypeFromDirection();

            // If selected point doesn't have connection type, we are adding it. If it already has, we are removing it
            bool addRoad = SelectedRoadPoint.GetConnectionType().HasFlag(dirConType) == false;

            if(!addRoad) length -= 1;

            int numLoops = 0; // Don't really need this, but basically ensures if something goes wrong we won't get stuck in infinite loop

            RoadPoint point = SelectedRoadPoint;
            int spacesTravelled = 0;

            // Loop through entire drag selection and either add or remove road accordingly
            do
            {
                RoadConnectionType prevConType = point.GetConnectionType(); // Connection type before we change it
                point.SetConnectionType(prevConType ^ dirConType, roadGrid); // Flip flag representing direction we're adding / removing


                RoadPoint newPoint = UpdateConnectingPointsInSingleDirection(point, dirConType, length - spacesTravelled - 1);

                spacesTravelled += (point.GetPos() - newPoint.GetPos()).GetLengthOfAxisAlignedVec();

                point = newPoint;

                numLoops++;

            } while(spacesTravelled < length && numLoops < MAX_DRAG_LENGTH);


            RoadPoint nextPoint = roadGrid.GetRoadPoint(point.GetPos() + dir); // Next point from end point

			if (nextPoint != null)
			{
				RoadConnectionType nextConType = nextPoint.GetConnectionType();

				RoadConnectionType nextOppConType = nextConType.GetOppositeConnectionType();
				RoadConnectionType nextCombinedConType = nextConType & nextOppConType;

            
                // If next point isn't connected to point, make sure point isn't trying to connect
                if(!nextPoint.GetConnectionType().HasFlag(dirConType.GetOppositeConnectionType()))
                {
                    point.SetConnectionType(point.GetConnectionType() & ~dirConType, roadGrid);
                }

                if(!addRoad) // If removing road, make sure point and next point aren't connected
                {
                    point.SetConnectionType(point.GetConnectionType() & ~dirConType, roadGrid);
                    nextPoint.SetConnectionType(nextConType & ~dirConType.GetOppositeConnectionType(), roadGrid);
                }

            }

            UpdateBridgesForPointAndNeighbors(point);
            UpdateBridgesForPointAndNeighbors(nextPoint);


            UnityEngine.Assertions.Assert.IsTrue(numLoops < MAX_DRAG_LENGTH);
        }


        /// <summary>
        /// Repaint everything in scene if repaintQueued is true, allows for editor tool to be very responsive
        /// </summary>
        static void RepaintSceneIfQueued()
        {
            if(repaintQueued)
            {
                SceneView.RepaintAll();
                repaintQueued = false;
            }
        }


        /// <summary>
        /// Draw everything in editor window, draws different things depending on context
        /// </summary>
        static void DrawContents()
        {
            if(roadGrid == null) // If roadGrid is null, prompt user to click terrain to spawn new RoadGrid or find existing RoadGrid on terrain
            {
                EditorGUILayout.HelpBox("Click on a terrain with a layer called 'Terrain' to set up a RoadGrid and get started drawing roads.", MessageType.Info);
            }
            else if(roadGrid.IsInitialized() == false) // If roadGrid isn't initialized, prompt user with initialization fields and button to initialize
            {
                InitializationInfoFields();

                InitializeRoadGridButton();

                UnselectAndDeleteButtons();
            }
            else if(SelectedRoadPoint == null) // If user hasn't selected a road point, prompt user to select one
            {
                EditorGUILayout.HelpBox("Click on a point on the terrain to select a point.", MessageType.Info);

                UnselectAndDeleteButtons();
            }
            else // If we have a road point selected, user can start modifying the different road points
            {
                DrawRoadPointModificationOptions();

                UnselectAndDeleteButtons();
            }
        }


        /// <summary>
        /// Displays in editor window variables for RoadGrid initialization
        /// </summary>
        static void InitializationInfoFields()
        {
            intervalBetweenRoadPoints = EditorGUILayout.Vector2Field("Interval Between Road Points", intervalBetweenRoadPoints);
            roadScale = EditorGUILayout.Vector3Field("Road Scale", roadScale);
        }


        /// <summary>
        /// Displays button in editor to initialize roadGrid, also does data validation when button pressed
        /// </summary>
        static void InitializeRoadGridButton()
        {
            bool initializeRoadGrid = GUILayout.Button("Initialize Road Grid");
            
            if(initializeRoadGrid)
            {
                if(intervalBetweenRoadPoints.x < MIN_INTERVAL_BETWEEN_POINTS || intervalBetweenRoadPoints.y < MIN_INTERVAL_BETWEEN_POINTS)
                {
                    EditorUtility.DisplayDialog("Invalid Interval!", "Interval Between Road Points cannot have any components less than " + 
                        MIN_INTERVAL_BETWEEN_POINTS.ToString() + ".", "Ok");

                    return;
                }

                if(roadScale.x < MIN_ROAD_SCALE || roadScale.y < MIN_ROAD_SCALE || roadScale.z < MIN_ROAD_SCALE)
                {
                    EditorUtility.DisplayDialog("Invalid Road Scale!", "Road Scale cannot have any components less than " + 
                        MIN_ROAD_SCALE.ToString() + ".", "Ok");

                    return;
                }

                roadGrid.Init(terrain, intervalBetweenRoadPoints, roadScale);
            }    
        }


        /// <summary>
        /// Put Unselect and Delete buttons at bottom of editor window
        /// </summary>
        static void UnselectAndDeleteButtons()
        {
            GUILayout.FlexibleSpace();
            UnselectRoadGridButton();
            DeleteRoadGridButton();
        }


        /// <summary>
        /// Button to remove selected RoadGrid
        /// </summary>
        static void UnselectRoadGridButton()
        {
            bool unselectRoadGrid = GUILayout.Button("Unselect Road Grid");
            if(unselectRoadGrid) roadGrid = null;
        }


        /// <summary>
        /// Button to delete selected RoadGrid, has confirmation window to confirm decision
        /// </summary>
        static void DeleteRoadGridButton()
        {
            bool deleteRoadGrid = GUILayout.Button("Delete Road Grid");
            if(deleteRoadGrid && EditorUtility.DisplayDialog("Are you sure you want to delete this Road Grid?",
                "This action cannot be undone.", "Confirm", "Cancel")) DestroyImmediate(roadGrid.gameObject);
        }


        /// <summary>
        /// Clamps down passed in point to align with either x or y axis of our selected road point
        /// </summary>
        /// <param name="point">Point we want to clamp to axis</param>
        /// <returns>Axis-aligned version of point</returns>
        static RoadPoint GetAxisAlignedPointFromSelectedPoint(RoadPoint point)
        {
            if(roadGrid == null || point == null) return null;

            return roadGrid.GetClosestAxisAlignedPointToRoadPoint(SelectedRoadPoint.GetPos(), point.GetPos());
        }


        /// <summary>
        /// If user clicked mouse on terrain, either get existing RoadGrid child on terrain or create new one if it doesn't exist
        /// </summary>
        /// <returns>True if roadGrid successfully set</returns>
        static bool TryGetRoadGridOnTerrain()
        {
            if(Event.current.rawType != EventType.MouseDown || Event.current.button != 0) return false;

            if(RaycastTerrainFromMouse(out RaycastHit hit))
            {
                terrain = hit.collider.GetComponent<Terrain>();
                
                UnityEngine.Assertions.Assert.IsNotNull(terrain);
                
                roadGrid = hit.collider.GetComponentInChildren<RoadGrid>();
                if(roadGrid == null) // Terrain did not have existing road grid
                {
                    roadGrid = Instantiate(roadGridPrefab); 
                    roadGrid.transform.parent = terrain.transform;
                }

                return true;
            }

            return false;
        }


        /// <summary>
        /// When road point is selected, show options for connection type and do corresponding updates if changed
        /// </summary>
        static void DrawRoadPointModificationOptions()
        {
			EditorGUILayout.LabelField("Road Point Connected Directions");
            RoadConnectionType connectionType = (RoadConnectionType) EditorGUILayout.EnumFlagsField(SelectedRoadPoint.GetConnectionType());

            RoadConnectionType previousConnectionType = SelectedRoadPoint.GetConnectionType();

            if(connectionType == previousConnectionType) return;

            bool connectionChanged = SelectedRoadPoint.SetConnectionType(connectionType, roadGrid);

            if(connectionChanged)
            {
                UpdateChangedConnectingPoints(SelectedRoadPoint, connectionType, previousConnectionType);
            }
        }


        /// <summary>
        /// Update all road points in direction of new connection type
        /// </summary>
        /// <param name="start">Point we start the update from</param>
        /// <param name="connectionType">The connection type the start point just got updated with</param>
        /// <param name="addedConnectionType">True if connectionType was added to start, false if connectionType was removed from start (changes cascading type)</param>
        /// <param name="maxLength">Max number of iterations it should run, default is essentially unlimited</param>
        /// <returns>End road point that cascading stopped at</returns>
        /// <summary>
        static RoadPoint UpdatePointsInDirection(Vector2Int start, RoadConnectionType connectionType, bool addedConnectionType, int maxLength)
        {
            if(roadGrid == null || connectionType == RoadConnectionType.NONE) return null;

            Vector2Int dir = connectionType.GetDirections()[0];

            UnityEngine.Assertions.Assert.IsTrue(connectionType.GetDirections().Count == 1); // connectionType should only ever have one flag set

            RoadConnectionType oppositeConnectionType = connectionType.GetOppositeConnectionType(); // Up -> Down, Right -> Left, etc.
            RoadConnectionType flippedOpposite = ~oppositeConnectionType; // Every flag is set except for oppositeConnectionType
            RoadConnectionType combinedConnectionType = connectionType | oppositeConnectionType; // Up and Down, Left and Right
            RoadConnectionType flippedCombined = ~combinedConnectionType; // Up and Down -> Left and Right, and vice versa

            /*
                Start                      End
                ^                          ^
                > <> <> <> <> <> <> <> <
                v                          v
            */

            void AddConnectionCascade(RoadPoint point, RoadConnectionType pointConnectionType, bool shouldEndHere)
            {
                if(shouldEndHere)
                {
                    // If we're ending at this point, add the opposite of the newly added connectionType
                    // As pictured above, end needs to have opposite to direction to match start
                    point.SetConnectionType((pointConnectionType | oppositeConnectionType), roadGrid);
                }
                else
                {
                    // In between road points must go both forwards and backwards
                    point.SetConnectionType(combinedConnectionType, roadGrid);
                }
            }


            /// <summary>
            /// Function to call when removing connection type
            /// </summary>
            void RemoveConnectionCascade(RoadPoint point, RoadConnectionType pointConnectionType, bool shouldEndHere)
            {
                if(shouldEndHere)
                {
                    // Removes just the opposite direction from the point
                    point.SetConnectionType(pointConnectionType & flippedOpposite, roadGrid);
                }
                else
                {
                    // Removes forwards and backwards directions from the point
                    point.SetConnectionType(pointConnectionType & flippedCombined, roadGrid);
                }
            }

            ConnectionCascadeFunction cascadeFunction; // Function to set connectionType info for points we run through

            if(addedConnectionType)
            {
                cascadeFunction = AddConnectionCascade;
            }
            else
            {
                cascadeFunction = RemoveConnectionCascade;
            }


            Vector2Int pos = start + dir;
            int numLoops = 0;

            bool shouldEndHere = false;

            // Loop through until we either hit the end of the grid or hit a road point with extra connections
            while(roadGrid.IsPointInGrid(pos) && shouldEndHere == false && numLoops <= maxLength)
            {
                numLoops++;

                RoadPoint point = roadGrid.GetRoadPoint(pos);
                RoadConnectionType pointConnectionType = point.GetConnectionType();

                // If & does not result in 0, we know this point has more connections, and we should end our cascading here
                shouldEndHere = (flippedCombined & pointConnectionType) != 0;

                cascadeFunction.Invoke(point, pointConnectionType, shouldEndHere);

                point.UpdateBridges(); // Update connectors attached to point, will fix any visual bridges attached to this point

                pos += dir; // Advance targeted pos in direction
            }

            
            // Set up variables used for bridge generation
            Vector2Int bridgeDir = dir;
            Vector2Int bridgeStart = start;
            RoadConnectionType bridgeConType = connectionType;

            if(addedConnectionType) // Only form bridges if we added a connection
            {
                // Since we have all road point bridges go up and right from a point (and never left / down),
                    // if the current direction would have us go left or down, we instead start at the end point
                    // and go right / up from there
                if(connectionType.HasFlag(RoadConnectionType.WEST) || connectionType.HasFlag(RoadConnectionType.SOUTH))
                {
                    bridgeStart += bridgeDir * numLoops;
                    bridgeDir *= -1;
                    bridgeConType = bridgeConType.GetOppositeConnectionType();
                }
                
                for(int i = 0; i < numLoops; i++) // For every road point we affected during this function, create a bridge to close gaps
                {
                    CreateBridgeBetweenPoints(roadGrid.GetRoadPoint(bridgeStart + (bridgeDir * i)), bridgeConType);
                }
            }

            return roadGrid.GetRoadPoint(pos - dir); // Returns the point we ended at
        }


        /// <summary>
        /// Since it's not guaranteed that each road point will instantly connect to each other visually, we need to add
        /// intermediary roads to bridge the gap
        /// </summary>
        /// <param name="startingRoadPoint">Origin road point of this bridge</param>
        /// <param name="addedConType">Connection type that's been added</param>
        static void CreateBridgeBetweenPoints(RoadPoint startingRoadPoint, RoadConnectionType addedConType)
        {
            Vector2Int pos = startingRoadPoint.GetPos();
            List<Vector2Int> directions = addedConType.GetDirections();

            foreach(Vector2Int dir in directions)
            {
                Vector2Int nextPos = pos + dir;

                if(roadGrid.IsPointInGrid(nextPos))
                {
                    RoadPoint nextRoadPoint = roadGrid.GetRoadPoint(nextPos);
                    Vector3 midPoint = (startingRoadPoint.transform.position + nextRoadPoint.transform.position) / 2f;

                    RoadPointBridge bridge = Instantiate<RoadPointBridge>(roadPointBridgePrefab, midPoint, Quaternion.identity, roadGrid.transform);

                    bridge.CreateConnection(dir.GetRoadConnectionTypeFromDirection(), startingRoadPoint, nextRoadPoint, roadGrid.GetScaleToApplyToRoads());
                
                    startingRoadPoint.AddRoadPointBridge(bridge);
                }
            }
        }


        /// <summary>
        /// Update bridges for point, left of point, and down from point. This handles all cases of bridges that possibly need updating
        /// </summary>
        /// <param name="point"></param>
        static void UpdateBridgesForPointAndNeighbors(RoadPoint point)
        {
            if(point == null) return;

            Vector2Int leftPos = point.GetPos() + Vector2Int.left;
            Vector2Int downPos = point.GetPos() + Vector2Int.down;

            if(roadGrid.IsPointInGrid(leftPos)) roadGrid.GetRoadPoint(leftPos).UpdateBridges();
            if(roadGrid.IsPointInGrid(downPos)) roadGrid.GetRoadPoint(downPos).UpdateBridges();

            point.UpdateBridges();
        }


        /// <summary>
        /// Updates all points in direction, until we hit end point. Also updates bridges for start point and end points
        /// </summary>
        /// <param name="point">Point we're originating update from</param>
        /// <param name="dir">Which direction we're updating in</param>
        /// <param name="maxLength">Max length of update in direction</param>
        /// <returns>End point</returns>
        static RoadPoint UpdateConnectingPointsInSingleDirection(RoadPoint point, RoadConnectionType dir, int maxLength = int.MaxValue)
        {
            RoadPoint endPoint = UpdatePointsInDirection(point.GetPos(), dir, (point.GetConnectionType() & dir) != 0, maxLength);
            UpdateBridgesForPointAndNeighbors(endPoint);
            UpdateBridgesForPointAndNeighbors(point);

            return endPoint;
        }


        /// <summary>
        /// When point is changed, updates other points in grid depending on new connection data for point
        /// </summary>
        /// <param name="point">Point that changed</param>
        /// <param name="current">Point's current RoadConnectionType</param>
        /// <param name="previous">Point's previous RoadConnectionType</param>
        static void UpdateChangedConnectingPoints(RoadPoint point, RoadConnectionType current, RoadConnectionType previous, int maxLength = int.MaxValue)
        {
            RoadConnectionType changedFlags = current ^ previous; // Changed flags will only have the flags that changed

            Queue<RoadPoint> endPoints = new Queue<RoadPoint>(); // All the endpoints in our "UpdatePointsInDirection" calls, keep these to update bridges later

            // Call "UpdatePointsInDirection" for every direction flag that has been changed
            if(changedFlags.HasFlag(RoadConnectionType.NORTH))
            {
                UpdateConnectingPointsInSingleDirection(point, RoadConnectionType.NORTH, maxLength);
            }

            if(changedFlags.HasFlag(RoadConnectionType.SOUTH))
            {
                UpdateConnectingPointsInSingleDirection(point, RoadConnectionType.SOUTH, maxLength);
            }

            if(changedFlags.HasFlag(RoadConnectionType.WEST))
            {
                UpdateConnectingPointsInSingleDirection(point, RoadConnectionType.WEST, maxLength);
            }

            if(changedFlags.HasFlag(RoadConnectionType.EAST))
            {
                UpdateConnectingPointsInSingleDirection(point, RoadConnectionType.EAST, maxLength);
            }
        }


        /// <summary>
        /// Loads highlightedRoadPoint with nearest grid position to mouse
        /// </summary>
        RoadPoint DetectNearestRoadPointToMouse()
        {
            if(RaycastTerrainFromMouse(out RaycastHit hit))
            {
                RoadPoint point = roadGrid?.GetClosestRoadPointToWorldPosition(hit.point);
                return point;
            }
            else
            {
                return null;
            }
        }


        void UpdateHighlightedRoadPointToNearestRoadPointToMouse()
        {
            RoadPoint result = DetectNearestRoadPointToMouse();
            HighlightedRoadPoint = result;
        }


        /// <summary>
        /// Raycasts from editor camera to terrain using mouse position
        /// </summary>
        /// <param name="hit">Where to put raycast results</param>
        /// <returns>True if raycast successful, false if not</returns>
        static bool RaycastTerrainFromMouse(out RaycastHit hit)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            return Physics.Raycast(ray, out hit, Mathf.Infinity, LayerRepo.TERRAIN.mask, QueryTriggerInteraction.Ignore);
        }


        /// <summary>
        /// If user clicks mouse, change selected point to whatever highlighted point currently is
        /// </summary>
        /// <returns>True if click successful, false if otherwise</returns>
        bool TrySelectPoint()
        {
            // Left click happened
            if(Event.current.rawType == EventType.MouseDown && Event.current.button == 0)
            {
                SelectedRoadPoint = HighlightedRoadPoint;
                return true;
            }

            return false;
        }


        /// <summary>
        /// Draws debug handles to the screen
        /// </summary>
        static void DrawHandles()
        {
            if(!EditorWindow.HasOpenInstances<RoadTool>()) return;
            if(roadGrid == null) return;

            if(HighlightedRoadPoint) // Draw highlighted road point
            {
                DrawRoadPoint(HighlightedRoadPoint, HIGHLIGHTED_POINT_COLOR, HIGHLIGHTED_POINT_RADIUS);
            }

            if(SelectedRoadPoint) // Draw selected road point
            {
                DrawRoadPoint(SelectedRoadPoint, SELECTED_POINT_COLOR, SELECTED_POINT_RADIUS);
            }

            TryDrawDragSelection();
            DrawGrid();
        }


        static void TryDrawDragSelection()
        {
            if(DragEndRoadPoint)
            {
                DrawRoadPoint(DragEndRoadPoint, DRAG_END_POINT_COLOR, DRAG_END_POINT_RADIUS);

                if(SelectedRoadPoint != null)
                {
                    Vector2 interval = roadGrid.GetIntervalBetweenPoints();

                    // posDiff guaranteed to have at least one component be 0
                    Vector2Int posDiff = DragEndRoadPoint.GetPos() - SelectedRoadPoint.GetPos();

                    int length = posDiff.GetLengthOfAxisAlignedVec();

                    if(length != 0)
                    {
                        Vector2Int axis = posDiff / length;

                        Vector3 absAxis = new Vector3(Mathf.Abs(axis.x), 0, Mathf.Abs(axis.y));
                        Vector3 absOppositeAxis = new Vector3(absAxis.z, 0, absAxis.x);

                        float intervalHalfLength = interval.x * absOppositeAxis.x + interval.y * absOppositeAxis.z;
                        intervalHalfLength /= 2;

                        Vector3[] verts = new Vector3[4];
                        verts[0] = DragEndRoadPoint.transform.position + (absOppositeAxis * -intervalHalfLength);
                        verts[1] = DragEndRoadPoint.transform.position + (absOppositeAxis * intervalHalfLength);
                        verts[2] = SelectedRoadPoint.transform.position + (absOppositeAxis * intervalHalfLength);
                        verts[3] = SelectedRoadPoint.transform.position + (absOppositeAxis * -intervalHalfLength);

                        Handles.color = Color.white;
                        Handles.DrawSolidRectangleWithOutline(verts, DRAG_RECTANGLE_COLORS.fill, DRAG_RECTANGLE_COLORS.outline);
                    }
                }   
            }
        }


        static void DrawGrid()
        {
            Handles.color = Color.red;
            RoadPoint[,] grid = roadGrid.GetGrid();

            // Draws grid, where the boxes represent road points
            if(grid.GetLength(0) > 0 && grid.GetLength(1) > 0)
            {
                Vector2 interval = roadGrid.GetIntervalBetweenPoints();
                Vector3 halfInterval = new Vector3(interval.x / 2f, 0, interval.y / 2f);
                Vector3 halfIntervalX = new Vector3(halfInterval.x, 0, 0);
                Vector3 halfIntervalZ = new Vector3(0, 0, halfInterval.z);

                for(int r = 1; r < grid.GetLength(0); r++)
                {
                    Handles.DrawLine(grid[r, 0].transform.position - halfInterval, grid[r, grid.GetLength(1) - 1].transform.position + halfIntervalX - halfIntervalZ);
                }

                for(int c = 1; c < grid.GetLength(1); c++)
                {
                    Handles.DrawLine(grid[0, c].transform.position - halfInterval, grid[grid.GetLength(0) - 1, c].transform.position + halfIntervalZ - halfIntervalX);
                }
            }
        }


        static void DrawRoadPoint(RoadPoint point, Color color, float radius)
        {
            Handles.color = color;

            Vector3 pointPos = point.transform.position;

            Handles.SphereHandleCap(0, pointPos, Quaternion.identity, radius, Event.current.rawType);
        }
    }
}
#endif