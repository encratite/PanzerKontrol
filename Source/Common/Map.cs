using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	class PositionComparer : IEqualityComparer<Position>
	{
		public bool Equals(Position a, Position b)
		{
			return a.X == b.X && a.Y == b.Y;
		}

		public int GetHashCode(Position position)
		{
			return (position.X << 16) | position.Y;
		}
	}

	public enum TerrainType
	{
		Clear,
		Forest,
		Mountain,
		Swamp,
		Hill,
	}

	public enum PlayerIdentifier
	{
		Player1,
		Player2,
	}

	public class Hex
	{
		public Position Position;
		public TerrainType Terrain;
		public PlayerIdentifier? Deployment;

		[XmlIgnore]
		public Unit Unit;

		public Hex()
		{
			Unit = null;
		}
	}

	public class Map
	{
		public string Name;
		public List<Hex> Hexes;

		[XmlIgnore]
		Dictionary<Position, Hex> PositionMap;

		public void Initialise()
		{
			PositionMap = new Dictionary<Position, Hex>(new PositionComparer());
			foreach (var hex in Hexes)
				PositionMap[hex.Position] = hex;
		}

		public Hex GetHex(Position position)
		{
			Hex output = null;
			PositionMap.TryGetValue(position, out output);
			return output;
		}

		public bool IsDeploymentZone(PlayerIdentifier player, Position position)
		{
			Hex hex = GetHex(position);
			return hex.Deployment != null && hex.Deployment.Value == player;
		}
	}
}
