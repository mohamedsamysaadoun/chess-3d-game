using System;

namespace Chess3D.Game
{
    [Serializable]
    public struct Move
    {
        public byte fromSquare;
        public byte toSquare;
        public byte promotionPiece;
        public byte moveFlags;

        public const byte FLAG_CAPTURE = 0x01;
        public const byte FLAG_CASTLE = 0x02;
        public const byte FLAG_EN_PASSANT = 0x04;
        public const byte FLAG_PROMOTION = 0x08;
        public const byte FLAG_DOUBLE_PAWN = 0x10;

        public static Move Empty => new Move();

        public static bool operator ==(Move m1, Move m2) =>
            m1.fromSquare == m2.fromSquare && m1.toSquare == m2.toSquare && m1.promotionPiece == m2.promotionPiece;
        public static bool operator !=(Move m1, Move m2) => !(m1 == m2);
        public override bool Equals(object obj) => obj is Move m && this == m;
        public override int GetHashCode() => fromSquare | (toSquare << 6) | (promotionPiece << 12);

        public static Move ParseRegularCAN(string can)
        {
            if (string.IsNullOrEmpty(can) || can.Length < 4) return Empty;
            var move = new Move();
            move.fromSquare = (byte)((8 - (can[1] - '0')) * 16 + (can[0] - 'a'));
            move.toSquare = (byte)((8 - (can[3] - '0')) * 16 + (can[2] - 'a'));
            if (can.Length >= 5)
            {
                move.promotionPiece = can[4] switch
                {
                    'q' => 5, 'r' => 4, 'b' => 3, 'n' => 2, _ => (byte)0
                };
                move.moveFlags |= FLAG_PROMOTION;
            }
            return move;
        }

        public override string ToString()
        {
            int fromFile = fromSquare % 16;
            int fromRank = 8 - (fromSquare / 16);
            int toFile = toSquare % 16;
            int toRank = 8 - (toSquare / 16);
            string result = $"{(char)('a' + fromFile)}{fromRank}{(char)('a' + toFile)}{toRank}";
            if (promotionPiece != 0)
                result += promotionPiece switch { 5 => 'q', 4 => 'r', 3 => 'b', 2 => 'n', _ => '?' };
            return result;
        }
    }

    [Serializable]
    public struct ScoredMove
    {
        public Move move;
        public int score;
    }

    public class OpeningBook
    {
        public short GetOpeningMove(int moveNumber) => -1;
        public OpeningBook() { }
    }
}
