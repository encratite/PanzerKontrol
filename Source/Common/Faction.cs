using System.Collections.Generic;

namespace PanzerKontrol
{
	public class Faction
	{
		// The name of the faction.
		public string Name { get; set; }

		// The lore.
		public string Description { get; set; }

		// Units that may be purchased during the picking phase when playing this faction.
		public List<UnitType> Units { get; set; }

		public Faction()
		{
			Units = new List<UnitType>();
		}
	}
}
