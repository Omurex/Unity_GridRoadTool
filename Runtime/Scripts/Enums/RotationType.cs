using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents rotation from default rotation
/// </summary>
public enum RotationType
{
    NO_ROTATION,
    ONE_CLOCKWISE,
    TWO_CLOCKWISE,
    THREE_CLOCKWISE
}


/// <summary>
/// Functions using RotationType
/// </summary>
public static class RotationTypeFunctions
{
    /// <summary>
    /// Gets rotation from rotType
    /// </summary>
    /// <param name="rotType"></param>
    /// <returns>Rotation in degrees</returns>
    public static float GetDegreeRotation(this RotationType rotType)
    {
        switch(rotType)
        {
            case RotationType.NO_ROTATION:
            {
                return 0;
            }

            case RotationType.ONE_CLOCKWISE:
            {
                return 90;
            }

            case RotationType.TWO_CLOCKWISE:
            {
                return 180;
            }

            case RotationType.THREE_CLOCKWISE:
            {
                return 270;
            }

            default:
            {
                throw new System.Exception("Rotation Type Not Valid!");
            }
        }
    }
}
