using System;
using System.Collections.Generic;

namespace Chess3D.Game
{
    /// <summary>
    /// Complete chess engine - reconstructed from Real Chess 3D.
    /// Uses alpha-beta search with quiescence and PST evaluation.
    /// </summary>
    public sealed class ChessEngine
    {
        // Constants
        private const int MAX_DEPTH = 10;
        private const int MAX_PLY = 32;
        private const int MATE_SCORE = 100000;
        private const int MATE_THRESHOLD = 99000;

        // Piece types
        internal const int EMPTY = 0;
        internal const int PIECE_PAWN = 1;
        internal const int PIECE_KNIGHT = 2;
        internal const int PIECE_BISHOP = 3;
        internal const int PIECE_ROOK = 4;
        internal const int PIECE_QUEEN = 5;
        internal const int PIECE_KING = 6;

        // Colors
        internal const int WHITE = 0;
        internal const int BLACK = 1;
        internal const int NO_COLOR = -1;

        // Move flags
        public const byte FLAG_CAPTURE = 0x01;
        public const byte FLAG_CASTLE = 0x02;
        public const byte FLAG_EN_PASSANT = 0x04;
        public const byte FLAG_PROMOTION = 0x08;
        public const byte FLAG_DOUBLE_PAWN = 0x10;

        // Square constants
        private const int A1 = 56, E1 = 60, H1 = 63;
        private const int A8 = 0, E8 = 4, H8 = 7;

        // Board state - using 0x88 board representation for easy off-board detection
        // 0x88: square = rank * 16 + file; off-board if (square & 0x88) != 0
        private int[] board;           // 128 squares (0x88)
        private int[] pieceColor;      // 128 squares
        private int side;              // Side to move
        private int castlingRights;    // KQkq bits
        private int enPassantSq;       // En passant target square (0x88) or -1
        private int halfMoveClock;
        private int fullMoveNumber;

        // Search state
        private int ply;
        private Move bestMove;
        private bool searching;

        // Move list for current ply
        private List<Move>[] moveList;
        private Stack<HistoryMove> history;

        // Piece values
        private static readonly int[] pieceValues = { 0, 100, 320, 330, 500, 1000, 20000 };

        // Piece-square tables (from white's perspective, 0x88 format)
        // Index: square = rank * 16 + file, where rank 0 = rank 8 (top), rank 7 = rank 1 (bottom)
        private static readonly int[] pawnPST = new int[128];
        private static readonly int[] knightPST = new int[128];
        private static readonly int[] bishopPST = new int[128];
        private static readonly int[] kingPST = new int[128];
        private static readonly int[] kingEndgamePST = new int[128];

        // Knight offsets (0x88)
        private static readonly int[] knightOffsets = { -33, -31, -18, -14, 14, 18, 31, 33 };
        // King offsets
        private static readonly int[] kingOffsets = { -17, -16, -15, -1, 1, 15, 16, 17 };
        // Bishop directions
        private static readonly int[] bishopDirs = { -17, -15, 15, 17 };
        // Rook directions
        private static readonly int[] rookDirs = { -16, -1, 1, 16 };
        // Queen directions
        private static readonly int[] queenDirs = { -17, -16, -15, -1, 1, 15, 16, 17 };

