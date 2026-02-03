using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using OpenAI.Chat;
using SabberStoneAICompetition.src.AIAgents.Helper;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks.PlayerTasks;
using Hero = SabberStoneCore.Model.Entities.Hero;
using Weapon = SabberStoneCore.Model.Entities.Weapon;
using System.Text.RegularExpressions;

namespace SabberStoneContract.Helper
{
	internal class LogHelper
	{
		public StreamWriter Writer { get => _writer; }
		private StreamWriter _writer;
		public string Path;
		public string Filename;
		public string Suffix;

		public void NewWriter()
		{
			_writer?.Close();
			_writer = new StreamWriter(System.IO.Path.Combine(Path, Filename + Suffix + ".txt"));
		}

		public void WriteLine(string line)
		{
			_writer.WriteLine(line);
		}

		public LogHelper(string path = "", string filename = "match_log", string suffix = "")
		{
			Path = path;
			Filename = filename;
			Suffix = suffix;
		}
	}

	internal class PromptHelper
	{
		private List<ChatMessage> _messages = new List<ChatMessage>();
		private Game _game;
		private PlayState _playState;
		private Controller _player;
		private Controller _opponent;
		private ChatClient _chatClient;
		private ChatCompletion _chatCompletion;
		private string _rulesString;
		private string _deckString;
		private bool _discoverPlayed = false;
		private ChatCompletionOptions _completionOptions = new ChatCompletionOptions();
		private LogHelper _logHelper;
		private bool _newTurn = true;
		private int _lastOpponentMove = 0;
		private int _turnCounter = 1;
		private AgentGameStats _stats;
		private int _prevAgentHealth = 30;
		private int _max_retries = 10;
		private bool _customPromptAfter = false;
		private string _customPrompt = "";
		private string _rulesPath = "rules.txt";
		private string _customSystemMessage = "";

		public AgentGameStats Stats { get => _stats; }

		public void InitStats(int gameNumber)
		{
			_stats = new AgentGameStats(gameNumber);
		}

		public ChatCompletionOptions ChatCompletionOptions
		{
			get => _completionOptions;
			set
			{
				if (value != null)
					_completionOptions = value;
			}
		}

		public PromptHelper(ref ChatClient client, Game game, LogHelper logHelper = null, int gameCount = 0)
		{
			_game = game;
			_player = game.CurrentPlayer;
			_opponent = game.CurrentOpponent;
			_chatClient = client;
			InitStats(gameCount);
			_rulesString = CreateRuleString();
			_deckString = CreateDeckString();
			_logHelper = logHelper;
		}

		public void StartChat()
		{
			_messages.Add(new SystemChatMessage(_rulesString));
			_messages.Add(new SystemChatMessage(_deckString));

			if (_customSystemMessage != "")
				_messages.Add(new SystemChatMessage(_customSystemMessage));

			if (_logHelper != null)
			{
				_logHelper.WriteLine("[Prompt]:\n" + _rulesString + "\n");
				_logHelper.WriteLine("[Prompt]:\n" + _deckString + "\n");
				if (_customSystemMessage != "")
					_logHelper.WriteLine("[Prompt]:\n" + _customSystemMessage + "\n");
			}
		}

		public void UpdateGame(Game game)
		{
			_game = game;
			_player = game.CurrentPlayer;
			_opponent = game.CurrentOpponent;
		}

		public void ClearMessages()
		{
			_messages.Clear();
		}

		public string CreateStringFromTask(PlayerTask task)
		{
			string result = "";
			switch (task.PlayerTaskType)
			{
				case PlayerTaskType.END_TURN:
					result = "End your turn";
					break;

				case PlayerTaskType.PLAY_CARD:
					result = GetPlayCardString(task);
					break;

				case PlayerTaskType.CHOOSE:
					result = GetChooseCardString(task);
					break;

				case PlayerTaskType.HERO_ATTACK:
					result = GetHeroAttackString(task);
					break;

				case PlayerTaskType.MINION_ATTACK:
					result = GetMinionAttackString(task);
					break;

				case PlayerTaskType.HERO_POWER:
					result = GetHeroPowerString(task);
					break;

				default:
					Console.WriteLine("[PromptHelper]: Task not recognized.");
					break;
			}
			return result;
		}

