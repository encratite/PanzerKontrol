using System.Collections.Generic;

namespace PanzerKontrol
{
	class UnitType
	{
		// The name of this unit.
		public string Name { get; set; }

		// The cost of points of this unit during the picking phase.
		public int Price { get; set; }

		// Particularly powerful units might have a limit per army.
		public int? Limit { get; set; }

		// Air units have no hardness
		public double? Hardness { get; set; }

		// Attack value against unarmoured/soft targets such as infantry.
		public int SoftAttack { get; set; }
		// Defence value against unarmoured/soft targets such as infantry.
		// Air units don't have this.
		public int? SoftDefence { get; set; }

		// Attack value against armoured/hard targets such as tanks.
		public int HardAttack { get; set; }
		// Defence value against armoured/hard targets such as tanks.
		// Air units don't have this.
		public int? HardDefence { get; set; }

		// Attack value against air units.
		// Only anti air units have this.
		public int? AirAttack { get; set; }
		// Defence value against attacks by air units.
		// Air units don't have this.
		public int? AirDefence { get; set; }

		// Defence value against anti-air attacks.
		// Only air units have this.
		public int? AntiAirDefence { get; set; }

		// The number of hexes this unit can move per turn.
		// Air units have no movement.
		public int? Movement { get; set; }

		// The range of hexes of the ground attack of this unit.
		// Only artillery has a range exceeding 1.
		// Optional, as air units don't have this.
		public int? Range { get; set; }

		// The range of the anti-air attack of this unit.
		// Optional, as only anti-air units have this.
		public int? AntiAirRange { get; set; }

		// The flags of this unit describe special properties/rules.
		public List<UnitFlag> Flags { get; set; }
	}
}