        static ChessEngine()
        {
            // Initialize PSTs (simplified - using standard values)
            // Pawn
            int[] pawnTable = {
                 0,  0,  0,  0,  0,  0,  0,  0,
                 5, 10, 10,-20,-20, 10, 10,  5,
                 5, -5,-10,  0,  0,-10, -5,  5,
                 0,  0,  0, 20, 20,  0,  0,  0,
                 5,  5, 10, 25, 25, 10,  5,  5,
                10, 10, 20, 30, 30, 20, 10, 10,
                50, 50, 50, 50, 50, 50, 50, 50,
                 0,  0,  0,  0,  0,  0,  0,  0
            };
            int[] knightTable = {
                -50,-40,-30,-30,-30,-30,-40,-50,
                -40,-20,  0,  5,  5,  0,-20,-40,
                -30,  5, 10, 15, 15, 10,  5,-30,
                -30,  0, 15, 20, 20, 15,  0,-30,
                -30,  5, 15, 20, 20, 15,  5,-30,
                -30,  0, 10, 15, 15, 10,  0,-30,
                -40,-20,  0,  0,  0,  0,-20,-40,
                -50,-40,-30,-30,-30,-30,-40,-50
            };
            int[] bishopTable = {
                -20,-10,-10,-10,-10,-10,-10,-20,
                -10,  5,  0,  0,  0,  0,  5,-10,
                -10, 10, 10, 10, 10, 10, 10,-10,
                -10,  0, 10, 10, 10, 10,  0,-10,
                -10,  5,  5, 10, 10,  5,  5,-10,
                -10,  0,  5, 10, 10,  5,  0,-10,
                -10,  0,  0,  0,  0,  0,  0,-10,
                -20,-10,-10,-10,-10,-10,-10,-20
            };
            int[] kingTable = {
                 20, 30, 10,  0,  0, 10, 30, 20,
                 20, 20,  0,  0,  0,  0, 20, 20,
                -10,-20,-20,-20,-20,-20,-20,-10,
                -20,-30,-30,-40,-40,-30,-30,-20,
                -30,-40,-40,-50,-50,-40,-40,-30,
                -30,-40,-40,-50,-50,-40,-40,-30,
                -30,-40,-40,-50,-50,-40,-40,-30,
                -30,-40,-40,-50,-50,-40,-40,-30
            };
            int[] kingEndgameTable = {
                -50,-30,-30,-30,-30,-30,-30,-50,
                -30,-30,  0,  0,  0,  0,-30,-30,
                -30,-10, 20, 30, 30, 20,-10,-30,
                -30,-10, 30, 40, 40, 30,-10,-30,
                -30,-10, 30, 40, 40, 30,-10,-30,
                -30,-10, 20, 30, 30, 20,-10,-30,
                -30,-20,-10,  0,  0,-10,-20,-30,
                -50,-40,-30,-20,-20,-30,-40,-50
            };

            // Fill 0x88 tables (file 0-7, rank 0-7 → square = rank*16 + file)
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int sq88 = rank * 16 + file;
                    int sq64 = rank * 8 + file;
                    pawnPST[sq88] = pawnTable[sq64];
                    knightPST[sq88] = knightTable[sq64];
                    bishopPST[sq88] = bishopTable[sq64];
                    kingPST[sq88] = kingTable[sq64];
                    kingEndgamePST[sq88] = kingEndgameTable[sq64];
                }
            }
        }

        public ChessEngine()
        {
            board = new int[128];
            pieceColor = new int[128];
            moveList = new List<Move>[MAX_PLY];
            for (int i = 0; i < MAX_PLY; i++)
                moveList[i] = new List<Move>(128);
            history = new Stack<HistoryMove>();
            bestMove = new Move();
            searching = false;
        }

        // ================================================================
        // PUBLIC API
        // ================================================================

        public string GetBestMove(string fen, string lastMove, int depth, bool exactMove)
        {
            // Reset state
            searching = false;
            bestMove = new Move();
            ply = 0;
            history.Clear();

            ParseFEN(fen);
            searching = true;

            // Iterative deepening
            for (int d = 1; d <= Math.Min(depth, MAX_DEPTH); d++)
            {
                int score = AlphaBeta(d, int.MinValue + 1, int.MaxValue, false);
                // bestMove is updated inside AlphaBeta
            }

            searching = false;

            if (bestMove.fromSquare == 0 && bestMove.toSquare == 0)
            {
                // Fallback: return first legal move
                var moves = GenerateLegalMoves();
                if (moves.Count > 0)
                    return MoveToCAN(moves[0]);
                return null;
            }

            return MoveToCAN(bestMove);
        }

        // ================================================================
        // FEN PARSING
        // ================================================================

        public void ParseFEN(string fen)
        {
            // Clear board
            for (int i = 0; i < 128; i++)
            {
                board[i] = EMPTY;
                pieceColor[i] = NO_COLOR;
            }
            castlingRights = 0;
            enPassantSq = -1;
            halfMoveClock = 0;
            fullMoveNumber = 1;

            string[] parts = fen.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return;

            // Parse board (FEN starts from rank 8 = top)
            int rank = 0, file = 0;
            foreach (char c in parts[0])
            {
                if (c == '/')
                {
                    rank++;
                    file = 0;
                }
                else if (char.IsDigit(c))
                {
                    file += c - '0';
                }
                else
                {
                    int sq88 = rank * 16 + file;
                    int color = char.IsUpper(c) ? WHITE : BLACK;
                    int piece = char.ToLower(c) switch
                    {
                        'p' => PIECE_PAWN,
                        'n' => PIECE_KNIGHT,
                        'b' => PIECE_BISHOP,
                        'r' => PIECE_ROOK,
                        'q' => PIECE_QUEEN,
                        'k' => PIECE_KING,
                        _ => EMPTY
                    };
                    board[sq88] = piece;
                    pieceColor[sq88] = color;
                    file++;
                }
            }

            // Side to move
            side = (parts.Length > 1 && parts[1] == "w") ? WHITE : BLACK;

            // Castling
            if (parts.Length > 2)
            {
                if (parts[2].Contains('K')) castlingRights |= 1;
                if (parts[2].Contains('Q')) castlingRights |= 2;
                if (parts[2].Contains('k')) castlingRights |= 4;
                if (parts[2].Contains('q')) castlingRights |= 8;
            }

            // En passant
            if (parts.Length > 3 && parts[3] != "-")
            {
                int epFile = parts[3][0] - 'a';
                int epRank = 8 - (parts[3][1] - '0');  // FEN rank 8 = our rank 0
                enPassantSq = epRank * 16 + epFile;
            }

            // Halfmove clock
            if (parts.Length > 4 && int.TryParse(parts[4], out int hm))
                halfMoveClock = hm;

            // Fullmove number
            if (parts.Length > 5 && int.TryParse(parts[5], out int fm))
                fullMoveNumber = fm;
        }

        // ================================================================
        // MOVE GENERATION (0x88 board)
        // ================================================================

        private static bool OnBoard(int sq) => (sq & 0x88) == 0;

        private List<Move> GenerateLegalMoves()
        {
            var pseudoMoves = new List<Move>();
            GeneratePseudoMoves(pseudoMoves);

            var legalMoves = new List<Move>();
            foreach (var move in pseudoMoves)
            {
                MakeMove(move);
                if (!IsInCheck(side == WHITE ? BLACK : WHITE))  // Check if OUR king is in check (side already swapped)
                    legalMoves.Add(move);
                UnmakeMove(move);
            }
            return legalMoves;
        }

        private void GeneratePseudoMoves(List<Move> moves)
        {
            for (int sq = 0; sq < 128; sq++)
            {
                if ((sq & 0x88) != 0) continue;  // Off board
                if (board[sq] == EMPTY) continue;
                if (pieceColor[sq] != side) continue;

                switch (board[sq])
                {
                    case PIECE_PAWN:
                        GeneratePawnMoves(sq, moves);
                        break;
                    case PIECE_KNIGHT:
                        GenerateKnightMoves(sq, moves);
                        break;
                    case PIECE_BISHOP:
                        GenerateSlidingMoves(sq, bishopDirs, moves);
                        break;
                    case PIECE_ROOK:
                        GenerateSlidingMoves(sq, rookDirs, moves);
                        break;
                    case PIECE_QUEEN:
                        GenerateSlidingMoves(sq, queenDirs, moves);
                        break;
                    case PIECE_KING:
                        GenerateKingMoves(sq, moves);
                        break;
                }
            }
        }

        private void GeneratePawnMoves(int from, List<Move> moves)
        {
            int forward = side == WHITE ? -16 : 16;  // 0x88: white moves up (-16), black moves down (+16)
            int startRank = side == WHITE ? 6 : 1;   // Rank 2 (white) = row 6, Rank 7 (black) = row 1
            int promoRank = side == WHITE ? 0 : 7;   // Rank 8 (white) = row 0, Rank 1 (black) = row 7

            // Forward one
            int to = from + forward;
            if (OnBoard(to) && board[to] == EMPTY)
            {
                if (to / 16 == promoRank)
                    AddPromotionMoves(from, to, 0, moves);
                else
                    moves.Add(MakeMove(from, to, 0));

                // Double push
                if (from / 16 == startRank)
                {
                    int to2 = from + 2 * forward;
                    if (OnBoard(to2) && board[to2] == EMPTY)
                        moves.Add(MakeMove(from, to2, FLAG_DOUBLE_PAWN));
                }
            }

            // Captures
            int[] capDirs = side == WHITE ? new[] { -15, -17 } : new[] { 15, 17 };
            foreach (int d in capDirs)
            {
                int capTo = from + d;
                if (!OnBoard(capTo)) continue;
                if (board[capTo] != EMPTY && pieceColor[capTo] != side)
                {
                    if (capTo / 16 == promoRank)
                        AddPromotionMoves(from, capTo, FLAG_CAPTURE, moves);
                    else
                        moves.Add(MakeMove(from, capTo, FLAG_CAPTURE));
                }
                else if (capTo == enPassantSq && enPassantSq >= 0)
                {
                    moves.Add(MakeMove(from, capTo, FLAG_CAPTURE | FLAG_EN_PASSANT));
                }
            }
        }

        private void AddPromotionMoves(int from, int to, byte flags, List<Move> moves)
        {
            moves.Add(MakeMove(from, to, flags, PIECE_QUEEN));
            moves.Add(MakeMove(from, to, flags, PIECE_ROOK));
            moves.Add(MakeMove(from, to, flags, PIECE_BISHOP));
            moves.Add(MakeMove(from, to, flags, PIECE_KNIGHT));
        }

        private void GenerateKnightMoves(int from, List<Move> moves)
        {
            foreach (int offset in knightOffsets)
            {
                int to = from + offset;
                if (!OnBoard(to)) continue;
                if (pieceColor[to] == side) continue;

                byte flags = (board[to] != EMPTY) ? FLAG_CAPTURE : (byte)0;
                moves.Add(MakeMove(from, to, flags));
            }
        }

        private void GenerateSlidingMoves(int from, int[] dirs, List<Move> moves)
        {
            foreach (int dir in dirs)
            {
                int to = from + dir;
                while (OnBoard(to))
                {
                    if (pieceColor[to] == side) break;

                    byte flags = (board[to] != EMPTY) ? FLAG_CAPTURE : (byte)0;
                    moves.Add(MakeMove(from, to, flags));

                    if (board[to] != EMPTY) break;  // Capture, stop
                    to += dir;
                }
            }
        }

        private void GenerateKingMoves(int from, List<Move> moves)
        {
            foreach (int offset in kingOffsets)
            {
                int to = from + offset;
                if (!OnBoard(to)) continue;
                if (pieceColor[to] == side) continue;

                byte flags = (board[to] != EMPTY) ? FLAG_CAPTURE : (byte)0;
                moves.Add(MakeMove(from, to, flags));
            }

            // Castling
            if (side == WHITE && from == E1)
            {
                // Kingside
                if ((castlingRights & 1) != 0 && board[E1 + 1] == EMPTY && board[E1 + 2] == EMPTY
                    && board[H1] == PIECE_ROOK && pieceColor[H1] == WHITE
                    && !IsSquareAttacked(E1, BLACK) && !IsSquareAttacked(E1 + 1, BLACK))
                    moves.Add(MakeMove(E1, E1 + 2, FLAG_CASTLE));
                // Queenside
                if ((castlingRights & 2) != 0 && board[E1 - 1] == EMPTY && board[E1 - 2] == EMPTY && board[E1 - 3] == EMPTY
                    && board[A1] == PIECE_ROOK && pieceColor[A1] == WHITE
                    && !IsSquareAttacked(E1, BLACK) && !IsSquareAttacked(E1 - 1, BLACK))
                    moves.Add(MakeMove(E1, E1 - 2, FLAG_CASTLE));
            }
            else if (side == BLACK && from == E8)
            {
                // Kingside
                if ((castlingRights & 4) != 0 && board[E8 + 1] == EMPTY && board[E8 + 2] == EMPTY
                    && board[H8] == PIECE_ROOK && pieceColor[H8] == BLACK
                    && !IsSquareAttacked(E8, WHITE) && !IsSquareAttacked(E8 + 1, WHITE))
                    moves.Add(MakeMove(E8, E8 + 2, FLAG_CASTLE));
                // Queenside
                if ((castlingRights & 8) != 0 && board[E8 - 1] == EMPTY && board[E8 - 2] == EMPTY && board[E8 - 3] == EMPTY
                    && board[A8] == PIECE_ROOK && pieceColor[A8] == BLACK
                    && !IsSquareAttacked(E8, WHITE) && !IsSquareAttacked(E8 - 1, WHITE))
                    moves.Add(MakeMove(E8, E8 - 2, FLAG_CASTLE));
            }
        }

        private static Move MakeMove(int from, int to, byte flags, int promo = 0)
        {
            return new Move
            {
                fromSquare = (byte)from,
                toSquare = (byte)to,
                moveFlags = flags,
                promotionPiece = (byte)promo
            };
        }

        // ================================================================
        // MAKE / UNMAKE MOVE
        // ================================================================

        private void MakeMove(Move move)
        {
            int from = move.fromSquare;
            int to = move.toSquare;
            int piece = board[from];
            int captured = board[to];

            // Save state
            history.Push(new HistoryMove
            {
                move = move,
                capturedPiece = captured,
                capturedColor = captured != EMPTY ? pieceColor[to] : NO_COLOR,
                castlingRights = castlingRights,
                enPassantSq = enPassantSq,
                halfMoveClock = halfMoveClock
            });

            // Move the piece
            board[to] = piece;
            pieceColor[to] = side;
            board[from] = EMPTY;
            pieceColor[from] = NO_COLOR;

            // En passant capture
            if ((move.moveFlags & FLAG_EN_PASSANT) != 0)
            {
                int epCaptured = side == WHITE ? to + 16 : to - 16;  // White captures upward, so EP pawn is below
                history.Peek().epCapturedSquare = epCaptured;
                history.Peek().epCapturedPiece = board[epCaptured];
                history.Peek().epCapturedColor = pieceColor[epCaptured];
                board[epCaptured] = EMPTY;
                pieceColor[epCaptured] = NO_COLOR;
            }

            // Set en passant square
            enPassantSq = (move.moveFlags & FLAG_DOUBLE_PAWN) != 0
                ? (side == WHITE ? from - 16 : from + 16)  // White double push: EP square is one step ahead
                : -1;

            // Promotion
            if (move.promotionPiece != 0)
                board[to] = move.promotionPiece;

            // Castling - move the rook
            if ((move.moveFlags & FLAG_CASTLE) != 0)
            {
                if (to == E1 + 2) { board[E1 + 1] = board[H1]; pieceColor[E1 + 1] = side; board[H1] = EMPTY; pieceColor[H1] = NO_COLOR; }
                else if (to == E1 - 2) { board[E1 - 1] = board[A1]; pieceColor[E1 - 1] = side; board[A1] = EMPTY; pieceColor[A1] = NO_COLOR; }
                else if (to == E8 + 2) { board[E8 + 1] = board[H8]; pieceColor[E8 + 1] = side; board[H8] = EMPTY; pieceColor[H8] = NO_COLOR; }
                else if (to == E8 - 2) { board[E8 - 1] = board[A8]; pieceColor[E8 - 1] = side; board[A8] = EMPTY; pieceColor[A8] = NO_COLOR; }
            }

            // Update castling rights
            if (piece == PIECE_KING)
            {
                if (side == WHITE) castlingRights &= ~3;  // Clear KQ
                else castlingRights &= ~12;  // Clear kq
            }
            // If rook moved from original square
            if (from == A1) castlingRights &= ~2;
            if (from == H1) castlingRights &= ~1;
            if (from == A8) castlingRights &= ~8;
            if (from == H8) castlingRights &= ~4;
            // If rook captured on original square
            if (to == A1) castlingRights &= ~2;
            if (to == H1) castlingRights &= ~1;
            if (to == A8) castlingRights &= ~8;
            if (to == H8) castlingRights &= ~4;

            // Update halfmove clock
            if (piece == PIECE_PAWN || captured != EMPTY)
                halfMoveClock = 0;
            else
                halfMoveClock++;

            // Swap side
            side = side == WHITE ? BLACK : WHITE;
            ply++;
        }

        private void UnmakeMove(Move move)
        {
            side = side == WHITE ? BLACK : WHITE;
            ply--;

            HistoryMove hist = history.Pop();
            int from = move.fromSquare;
            int to = move.toSquare;

            // Move piece back
            int piece = board[to];
            if (move.promotionPiece != 0)
                piece = PIECE_PAWN;  // Undo promotion
            board[from] = piece;
            pieceColor[from] = side;
            board[to] = EMPTY;
            pieceColor[to] = NO_COLOR;

            // Restore captured piece
            if (hist.capturedPiece != EMPTY)
            {
                board[to] = hist.capturedPiece;
                pieceColor[to] = hist.capturedColor;
            }

            // Restore en passant capture
            if ((move.moveFlags & FLAG_EN_PASSANT) != 0 && hist.epCapturedSquare >= 0)
            {
                board[hist.epCapturedSquare] = hist.epCapturedPiece;
                pieceColor[hist.epCapturedSquare] = hist.epCapturedColor;
            }

            // Undo castling - move rook back
            if ((move.moveFlags & FLAG_CASTLE) != 0)
            {
                if (to == E1 + 2) { board[H1] = board[E1 + 1]; pieceColor[H1] = side; board[E1 + 1] = EMPTY; pieceColor[E1 + 1] = NO_COLOR; }
                else if (to == E1 - 2) { board[A1] = board[E1 - 1]; pieceColor[A1] = side; board[E1 - 1] = EMPTY; pieceColor[E1 - 1] = NO_COLOR; }
                else if (to == E8 + 2) { board[H8] = board[E8 + 1]; pieceColor[H8] = side; board[E8 + 1] = EMPTY; pieceColor[E8 + 1] = NO_COLOR; }
                else if (to == E8 - 2) { board[A8] = board[E8 - 1]; pieceColor[A8] = side; board[E8 - 1] = EMPTY; pieceColor[E8 - 1] = NO_COLOR; }
            }

            // Restore state
            castlingRights = hist.castlingRights;
            enPassantSq = hist.enPassantSq;
            halfMoveClock = hist.halfMoveClock;
        }

        // ================================================================
        // ATTACK DETECTION
        // ================================================================

        private bool IsSquareAttacked(int sq, int byColor)
        {
            // Pawn attacks
            int[] pawnDirs = byColor == WHITE ? new[] { 15, 17 } : new[] { -15, -17 };  // If white attacks, pawn is below the target
            foreach (int d in pawnDirs)
            {
                int from = sq + d;
                if (OnBoard(from) && board[from] == PIECE_PAWN && pieceColor[from] == byColor)
                    return true;
            }

            // Knight attacks
            foreach (int d in knightOffsets)
            {
                int from = sq + d;
                if (OnBoard(from) && board[from] == PIECE_KNIGHT && pieceColor[from] == byColor)
                    return true;
            }

            // King attacks
            foreach (int d in kingOffsets)
            {
                int from = sq + d;
                if (OnBoard(from) && board[from] == PIECE_KING && pieceColor[from] == byColor)
                    return true;
            }

            // Bishop/Queen diagonal
            foreach (int d in bishopDirs)
            {
                int from = sq + d;
                while (OnBoard(from))
                {
                    if (board[from] != EMPTY)
                    {
                        if (pieceColor[from] == byColor && (board[from] == PIECE_BISHOP || board[from] == PIECE_QUEEN))
                            return true;
                        break;
                    }
                    from += d;
                }
            }

            // Rook/Queen orthogonal
            foreach (int d in rookDirs)
            {
                int from = sq + d;
                while (OnBoard(from))
                {
                    if (board[from] != EMPTY)
                    {
                        if (pieceColor[from] == byColor && (board[from] == PIECE_ROOK || board[from] == PIECE_QUEEN))
                            return true;
                        break;
                    }
                    from += d;
                }
            }

            return false;
        }

        private bool IsInCheck(int color)
        {
            // Find king
            int kingSq = -1;
            for (int sq = 0; sq < 128; sq++)
            {
                if ((sq & 0x88) != 0) continue;
                if (board[sq] == PIECE_KING && pieceColor[sq] == color)
                {
                    kingSq = sq;
                    break;
                }
            }
            if (kingSq < 0) return false;
            return IsSquareAttacked(kingSq, color == WHITE ? BLACK : WHITE);
        }

        // ================================================================
        // EVALUATION
        // ================================================================

        private int Evaluate()
        {
            int score = 0;
            int whiteMaterial = 0, blackMaterial = 0;


            for (int sq = 0; sq < 128; sq++)
            {
                if ((sq & 0x88) != 0) continue;
                if (board[sq] == EMPTY) continue;

                int piece = board[sq];
                int color = pieceColor[sq];
                int value = pieceValues[piece];

                if (color == WHITE) whiteMaterial += value;
                else blackMaterial += value;

                // PST (from white's perspective; flip for black)
                int pstSq = color == WHITE ? sq : MirrorSquare(sq);
                int pstScore = piece switch
                {
                    PIECE_PAWN => pawnPST[pstSq],
                    PIECE_KNIGHT => knightPST[pstSq],
                    PIECE_BISHOP => bishopPST[pstSq],
                    PIECE_KING => IsEndgame() ? kingEndgamePST[pstSq] : kingPST[pstSq],
                    _ => 0
                };

                score += color == WHITE ? (value + pstScore) : -(value + pstScore);
            }

            // Return from side to move's perspective
            return side == WHITE ? score : -score;
        }

        private static int MirrorSquare(int sq)
        {
            int rank = sq / 16;
            int file = sq % 16;
            return (7 - rank) * 16 + file;
        }

        private bool IsEndgame()
        {
            int totalMaterial = 0;
            for (int sq = 0; sq < 128; sq++)
            {
                if ((sq & 0x88) != 0) continue;
                if (board[sq] == EMPTY || board[sq] == PIECE_KING) continue;
                totalMaterial += pieceValues[board[sq]];
            }
            return totalMaterial < 2600;
        }

        // ================================================================
        // ALPHA-BETA SEARCH
        // ================================================================

        private int AlphaBeta(int depth, int alpha, int beta, bool nullMove)
        {
            if (depth == 0)
                return Quiescence(alpha, beta);

            var moves = GenerateLegalMoves();

            if (moves.Count == 0)
            {
                // Checkmate or stalemate
                if (IsInCheck(side))
                    return -MATE_SCORE + ply;  // Prefer later mates
                return 0;  // Stalemate
            }

            // Move ordering: captures first (MVV-LVA)
            moves.Sort((a, b) => ScoreMove(b).CompareTo(ScoreMove(a)));

            int bestScore = int.MinValue + 1;

            bool isRoot = (ply == 0);
            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                MakeMove(move);
                int score;
                if (isRoot)
                {
                    // At root, search with full window to get exact scores
                    score = -AlphaBeta(depth - 1, int.MinValue + 1, int.MaxValue, false);
                }
                else
                {
                    score = -AlphaBeta(depth - 1, -beta, -alpha, false);
                }
                UnmakeMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    if (ply == 0)
                        bestMove = move;
                }

                if (score > alpha)
                    alpha = score;

                if (alpha >= beta && ply > 0)
                    break;  // Beta cutoff (only for non-root)
            }

            return bestScore;
        }

        private int Quiescence(int alpha, int beta)
        {
            int standPat = Evaluate();
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;

            // Generate only captures
            var captures = new List<Move>();
            GeneratePseudoMoves(captures);
            captures.RemoveAll(m => (m.moveFlags & FLAG_CAPTURE) == 0 && (m.moveFlags & FLAG_PROMOTION) == 0);

            // Order by MVV-LVA
            captures.Sort((a, b) => ScoreMove(b).CompareTo(ScoreMove(a)));

            foreach (var move in captures)
            {
                MakeMove(move);
                if (IsInCheck(side == WHITE ? BLACK : WHITE))
                {
                    UnmakeMove(move);
                    continue;
                }
                int score = -Quiescence(-beta, -alpha);
                UnmakeMove(move);

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }

        private int ScoreMove(Move move)
        {
            int score = 0;
            if ((move.moveFlags & FLAG_CAPTURE) != 0)
            {
                int victim = board[move.toSquare];
                int attacker = board[move.fromSquare];
                score += pieceValues[victim] * 10 - pieceValues[attacker];
            }
            if ((move.moveFlags & FLAG_PROMOTION) != 0)
                score += pieceValues[move.promotionPiece];
            return score;
        }

        // ================================================================
        // MOVE NOTATION
        // ================================================================

        private static string MoveToCAN(Move move)
        {
            int from = move.fromSquare;
            int to = move.toSquare;
            int fromFile = from % 16;
            int fromRank = 8 - (from / 16);
            int toFile = to % 16;
            int toRank = 8 - (to / 16);

            string result = $"{(char)('a' + fromFile)}{fromRank}{(char)('a' + toFile)}{toRank}";
            if (move.promotionPiece != 0)
            {
                result += move.promotionPiece switch
                {
                    PIECE_QUEEN => 'q',
                    PIECE_ROOK => 'r',
                    PIECE_BISHOP => 'b',
                    PIECE_KNIGHT => 'n',
                    _ => '?'
                };
            }
            return result;
        }

        // ================================================================
        // DEBUG
        // ================================================================

        public void DebugBoard()
        {
            Console.WriteLine("  +---+---+---+---+---+---+---+---+");
            for (int rank = 0; rank < 8; rank++)
            {
                Console.Write((8 - rank) + " |");
                for (int file = 0; file < 8; file++)
                {
                    int sq = rank * 16 + file;
                    int piece = board[sq];
                    int color = pieceColor[sq];
                    char c = piece switch
                    {
                        PIECE_PAWN => 'p',
                        PIECE_KNIGHT => 'n',
                        PIECE_BISHOP => 'b',
                        PIECE_ROOK => 'r',
                        PIECE_QUEEN => 'q',
                        PIECE_KING => 'k',
                        _ => '.'
                    };
                    if (color == WHITE) c = char.ToUpper(c);
                    Console.Write($" {c} |");
                }
                Console.WriteLine();
                Console.WriteLine("  +---+---+---+---+---+---+---+---+");
            }
            Console.WriteLine("    a   b   c   d   e   f   g   h");
            Console.WriteLine($"  Side: {(side == WHITE ? "White" : "Black")}, Castling: 0x{castlingRights:X}, EP: {enPassantSq}");
        }

        public int DebugCountMoves()
        {
            var moves = GenerateLegalMoves();
            return moves.Count;
        }
    }

    // Extended HistoryMove with EP capture info
    internal class HistoryMove
    {
        public Move move;
        public int capturedPiece;
        public int capturedColor;
        public int castlingRights;
        public int enPassantSq;
        public int halfMoveClock;
        public int epCapturedSquare;
        public int epCapturedPiece;
        public int epCapturedColor;
    }
}
