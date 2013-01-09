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

	public class Path
	{
		public List<Hex> Hexes;
		public int MovementPointsLeft;

		public Path(int movementPointsLeft)
		{
			Hexes = new List<Hex>();
			MovementPointsLeft = movementPointsLeft;
		}

		public Path(Path currentPath, Hex newHex, int movementPointsLeft)
		{
			Hexes = new List<Hex>();
			Hexes.AddRange(currentPath.Hexes);
			Hexes.Add(newHex);
			MovementPointsLeft = movementPointsLeft;
		}
	}

	public class Map
	{
		public static Position[] HexOffsets =
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

		[XmlIgnore]
		Dictionary<PlayerIdentifier, List<Hex> > SupplySources;

		#region Generic public functions

		public void Initialise()
		{
			PositionMap = new Dictionary<Position, Hex>(new PositionComparer());
			SupplySources = new Dictionary<PlayerIdentifier, List<Hex> >();
			// Clumsy but whatever
			SupplySources[PlayerIdentifier.Player1] = new List<Hex>();
			SupplySources[PlayerIdentifier.Player2] = new List<Hex>();
			foreach (var hex in Hexes)
			{
				PositionMap[hex.Position] = hex;
				if (hex.SupplySource != null)
					SupplySources[hex.SupplySource.Value].Add(hex);
			}
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

		// This calculates a map of positions a unit can move to (keys) and best paths discovered (values)
		public Dictionary<Position, Path> CreateMovementMap(Unit unit)
		{
			var map = new Dictionary<Position, Path>(new PositionComparer());
			Path path = new Path(unit.MovementPoints);
			CreateMovementMap(unit, unit.Hex, path, unit.Owner.Identifier, map);
			// Remove all the hexes occupied by friendly units since those were only permitted for passing through
			foreach (var position in map.Keys)
			{
				Hex hex = GetHex(position);
				if (hex.Unit != null)
					map.Remove(position);
			}
			return map;
		}

		public static int GetHexOffsetIndex(Hex hex1, Hex hex2)
		{
			Position difference = hex2.Position - hex2.Position;
			int index = Array.FindIndex(HexOffsets, (Position x) => x.SamePosition(difference));
			if (index < 0)
				throw new Exception("Encountered an invalid hex offset difference in a river edge pair");
			return index;
		}


		public List<Hex> GetIndirectlyCapturedRegion(Hex seed, PlayerIdentifier conqueror)
		{
			if (seed.Owner == conqueror)
			{
				// This can't be the seed of an empty region as it is already owned by the player
				return null;
			}
			List<Hex> capturedRegion = new List<Hex>();
			// Initially only the seed is virtually captured
			capturedRegion.Add(seed);
			HashSet<Hex> scannedHexes = new HashSet<Hex>(new HexComparer());
			// Perform depth-first search to determine the size of the region
			if (IsValidIndirectCapture(seed, conqueror, capturedRegion, scannedHexes))
				return capturedRegion;
			else
				return null;
		}

		// Determines how much of the map is controlled by each player
		public Dictionary<PlayerIdentifier, int> GetMapControl()
		{
			var output = new Dictionary<PlayerIdentifier, int>();
			foreach (var hex in Hexes)
			{
				if (hex.Owner != null)
					output[hex.Owner.Value]++;
			}
			return output;
		}

		// Determines which parts of the map are currently being supplied
		public HashSet<Hex> GetSupplyMap(PlayerIdentifier identifier)
		{
			var output = new HashSet<Hex>(new HexComparer());
			foreach (var supplySource in SupplySources[identifier])
				CreatePartialSupplyMap(supplySource, identifier, output);
			return output;
		}

		#endregion

		#region Generic internal functions

		List<Hex> GetNeighbours(Hex target)
		{
			var output = new List<Hex>();
			foreach (var offset in HexOffsets)
			{
				Position neighbourPosition = target.Position + offset;
				Hex neighbour = GetHex(neighbourPosition);
				if (neighbour != null)
					output.Add(neighbour);
			}
			return output;
		}

		void CreateMovementMap(Unit unit, Hex currentHex, Path currentPath, PlayerIdentifier owner, Dictionary<Position, Path> map)
		{
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
				if (neighbourHex.Unit != null && neighbourHex.Unit.Owner.Identifier != owner)
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
					if (currentPath.MovementPointsLeft < maximumMovementPoints)
					{
						// The unit had already moved so it can't cross the river
						continue;
					}
					if (currentPath.MovementPointsLeft < terrainMovementPoints)
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
				int newMovementPoints = currentPath.MovementPointsLeft - movementPointsLost;
				if (newMovementPoints < 0)
				{
					// The unit doesn't have enough movement points left to enter this hex
					continue;
				}
				Path previousPath;
				if (map.TryGetValue(neighbourPosition, out previousPath))
				{
					// This neighbouring hex was already analysed by a previous recursive call to this function, check if we can even improve on what it calculated
					if (previousPath.MovementPointsLeft <= newMovementPoints)
					{
						// The solution is inferior or just as good, skip it
						continue;
					}
				}
				// Create or update the entry in the movement map
				Path newPath = new Path(currentPath, neighbourHex, newMovementPoints);
				map[neighbourPosition] = newPath;
				CreateMovementMap(unit, neighbourHex, newPath, owner, map);
			}
		}

		void SetHexRiverData(Hex target, Hex neighbour, RiverEdge edge)
		{
			int index = GetHexOffsetIndex(target, neighbour);
			target.RiverEdges[index] = edge;
		}

		bool IsValidIndirectCapture(Hex currentHex, PlayerIdentifier conqueror, List<Hex> capturedRegion, HashSet<Hex> scannedHexes)
		{
			foreach (var neighbour in GetNeighbours(currentHex))
			{
				if (scannedHexes.Contains(neighbour))
				{
					// This hex has already been scanned
					continue;
				}
				scannedHexes.Add(neighbour);
				if (neighbour.Owner == conqueror)
				{
					// This hex is already owned by the player
					continue;
				}
				if (neighbour.Unit != null)
				{
					// An enemy unit is occupying a hex in this region so it can't be captured
					return false;
				}
				if (scannedHexes.Count >= GameConstants.IndirectCaptureLimit)
				{
					// This hex passed the criteria but the region is already too large to be captured indirectly
					return false;
				}
				capturedRegion.Add(neighbour);
				bool isEmptyRegion = IsValidIndirectCapture(neighbour, conqueror, capturedRegion, scannedHexes);
				if (!isEmptyRegion)
					return false;
			}
			return true;
		}

		void CreatePartialSupplyMap(Hex currentPosition, PlayerIdentifier identifier, HashSet<Hex> suppliedHexes)
		{
			foreach (var neighbour in GetNeighbours(currentPosition))
			{
				if (suppliedHexes.Contains(neighbour))
				{
					// We've reached a part of the map that has already been covered by previous searches
					continue;
				}
				if (neighbour.Owner != identifier)
				{
					// Only hexes that are owned by the player can be supplied
					continue;
				}
				suppliedHexes.Add(neighbour);
				CreatePartialSupplyMap(neighbour, identifier, suppliedHexes);
			}
		}

		#endregion
	}
}
