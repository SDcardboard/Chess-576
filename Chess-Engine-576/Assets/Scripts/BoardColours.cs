#region

using System;
using UnityEngine;

#endregion

[CreateAssetMenu(menuName = "BoardObjects/BoardColours")]
public class BoardColours : ScriptableObject
{
    public SquareColours white;
    public SquareColours black;

    [Serializable]
    public struct SquareColours
    {
        public Color normalColour;
        public Color legalMoveColor;
        public Color selectedPieceColor;
    }
}