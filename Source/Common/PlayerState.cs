using System;
using System.Collections.Generic;

namespace PanzerKontrol
{
	public enum PlayerStateType
	{
		// The player has yet to submit their initial deployment
		DeployingUnits,
		// The player has deployed their units and is waiting for their opponent to do the same
		HasDeployedUnits,
		// It's this player's turn
		MyTurn,
		// It's the opponent's turn
		OpponentTurn,
	}

	// This class represents the state of a client within a game
	public class PlayerState
	{
		// The game the player is currently in
		public readonly Game Game;

		// The faction chosen by this player, for the current lobby or game
		public readonly Faction Faction;

		// A numeric identifier used in both the messaging system but also to indicate ownership of hexes in the map data
		// This justifies its existence in the non-multiplayer specific game state
		public readonly PlayerIdentifier Identifier;

		// The state of the player from their own perspective
		public PlayerStateType State;

		// Reinforcement points remaining for the current game
		int _ReinforcementPoints;

		// Units remaining
		List<Unit> Units;

		// The opponent of this player
		public PlayerState Opponent;

		#region Accessors

		public int ReinforcementPoints
		{
			get
			{
				return _ReinforcementPoints;
			}
		}

		Map Map
		{
			get
			{
				return Game.Map;
			}
		}

		#endregion

		public PlayerState(Game game, Faction faction, PlayerIdentifier identifier)
		{
			Game = game;
			Faction = faction;
			Identifier = identifier;
		}

		#region Main player state modifiers

		public void InitialUnitDeployment(Unit unit, Position position)
		{
			if (unit.Deployed)
				throw new GameException("Tried to specify the position of a unit that has already been deployed");
			if (!Map.IsInInitialDeploymentZone(Identifier, position))
				throw new GameException("Tried to deploy units outside the player's deployment zone");
			Hex hex = Map.GetHex(position);
			if (hex.Unit != null)
				throw new GameException("Tried to deploy a unit on a hex that is already occupied");
			unit.MoveToHex(hex);
		}

		public void MoveUnit(Unit unit, Position newPosition, out int movementPointsLeft, out List<Hex> captures)
		{
			if (!unit.Deployed)
				throw new GameException("Tried to move an undeployed unit");
			var movementMap = Map.CreateMovementMap(unit);
			Path pathUsed;
			if (!movementMap.TryGetValue(newPosition, out pathUsed))
				throw new GameException("The unit can't reach the specified hex");
			unit.MovementPoints = pathUsed.MovementPointsLeft;
			Hex hex = Map.GetHex(newPosition);
			unit.MoveToHex(hex);
			captures = CaptureHexes(pathUsed);
			movementPointsLeft = pathUsed.MovementPointsLeft;
		}

		public void EntrenchUnit(Unit unit)
		{
			if (!unit.Deployed)
				throw new GameException("Tried to entrench an undeployed unit");
			if (!unit.CanEntrench())
				throw new GameException("This unit cannot entrench");
			unit.Entrench();
		}

		public void AttackUnit(Unit attacker, Unit defender)
		{
			if (!attacker.Deployed)
				throw new GameException("Tried to attack with an undeployed unit");
			if (!defender.Deployed)
				throw new GameException("Tried to attack an undeployed unit");
			if (!attacker.CanPerformAction)
				throw new GameException("This unit cannot perform any more actions this turn");
			Combat outcome;
			if (attacker.IsAirUnit())
			{
				List<Unit> antiAirUnits = Opponent.GetAntiAirUnits(defender);
				outcome = new Combat(attacker, defender, true, antiAirUnits);
			}
			else
			{
				int distance = attacker.Hex.GetDistance(defender.Hex);
				if (distance > attacker.Stats.Range)
					throw new GameException("The target is out of range");
				outcome = new Combat(attacker, defender, true);
			}
			attacker.CanPerformAction = false;
			// Attacking a unit breaks entrenchment
			attacker.BreakEntrenchment();
			attacker.Strength = outcome.AttackerStrength;
			defender.Strength = outcome.DefenderStrength;
			if (!attacker.IsAlive())
				OnUnitDeath(attacker);
			if (!defender.IsAlive())
				Opponent.OnUnitDeath(defender);
		}

		public void DeployUnit(Unit unit, Position position)
		{
			if (unit.Deployed)
				throw new GameException("Tried to deploy a unit that has already been deployed");
			Hex hex = Map.GetHex(position);
			if (hex == null)
				throw new GameException("Encountered an invalid deployment position in a deployment request");
			if (hex.InitialDeploymentZone == null || hex.InitialDeploymentZone.Value != Identifier)
				throw new GameException("Tried to deploy a unit outside the deployment zone");
			if (hex.Owner == null || hex.Owner.Value != Identifier)
				throw new GameException("Tried to deploy a unit in a deployment zone that is currently not controlled by the player");
			if (hex.Unit != null)
				throw new GameException("Tried to deploy a unit on a hex that is already occupied");
			unit.MoveToHex(hex);
		}

