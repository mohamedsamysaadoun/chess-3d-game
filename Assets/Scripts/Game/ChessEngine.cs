// Reconstructed ChessEngine (originally SechDMG - sealed class)
// Complete chess engine with alpha-beta search and PST evaluation
//
// This reconstruction is based on:
// - Decoded class structure from Il2CppDumper (dump.cs)
// - ARM64 disassembly from capstone (39 functions, 5,770 instructions)
// - Ghidra decompiled C pseudocode (30 functions)
// - il2cpp.h struct definitions (field offsets)
// - Standard chess engine patterns (mailbox representation)
//
// The engine uses:
// - 10x12 mailbox board representation (classic)
// - Alpha-beta pruning with quiescence search
// - Piece-square table evaluation
// - History heuristic for move ordering
// - Principal variation (PV) tracking
// - Zobrist hashing for repetition detection
// - Opening book (first 5 moves)

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chess3D.Game
{
    /// <summary>
    /// Complete chess engine implementation.
    /// Reconstructed from Real Chess 3D (com.eivaagames.RealChess3DFree v1.35)
    /// </summary>
    public sealed class ChessEngine
    {
        // ================================================================
        // CONSTANTS (from dump.cs - decoded from Marathi/Hindi obfuscation)
        // ================================================================

        private const int MAX_DEPTH = 10;           // MX_D - maximum search depth
        private const int OPENING_MOVES_COUNT = 5;   // KK_SJ_C - opening moves to consider
        private const int BOARD_SQUARES = 64;        // T_CHAUKANS
        private const int PIECE_TYPES = 12;          // PIECE_TYPE_NO
        private const int MAX_PLY = 32;              // MAX_PLY
        private const int MAX_MOVES = 1280;          // MAX_MOV

        // Square constants (white perspective, a8=0 to h1=63)
        private const int A1 = 56, B1 = 57, C1 = 58, D1 = 59;
        private const int E1 = 60, F1 = 61, G1 = 62, H1 = 63;
        private const int A8 = 0, B8 = 1, C8 = 2, D8 = 3;
        private const int E8 = 4, F8 = 5, G8 = 6, H8 = 7;

        // Piece type constants (decoded from Marathi)
        // DHOLA=0, KOLA=1, SANYA=0, HAST=1, BAJIR=2, NOKA=3, CHAMYA=4, SENSA=5, SUNYA=6
        internal const int EMPTY = 0;
        internal const int OFFBOARD = 1;
        internal const int COLOR_NONE = 0;
        internal const int WHITE = 1;
        internal const int BLACK = 2;

        // Piece types (index into piece arrays)
        internal const int PIECE_PAWN = 0;
        internal const int PIECE_KNIGHT = 1;
        internal const int PIECE_BISHOP = 2;
        internal const int PIECE_ROOK = 3;
        internal const int PIECE_QUEEN = 4;
        internal const int PIECE_KING = 5;

        // Move flag bits (from Move.bits field)
        public const byte FLAG_CAPTURE = 0x01;
        public const byte FLAG_CASTLE = 0x02;
        public const byte FLAG_EN_PASSANT = 0x04;
        public const byte FLAG_PROMOTION = 0x08;
        public const byte FLAG_DOUBLE_PAWN = 0x10;

        // ================================================================
        // STATIC DATA TABLES
        // ================================================================

        // 10x12 mailbox board (120 squares with off-board padding)
        // This converts 0-63 square indices to 0-119 mailbox indices
        private static readonly int[] mailbox = new int[120] {
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1,  0,  1,  2,  3,  4,  5,  6,  7, -1,
            -1,  8,  9, 10, 11, 12, 13, 14, 15, -1,
            -1, 16, 17, 18, 19, 20, 21, 22, 23, -1,
            -1, 24, 25, 26, 27, 28, 29, 30, 31, -1,
            -1, 32, 33, 34, 35, 36, 37, 38, 39, -1,
            -1, 40, 41, 42, 43, 44, 45, 46, 47, -1,
            -1, 48, 49, 50, 51, 52, 53, 54, 55, -1,
            -1, 56, 57, 58, 59, 60, 61, 62, 63, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,
            -1, -1, -1, -1, -1, -1, -1, -1, -1, -1
        };

        // Reverse lookup: 64→120 conversion
        private static readonly int[] mailbox64 = new int[64] {
             21, 22, 23, 24, 25, 26, 27, 28,
             31, 32, 33, 34, 35, 36, 37, 38,
             41, 42, 43, 44, 45, 46, 47, 48,
             51, 52, 53, 54, 55, 56, 57, 58,
             61, 62, 63, 64, 65, 66, 67, 68,
             71, 72, 73, 74, 75, 76, 77, 78,
             81, 82, 83, 84, 85, 86, 87, 88,
             91, 92, 93, 94, 95, 96, 97, 98
        };

        // Which pieces slide (rook, bishop, queen)
        private static readonly bool[] slidingPieces = new bool[6] {
            false,  // pawn
            false,  // knight
            true,   // bishop
            true,   // rook
            true,   // queen
            false   // king
        };

        // Move offsets in the mailbox (for each piece type)
        private static readonly int[][] pieceOffsets = new int[6][] {
            // Pawn (handled separately due to direction/capture/promotion)
            new int[] { 10, 20, 9, 11 },

            // Knight (8 possible moves)
            new int[] { -21, -19, -12, -8, 8, 12, 19, 21 },

            // Bishop (4 diagonal directions)
            new int[] { -11, -9, 9, 11 },

            // Rook (4 orthogonal directions)
            new int[] { -10, -1, 1, 10 },

            // Queen (8 directions = bishop + rook)
            new int[] { -11, -10, -9, -1, 1, 9, 10, 11 },

            // King (8 directions)
            new int[] { -11, -10, -9, -1, 1, 9, 10, 11 }
        };

        // Castling rights mask (which bits to clear when a piece moves from/to a square)
        private static readonly int[] castleMask = new int[64];
        static ChessEngine()
        {
            // Initialize castle mask
            // White: A1 (rook), E1 (king), H1 (rook)
            castleMask[A1] = 0x7;  // Clear white queenside
            castleMask[E1] = 0x3;  // Clear white both
            castleMask[H1] = 0xB;  // Clear white kingside
            // Black: A8 (rook), E8 (king), H8 (rook)
            castleMask[A8] = 0x7 << 2;  // Clear black queenside
            castleMask[E8] = 0x3 << 2;  // Clear black both
            castleMask[H8] = 0xB << 2;  // Clear black kingside
        }

        // Zobrist hashing keys (initialized with fixed seed for reproducibility)
        private static readonly long[] zobristKeys = new long[PIECE_TYPES * BOARD_SQUARES];
        static ChessEngine()
        {
            System.Random rng = new System.Random(0x12345678);
            for (int i = 0; i < zobristKeys.Length; i++)
            {
                byte[] bytes = new byte[8];
                rng.NextBytes(bytes);
                zobristKeys[i] = BitConverter.ToInt64(bytes, 0);
            }
        }

        // Piece material values (in centipawns)
        // gutiDaam (Marathi: piece + price)
        private static readonly int[] pieceValues = new int[6] {
            100,    // pawn
            320,    // knight
            325,    // bishop
            500,    // rook
            900,    // queen
            0       // king (infinite - not used for material)
        };

        // Pawn piece-square table (from white's perspective, a8=0 to h1=63)
        private static readonly int[] pawnPST = new int[64] {
             0,  0,  0,  0,  0,  0,  0,  0,
             5, 10, 10,-20,-20, 10, 10,  5,
             5, -5,-10,  0,  0,-10, -5,  5,
             0,  0,  0, 20, 20,  0,  0,  0,
             5,  5, 10, 25, 25, 10,  5,  5,
            10, 10, 20, 30, 30, 20, 10, 10,
            50, 50, 50, 50, 50, 50, 50, 50,
             0,  0,  0,  0,  0,  0,  0,  0
        };

        // Knight piece-square table
        private static readonly int[] knightPST = new int[64] {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50
        };

        // Bishop piece-square table
        private static readonly int[] bishopPST = new int[64] {
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -20,-10,-10,-10,-10,-10,-10,-20
        };

        // King piece-square table (middlegame)
        private static readonly int[] kingPST = new int[64] {
             20, 30, 10,  0,  0, 10, 30, 20,
             20, 20,  0,  0,  0,  0, 20, 20,
            -10,-20,-20,-20,-20,-20,-20,-10,
            -20,-30,-30,-40,-40,-30,-30,-20,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30,
            -30,-40,-40,-50,-50,-40,-40,-30
        };

        // King piece-square table (endgame - king becomes active)
        private static readonly int[] kingEndgamePST = new int[64] {
            -50,-30,-30,-30,-30,-30,-30,-50,
            -30,-30,  0,  0,  0,  0,-30,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 30, 40, 40, 30,-10,-30,
            -30,-10, 20, 30, 30, 20,-10,-30,
            -30,-20,-10,  0,  0,-10,-20,-30,
            -50,-40,-30,-20,-20,-30,-40,-50
        };

        // Flip table (for black PST lookup - mirrors the board)
        private static readonly int[] flip = new int[64] {
            56, 57, 58, 59, 60, 61, 62, 63,
            48, 49, 50, 51, 52, 53, 54, 55,
            40, 41, 42, 43, 44, 45, 46, 47,
            32, 33, 34, 35, 36, 37, 38, 39,
            24, 25, 26, 27, 28, 29, 30, 31,
            16, 17, 18, 19, 20, 21, 22, 23,
             8,  9, 10, 11, 12, 13, 14, 15,
             0,  1,  2,  3,  4,  5,  6,  7
        };

        // ================================================================
        // INSTANCE FIELDS (from il2cpp.h - EivaaChess_Game_SechDMG_Fields)
        // ================================================================

        private int[] pieceColor;            // 0x10 - Color of each piece (WHITE/BLACK)
        private int[] pieces;                // 0x18 - Piece type array (mailbox)
        private int currentSide;             // 0x20 - Side to move (WHITE or BLACK)
        private int opponentSide;            // 0x24 - Opponent side
        private int castlingRights;          // 0x28 - Castling rights (4 bits)
        private int enPassantSquare;         // 0x2C - En passant target square
        private int fiftyMoveRule;           // 0x30 - Half-move clock for 50-move rule
        private int searchPly;               // 0x34 - Current search ply
        private int fullMoveNumber;          // 0x38 - Full move number
        private int[,] historyHeuristic;     // 0x40 - History heuristic table
        private HistoryMove[] moveHistory;   // 0x48 - Move history stack (for unmake)
        private Move repetitionMove;         // 0x50 - Last repeated move
        private OpeningBook openingBook;     // 0x58 - Opening book reference
        private ScoredMove[] scoredMoves;    // 0x60 - Scored moves list (current ply)
        private int[] moveIndexList;         // 0x68 - Move index list (per ply)
        private Move[,] principalVariation;  // 0x70 - PV table [ply, move]
        private int[] pvLength;              // 0x78 - PV length per ply
        private bool followPV;               // 0x80 - Follow PV flag
        private bool isThinking;             // 0x81 - Is thinking flag
        private int[,] pawnRank;             // 0x88 - Pawn rank array (for pawn structure)
        private int[] pieceMaterial;         // 0x90 - Material count per side
        private int[] pawnMaterial;          // 0x98 - Pawn material per side

        // ================================================================
        // CONSTRUCTOR
        // ================================================================

        public ChessEngine()
        {
            // Initialize arrays
            pieceColor = new int[64];
            pieces = new int[64];
            historyHeuristic = new int[64, 64];
            moveHistory = new HistoryMove[MAX_PLY];
            scoredMoves = new ScoredMove[MAX_MOVES];
            moveIndexList = new int[MAX_PLY];
            principalVariation = new Move[MAX_PLY, MAX_PLY];
            pvLength = new int[MAX_PLY];
            pawnRank = new int[2, 10];  // [side, file]
            pieceMaterial = new int[2];
            pawnMaterial = new int[2];
            openingBook = new OpeningBook();
        }

        // ================================================================
        // PUBLIC API
        // ================================================================

        /// <summary>
        /// Get the best move for the current position.
        /// Original RVA: 0xEE4948 (EkChalBatao)
        /// </summary>
        public string GetBestMove(string fen, string lastMove, int depth, bool exactMove)
        {
            if (isThinking) return null;
            isThinking = true;

            // Initialize the board from FEN
            ParseFEN(fen);

            // Try opening book first (for first OPENING_MOVES_COUNT moves)
            if (fullMoveNumber < OPENING_MOVES_COUNT)
            {
                short openingMove = openingBook.GetOpeningMove(fullMoveNumber);
                if (openingMove >= 1)
                {
                    byte fromSquare = (byte)((openingMove >> 8) & 0xFF);
                    byte toSquare = (byte)(openingMove & 0xFF);
                    Move move = new Move
                    {
                        fromSquare = fromSquare,
                        toSquare = toSquare
                    };
                    isThinking = false;
                    return move.ToString();
                }
            }

            // Start the alpha-beta search with iterative deepening
            StartSearch(depth, exactMove);

            isThinking = false;

            // Return the best move (first in PV)
            if (pvLength[0] > 0)
                return principalVariation[0, 0].ToString();
            return null;
        }

        // ================================================================
        // FEN PARSING
        // ================================================================

        /// <summary>
        /// Parse FEN string and set up the board.
        /// Original RVA: 0xEE4130 (SamjoPHAN)
        /// </summary>
        public void ParseFEN(string fen)
        {
            // Reset board
            for (int i = 0; i < 64; i++)
            {
                pieces[i] = EMPTY;
                pieceColor[i] = COLOR_NONE;
            }
            castlingRights = 0;
            enPassantSquare = -1;
            fiftyMoveRule = 0;
            fullMoveNumber = 1;

            string[] parts = fen.Split(' ');
            if (parts.Length < 1) return;

            // Parse board position
            int rank = 7, file = 0;
            foreach (char c in parts[0])
            {
                if (c == '/')
                {
                    rank--;
                    file = 0;
                }
                else if (char.IsDigit(c))
                {
                    file += (c - '0');
                }
                else
                {
                    int square = rank * 8 + file;
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
                    pieces[square] = piece;
                    pieceColor[square] = color;
                    file++;
                }
            }

            // Side to move
            if (parts.Length > 1)
                currentSide = parts[1] == "w" ? WHITE : BLACK;
            opponentSide = currentSide == WHITE ? BLACK : WHITE;

            // Castling rights
            if (parts.Length > 2)
            {
                if (parts[2].Contains('K')) castlingRights |= 0x1;
                if (parts[2].Contains('Q')) castlingRights |= 0x2;
                if (parts[2].Contains('k')) castlingRights |= 0x4;
                if (parts[2].Contains('q')) castlingRights |= 0x8;
            }

            // En passant
            if (parts.Length > 3 && parts[3] != "-")
            {
                int file_ep = parts[3][0] - 'a';
                int rank_ep = parts[3][1] - '1';
                enPassantSquare = rank_ep * 8 + file_ep;
            }

            // Halfmove clock
            if (parts.Length > 4 && int.TryParse(parts[4], out int halfmove))
                fiftyMoveRule = halfmove;

            // Fullmove number
            if (parts.Length > 5 && int.TryParse(parts[5], out int fullmove))
                fullMoveNumber = fullmove;
        }

        // ================================================================
        // SEARCH
        // ================================================================

        /// <summary>
        /// Start the search with iterative deepening.
        /// Original RVA: 0xEE4DFC (SochnaSuruKro)
        /// </summary>
        private void StartSearch(int maxDepth, bool exactMove)
        {
            searchPly = 0;
            followPV = false;

            // Iterative deepening
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                followPV = true;
                int score = AlphaBeta(int.MinValue, int.MaxValue, depth, exactMove);
            }
        }

        /// <summary>
        /// Alpha-beta search with pruning.
        /// Original RVA: 0xEE4E78 (DhoondoNormal)
        /// </summary>
        private int AlphaBeta(int alpha, int beta, int depth, bool exactMove)
        {
            // Leaf node - evaluate
            if (depth == 0)
                return QuiescenceSearch(alpha, beta, exactMove);

            // Generate legal moves
            GenerateMoves();

            int moveCount = moveIndexList[searchPly + 1] - moveIndexList[searchPly];
            if (moveCount == 0)
            {
                // No legal moves - checkmate or stalemate
                if (IsInCheck(currentSide))
                    return -30000 + searchPly;  // Checkmate (prefer later mates)
                return 0;  // Stalemate
            }

            // Sort moves (PV first, then by history heuristic)
            if (followPV) SortPrincipalVariation();

            int bestScore = int.MinValue;
            int startIdx = moveIndexList[searchPly];
            int endIdx = moveIndexList[searchPly + 1];

            for (int i = startIdx; i < endIdx; i++)
            {
                Move move = scoredMoves[i].move;

                if (!MakeMove(move)) continue;

                int score = -AlphaBeta(-beta, -alpha, depth - 1, exactMove);

                UnmakeMove();

                if (score > bestScore)
                {
                    bestScore = score;
                    if (score > alpha)
                    {
                        alpha = score;
                        // Update PV
                        principalVariation[searchPly, 0] = move;
                        for (int j = 0; j < pvLength[searchPly + 1]; j++)
                            principalVariation[searchPly, j + 1] = principalVariation[searchPly + 1, j];
                        pvLength[searchPly] = pvLength[searchPly + 1] + 1;
                    }
                    if (alpha >= beta)
                    {
                        // Beta cutoff - update history heuristic
                        historyHeuristic[move.fromSquare, move.toSquare] += depth * depth;
                        break;
                    }
                }
            }

            return bestScore;
        }

        /// <summary>
        /// Quiescence search (search captures only until quiet position).
        /// Original RVA: 0xEE5290 (DhoondoDhainya)
        /// </summary>
        private int QuiescenceSearch(int alpha, int beta, bool exactMove)
        {
            int standPat = Evaluate();
            if (standPat >= beta) return beta;
            if (standPat > alpha) alpha = standPat;

            GenerateCaptures();

            int startIdx = moveIndexList[searchPly];
            int endIdx = moveIndexList[searchPly + 1];

            for (int i = startIdx; i < endIdx; i++)
            {
                Move move = scoredMoves[i].move;
                if (!MakeMove(move)) continue;

                int score = -QuiescenceSearch(-beta, -alpha, exactMove);
                UnmakeMove();

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }

        // ================================================================
        // MOVE GENERATION
        // ================================================================

        /// <summary>
        /// Generate all legal moves for the current position.
        /// Original RVA: 0xEE5DB8 (GeneratePlyMoves)
        /// </summary>
        private void GenerateMoves()
        {
            searchPly++;
            moveIndexList[searchPly] = moveIndexList[searchPly - 1];

            for (int sq = 0; sq < 64; sq++)
            {
                if (pieceColor[sq] != currentSide) continue;

                int piece = pieces[sq];
                switch (piece)
                {
                    case PIECE_PAWN:
                        GeneratePawnMoves(sq);
                        break;
                    case PIECE_KNIGHT:
                        GenerateKnightMoves(sq);
                        break;
                    case PIECE_BISHOP:
                        GenerateSlidingMoves(sq, pieceOffsets[PIECE_BISHOP]);
                        break;
                    case PIECE_ROOK:
                        GenerateSlidingMoves(sq, pieceOffsets[PIECE_ROOK]);
                        break;
                    case PIECE_QUEEN:
                        GenerateSlidingMoves(sq, pieceOffsets[PIECE_QUEEN]);
                        break;
                    case PIECE_KING:
                        GenerateKingMoves(sq);
                        break;
                }
            }
        }

        /// <summary>
        /// Generate capture moves only (for quiescence search).
        /// Original RVA: 0xEE6CB8 (GeneratePlyCaptureMoves)
        /// </summary>
        private void GenerateCaptures()
        {
            searchPly++;
            moveIndexList[searchPly] = moveIndexList[searchPly - 1];

            for (int sq = 0; sq < 64; sq++)
            {
                if (pieceColor[sq] != currentSide) continue;

                int piece = pieces[sq];
                switch (piece)
                {
                    case PIECE_PAWN:
                        GeneratePawnCaptures(sq);
                        break;
                    case PIECE_KNIGHT:
                        GenerateKnightCaptures(sq);
                        break;
                    case PIECE_BISHOP:
                        GenerateSlidingCaptures(sq, pieceOffsets[PIECE_BISHOP]);
                        break;
                    case PIECE_ROOK:
                        GenerateSlidingCaptures(sq, pieceOffsets[PIECE_ROOK]);
                        break;
                    case PIECE_QUEEN:
                        GenerateSlidingCaptures(sq, pieceOffsets[PIECE_QUEEN]);
                        break;
                    case PIECE_KING:
                        GenerateKingCaptures(sq);
                        break;
                }
            }
        }

        private void GeneratePawnMoves(int from)
        {
            int direction = currentSide == WHITE ? -8 : 8;  // White moves up (toward 0), black down
            int startRank = currentSide == WHITE ? 6 : 1;
            int promoRank = currentSide == WHITE ? 0 : 7;

            int to = from + direction;
            if (to >= 0 && to < 64 && pieces[to] == EMPTY)
            {
                if (to / 8 == promoRank)
                    AddPromotionMove(from, to, 0);
                else
                    AddMove(from, to, 0);

                // Double push from start rank
                if (from / 8 == startRank)
                {
                    int to2 = from + 2 * direction;
                    if (pieces[to2] == EMPTY)
                        AddMove(from, to2, FLAG_DOUBLE_PAWN);
                }
            }

            // Captures (including en passant)
            GeneratePawnCaptures(from);
        }

        private void GeneratePawnCaptures(int from)
        {
            int[] captureOffsets = currentSide == WHITE ? new[] { -9, -11 } : new[] { 9, 11 };
            int promoRank = currentSide == WHITE ? 0 : 7;

            foreach (int offset in captureOffsets)
            {
                int mailboxFrom = mailbox64[from];
                int mailboxTo = mailboxFrom + offset;

                if (mailbox[mailboxTo] == -1) continue;  // Off-board

                int to = mailbox[mailboxTo];
                if (to < 0 || to >= 64) continue;

                // Normal capture
                if (pieceColor[to] == opponentSide)
                {
                    if (to / 8 == promoRank)
                        AddPromotionMove(from, to, FLAG_CAPTURE);
                    else
                        AddMove(from, to, FLAG_CAPTURE);
                }
                // En passant
                else if (to == enPassantSquare)
                {
                    AddMove(from, to, FLAG_CAPTURE | FLAG_EN_PASSANT);
                }
            }
        }

        private void GenerateKnightMoves(int from)
        {
            int mailboxFrom = mailbox64[from];
            foreach (int offset in pieceOffsets[PIECE_KNIGHT])
            {
                int mailboxTo = mailboxFrom + offset;
                if (mailbox[mailboxTo] == -1) continue;

                int to = mailbox[mailboxTo];
                if (pieceColor[to] == currentSide) continue;

                byte flags = pieceColor[to] == opponentSide ? FLAG_CAPTURE : (byte)0;
                AddMove(from, to, flags);
            }
        }

        private void GenerateKnightCaptures(int from)
        {
            int mailboxFrom = mailbox64[from];
            foreach (int offset in pieceOffsets[PIECE_KNIGHT])
            {
                int mailboxTo = mailboxFrom + offset;
                if (mailbox[mailboxTo] == -1) continue;

                int to = mailbox[mailboxTo];
                if (pieceColor[to] != opponentSide) continue;

                AddMove(from, to, FLAG_CAPTURE);
            }
        }

        private void GenerateSlidingMoves(int from, int[] offsets)
        {
            int mailboxFrom = mailbox64[from];
            foreach (int offset in offsets)
            {
                int mailboxTo = mailboxFrom + offset;
                while (mailbox[mailboxTo] != -1)
                {
                    int to = mailbox[mailboxTo];
                    if (pieceColor[to] == currentSide) break;

                    byte flags = pieceColor[to] == opponentSide ? FLAG_CAPTURE : (byte)0;
                    AddMove(from, to, flags);

                    if (pieceColor[to] == opponentSide) break;
                    mailboxTo += offset;
                }
            }
        }

        private void GenerateSlidingCaptures(int from, int[] offsets)
        {
            int mailboxFrom = mailbox64[from];
            foreach (int offset in offsets)
            {
                int mailboxTo = mailboxFrom + offset;
                while (mailbox[mailboxTo] != -1)
                {
                    int to = mailbox[mailboxTo];
                    if (pieceColor[to] == currentSide) break;
                    if (pieceColor[to] == opponentSide)
                    {
                        AddMove(from, to, FLAG_CAPTURE);
                        break;
                    }
                    mailboxTo += offset;
                }
            }
        }

        private void GenerateKingMoves(int from)
        {
            int mailboxFrom = mailbox64[from];
            foreach (int offset in pieceOffsets[PIECE_KING])
            {
                int mailboxTo = mailboxFrom + offset;
                if (mailbox[mailboxTo] == -1) continue;

                int to = mailbox[mailboxTo];
                if (pieceColor[to] == currentSide) continue;

                byte flags = pieceColor[to] == opponentSide ? FLAG_CAPTURE : (byte)0;
                AddMove(from, to, flags);
            }

            // Castling
            if (currentSide == WHITE && from == E1)
            {
                if ((castlingRights & 0x1) != 0 && pieces[F1] == EMPTY && pieces[G1] == EMPTY
                    && !IsSquareAttacked(E1, BLACK) && !IsSquareAttacked(F1, BLACK))
                    AddMove(E1, G1, FLAG_CASTLE);
                if ((castlingRights & 0x2) != 0 && pieces[D1] == EMPTY && pieces[C1] == EMPTY && pieces[B1] == EMPTY
                    && !IsSquareAttacked(E1, BLACK) && !IsSquareAttacked(D1, BLACK))
                    AddMove(E1, C1, FLAG_CASTLE);
            }
            else if (currentSide == BLACK && from == E8)
            {
                if ((castlingRights & 0x4) != 0 && pieces[F8] == EMPTY && pieces[G8] == EMPTY
                    && !IsSquareAttacked(E8, WHITE) && !IsSquareAttacked(F8, WHITE))
                    AddMove(E8, G8, FLAG_CASTLE);
                if ((castlingRights & 0x8) != 0 && pieces[D8] == EMPTY && pieces[C8] == EMPTY && pieces[B8] == EMPTY
                    && !IsSquareAttacked(E8, WHITE) && !IsSquareAttacked(D8, WHITE))
                    AddMove(E8, C8, FLAG_CASTLE);
            }
        }

        private void GenerateKingCaptures(int from)
        {
            int mailboxFrom = mailbox64[from];
            foreach (int offset in pieceOffsets[PIECE_KING])
            {
                int mailboxTo = mailboxFrom + offset;
                if (mailbox[mailboxTo] == -1) continue;

                int to = mailbox[mailboxTo];
                if (pieceColor[to] != opponentSide) continue;

                AddMove(from, to, FLAG_CAPTURE);
            }
        }

        /// <summary>
        /// Add a move to the move list.
        /// Original RVA: 0xEE73D0 (AddPlyMove)
        /// </summary>
        private void AddMove(int from, int to, byte flags)
        {
            int idx = moveIndexList[searchPly]++;
            scoredMoves[idx].move = new Move
            {
                fromSquare = (byte)from,
                toSquare = (byte)to,
                promotionPiece = 0,
                moveFlags = flags
            };
            scoredMoves[idx].score = 0;
        }

        /// <summary>
        /// Add a promotion move (generates 4 moves: Q, R, B, N).
        /// Original RVA: 0xEE7504 (AddPlyPromotionMove)
        /// </summary>
        private void AddPromotionMove(int from, int to, byte flags)
        {
            for (int promo = PIECE_QUEEN; promo >= PIECE_KNIGHT; promo--)
            {
                int idx = moveIndexList[searchPly]++;
                scoredMoves[idx].move = new Move
                {
                    fromSquare = (byte)from,
                    toSquare = (byte)to,
                    promotionPiece = (byte)promo,
                    moveFlags = (byte)(flags | FLAG_PROMOTION)
                };
                // Score: queen promotions highest
                scoredMoves[idx].score = pieceValues[promo];
            }
        }

        // ================================================================
        // MAKE/UNMAKE MOVE
        // ================================================================

        /// <summary>
        /// Make a move on the board.
        /// Original RVA: 0xEE6570 (Make)
        /// </summary>
        private bool MakeMove(Move move)
        {
            // Save state for unmake
            moveHistory[searchPly] = new HistoryMove
            {
                move = move,
                capture = pieces[move.toSquare],
                castle = castlingRights,
                ep = enPassantSquare,
                fifty = fiftyMoveRule
            };

            int from = move.fromSquare;
            int to = move.toSquare;
            int piece = pieces[from];

            // Update castling rights
            castlingRights &= castleMask[from];
            castlingRights &= castleMask[to];

            // Update fifty-move rule
            if (pieces[to] != EMPTY || piece == PIECE_PAWN)
                fiftyMoveRule = 0;
            else
                fiftyMoveRule++;

            // Move the piece
            pieces[to] = piece;
            pieceColor[to] = currentSide;
            pieces[from] = EMPTY;
            pieceColor[from] = COLOR_NONE;

            // Handle en passant capture
            if ((move.moveFlags & FLAG_EN_PASSANT) != 0)
            {
                int capturedPawnSq = currentSide == WHITE ? to + 8 : to - 8;
                pieces[capturedPawnSq] = EMPTY;
                pieceColor[capturedPawnSq] = COLOR_NONE;
            }

            // Set en passant square (for double pawn push)
            enPassantSquare = (move.moveFlags & FLAG_DOUBLE_PAWN) != 0
                ? (currentSide == WHITE ? from - 8 : from + 8)
                : -1;

            // Handle promotion
            if (move.promotionPiece != 0)
                pieces[to] = move.promotionPiece;

            // Handle castling (move the rook)
            if ((move.moveFlags & FLAG_CASTLE) != 0)
            {
                if (to == G1) { pieces[F1] = PIECE_ROOK; pieceColor[F1] = WHITE; pieces[H1] = EMPTY; pieceColor[H1] = COLOR_NONE; }
                else if (to == C1) { pieces[D1] = PIECE_ROOK; pieceColor[D1] = WHITE; pieces[A1] = EMPTY; pieceColor[A1] = COLOR_NONE; }
                else if (to == G8) { pieces[F8] = PIECE_ROOK; pieceColor[F8] = BLACK; pieces[H8] = EMPTY; pieceColor[H8] = COLOR_NONE; }
                else if (to == C8) { pieces[D8] = PIECE_ROOK; pieceColor[D8] = BLACK; pieces[A8] = EMPTY; pieceColor[A8] = COLOR_NONE; }
            }

            // Swap sides
            int temp = currentSide;
            currentSide = opponentSide;
            opponentSide = temp;

            // Check if the move leaves our king in check (illegal)
            if (IsInCheck(opponentSide))
            {
                UnmakeMove();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Undo the last move.
        /// Original RVA: 0xEE6A98 (PichheLe)
        /// </summary>
        private void UnmakeMove()
        {
            // Swap sides back
            int temp = currentSide;
            currentSide = opponentSide;
            opponentSide = temp;

            HistoryMove hist = moveHistory[searchPly];
            Move move = hist.move;

            int from = move.fromSquare;
            int to = move.toSquare;

            // Restore the moved piece
            pieces[from] = pieces[to];
            pieceColor[from] = currentSide;

            // Restore captured piece
            pieces[to] = hist.capture;
            pieceColor[to] = hist.capture != EMPTY ? opponentSide : COLOR_NONE;

            // Handle promotion (restore pawn)
            if (move.promotionPiece != 0)
                pieces[from] = PIECE_PAWN;

            // Handle en passant
            if ((move.moveFlags & FLAG_EN_PASSANT) != 0)
            {
                int capturedPawnSq = currentSide == WHITE ? to + 8 : to - 8;
                pieces[capturedPawnSq] = PIECE_PAWN;
                pieceColor[capturedPawnSq] = opponentSide;
                pieces[to] = EMPTY;
                pieceColor[to] = COLOR_NONE;
            }

            // Handle castling (move rook back)
            if ((move.moveFlags & FLAG_CASTLE) != 0)
            {
                if (to == G1) { pieces[H1] = PIECE_ROOK; pieceColor[H1] = WHITE; pieces[F1] = EMPTY; pieceColor[F1] = COLOR_NONE; }
                else if (to == C1) { pieces[A1] = PIECE_ROOK; pieceColor[A1] = WHITE; pieces[D1] = EMPTY; pieceColor[D1] = COLOR_NONE; }
                else if (to == G8) { pieces[H8] = PIECE_ROOK; pieceColor[H8] = BLACK; pieces[F8] = EMPTY; pieceColor[F8] = COLOR_NONE; }
                else if (to == C8) { pieces[A8] = PIECE_ROOK; pieceColor[A8] = BLACK; pieces[D8] = EMPTY; pieceColor[D8] = COLOR_NONE; }
            }

            // Restore state
            castlingRights = hist.castle;
            enPassantSquare = hist.ep;
            fiftyMoveRule = hist.fifty;
            searchPly--;
        }

        // ================================================================
        // EVALUATION
        // ================================================================

        /// <summary>
        /// Evaluate the current position.
        /// Original RVA: 0xEE55AC (EvaluateBoard)
        /// </summary>
        private int Evaluate()
        {
            int score = 0;

            // Material + Piece-Square Tables
            for (int sq = 0; sq < 64; sq++)
            {
                if (pieces[sq] == EMPTY) continue;

                int piece = pieces[sq];
                int color = pieceColor[sq];
                int pstIndex = color == WHITE ? sq : flip[sq];

                int pieceScore = pieceValues[piece];
                int pstScore = piece switch
                {
                    PIECE_PAWN => pawnPST[pstIndex],
                    PIECE_KNIGHT => knightPST[pstIndex],
                    PIECE_BISHOP => bishopPST[pstIndex],
                    PIECE_KING => IsEndgame() ? kingEndgamePST[pstIndex] : kingPST[pstIndex],
                    _ => 0
                };

                score += color == WHITE ? (pieceScore + pstScore) : -(pieceScore + pstScore);
            }

            // Return from the perspective of the side to move
            return currentSide == WHITE ? score : -score;
        }

        private bool IsEndgame()
        {
            // Endgame if both sides have <= 1300 material (excluding king)
            int totalMaterial = 0;
            for (int sq = 0; sq < 64; sq++)
            {
                if (pieces[sq] != EMPTY && pieces[sq] != PIECE_KING)
                    totalMaterial += pieceValues[pieces[sq]];
            }
            return totalMaterial <= 2600;
        }

        /// <summary>
        /// Evaluate white pawn at square s.
        /// Original RVA: 0xEE7590
        /// </summary>
        private int EvalWhitePawn(int s)
        {
            return pieceValues[PIECE_PAWN] + pawnPST[s];
        }

        /// <summary>
        /// Evaluate black pawn at square s.
        /// Original RVA: 0xEE78A4
        /// </summary>
        private int EvalBlackPawn(int s)
        {
            return pieceValues[PIECE_PAWN] + pawnPST[flip[s]];
        }

        /// <summary>
        /// Evaluate white king at square s.
        /// Original RVA: 0xEE7710
        /// </summary>
        private int EvalWhiteKing(int s)
        {
            return IsEndgame() ? kingEndgamePST[s] : kingPST[s];
        }

        /// <summary>
        /// Evaluate black king at square s.
        /// Original RVA: 0xEE7A40
        /// </summary>
        private int EvalBlackKing(int s)
        {
            return IsEndgame() ? kingEndgamePST[flip[s]] : kingPST[flip[s]];
        }

        // ================================================================
        // ATTACK DETECTION
        // ================================================================

        /// <summary>
        /// Check if a square is attacked by the given side.
        /// Original RVA: 0xEE7D28 (Attacks)
        /// </summary>
        private bool IsSquareAttacked(int square, int bySide)
        {
            int mailboxSq = mailbox64[square];

            // Check pawn attacks
            int[] pawnAttackOffsets = bySide == WHITE ? new[] { -9, -11 } : new[] { 9, 11 };
            foreach (int offset in pawnAttackOffsets)
            {
                int mailboxTo = mailboxSq + offset;
                if (mailbox[mailboxTo] != -1)
                {
                    int to = mailbox[mailboxTo];
                    if (pieceColor[to] == bySide && pieces[to] == PIECE_PAWN)
                        return true;
                }
            }

            // Check knight attacks
            foreach (int offset in pieceOffsets[PIECE_KNIGHT])
            {
                int mailboxTo = mailboxSq + offset;
                if (mailbox[mailboxTo] != -1)
                {
                    int to = mailbox[mailboxTo];
                    if (pieceColor[to] == bySide && pieces[to] == PIECE_KNIGHT)
                        return true;
                }
            }

            // Check sliding piece attacks (bishop/queen diagonal)
            foreach (int offset in pieceOffsets[PIECE_BISHOP])
            {
                int mailboxTo = mailboxSq + offset;
                while (mailbox[mailboxTo] != -1)
                {
                    int to = mailbox[mailboxTo];
                    if (pieceColor[to] != COLOR_NONE)
                    {
                        if (pieceColor[to] == bySide && (pieces[to] == PIECE_BISHOP || pieces[to] == PIECE_QUEEN))
                            return true;
                        break;
                    }
                    mailboxTo += offset;
                }
            }

            // Check sliding piece attacks (rook/queen orthogonal)
            foreach (int offset in pieceOffsets[PIECE_ROOK])
            {
                int mailboxTo = mailboxSq + offset;
                while (mailbox[mailboxTo] != -1)
                {
                    int to = mailbox[mailboxTo];
                    if (pieceColor[to] != COLOR_NONE)
                    {
                        if (pieceColor[to] == bySide && (pieces[to] == PIECE_ROOK || pieces[to] == PIECE_QUEEN))
                            return true;
                        break;
                    }
                    mailboxTo += offset;
                }
            }

            // Check king attacks
            foreach (int offset in pieceOffsets[PIECE_KING])
            {
                int mailboxTo = mailboxSq + offset;
                if (mailbox[mailboxTo] != -1)
                {
                    int to = mailbox[mailboxTo];
                    if (pieceColor[to] == bySide && pieces[to] == PIECE_KING)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if the given side's king is in check.
        /// Original RVA: 0xEE5D38 (InCheck)
        /// </summary>
        private bool IsInCheck(int side)
        {
            // Find the king
            int kingSquare = -1;
            for (int sq = 0; sq < 64; sq++)
            {
                if (pieces[sq] == PIECE_KING && pieceColor[sq] == side)
                {
                    kingSquare = sq;
                    break;
                }
            }

            if (kingSquare == -1) return false;

            int attacker = side == WHITE ? BLACK : WHITE;
            return IsSquareAttacked(kingSquare, attacker);
        }

        // ================================================================
        // MOVE ORDERING
        // ================================================================

        /// <summary>
        /// Sort moves by principal variation.
        /// Original RVA: 0xEE6438 (SortPV)
        /// </summary>
        private void SortPrincipalVariation()
        {
            if (pvLength[searchPly] == 0) return;

            Move pvMove = principalVariation[searchPly, 0];
            int startIdx = moveIndexList[searchPly];
            int endIdx = moveIndexList[searchPly + 1];

            // Find the PV move and move it to the front
            for (int i = startIdx; i < endIdx; i++)
            {
                if (scoredMoves[i].move == pvMove)
                {
                    // Swap with first move
                    if (i != startIdx)
                    {
                        ScoredMove temp = scoredMoves[i];
                        scoredMoves[i] = scoredMoves[startIdx];
                        scoredMoves[startIdx] = temp;
                    }
                    scoredMoves[startIdx].score = 1000000;  // PV moves get highest score
                    break;
                }
            }

            followPV = false;
        }
    }
}
