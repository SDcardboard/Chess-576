#region

using System;
using UnityEngine;

#endregion

[CreateAssetMenu(menuName = "BoardObjects/Pieces")]
public class Sprites : ScriptableObject
{
    public SpritesHolder white;
    public SpritesHolder black;

    public Sprite GetPieceSprite(int piece)
    {
        SpritesHolder sprites;
        if (Pieces.PieceObj.IsColour(piece, Pieces.PieceObj.White))
            sprites = white;
        else
            sprites = black;

        return Pieces.PieceObj.PieceType(piece) switch
        {
            Pieces.PieceObj.Pawn => sprites.pawn,
            Pieces.PieceObj.Rook => sprites.rook,
            Pieces.PieceObj.Knight => sprites.knight,
            Pieces.PieceObj.Bishop => sprites.bishop,
            Pieces.PieceObj.Queen => sprites.queen,
            Pieces.PieceObj.King => sprites.king,
            _ => null
        };
    }

    [Serializable]
    public struct SpritesHolder
    {
        public Sprite pawn, rook, knight, bishop, queen, king;
    }
}