		public void ReinforceUnit(Unit unit)
		{
			double maximumReinforcements = unit.IsInfantry() ? GameConstants.InfantryReinforcementsMaximum : GameConstants.MotorisedReinforcementsMaximum;
			double newStrength = Math.Min(unit.Strength + maximumReinforcements, GameConstants.FullUnitStrength);
			if (newStrength == unit.Strength)
				throw new GameException("Reinforcing this unit has no effect");
			int expenses = (int)Math.Round((newStrength - unit.Strength) * unit.Points * GameConstants.ReinforcementCostMitigation);
			if (expenses > ReinforcementPoints)
				throw new GameException("You do not have enough reinforcement points to reinforce this unit");
			if (unit.AttritionDuration > 0)
				throw new GameException("Units that are out of supplies cannot be reinforced");
			if (unit.MovementPoints != unit.Stats.Movement)
				throw new GameException("A unit cannot be reinforced in the current turn once it has moved");
			if (!unit.CanPerformAction)
				throw new GameException("This unit cannot perform any more actions this turn");
			_ReinforcementPoints -= expenses;
			unit.Strength = newStrength;
			// Reinforcing a unit uses up all its movement points and prevents it from performing any more actions in the current turn
			unit.MovementPoints = 0;
			unit.CanPerformAction = false;
		}

		#endregion

		#region Public utility functions

		public void SetTurnStates()
		{
			State = PlayerStateType.MyTurn;
			Opponent.State = PlayerStateType.OpponentTurn;
		}

		public void FlipTurnState()
		{
			State = State == PlayerStateType.MyTurn ? PlayerStateType.OpponentTurn : PlayerStateType.MyTurn;
		}

		public BaseArmy GetBaseArmy()
		{
			return new BaseArmy(Faction, Units);
		}

		public Unit GetUnit(int id)
		{
			return Units.Find((Unit x) => x.Id == id);
		}

		public List<UnitPosition> GetDeployment()
		{
			List<UnitPosition> output = new List<UnitPosition>();
			foreach (var unit in Units)
			{
				if (!unit.Deployed)
					continue;
				UnitPosition unitPosition = new UnitPosition(unit.Id, unit.Hex.Position);
				output.Add(unitPosition);
			}
			return output;
		}

		// Retrieve a list of anti-air units capable of protecting the target
		public List<Unit> GetAntiAirUnits(Unit target)
		{
			List<Unit> output = new List<Unit>();
			foreach (var unit in Units)
			{
				if (unit.Deployed && unit.IsAntiAirUnit() && unit.Hex.GetDistance(target.Hex) <= unit.Stats.AntiAirRange.Value)
					output.Add(unit);
			}
			return output;
		}

		public void InitialiseArmy(List<Unit> units, int reinforcementPoints)
		{
			Units = units;
			_ReinforcementPoints = reinforcementPoints;
		}

		public void ResetUnits()
		{
			foreach (var unit in Units)
				unit.ResetUnitForNewTurn();
		}

		public void OnUnitDeath(Unit unit)
		{
			Units.Remove(unit);
		}

		public bool HasUnitsLeft()
		{
			return Units.Count > 0;
		}

		public void EvaluateSupply(HashSet<Hex> supplyMap, List<Unit> attritionUnits)
		{
			foreach (var unit in Units)
			{
				if (supplyMap.Contains(unit.Hex))
				{
					// The unit received new supplies
					unit.AttritionDuration = 0;
				}
				else
				{
					unit.AttritionDuration++;
					if (unit.AttritionDuration > 1)
					{
						// The unit is subjected to attrition
						// It will take casualties and loses the ability to perform an action
						unit.TakeAttritionDamage();
						if (!unit.IsAlive())
							OnUnitDeath(unit);
					}
					attritionUnits.Add(unit);
				}
			}
		}

		public void PurchaseUnit(Unit unit)
		{
			if (unit.Points > ReinforcementPoints)
				throw new GameException("Not enough reinforcement points remaining to purchase this unit");
			_ReinforcementPoints -= unit.Points;
			Units.Add(unit);
		}

		#endregion

		#region Generic internal functions

		List<Hex> CaptureHexes(Path path)
		{
			List<Hex> captures = new List<Hex>();
			var map = Game.Map;
			// Capture hexes
			foreach (var pathHex in path.Hexes)
			{
				if (pathHex.Owner != Identifier)
				{
					// Direct capture
					pathHex.Owner = Identifier;
					captures.Add(pathHex);
					// Check for indirect captures
					foreach (var offset in Map.HexOffsets)
					{
						Position neighbourPosition = pathHex.Position + offset;
						Hex neighbourHex = map.GetHex(neighbourPosition);
						List<Hex> capturedRegion = map.GetIndirectlyCapturedRegion(neighbourHex, Identifier);
						if (capturedRegion == null)
						{
							// No region to capture could be found
							continue;
						}
						foreach (var emptyRegionHex in capturedRegion)
						{
							emptyRegionHex.Owner = Identifier;
							captures.Add(emptyRegionHex);
						}
					}
				}
			}
			return captures;
		}

		#endregion
	}
}
