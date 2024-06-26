using JetBrains.Annotations;
using USharpLibs.Common.IO;

namespace USharpLibs.Engine.Math {
	[PublicAPI]
	public readonly record struct HexPos {
		public static HexPos Identity { get; } = new();

		public static HexPos FlatNorth { get; } = new(0, -1, 1);
		public static HexPos FlatNorthEast { get; } = new(1, -1, 0);
		public static HexPos FlatSouthEast { get; } = new(1, 0, -1);
		public static HexPos FlatSouth { get; } = new(0, 1, -1);
		public static HexPos FlatSouthWest { get; } = new(-1, 1, 0);
		public static HexPos FlatNorthWest { get; } = new(-1, 0, 1);

		public static HexPos PointyEast { get; } = new(1, 0, -1);
		public static HexPos PointyNorthEast { get; } = new(1, -1, 0);
		public static HexPos PointyNorthWest { get; } = new(0, -1, 1);
		public static HexPos PointyWest { get; } = new(-1, 0, 1);
		public static HexPos PointySouthWest { get; } = new(-1, 1, 0);
		public static HexPos PointySouthEast { get; } = new(0, 1, -1);

		public static HexPos[] PointyDirections { get; } = { PointyEast, PointyNorthEast, PointyNorthWest, PointyWest, PointySouthWest, PointySouthEast, };
		public static HexPos[] FlatDirections { get; } = { FlatNorth, FlatNorthEast, FlatSouthEast, FlatSouth, FlatSouthWest, FlatNorthWest, };

		public int Q { get; } = 0;
		public int R { get; } = 0;
		public int S { get; } = 0;

		public HexPos(int q, int r, int s) {
			if (q + r + s != 0) {
				Logger.Warn($"Attempted to create HexPos with invalid coordinates: ({q}, {r}, {s}). Hexagon coordinates when added together must equal 0.");
				return;
			}

			Q = q;
			R = r;
			S = s;
		}

		public HexPos() { }

		public static int Distance(in HexPos start, in HexPos end) => (System.Math.Abs(start.Q - end.Q) + System.Math.Abs(start.R - end.R) + System.Math.Abs(start.S - end.S)) / 2;
		public int Distance(in HexPos pos) => (System.Math.Abs(Q - pos.Q) + System.Math.Abs(R - pos.R) + System.Math.Abs(S - pos.S)) / 2;

		public static HexPos operator +(HexPos hex, HexPos value) => new(hex.Q + value.Q, hex.R + value.R, hex.S + value.S);
		public static HexPos operator +(HexPos hex, int value) => new(hex.Q + value, hex.R + value, hex.S + value);

		public static HexPos operator -(HexPos hex, HexPos value) => new(hex.Q - value.Q, hex.R - value.R, hex.S - value.S);
		public static HexPos operator -(HexPos hex, int value) => new(hex.Q - value, hex.R - value, hex.S - value);

		public static HexPos operator *(HexPos hex, HexPos value) => new(hex.Q * value.Q, hex.R * value.R, hex.S * value.S);
		public static HexPos operator *(HexPos hex, int value) => new(hex.Q * value, hex.R * value, hex.S * value);

		public static HexPos operator /(HexPos hex, HexPos value) => new(hex.Q / value.Q, hex.R / value.R, hex.S / value.S);
		public static HexPos operator /(HexPos hex, int value) => new(hex.Q / value, hex.R / value, hex.S / value);

		public override int GetHashCode() => (Q, R, S).GetHashCode();
		public override string ToString() => $"({Q}, {R}, {S})";
	}
}