using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	public class Map
	{
		static Position[] HexOffsets =
		{
			// Northwest
			new Position(0, -1),
			// North
			new Position(-1, 0),
			// Northeast
			new Position(-1, 1),
			// Southwest
			new Position(1, -1),
			// South
			new Position(1, 0),
			// Southeast
			new Position(0, 1),
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

		public bool IsInInitialDeploymentZone(PlayerIdentifier player, Position position)
		{
			Hex hex = GetHex(position);
			return hex.InitialDeploymentZone != null && hex.InitialDeploymentZone.Value == player;
		}

		public List<Hex> GetInitialDeploymentZone(PlayerIdentifier player)
		{
			return Hexes.FindAll((Hex x) => x.InitialDeploymentZone != null && x.InitialDeploymentZone.Value == player);
		}

		// This calculates a map of positions a unit can move to (keys) and how movement points are left after entering a hex (values)
		public Dictionary<Position, int> CreateMovementMap(Unit unit)
		{
			Dictionary<Position, int> map = new Dictionary<Position, int>(new PositionComparer());
			CreateMovementMap(unit.Hex.Position, unit.MovementPoints, unit.Owner, ref map);
			// Remove all the hexes occupied by friendly units since those were only permitted for passing through
			foreach (var position in map.Keys)
			{
				Hex hex = GetHex(position);
				if (hex.Unit != null)
					map.Remove(position);
			}
			return map;
		}

		void CreateMovementMap(Position currentPosition, int movementPoints, PlayerIdentifier owner, ref Dictionary<Position, int> map)
		{
			List<Hex> neighbours = new List<Hex>();
			foreach (var offset in HexOffsets)
			{
				Position neighbourPosition = currentPosition + offset;
				Hex neighbour = GetHex(neighbourPosition);
				if (neighbour == null)
				{
					// This hex is not part of the map, skip it
					continue;
				}
				if (neighbour.Unit != null && neighbour.Unit.Owner != owner)
				{
					// This hex is already occupied by an enemy unit, skip it
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
				CreateMovementMap(neighbourPosition, newMovementPoints, owner, ref map);
			}
		}
	}
}