		private string GetHeroPowerString(PlayerTask task)
		{
			string result = "";
			if (task?.Controller?.Hero?.HeroPower?.Card != null)
			{
				result += "Use your hero power ";
				result += $"\"{task.Controller.Hero.HeroPower.Card.Name}\" " +
					$"(Cost: {task.Controller.Hero.HeroPower.Card.Cost}, Card text: ";
				result += $"\"{task.Controller.Hero.HeroPower.Card.Text.Split('\n')[1]}\")";

				if (task.Target != null)
				{
					string belongsTo = task.Target.Controller == _player ? "your" : "opponent's";
					result += $" on {belongsTo} {GetCardStatsAsString(task.Target, false)}";
				}
			}
			else
				Console.WriteLine("[PromptHelper]: Hero Power Error");
			return result;
		}

		private string GetPlayCardString(PlayerTask task)
		{
			string result = "";
			if (task.Source != null && task.Source.Card != null)
			{
				IPlayable playable = task.Source;

#if DEBUG
				int choose = 0;
				if (task.ChooseOne > -1)
					choose = task.ChooseOne;
#endif

				switch (playable.Card.Type)
				{
					case CardType.MINION:
						result = $"Summon {GetCardStatsAsString(playable)} from your hand";
						if (task.Target != null)
						{
							string belongsTo = task.Target.Controller == _player ? "your" : "opponent's";
							result += $" and use it's effect on {belongsTo} {GetCardStatsAsString(task.Target, false)}";
						}
						break;
					case CardType.SPELL:
						result = $"Cast {GetCardStatsAsString(playable)} from your hand";
						if (task.Target != null)
						{
							string belongsTo = task.Target.Controller == _player ? "your" : "opponent's";
							result += $" on {belongsTo} {GetCardStatsAsString(task.Target, false)}";
						}
						break;
					case CardType.WEAPON:
						result = $"Equip {GetCardStatsAsString(playable)}";
						break;
					default:
						result = $"Play {GetCardStatsAsString(playable)} from your hand";
						if (task.Target != null)
						{
							string belongsTo = task.Target.Controller == _player ? "your" : "opponent's";
							result += $" and use on {belongsTo} {GetCardStatsAsString(task.Target, false)}";
						}
						break;
				}
			}
			else
				Console.WriteLine("[PromptHelper]: Play card Error");
			return result;
		}

		private string GetChooseCardString(PlayerTask task)
		{
			string result = "";
			var chooseTask = task as ChooseTask;
			if (chooseTask != null && chooseTask.Choices.Count > 0)
				result += $"Choose {GetCardStatsAsString(_game.IdEntityDic[chooseTask.Choices[0]])}";
			if (_game.CurrentPlayer.LastCardPlayed != 0)
			{
				IPlayable lastCard = _game.IdEntityDic[_game.CurrentPlayer.LastCardPlayed];
				if (lastCard.Card.Text != null && lastCard.Card.Text.Contains("<b>Discover</b>"))
					_discoverPlayed = true;
			}
			return result;
		}

		private string GetHeroAttackString(PlayerTask task)
		{
			string result = "";
			if (task.Target != null)
			{ 
				result += $"Use your hero's weapon to attack your opponent's {GetCardStatsAsString(task.Target, false, false)}";
			}
			else
				Console.WriteLine("[PromptHelper]: Hero attack Error");
			return result;
		}

		private string GetMinionAttackString(PlayerTask task)
		{
			string result = "";
			if (task.Source != null)
			{
				result = "With your " + GetCardStatsAsString(task.Source, false, false);

				if (task.Target != null)
				{
					result += $" attack your opponent's {GetCardStatsAsString(task.Target, false, false)}.";
				}
				else
					Console.WriteLine("[PromptHelper]: Minion attack target Error");
			}
			else
				Console.WriteLine("[PromptHelper]: Minion attack source Error");

			return result;
		}

