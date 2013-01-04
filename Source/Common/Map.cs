using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	public class RiverEdge
	{
		public bool IsBridge;

		public Position Position1;
		public Position Position2;

		public RiverEdge()
		{
		}
	}

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

		public List<RiverEdge> Rivers;

		[XmlIgnore]
		Dictionary<Position, Hex> PositionMap;

		public void Initialise()
		{
			PositionMap = new Dictionary<Position, Hex>(new PositionComparer());
			foreach (var hex in Hexes)
				PositionMap[hex.Position] = hex;
			foreach (var riverEdge in Rivers)
			{
				Hex hex1 = GetHex(riverEdge.Position1);
				Hex hex2 = GetHex(riverEdge.Position2);
				if (hex1 == null || hex2 == null)
					throw new Exception("Encountered invalid river data in a map");
				SetHexRiverData(hex1, hex2, riverEdge);
				SetHexRiverData(hex2, hex1, riverEdge);
			}
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
			CreateMovementMap(unit, unit.Hex, unit.MovementPoints, unit.Owner, ref map);
			// Remove all the hexes occupied by friendly units since those were only permitted for passing through
			foreach (var position in map.Keys)
			{
				Hex hex = GetHex(position);
				if (hex.Unit != null)
					map.Remove(position);
			}
			return map;
		}

		void CreateMovementMap(Unit unit, Hex currentHex, int movementPoints, PlayerIdentifier owner, ref Dictionary<Position, int> map)
		{
			List<Hex> neighbours = new List<Hex>();
			for(int i = 0; i < HexOffsets.Length; i++)
			{
				RiverEdge riverEdge = currentHex.RiverEdges[i];
				Position offset = HexOffsets[i];
				Position neighbourPosition = currentHex.Position + offset;
				Hex neighbourHex = GetHex(neighbourPosition);
				if (neighbourHex == null)
				{
					// This hex is not part of the map, skip it
					continue;
				}
				if (neighbourHex.Unit != null && neighbourHex.Unit.Owner != owner)
				{
					// This hex is already occupied by an enemy unit, skip it
					continue;
				}
				int terrainMovementPoints = neighbourHex.GetTerrainMovementPoints();
				int movementPointsLost;
				if (riverEdge != null && !riverEdge.IsBridge)
				{
					// It's a move across a river without a bridge
					// This is only possible under special circumstances
					// The unit must have its full movement points and it will lose all of them after the crossing
					int maximumMovementPoints = unit.Stats.Movement.Value;
					if (movementPoints < maximumMovementPoints)
					{
						// The unit had already moved so it can't cross the river
						continue;
					}
					if (movementPoints < terrainMovementPoints)
					{
						// This is an extraordinarily rare case but it means that the unit can't cross the river because it couldn't enter the target terrain type, even if the river wasn't there
						continue;
					}
					movementPointsLost = maximumMovementPoints;
				}
				else
				{
					// It's either a regular move without a river or a move across a bridge
					movementPointsLost = terrainMovementPoints;
				}
				int newMovementPoints = movementPoints - movementPointsLost;
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
				CreateMovementMap(unit, neighbourHex, newMovementPoints, owner, ref map);
			}
		}

		public static int GetHexOffsetIndex(Hex hex1, Hex hex2)
		{
			Position difference = hex2.Position - hex2.Position;
			int index = Array.FindIndex(HexOffsets, (Position x) => x.SamePosition(difference));
			if (index < 0)
				throw new Exception("Encountered an invalid hex offset difference in a river edge pair");
			return index;
		}

		void SetHexRiverData(Hex target, Hex neighbour, RiverEdge edge)
		{
			int index = GetHexOffsetIndex(target, neighbour);
			target.RiverEdges[index] = edge;
		}
	}
}
