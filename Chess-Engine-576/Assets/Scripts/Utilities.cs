#region

using System.Collections.Generic;
using UnityEngine;

#endregion

public class Utilities : MonoBehaviour
{
    public const int MAXPawn = 8;
    public const int MAXPiece = 2;
    public const int MAXQueen = 9;

    public const string FileNames = "abcdefgh";

    public static int RankPos(int squarePos)
    {
        return squarePos >> 3;
    }

    public static int FilePos(int squarePos)
    {
        return squarePos & 7;
    }

    public static int FindPosition(SquarePosition squarePosition)
    {
        return squarePosition.rankPos * 8 + squarePosition.filePos;
    }

    public static SquarePosition MakeSquarePosition(int squarePos)
    {
        return SquarePosition.CreatePositionInstance(FilePos(squarePos), RankPos(squarePos));
    }

    public static bool ContainsSquare(ulong bitboard, int square)
    {
        return ((bitboard >> square) & 1) != 0;
    }


    public static PositionData StartBoardFen()
    {
        const string fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        var strings = fen.Split(' ');
        var pieceDict = new Dictionary<char, int>
        {
            ['k'] = Pieces.PieceObj.King,
            ['p'] = Pieces.PieceObj.Pawn,
            ['n'] = Pieces.PieceObj.Knight,
            ['b'] = Pieces.PieceObj.Bishop,
            ['r'] = Pieces.PieceObj.Rook,
            ['q'] = Pieces.PieceObj.Queen
        };

        var positionData = PositionData.CreatePositionDataInstance();
        var file = 0;
        var rank = 7;
        for (var i = 0; i < strings[0].Length; i++)
        {
            var c = strings[0][i];
            if (c == '/')
            {
                file = 0;
                rank--;
            }
            else
            {
                if (char.IsDigit(c))
                {
                    file += (int) char.GetNumericValue(c);
                }
                else
                {
                    var pieceColour = char.IsUpper(c) ? Pieces.PieceObj.White : Pieces.PieceObj.Black;
                    var pieceType = pieceDict[char.ToLower(c)];
                    positionData.squares[rank * 8 + file] = pieceType | pieceColour;
                    file++;
                }
            }
        }

        positionData.wTurn = strings[1] == "w";
        return positionData;
    }


    public class PositionData
    {
        #region 02. Actions

        public static PositionData CreatePositionDataInstance()
        {
            return new PositionData();
        }

        #endregion

        #region 04. Public variables

        public readonly int[] squares;
        public bool wTurn;

        #endregion

        private PositionData()
        {
            squares = new int[64];
        }
    }

    public readonly struct SquarePosition
    {
        public readonly int filePos;
        public readonly int rankPos;

        private SquarePosition(int filePos, int rankPos)
        {
            this.filePos = filePos;
            this.rankPos = rankPos;
        }

        public static SquarePosition CreatePositionInstance(int filePos, int rankPos)
        {
            return new SquarePosition(filePos, rankPos);
        }

        public bool WhiteSquare()
        {
            return (filePos + rankPos) % 2 != 0;
        }
    }

    public readonly struct Move
    {
        public struct Marker
        {
            public const int Ep = 1;
            public const int Castling = 2;
            public const int QueenPromotion = 3;
        }

        private const ushort startBitMask = 63;
        private const ushort targetBitMask = 4032;

        private ushort MoveValue { get; }

        private Move(int startSquare, int targetSquare)
        {
            MoveValue = (ushort) (startSquare | (targetSquare << 6));
        }

        public static Move CreateMoveInstance(int startSquare, int targetSquare)
        {
            return new Move(startSquare, targetSquare);
        }

        private Move(int startSquare, int targetSquare, int marker)
        {
            MoveValue = (ushort) (startSquare | (targetSquare << 6) | (marker << 12));
        }

        public static Move CreateMoveInstanceMarker(int startSquare, int targetSquare, int marker)
        {
            return new Move(startSquare, targetSquare, marker);
        }

        public int StartSquare
        {
            get
            {
                var startSquare = MoveValue & startBitMask;
                return startSquare;
            }
        }

        public int TargetSquare
        {
            get
            {
                var targetSquare = (MoveValue & targetBitMask) >> 6;
                return targetSquare;
            }
        }

        public bool Promotion
        {
            get
            {
                var marker = MoveMarker;
                return marker == Marker.QueenPromotion;
            }
        }

        public int MoveMarker
        {
            get
            {
                var moveMarker = MoveValue >> 12;
                return moveMarker;
            }
        }
    }
}