		public void EvaluateAgentChoice(PlayerTask task)
		{
			int damageDealt = 0;
			switch (task.PlayerTaskType)
			{
				case PlayerTaskType.END_TURN:
					SetTurnEnd();
					return;
				case PlayerTaskType.MINION_ATTACK:
					Minion minion = task.Source as Minion;
					damageDealt = minion.AttackDamage;
					break;
				case PlayerTaskType.HERO_ATTACK:
					damageDealt = _player.Hero.AttackDamage;
					break;
				default:
					return;
			}
			

			if (_stats != null)
			{
				_stats.AttackDamageDealt += damageDealt;
			}
		}

		private int GetAgentHeroDamage(Controller controller, ref int heroHealth)
		{
			if (_stats == null)
				return 0;

			int result = 0;

			if (controller.Hero.Health < heroHealth)
			{
				result += (heroHealth - controller.Hero.Health);
			}
			heroHealth = controller.Hero.Health;

			return result;
		}

		private string GetCardStatsAsString(IPlayable playable, bool writeHeroName = true, bool writeCost = true)
		{
			if (playable == null || playable.Card == null)
			{
				Console.WriteLine("[PromptHelper]: Error in GetCardStatsAsString");
				return "";
			}

			string result = $"\"{playable.Card.Name}\" (Type: {playable.Card.Type}";

			if (writeCost)
			{
				result += $", Cost: {playable.Cost}";
			}

			switch (playable.Card.Type)
			{
				case CardType.HERO:
					Hero hero = playable as Hero;
					if (!writeHeroName)
						result = $"hero (Armor: {hero.Armor}";
					else
						result += $", Armor: {hero.Armor}";
					break;
				case CardType.MINION:
					Minion minion = playable as Minion;
					result += $", Attack: {minion.AttackDamage}, Health: {minion.Health}";
					break;
				case CardType.WEAPON:
					Weapon weapon = playable as Weapon;
					result += $", Attack: {weapon.AttackDamage}, Durability: {weapon.Durability}";
					break;
				default:
					break;
			}

			if (playable.Card.Text != null)
				result += $", Card text: \"{playable.Card.Text}\"";

			return result += ")";
		}

		private string GetCardStatsAsString(Card card, bool writeHeroName = true, bool writeCost = true)
		{
			if (card == null)
			{
				Console.WriteLine("[PromptHelper]: Error in GetCardStatsAsString");
				return "";
			}

			string result = $"\"{card.Name}\" (Type: {card.Type}";

			if (writeCost)
			{
				result += $", Cost: {card.Cost}";
			}

			switch (card.Type)
			{
				case CardType.HERO:
					if (!writeHeroName)
						result = $"hero";
					break;
				case CardType.MINION:
					result += $", Attack: {card.ATK}, Health: {card.Health}";
					break;
				case CardType.WEAPON:
					result += $", Attack: {card.ATK}";
					break;
				default:
					break;
			}

			if (card.Text != null)
				result += $", Card text: \"{card.Text}\"";

			if (card.Type != CardType.HERO)
				result += ")";

			return result;
		}

		private string CreateRuleString()
		{
			string result = "";
			try
			{
				StreamReader reader = new StreamReader("rules.txt");

				if (_rulesPath != "")
					reader = new StreamReader(_rulesPath);

				result = reader.ReadToEnd();

				//Console.WriteLine("[PromptHelper] Rules successfully read from file");
			}
			catch (IOException e)
			{
				Console.WriteLine("[PromptHelper] The file could not be read:");
				Console.WriteLine(e.Message);
			}
			return result;
		}

