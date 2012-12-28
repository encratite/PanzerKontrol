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
		static Dictionary<TerrainType, int> TerrainMovementMap;

		public Position Position;
		public TerrainType Terrain;
		public PlayerIdentifier? Deployment;

		[XmlIgnore]
		public Unit Unit;

		public Hex()
		{
			Unit = null;
		}

		static void InitialiseTerrainMovementMap()
		{
			if (TerrainMovementMap == null)
			{
				TerrainMovementMap = new Dictionary<TerrainType, int>();
				TerrainMovementMap[TerrainType.Clear] = 1;
				TerrainMovementMap[TerrainType.Forest] = 2;
				TerrainMovementMap[TerrainType.Mountain] = 3;
				TerrainMovementMap[TerrainType.Swamp] = 2;
				TerrainMovementMap[TerrainType.Hill] = 2;
			}
		}

		public int GetTerrainMovementPoints()
		{
			InitialiseTerrainMovementMap();
			return TerrainMovementMap[Terrain];
		}
	}

	public class Map
	{
		static Position[] HexPositions =
		{
			// Northwest
			new Position(-1, 0),
			// North
			new Position(0, -1),
			// Northeast
			new Position(1, 0),
			// Southwest
			new Position(-1, 1),
			// South
			new Position(0, 1),
			// Southeast
			new Position(1, 1),
		};

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

		// This calculates a map of positions a unit can move to (keys) and how movement points are left after entering a hex (values)
		public Dictionary<Position, int> CreateMovementMap(Unit unit)
		{
			Dictionary<Position, int> map = new Dictionary<Position, int>(new PositionComparer());
			CreateMovementMap(unit.Hex.Position, unit.MovementPoints, ref map);
			return map;
		}

		void CreateMovementMap(Position currentPosition, int movementPoints, ref Dictionary<Position, int> map)
		{
			List<Hex> neighbours = new List<Hex>();
			foreach (var offset in HexPositions)
			{
				Position neighbourPosition = currentPosition + offset;
				Hex neighbour = GetHex(neighbourPosition);
				if (neighbour == null)
				{
					// This hex is not part of the map, skip it
					continue;
				}
				if (neighbour.Unit != null)
				{
					// This hex is already occupied by another unit, skip it
					continue;
				}
				int newMovementPoints = movementPoints - neighbour.GetTerrainMovementPoints();
				if (newMovementPoints < 0)
				{
					// The unit doesn't have enough movement points left to enter this hex
					continue;
				}
				int previousMovementPoints;
				if (map.TryGetValue(neighbourPosition, out previousMovementPoints))
				{
					// This neighbouring hex was already analysed by a previous recursive call to this function, check if we can even improve on what it calculated
					if (previousMovementPoints <= newMovementPoints)
					{
						// The solution is inferior or just as good, skip it
						continue;
					}
				}
				// Create or update the entry in the movement map
				map[neighbourPosition] = newMovementPoints;
				CreateMovementMap(neighbourPosition, newMovementPoints, ref map);
			}
		}
	}
}
