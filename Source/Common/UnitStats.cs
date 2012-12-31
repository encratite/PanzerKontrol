using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace PanzerKontrol
{
	public class UnitStats
	{
		// Attack value against unarmoured/soft targets such as infantry.
		public int? SoftAttack { get; set; }
		// Defence value against unarmoured/soft targets such as infantry.
		// Air units don't have this.
		public int? SoftDefence { get; set; }

		// Attack value against armoured/hard targets such as tanks.
		public int? HardAttack { get; set; }
		// Defence value against armoured/hard targets such as tanks.
		// Air units don't have this.
		public int? HardDefence { get; set; }

		// Defence value against artillery bombardments or aerial bombardments.
		// Air units don't have this.
		public int? BombardmentDefence { get; set; }

		// Attack value against air units.
		// Only anti-air units have this.
		public int? AirAttack { get; set; }
		// Defence value against anti-air attacks.
		// Only air units have this.
		public int? AntiAirDefence { get; set; }

		// The range of hexes of the ground attack of this unit.
		// Only artillery has a range.
		// Optional, as air units don't have this.
		public int? Range { get; set; }

		// The range of the anti-air attack of this unit.
		// Optional, as only anti-air units have this.
		public int? AntiAirRange { get; set; }

		// The number of hexes this unit can move per turn.
		// Air units have no movement.
		public int? Movement { get; set; }

		// The flags of a unit describe special properties/rules.
		public List<UnitFlag> Flags { get; set; }

		public UnitStats()
		{
			Flags = new List<UnitFlag>();
		}

		public UnitStats Clone()
		{
			IFormatter formatter = new BinaryFormatter();
			Stream stream = new MemoryStream();
			using (stream)
			{
				formatter.Serialize(stream, this);
				stream.Seek(0, SeekOrigin.Begin);
				return (UnitStats)formatter.Deserialize(stream);
			}
		}

		public void Combine(UnitStats stats)
		{
			SoftAttack = Add(SoftAttack, stats.SoftAttack);
			SoftDefence = Add(SoftDefence, stats.SoftDefence);

			HardAttack = Add(HardAttack, stats.HardAttack);
			HardDefence = Add(HardDefence, stats.HardDefence);

			BombardmentDefence = Add(BombardmentDefence, stats.BombardmentDefence);

			AirAttack = Add(AirAttack, stats.AirAttack);
			AntiAirDefence = Add(AntiAirDefence, stats.AntiAirDefence);

			Range = Add(Range, stats.Range);

			AntiAirRange = Add(AntiAirRange, stats.AntiAirRange);

			Movement = Add(Movement, stats.Movement);
			Flags.AddRange(stats.Flags);
		}

		int? Add(int? x, int? y)
		{
			if (x == null)
				return y;
			else if (y == null)
				return x;
			else
				return x + y;
		}
	}
}
