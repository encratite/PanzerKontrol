using System.Collections.Generic;

namespace PanzerKontrol
{
	class Faction
	{
		// The name of the faction.
		public string Name { get; set; }

		// A ridiculous description of the faction.
		public string Lore { get; set; }

		// Units that may be purchased during the picking phase when playing this faction.
		public List<UnitType> Units { get; set; }
	}
}
