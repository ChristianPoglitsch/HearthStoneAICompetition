using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;

namespace SabberStoneAICompetition.src.AIAgents.Helper
{
	internal class AgentGameStats
	{
		public int GameNumber { get; }
		public int Player { get; set; }
		public int AgentDeck { get; set; }
		public int OpponentDeck { get; set; }
		public int Win { get; set; }
		public int Turns { get; set; }
		public int AttackDamageDealt { get; set; }
		public int OpponentHealth { get; set; }
		public int AgentHealth { get; set; }
		public int HeroDamageRecieved { get; set; }
		public int MinionsDestroyed { get; set; }
		public int MinionsLost { get; set; }
		public int MinionsSummoned { get; set; }
		public int SpellsUsed { get; set; }
		public int UsedMana { get; set; }
		public int UnusedMana { get; set; }
		public int CardsPlayed { get; set; }

		public AgentGameStats(int gameNumber)
		{
			GameNumber = gameNumber;
			Turns = 0;
			AttackDamageDealt = 0;
			HeroDamageRecieved = 0;
			MinionsDestroyed = 0;
			MinionsLost = 0;
			MinionsSummoned = 0;
			SpellsUsed = 0;
			UsedMana = 0;
			UnusedMana = 0;
			CardsPlayed = 0;
		}

		public override string ToString()
		{
			string result = $"##### Game {GameNumber} #####\n" +
				$"Player {Player}\n" +
				$"Used deck: {AgentDeck}\n" +
				$"Opponent used deck: {OpponentDeck}\n" +
				$"Turns: {Turns}\n" +
				$"Win: {Win}\n" +
				$"Damage delt: {AttackDamageDealt}\n" +
				$"Hero damage recieved: {HeroDamageRecieved}\n" +
				$"Minions summoned: {MinionsSummoned}\n" +
				$"Minions destroyed: {MinionsDestroyed}\n" +
				$"Minions lost: {MinionsLost}\n" +
				$"Used mana: {UsedMana}\n" +
				$"Unused mana: {UnusedMana}\n" +
				$"Agent last turn health: {AgentHealth}\n" +
				$"Opponent last turn health: {OpponentHealth}\n" +
				$"Cards played: {CardsPlayed}";
				
			return result;
		}
	}

	class StatHelper
    {
		private enum DeckEnum
		{
			None = 0,
			Mage = 1,
			Warrior = 2,
			Shaman = 3
		}

		private Dictionary<int, AgentGameStats> _stats;

		public StatHelper()
		{
			_stats = new Dictionary<int, AgentGameStats>();
		}

		public void AddStats(AgentGameStats stats)
		{
			_stats.Add(stats.GameNumber, stats);
		}

		public string PrintStats(int key)
		{
			if (_stats.Keys.Contains(key))
			{
				return _stats[key].ToString();
			}
			return String.Empty;
		}

		public void WriteCsvFile(string filePath)
		{
			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(_stats.Values);
			}
		}

		public string PrintFinalStats()
		{
			string result = "Total turns: ";

			return result;
		}
    }
}