		private string CreateDeckString()
		{
			string result = "";

			if (_player == null || _player.DeckCards == null || _player.Hero == null)
				return result;

			CardClass heroClass = _player.HeroClass;

			result += $"Your class is {heroClass}.\n";
			result += "Your deck consists of:\n";

			// Not pretty, since DeckZone reveals order of the deck
			var deckList = _player.DeckZone.OrderBy(x => x.Card.Cost).ToList();
			foreach (var playable in deckList)
			{
				result += GetCardStatsAsString(playable) + "\n";
			}
			result += _player.PlayerId == _game.FirstPlayer.PlayerId ? "You are Player 1. Therefore you start.\n" : "You are Player 2. You are going second.\n";
			result += "Just answer if you understand the rules. Wait for further prompts.";

			if (_stats != null)
			{
				_stats.Player = _player.PlayerId == _game.FirstPlayer.PlayerId ? 1 : 2;
				AddDecksToStats();			
			}

			return result;
		}

		private void AddDecksToStats()
		{
			if (_stats == null)
				return;

			int agentDeck = 0;
			int opponentDeck = 0;

			switch (_player.HeroClass)
			{
				case CardClass.MAGE: agentDeck = 1; break;
				case CardClass.WARRIOR: agentDeck = 2; break;
				case CardClass.SHAMAN: agentDeck = 3; break;
			}

			switch (_opponent.HeroClass)
			{
				case CardClass.MAGE: opponentDeck = 1; break;
				case CardClass.WARRIOR: opponentDeck = 2; break;
				case CardClass.SHAMAN: opponentDeck = 3; break;
			}

			_stats.AgentDeck = agentDeck;
			_stats.OpponentDeck = opponentDeck;
		}

		public string CreateOptionsPrompt(Dictionary<int, string> stringDict, bool explanation = false)
		{
			string result = "Your options for this move are:\n";

			if (_discoverPlayed)
			{
				result = "You played a card with Discover. Choose one of the following cards:\n";
				_discoverPlayed = false;
			}

			foreach (var keyPair in stringDict)
			{
				result += $"{keyPair.Key}. {keyPair.Value}\n";
			}

			if (!_customPromptAfter && _customPrompt != "")
			{
				result += _customPrompt + "\n";
			}

			if (!explanation || _customPrompt != "")
				result += "Respond with the number of the option you choose and write double hashtags before the number like this: ##<number>\n";
			else
				result += "Respond with the number of the option you choose and write double hashtags before the number like this: ##<number>\n" +
					"In a new line give a 1 sentence explanation for your reasoning.\n" +
					"Make sure the number of the option you choose corresponds with your reasoning.";

			if (_customPromptAfter && _customPrompt != "")
			{
				result += _customPrompt + "\n";
			}

			return result;
		}

		public void SetCustomPrompt(string prompt, bool afterInstruction = false)
		{
			_customPrompt = prompt;
			_customPromptAfter = afterInstruction;
		}

		public void SetRulesPath(string path)
		{
			_rulesPath = path;
		}

		public void ResetCustomPrompt()
		{
			_customPrompt = "";
			_customPromptAfter = false;
		}

		private void SetTurnEnd()
		{
			_newTurn = true;
			_turnCounter++;
			if (_stats !=  null)
			{
				_stats.UsedMana += _player.UsedMana;
				_stats.UnusedMana += _player.RemainingMana;
				_stats.MinionsSummoned += _player.NumMinionsPlayedThisTurn;
				_stats.MinionsLost += _player.NumFriendlyMinionsThatDiedThisTurn;
				_stats.MinionsDestroyed += _player.NumMinionsPlayerKilledThisTurn;
				_stats.CardsPlayed += _player.NumCardsPlayedThisTurn;
				_stats.Turns++;
			}
			_lastOpponentMove = _game.CurrentOpponent.PlayHistory.Count;
			_messages.Clear();
			_messages.Add(new SystemChatMessage(_rulesString));
			_messages.Add(new SystemChatMessage(_deckString));
			if (_customSystemMessage != "")
				_messages.Add(new SystemChatMessage(_customSystemMessage));
		}

		public string CreateTurnPrefaceString()
		{
			string result = $"Turn {_turnCounter}\n";

			if (_newTurn)
			{
				result += "This is the start of your turn.\n";
				_newTurn = false;
			}

			if (_player == null || _opponent == null)
				return result;

			result += CreateOpponentPrefaceString();
			result += CreatePlayerPrefaceString();

			if (_stats != null)
			{
				_stats.HeroDamageRecieved += GetAgentHeroDamage(_player, ref _prevAgentHealth);
			}

			return result;
		}

