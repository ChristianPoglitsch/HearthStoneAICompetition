using System;
using System.Collections.Generic;
using SabberStoneCore.Tasks.PlayerTasks;
using SabberStoneBasicAI.PartialObservation;
using OpenAI.Chat;
using SabberStoneContract.Helper;
using SabberStoneCore.Model;
using OpenAI;
using System.ClientModel;
using System.IO;
using SabberStoneCore.Enums;
using CsvHelper;
using System.Globalization;
using SabberStoneAICompetition.src.AIAgents.Helper;

namespace SabberStoneBasicAI.AIAgents.submission_tag
{
	class LLMAgent : AbstractAgent
	{
		private Random _random;

		private ChatClient _chatClient;
		public ChatClient ChatClient { get => _chatClient; set => _chatClient = value; }

		private ChatCompletion _chatCompletion;
		private ChatCompletionOptions _chatCompletionOptions;
		public ChatCompletion ChatCompletion { get => _chatCompletion; set => _chatCompletion = value; }

		private string _model;
		public string Model { get => _model; set => _model = value; }

		private bool _log;
		private string _logPath;

		private int _gameCounter = 1;

		private PromptHelper _helper;

		private LogHelper _logHelper;

		private List<AgentGameStats> _stats = new List<AgentGameStats>();

		private bool _customPromptAfter = false;
		private string _customPrompt = "";
		private string _rulesPath = "rules.txt";

		private bool _replaceRules = false;
		private string _customSystemMessage = "";

		public LLMAgent(ChatClient client, string ruleFile = "rules.txt", bool createLogFiles = false, string logPath = "", string filename = "match_log", string folder = "matches")
		{
			_chatClient = client;
			_log = createLogFiles;
			InitLog(logPath, filename, folder);
		}

		public LLMAgent(string ruleFile = "rules.txt", bool createLogFiles = false, string logPath = "", string filename = "match_log", string folder = "matches")
		{
			_random = new Random();
			string key = "";
			_chatClient = new ChatClient(model: "gpt-4o-mini", apiKey: key);
			_log = createLogFiles;
			InitLog(logPath, filename, folder);
		}

		public LLMAgent(string model, string apiKey, string ruleFile = "rules.txt", bool createLogFiles = false, string logPath = "", string filename = "match_log", string folder = "matches")
		{
			_chatClient = new ChatClient(model: model, apiKey: apiKey);
			_log = createLogFiles;
			InitLog(logPath, filename, folder);
		}

		public LLMAgent(string model, string apiKey, string endpointURI, string ruleFile = "rules.txt", bool createLogFile = false, string logPath = "", string filename = "match_log", string folder = "matches")
		{
			OpenAIClientOptions options = new OpenAIClientOptions();
			options.Endpoint = new Uri(endpointURI);
			ApiKeyCredential credential = new ApiKeyCredential(apiKey);
			_chatClient = new ChatClient(model: model, credential: credential, options: options);
			_log = createLogFile;
			InitLog(logPath, filename, folder);
		}

		public void SetCustomPrompt(string prompt, bool afterInstruction = false)
		{
			_customPrompt = prompt;
			_customPromptAfter = afterInstruction;
		}

		public void SetCustomPromptFromFile(string path, bool afterInstruction = false)
		{
			string result = "";
			try
			{
				StreamReader reader = new StreamReader(path);

				result = reader.ReadToEnd();

				_customPrompt = result;
				_customPromptAfter = afterInstruction;
			}
			catch (IOException e)
			{
				Console.WriteLine("[PromptHelper] The file could not be read:");
				Console.WriteLine(e.Message);
			}
		}

		public void SetCustomSystemMessage(string message, bool replaceRules = false)
		{
			_customSystemMessage = message;
		}

		public void SetCustomSystemMessageFromFile(string path, bool replaceRules = false)
		{
			string result = "";
			try
			{
				StreamReader reader = new StreamReader(path);

				result = reader.ReadToEnd();

				_customSystemMessage = result;
			}
			catch (IOException e)
			{
				Console.WriteLine("[PromptHelper] The file could not be read:");
				Console.WriteLine(e.Message);
			}
		}

		public void SetRulesPath(string path)
		{
			_rulesPath = path;
		}

