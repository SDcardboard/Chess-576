public class Pieces
{
    #region 02. Actions

    public void AddPieceAtSquare(int square)
    {
        _occupiedSquares[Count] = square;
        _boardMap[square] = Count;
        Count++;
    }

    public void MovePiece(int startSquare, int targetSquare)
    {
        var pieceIndex = _boardMap[startSquare];
        _occupiedSquares[pieceIndex] = targetSquare;
        _boardMap[targetSquare] = pieceIndex;
    }

    public void RemovePieceAtSquare(int square)
    {
        var pieceIndex = _boardMap[square];
        _occupiedSquares[pieceIndex] =
            _occupiedSquares[Count - 1];
        _boardMap[_occupiedSquares[pieceIndex]] =
            pieceIndex;
        Count--;
    }

    #endregion

    #region 04. Public variables

    public int Count { get; private set; }

    public int this[int index]
    {
        get
        {
            if (_occupiedSquares != null) return _occupiedSquares[index];
            return 0;
        }
    }

    #endregion

    #region 05. Private variables

    private readonly int[] _boardMap;


    private readonly int[] _occupiedSquares;

    #endregion

    #region 07. Nested Types

    public struct PieceObj
    {
        public const int None = 0;
        public const int King = 1;
        public const int Pawn = 2;
        public const int Knight = 3;
        public const int Bishop = 5;
        public const int Rook = 6;
        public const int Queen = 7;

        public const int White = 8;
        public const int Black = 16;

        private const int typeMask = 7;
        private const int blackMask = 16;
        private const int whiteMask = 8;
        private const int colourMask = whiteMask | blackMask;

        public static bool IsColour(int piece, int colour)
        {
            return (piece & colourMask) == colour;
        }

        public static int PieceType(int piece)
        {
            return piece & typeMask;
        }

        public static bool RookOrQueen(int piece)
        {
            return (piece & 6) == 6;
        }

        public static bool BishopOrQueen(int piece)
        {
            return (piece & 5) == 5;
        }
    }

    #endregion

    public Pieces(int maxPieceCount = 16)
    {
        _occupiedSquares = new int[maxPieceCount];
        _boardMap = new int[64];
        Count = 0;
    }
}