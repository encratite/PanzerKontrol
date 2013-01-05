using PanzerKontrol;

namespace Test
{
	class Program
	{
		static void GenerateConfiguration()
		{
			var configuration = new ServerConfiguration();
			var serialiser = new Nil.Serialiser<ServerConfiguration>("Configuration.xml");
			serialiser.Store(configuration);
		}

		static void GenerateFactions()
		{
			UnitStats stats = new UnitStats();
			stats.SoftAttack = 4;
			stats.SoftDefence = 4;
			stats.HardAttack = 3;
			stats.HardDefence = 3;
			stats.BombardmentDefence = 3;
			stats.Movement = 3;
			stats.Flags.Add(UnitFlag.Infantry);

			UnitStats bonus = new UnitStats();
			bonus.HardAttack = 1;
			bonus.HardDefence = 1;

			UnitUpgrade upgrade = new UnitUpgrade();
			upgrade.Name = "Upgrade";
			upgrade.Points = 5;
			upgrade.Slot = 0;

			UnitType unit = new UnitType();
			unit.Name = "Name";
			unit.Points = 20;
			unit.Hardness = 0.0;
			unit.Stats = stats;;
			unit.Upgrades.Add(upgrade);

			Faction faction = new Faction();
			faction.Name = "Faction";
			faction.Description = "Description";
			faction.Units.Add(unit);

			FactionConfiguration factions = new FactionConfiguration();
			factions.Factions.Add(faction);

			var serialiser = new Nil.Serialiser<FactionConfiguration>("Factions.xml");
			serialiser.Store(factions);
		}

		static void Main(string[] arguments)
		{
			GenerateConfiguration();
			GenerateFactions();
		}
	}
}