		private string GetCardsOnField(Controller player)
		{
			string result = "";
			string minions = String.Empty;
			foreach (var entity in player.BoardZone)
			{
				switch (entity.Card.Type)
				{
					case CardType.MINION:
						if (minions == String.Empty)
							minions = "Minions controlled:\n";
						minions += GetCardStatsAsString(entity) + "\n";
						break;
				}
			}
			result += minions;
			return result;
		}

		private string CreatePlayerPrefaceString()
		{
			string result = "";

			if (_player == null)
				return result;

			result += $"Your current health: {_player.Hero.Health}\n";
			result += $"Your currently available mana: {_player.RemainingMana}\n";

			if (_player.HandZone.Count > 0)
			{
				result += $"Your hand cards: \n";
				foreach (var entity in _player.HandZone)
				{
					result += GetCardStatsAsString(entity) + "\n";
				}
			}
			else
				result += "You have no cards in your hand.\n";

			if (_player.BoardZone.Count > 0)
			{
				result += "You control the following cards on the battlefield:\n";
				result += GetCardsOnField(_player);
			}
			else
				result += "You control no cards or minions on the battlefield.\n";

			if (_player.Hero.Weapon != null)
			{
				Weapon weapon = _player.Hero.Weapon;
				result += $"Your hero has equipped: {GetCardStatsAsString(weapon)}\n";
			}
			else
				result += "Your hero has no weapon equipped.\n";

			if (_player.Hero.Armor != 0)
			{
				result += $"Your hero has {_player.Hero.Armor} armor.";
			}

			if (_player.DiscardedEntities.Count > 0)
			{
				result += $"Your discarded cards:\n";
				foreach (var id in _player.DiscardedEntities)
				{
					IPlayable entity = _game.IdEntityDic[id];
					result += GetCardStatsAsString(entity) + "\n";
				}
			}

			return result;
		}

		private string CreateOpponentPrefaceString()
		{
			string result = "";

			if (_opponent == null)
				return result;

			result += $"Opponent's current health: {_opponent.Hero.Health}\n";
			result += $"Opponent's maximum available mana: {_opponent.BaseMana}\n";
			result += $"Number of opponent's available hand cards: {_opponent.HandZone.Count}\n";

			if (_opponent.BoardZone.Count > 0)
			{
				result += $"Cards controlled by opponent on the battlefield:\n";
				result += GetCardsOnField(_opponent);
			}
			else
				result += "Opponent controls no cards or minions on the battlefield.\n";

			if (_opponent.Hero.EquippedWeapon != 0)
			{
				IPlayable weapon = _game.IdEntityDic[_opponent.Hero.EquippedWeapon];
				result += $"Opponent's hero has equipped: {GetCardStatsAsString(weapon)}\n";
			}
			else
				result += "Opponent's hero has no weapon equipped.\n";

			if (_opponent.Hero.Armor != 0)
			{
				result += $"Opponent's hero has {_opponent.Hero.Armor} armor.";
			}

			//if (_opponent.GraveyardZone.Count > 0 || _opponent.DiscardedEntities.Count > 0)
			//{
			//	result += $"Used or discarded cards by your opponent:\n";
			//	foreach (var id in _opponent.DiscardedEntities)
			//	{
			//		var entity = _game.IdEntityDic[id];
			//		result += GetCardStatsAsString(entity) + "\n";
			//	}
			//	foreach (var entity in _opponent.GraveyardZone)
			//	{
			//		result += GetCardStatsAsString(entity) + "\n";
			//	}
			//}

			if (_opponent.PlayHistory.Count > _lastOpponentMove)
			{
				result += "Opponent's moves during their last turn:\n";
				for (int i = _lastOpponentMove; i < _opponent.PlayHistory.Count; i++)
				{
					PlayHistoryEntry entry = _opponent.PlayHistory[i];
					if (entry.Equals(_lastOpponentMove))
						break;
					result += $" - {GetPlayHistoryString(entry)}\n";
				}
			}

			return result;
		}

