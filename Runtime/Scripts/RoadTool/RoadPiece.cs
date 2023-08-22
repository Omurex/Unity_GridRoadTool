using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RotationType;


namespace GridRoadTool
{
    /// <summary>
    /// Road model with data for connecting and rotating it
    /// </summary>
    public class RoadPiece : MonoBehaviour
    {
        // Bounds used to determine how to connect different road points together
        [SerializeField] Transform lowerBound; // Lowerbound of connecting bounds
        [SerializeField] Transform upperBound; // Upperbound of connecting bounds

        public RotationType rotationType { get; protected set; }


        /// <summary>
        /// Rotates piece given rotationType
        /// </summary>
        /// <param name="_rotationType"></param>
        public void SetRotation(RotationType _rotationType)
        {
            if(rotationType == _rotationType) return;

            rotationType = _rotationType;

            Vector3 euler = transform.rotation.eulerAngles;

            euler.y = rotationType.GetDegreeRotation();

            transform.rotation = Quaternion.Euler(euler);
        }


        /// <summary>
        /// Rotates piece by 90 degrees * numTimes
        /// </summary>
        /// <param name="numTimes">How many times to rotate 90 clockwise</param>
        public void Rotate90Clockwise(int numTimes)
        {
            int numRotationPossibilities = System.Enum.GetNames(typeof(RotationType)).Length;

            // Since we're rotating 90 degrees every time, every 4 turns will bring the piece back to what it was
            numTimes %= numRotationPossibilities;

            int currentRotation = (int) rotationType;
            currentRotation = (currentRotation + numTimes) % numRotationPossibilities;

            Vector3 euler = transform.rotation.eulerAngles;
            euler.z += 90 * numTimes;
            transform.rotation = Quaternion.Euler(euler);

            rotationType = (RotationType) currentRotation;
        }


        public (Transform lB, Transform uB) GetBoundTransforms() { return (lowerBound, upperBound); }


        /// <summary>
        /// Gets the lowerbound and upperbound of the object taking its rotation into account
        /// </summary>
        public (Vector2 lB, Vector2 uB) GetRotatedBounds()
        {
            (Vector2, Vector2) CreateBoundsReturn(float lBX, float lBY, float uBX, float uBY)
            {
                Vector3 pos = transform.position;

                lBX -= pos.x;
                lBY -= pos.z;
                uBX -= pos.x;
                uBY -= pos.z;

                return (new Vector2(lBX, lBY), new Vector2(uBX, uBY));
            }

            Vector2 lB = new Vector2(lowerBound.position.x, lowerBound.position.z);

            Vector2 uB = new Vector2(upperBound.position.x, upperBound.position.z);

            switch(rotationType)
            {
                case(NO_ROTATION):
                {
                    return CreateBoundsReturn(lB.x, lB.y, uB.x, uB.y);
                }

                case(ONE_CLOCKWISE):
                {
                    return CreateBoundsReturn(lB.x, uB.y, uB.x, lB.y);
                }

                case(TWO_CLOCKWISE):
                {
                    return CreateBoundsReturn(uB.x, uB.y, lB.x, lB.y);
                }

                case(THREE_CLOCKWISE):
                {
                    return CreateBoundsReturn(uB.x, lB.y, lB.x, uB.y);
                }
                
                default:
                {
                    throw new System.Exception("Rotation Type Not Valid!");
                }
            }
        }
    }
}