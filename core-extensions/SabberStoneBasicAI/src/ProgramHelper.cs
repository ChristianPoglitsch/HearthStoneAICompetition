using System;
using System.Collections.Generic;
using System.Text;
using SabberStoneBasicAI.Meta;
using SabberStoneCore.Config;
using SabberStoneCore.Enums;

namespace SabberStoneAICompetition.src
{
    class ProgramHelper
    {
		public static List<GameConfig> GetGameConfigs()
		{
			List<GameConfig> configs = new List<GameConfig>() {
				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.MAGE,
					Player2HeroClass = CardClass.MAGE,
					Player1Deck = Decks.RenoKazakusMage,
					Player2Deck = Decks.RenoKazakusMage,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.MAGE,
					Player2HeroClass = CardClass.MAGE,
					Player1Deck = Decks.RenoKazakusMage,
					Player2Deck = Decks.RenoKazakusMage,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.WARRIOR,
					Player2HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Deck = Decks.AggroPirateWarrior,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.WARRIOR,
					Player2HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Deck = Decks.AggroPirateWarrior,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.SHAMAN,
					Player2HeroClass = CardClass.SHAMAN,
					Player1Deck = Decks.MidrangeJadeShaman,
					Player2Deck = Decks.MidrangeJadeShaman,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.SHAMAN,
					Player2HeroClass = CardClass.SHAMAN,
					Player1Deck = Decks.MidrangeJadeShaman,
					Player2Deck = Decks.MidrangeJadeShaman,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.MAGE,
					Player2HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.RenoKazakusMage,
					Player2Deck = Decks.AggroPirateWarrior,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.MAGE,
					Player2HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.RenoKazakusMage,
					Player2Deck = Decks.AggroPirateWarrior,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.WARRIOR,
					Player2HeroClass = CardClass.MAGE,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Deck = Decks.RenoKazakusMage,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.WARRIOR,
					Player2HeroClass = CardClass.MAGE,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Deck = Decks.RenoKazakusMage,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.MAGE,
					Player2HeroClass = CardClass.SHAMAN,
					Player1Deck = Decks.RenoKazakusMage,
					Player2Deck = Decks.MidrangeJadeShaman,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.MAGE,
					Player2HeroClass = CardClass.SHAMAN,
					Player1Deck = Decks.RenoKazakusMage,
					Player2Deck = Decks.MidrangeJadeShaman,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.SHAMAN,
					Player2HeroClass = CardClass.MAGE,
					Player1Deck = Decks.MidrangeJadeShaman,
					Player2Deck = Decks.RenoKazakusMage,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.SHAMAN,
					Player2HeroClass = CardClass.MAGE,
					Player1Deck = Decks.MidrangeJadeShaman,
					Player2Deck = Decks.RenoKazakusMage,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.SHAMAN,
					Player2HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.MidrangeJadeShaman,
					Player2Deck = Decks.AggroPirateWarrior,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.SHAMAN,
					Player2HeroClass = CardClass.WARRIOR,
					Player1Deck = Decks.MidrangeJadeShaman,
					Player2Deck = Decks.AggroPirateWarrior,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 1,
					Player1HeroClass = CardClass.WARRIOR,
					Player2HeroClass = CardClass.SHAMAN,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Deck = Decks.MidrangeJadeShaman,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},

				new GameConfig()
				{
					StartPlayer = 2,
					Player1HeroClass = CardClass.WARRIOR,
					Player2HeroClass = CardClass.SHAMAN,
					Player1Deck = Decks.AggroPirateWarrior,
					Player2Deck = Decks.MidrangeJadeShaman,
					FillDecks = false,
					Shuffle = true,
					Logging = false
				},
			};
			return configs;
		}
	}
}