		private string GetPlayHistoryString(PlayHistoryEntry entry)
		{
			string result = String.Empty;

			switch (entry.SourceCard.Type)
			{
				case CardType.MINION:
					result += GetMinionPlayHistory(entry);
					break;
				case CardType.SPELL:
					result += GetSpellPlayHistory(entry);
					break;
				case CardType.HERO:
					result += GetHeroPlayHistory(entry);
					break;
				case CardType.HERO_POWER:
					result += GetHeroPowerPlayHistory(entry);
					break;
				default:
					result += $"Played {GetCardStatsAsString(entry.SourceCard)}";
					if (entry.TargetCard != null)
					{
						string owner = entry.TargetController == _game.CurrentPlayer.PlayerId ? "your" : "their";
						result += $" on {owner} {GetCardStatsAsString(entry.TargetCard)}";
					}
					break;
			}

			return result;
		}
		private string GetOwnerString(int playerId)
		{
			return playerId == _game.CurrentPlayer.PlayerId ? "your" : "their";
		}
		private string GetMinionPlayHistory(PlayHistoryEntry entry)
		{
			string result = String.Empty;
			if (entry.TargetCard == null)
			{
				result = $"Played {GetCardStatsAsString(entry.SourceCard)} from their hand.";
			}
			else
			{
				result = $"Attacked {GetOwnerString(entry.TargetController)} {GetCardStatsAsString(entry.TargetCard, false)} with" +
					$" {GetOwnerString(entry.SourceController)} {GetCardStatsAsString(entry.SourceCard)}";
			}
			return result;
		}

		private string GetSpellPlayHistory(PlayHistoryEntry entry)
		{
			string result = String.Empty;
			if (entry.TargetCard == null)
			{
				result = $"Played {GetCardStatsAsString(entry.SourceCard)} from their hand.";
			}
			else
			{
				result = $"Played {GetCardStatsAsString(entry.SourceCard)} on" +
					$" {GetOwnerString(entry.TargetController)} {GetCardStatsAsString(entry.TargetCard, false)}";
			}
			return result;
		}

		private string GetHeroPlayHistory(PlayHistoryEntry entry)
		{
			string result = String.Empty;
			if (entry.TargetCard == null)
			{
				result = $"Used {GetCardStatsAsString(entry.SourceCard)}";
			}
			else
			{
				result = $"Attacked {GetOwnerString(entry.TargetController)} {GetCardStatsAsString(entry.TargetCard, false)} with their hero";
			}
			return result;
		}

		private string GetHeroPowerPlayHistory(PlayHistoryEntry entry)
		{
			string result = String.Empty;
			if (entry.TargetCard == null)
			{
				result = $"Used their hero power {GetCardStatsAsString(entry.SourceCard)}";
			}
			else
			{
				result = $"Used their hero power {GetCardStatsAsString(entry.SourceCard)} on" +
					$" {GetOwnerString(entry.TargetController)} {GetCardStatsAsString(entry.TargetCard, false)}";
			}
			return result;
		}

		private void SendMessages()
		{
			int retries = 0;
			int delay = 100;
			while (retries < _max_retries)
			{
				try
				{
					_chatCompletion = _chatClient.CompleteChat(messages: _messages, options: ChatCompletionOptions);
					break;
				}
				catch (Exception e)
				{
					Console.WriteLine("[PromptHelper] Exception: " + e.Message);
					retries++;
					Thread.Sleep(delay);
					delay *= 2;
					continue;
				}
			}
		}

