using System.Collections.Generic;
using System.Xml.Serialization;

namespace PanzerKontrol
{
	public class Faction
	{
		// The numeric identifier of this faction.
		// This value is generated automatically by the server based on the order of factions in the configurationfile.
		[XmlIgnore]
		public int? Id;

		// The name of the faction.
		public string Name { get; set; }

		// The lore.
		public string Description { get; set; }

		// Units that may be purchased during the picking phase when playing this faction.
		public List<Unit> Units { get; set; }

		public Faction()
		{
			Units = new List<Unit>();
		}

		public void SetUnitIds()
		{
			int id = 0;
			foreach (Unit unit in Units)
			{
				unit.Id = id;
				id++;
			}
		}
	}
}