		private void WriteCsvFile(string filePath)
		{
			using (var writer = new StreamWriter(filePath))
			using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
			{
				csv.WriteRecords(_stats);
			}
		}

		private void InitilizeLogFolder(string path = "", string folder = "matches")
		{
			string folderPath = folder + "_" + DateTime.Now.ToString("dd_MM_yyyy_HH_mm");
			_logPath = Path.Combine(path, folderPath);
			Directory.CreateDirectory(_logPath);
		}

		public override void InitializeAgent()
		{
		}

		public override void FinalizeAgent()
		{
		}

		public override void FinalizeGame()
		{
			if (_helper != null)
			{
				_stats.Add(_helper.Stats);
				WriteCsvFile(Path.Combine(_logPath, "stats.csv"));
			}
			_gameCounter++;
			_helper = null;
		}

		internal void FinalizeGame(PlayState playState)
		{
			if (_helper != null)
			{
				_helper.CreateGameResultString(playState);
				_stats.Add(_helper.Stats);
				WriteCsvFile(Path.Combine(_logPath, "stats.csv"));
			}
			_gameCounter++;
			_helper = null;
		}

		public void SetCompletionOptions(float temperature = 1f, float topP = 1f, float freqPenalty = 0f, float presPenalty = 0f, bool store = false)
		{
			ChatCompletionOptions completionOptions = new ChatCompletionOptions();
			completionOptions.Temperature = temperature;
			completionOptions.TopP = topP;
			completionOptions.FrequencyPenalty = freqPenalty;
			completionOptions.PresencePenalty = presPenalty;
			completionOptions.StoredOutputEnabled = store;
			_chatCompletionOptions = completionOptions;
		}

		private void LogNewGame()
		{
			if (_log)
			{
				_logHelper.Suffix = $"_{_gameCounter}";
				_logHelper.NewWriter();
			}
		}

		private void InitLog(string logPath, string filename, string folder)
		{
			if (_log)
			{
				InitilizeLogFolder(logPath, folder);
				_logHelper = new LogHelper(path: _logPath, filename: filename + "_" + DateTime.Now.ToString("HH_mm_ss_ffffff"));
			}
		}

		public override PlayerTask GetMove(POGame poGame)
		{
			List<PlayerTask> options = poGame.CurrentPlayer.Options();
			Game game = poGame.getGame();
			string prompt = "";

			if (_helper == null)
			{
				LogNewGame();
				_helper = new PromptHelper(ref _chatClient, game, _logHelper, _gameCounter);
				_helper.ChatCompletionOptions = _chatCompletionOptions;
				if (_rulesPath != "")
					_helper.SetRulesPath(_rulesPath);

				if (_customPrompt != "")
					_helper.SetCustomPrompt(_customPrompt, _customPromptAfter);

				if (_customSystemMessage != "")
					_helper.SetCustomSystemMessage(_customSystemMessage);

				_helper.StartChat();
			}
			_helper.UpdateGame(game);
			prompt = _helper.CreateTurnPrefaceString();

			Dictionary<int, string> optionsStrings = new Dictionary<int, string>();
			Dictionary<int, PlayerTask> playerTaskOptions = new Dictionary<int, PlayerTask>() { };
			int index = 1;
			foreach (PlayerTask option in options)
			{
				optionsStrings.Add(index, _helper.CreateStringFromTask(option));
				playerTaskOptions.Add(index, option);
				index++;
			}

			prompt += _helper.CreateOptionsPrompt(optionsStrings, false);
			int playerTaskOptionsId = _helper.GetOptionResponse(prompt, true);
			//int playerTaskOptionsId = _random.Next(1, playerTaskOptions.Count);

			while (playerTaskOptionsId > playerTaskOptions.Count || playerTaskOptionsId < 0)
			{
				string newPrompt = "The option you picked was invalid. Please try again.";
				playerTaskOptionsId = _helper.GetOptionResponse(newPrompt, true);
			}

			_helper.EvaluateAgentChoice(playerTaskOptions[playerTaskOptionsId]);

			return playerTaskOptions[playerTaskOptionsId];
		}

		public override void InitializeGame()
		{
		}
	}
}