		public int GetOptionResponse(string prompt, bool writeResponseToConsole = false)
		{
			int responseNumber = 1;
			UserChatMessage message = new UserChatMessage(prompt);
			_messages.Add(message);

			SendMessages();

			string pattern = @"(?<=##)\d*";
			MatchCollection matches = Regex.Matches(_chatCompletion.Content[0].Text, pattern);
			string number = matches.Count > 0 ? matches[0].Value : "N";

			while (matches.Count == 0 || !int.TryParse(number, out responseNumber))
			{
				SendMessages();
				matches = Regex.Matches(_chatCompletion.Content[0].Text, pattern);
				number = matches.Count > 0 ? matches[0].Value : "N";
			}

			_messages.Add(new AssistantChatMessage(_chatCompletion));

			if (writeResponseToConsole)
				Console.WriteLine("[GPT Response] " + _chatCompletion.Content[0].Text);

			if (_logHelper != null)
			{
				_logHelper.WriteLine("[Prompt]:\n" + message.Content[0].Text + "\n");
				_logHelper.WriteLine("[Response]:\n" + _chatCompletion.Content[0].Text);
				_logHelper.WriteLine("----------------------------//MOVE END\\\\----------------------------\n");
			}

			return responseNumber;
		}

		public void GetResponse(string prompt, bool writeResponseToConsole = false)
		{
			UserChatMessage message = new UserChatMessage(prompt);
			_messages.Add(message);
			SendMessages();
			_messages.Add(new AssistantChatMessage(_chatCompletion));
			if (writeResponseToConsole)
				Console.WriteLine("[GPT Response] " + _chatCompletion.Content[0].Text);
			if (_logHelper != null)
			{
				_logHelper.WriteLine("[Prompt]:\n" + message.Content[0].Text + "\n");
				_logHelper.WriteLine("[Response]:\n" + _chatCompletion.Content[0].Text);
				_logHelper.WriteLine("----------------------------//MOVE END\\\\----------------------------\n");
			}

		}

		public int GetResponse(string prompt, out string response)
		{
			int responseNumber = 1;
			UserChatMessage message = new UserChatMessage(prompt);
			_messages.Add(message);
			_chatCompletion = _chatClient.CompleteChat(messages: _messages, options: ChatCompletionOptions);

			string number = new string(_chatCompletion.Content[0].Text.SkipWhile(c => !Char.IsDigit(c))
						 .TakeWhile(c => char.IsDigit(c))
						 .ToArray());

			if (!Int32.TryParse(number, out responseNumber))
			{
				Console.WriteLine("[PromptHelper] Invalid input. Defaulting to 1.");
			}
			_messages.Add(new AssistantChatMessage(_chatCompletion));
			response = "[GPT Response] " + _chatCompletion.Content[0].Text;

			return responseNumber;
		}

		public void CreateGameResultString(PlayState state)
		{
			int won = -1;
			string prompt = "The match is over.\n";
			switch (state)
			{
				case PlayState.LOST:
				case PlayState.LOSING:
				case PlayState.CONCEDED:
					prompt += "You lost.";
					won = 0;
					break;
				case PlayState.WINNING:
				case PlayState.WON:
					prompt += "You won!";
					won = 1;
					break;
				default:
					break;
			}
			if (_stats != null)
			{
				_stats.Win = won;
				_stats.SpellsUsed = _player.NumSpellsPlayedThisGame;
				_stats.AgentHealth = _player.Hero.Health;
				_stats.OpponentHealth = _opponent.Hero.Health;
				Console.WriteLine(_stats.ToString());
			}
			GetResponse(prompt, true);
		}

		public void LogToFile(string path = "", string postfix = "")
		{
			using (StreamWriter outputFile = new StreamWriter(Path.Combine(path, "match_log" + postfix + ".txt")))
			{
				foreach (ChatMessage message in _messages)
				{
					if (message is AssistantChatMessage)
					{
						outputFile.WriteLine("[Response]:\n" + message.Content[0].Text);
						outputFile.WriteLine("----------------------------//MOVE END\\\\----------------------------\n");
					}
					else
					{
						outputFile.WriteLine("[Prompt]:\n" + message.Content[0].Text+ "\n");
					}
				}
			}
		}

		internal void SetCustomSystemMessage(string customSystemMessage)
		{
			_customSystemMessage = customSystemMessage;
		}
	}
}
