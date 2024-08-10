using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;
using System.Xml.Linq;
using System.Net.Http.Headers;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Text;
using System.Runtime.Remoting.Contexts;
using Discord.Rest;
using System.Drawing.Text;
using System.Security.Policy;

#region Classes
internal class User
{
    public string userId = "";
    public List<Trivia> myTrivias = new List<Trivia>();
    public UserStats userStats = new UserStats();

    public static void SaveAllToJson(List<User> users, string filePath)
    {
        string json = JsonConvert.SerializeObject(users, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }
}
internal class Trivia
{
    public ulong id = 0;
    public int time = 0;
    public int totalTime = 0;
    public string gameType = "normal";
    public int amount = 1;
    public int category = 0;
    public string difficulty = "";
    public string type = "";
    public bool isFinished = false;
    public int answeredQuestionsCount = 0;
    public int score = 0;
    public List<Question> questions = new List<Question>();
    public List<string> answersCorrectness = new List<string>();
    public List<string> userAnswers = new List<string>();
    public int currentQuestionNumber = 0;
    public bool forLobby = false;

    public Trivia()
    {
        string hexGuid = Guid.NewGuid().ToString("N").Substring(0, 16);
        // Ensure that the string is not too long for ulong.Parse
        if (hexGuid.Length > 16)
        {
            hexGuid = hexGuid.Substring(0, 16);
        }

        id = ulong.Parse(hexGuid, System.Globalization.NumberStyles.HexNumber);
    }

}
internal class Question
{
    public int questionNumber = 0;
    public int time { get; set; }
    public object TimeLock = new object();
    public string question = "";
    public string correctAnswer = "";
    public string type = "";
    public string category = "";
    public string difficulty = "";
    public List<string> inCorrectAnswers = new List<string>();
    public List<string> answers = new List<string>();
}
internal class Lobby
{
    public string lobbyName = "";
    public string lobbyId = "";
    public bool isPrivate = true;
    public string type = "";
    public DateTime createDate = DateTime.Now;
    public List<LobbyUser> users = new List<LobbyUser>();
    public ComponentBuilder builder = new ComponentBuilder();
    public EmbedBuilder embed = new EmbedBuilder();
    public SocketSlashCommand arg;
    public bool isFinished = false;
    public int MaxPeople = 2;
    public int amount = 1;
    public int time = 0;
    public bool isEveryoneAnswered=false;
    public Lobby()
    {
        lobbyId = DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
    }
}
internal class LobbyUser : User
{
    public string userName;
    public string _userName;
    public bool isReady = false;
    public bool _isCreator = false;
    public bool update = false;
    public bool isWinner = false;
    public Trivia trivia;
    public List<IDiscordInteraction> args = new List<IDiscordInteraction>();
    public bool isCreator
    {
        get { return _isCreator; }
        set
        {
            _isCreator = value;
            if (value == true)
            {
                isReady = true;
            }
        }
    }
}
public class UserStats
{
    public Achievements userAchievements = new Achievements();
    public int unlockedAchivementsCount = 0;
    public HashSet<string> examinedCategories = new HashSet<string>();
    private int _xp = 0;
    public int currXp = 0;
    public int level = 1;
    public int xp
    {
        get { return _xp; }
        set
        {
            currXp = value;
            _xp = value;

            if (level <= 40)
            {
                if (_xp >= CalculateRequiredXP(level))
                {
                    currXp -= CalculateRequiredXP(level);
                    level++;
                    
                }
            }
        }
    }
    public int neededXpForNextLevel
    {
        get { return CalculateRequiredXP(level); }
    }
    private static int CalculateRequiredXP(int level)
    {
       return level * (level + 1) / 2 * 50;
    }
}
public class Achievements
{
    public Dictionary<string, int> categories = new Dictionary<string, int> { { "General Knowledge", 0 },
        { "Entertainment: Books", 0 }, { "Entertainment: Film", 0 }, { "Entertainment: Music", 0 }, { "Entertainment: Musicals & Theatres", 0 }, { "Entertainment: Television", 0 }, { "Entertainment: Video Games", 0 }, { "Entertainment: Board Games", 0 }, { "Science & Nature", 0 }, { "Science: Computers", 0 }, { "Science: Mathematics", 0 }, { "Mythology", 0 }, { "Sports", 0 }, { "Geography", 0 }, { "History", 0 }, { "Politics", 0 }, { "Art", 0 }, { "Celebrities", 0 }, { "Animals", 0 }, { "Vehicles", 0 }, { "Entertainment: Comics", 0}, { "Science: Gadgets", 0 }, { "Entertainment: Japanese Anime & Manga",0  }, { "Entertainment: Cartoon & Animations", 0 } };
}
#endregion

internal static class Program
{
    #region Variables
    public static DiscordSocketClient _client;
    public static string botToken = "";
    public static ulong guildId = 0;
    public static DiscordSocketConfig config = new DiscordSocketConfig();

    public static List<User> _users = LoadTriviasFromJson("TriviasData.json");
    public static List<Lobby> lobies = new List<Lobby>();
    public static Dictionary<string, string> categoriesDic = new Dictionary<string, string> { { "General Knowledge", "9" }, 
        { "Entertainment: Books", "10" }, { "Entertainment: Film", "11" }, { "Entertainment: Music", "12" }, { "Entertainment: Musicals & Theatres", "13" }, { "Entertainment: Television", "14" }, { "Entertainment: Video Games", "15" }, { "Entertainment: Board Games", "16" }, { "Science & Nature", "17" }, { "Science: Computers", "18" }, { "Science: Mathematics", "19" }, { "Mythology", "20" }, { "Sports", "21" }, { "Geography", "22" }, { "History", "23" }, { "Politics", "24" }, { "Art", "25" }, { "Celebrities", "26" }, { "Animals", "27" }, { "Vehicles", "28" }, { "Entertainment: Comics", "29" }, { "Science: Gadgets", "30" }, { "Entertainment: Japanese Anime & Manga", "31" }, { "Entertainment: Cartoon & Animations", "32" } };
    public static Dictionary<string, string> difficultiesDic = new Dictionary<string, string>() { { "Easy", "easy" }, { "Medium", "medium" }, { "Hard", "hard" } };
    public static Dictionary<string, string> typesDic = new Dictionary<string, string>() { { "Multiple Choice", "multiple" }, { "True/False", "boolean" } };
    #endregion

    #region Main
    public static void Main()
    {
        MainAsync().GetAwaiter().GetResult();
    }
    public static async Task MainAsync()
    {
        try
        {
            _client = new DiscordSocketClient(config);

            _client.Ready += _client_Ready;
            _client.SlashCommandExecuted += _client_SlashCommandExecuted;
            _client.ButtonExecuted += _client_ButtonExecuted;
            _client.SelectMenuExecuted += _client_SelectMenuExecuted;
            _client.ModalSubmitted += _client_ModalSubmitted;
            _client.Log += _client_Log;

            await _client.LoginAsync(TokenType.Bot, botToken);
            await _client.StartAsync();

            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MainAsync: {ex.Message}");
        }
    }
    #endregion

    #region Clients
    private static Task _client_Log(LogMessage arg) 
    {
        Console.WriteLine($"{arg.Message}");
        return Task.CompletedTask;
    }
    private static async Task _client_Ready()
    {
        try
        {
            SlashCommandOptionBuilder categories = new SlashCommandOptionBuilder()
                    .WithName("category")
                    .WithDescription("Choose a category")
                    .WithType(ApplicationCommandOptionType.Integer);
            SlashCommandOptionBuilder difficulties = new SlashCommandOptionBuilder()
                     .WithName("difficulty")
                     .WithDescription("Select Difficulty")
                     .WithType(ApplicationCommandOptionType.String);
            SlashCommandOptionBuilder types = new SlashCommandOptionBuilder()
                    .WithName("type")
                    .WithDescription("Select Type")
                     .WithType(ApplicationCommandOptionType.String);
            SlashCommandOptionBuilder lobbygametypes = new SlashCommandOptionBuilder()
                    .WithName("type")
                    .WithDescription("Chosoe a type")
                    .AddChoice("First Answer", "firstanswer")
                    .AddChoice("Max Point", "maxpoint")
                    .WithType(ApplicationCommandOptionType.String)
                    .WithRequired(true);

            foreach (var item in categoriesDic)
            {
                categories.AddChoice(item.Key, item.Value);
            }
            foreach (var item in difficultiesDic)
            {
                difficulties.AddChoice(item.Key, item.Value);
            }
            foreach (var item in typesDic)
            {
                types.AddChoice(item.Key, item.Value);
            }

            SlashCommandBuilder trivia = new SlashCommandBuilder()
                .WithName("trivia")
                .WithDescription("Lets create trivia quiz")
                .AddOption("amount", ApplicationCommandOptionType.Integer, "How much question do you want?", isRequired: true)
                .AddOption(categories)
                .AddOption(difficulties)
                .AddOption(types);
            SlashCommandBuilder triviaWithTime = new SlashCommandBuilder()
                .WithName("trivia-with-time")
                .WithDescription("Lets create trivia quiz with time")
                .AddOption("time", ApplicationCommandOptionType.Integer, "How many seconds do you want per question?", isRequired: true)
                .AddOption("amount", ApplicationCommandOptionType.Integer, "How much question do you want?", isRequired: true)
                .AddOption(categories)
                .AddOption(difficulties)
                .AddOption(types);
            SlashCommandBuilder trivias = new SlashCommandBuilder()
                .WithName("trivias")
                .WithDescription("See trivias which you solved before");
            SlashCommandBuilder lobbycreate = new SlashCommandBuilder()
               .WithName("lobby-create")
               .WithDescription("Lets create a lobby")
               .AddOption("name", ApplicationCommandOptionType.String, "Give a name to your lobby", isRequired: true)
               .AddOption(lobbygametypes)
               .AddOption("max-participant-count", ApplicationCommandOptionType.Integer, "How much participants do you want?", isRequired: true)
               .AddOption("is-private", ApplicationCommandOptionType.Boolean, "private or public", isRequired: true)
               .AddOption("amount", ApplicationCommandOptionType.Integer, "How much question do you want?", isRequired: true)
               .AddOption("time", ApplicationCommandOptionType.Integer, "How many seconds do you want per question?");
            SlashCommandBuilder lobbyjoin = new SlashCommandBuilder()
                 .WithName("lobby-join")
                 .WithDescription("Lets join a lobby")
                 .AddOption("id", ApplicationCommandOptionType.String, "input an id");
            SlashCommandBuilder lobbylist = new SlashCommandBuilder()
                .WithName("lobby-list")
                .WithDescription("See all lobies");
            SlashCommandBuilder profile = new SlashCommandBuilder()
                .WithName("profile")
                .WithDescription("See your profie");

            _ = await _client.CreateGlobalApplicationCommandAsync(trivia.Build());
            _ = await _client.CreateGlobalApplicationCommandAsync(triviaWithTime.Build());
            _ = await _client.CreateGlobalApplicationCommandAsync(trivias.Build());
            _ = await _client.CreateGlobalApplicationCommandAsync(lobbycreate.Build());
            _ = await _client.CreateGlobalApplicationCommandAsync(lobbyjoin.Build());
            _ = await _client.CreateGlobalApplicationCommandAsync(lobbylist.Build());
            _ = await _client.CreateGlobalApplicationCommandAsync(profile.Build());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in clientReady: {ex.Message}");
        }
    }
    private static async Task _client_SlashCommandExecuted(SocketSlashCommand arg)
    {
        try
        {
            switch (arg.Data.Name)
            {
                case "trivia":
                    var (myTrivia, jObject) = await createTrivia(arg, false);
                    if (myTrivia.amount < 1 || myTrivia.amount > 25 )
                    {
                        await arg.RespondAsync("please enter values ​​within the appropriate range",ephemeral:true);
                    }
                    else
                    {
                        createQuestions(myTrivia, jObject);
                        showQuestions(arg, myTrivia.id, false,null);
                    }
                    break;
                case "trivia-with-time":
                    (myTrivia, jObject) = await createTrivia(arg, true);
                    if (myTrivia.amount < 1 || myTrivia.amount > 25 && myTrivia.time < 1 || myTrivia.time > 30)
                    {
                        await arg.RespondAsync("please enter values ​​within the appropriate range", ephemeral: true);
                    }
                    else
                    {
                        createQuestions(myTrivia, jObject);
                        showQuestions(arg, myTrivia.id, false,null);
                    }
                  
                    break;
                case "trivias":
                    await pastTrivias(arg);
                    break;
                case "lobby-create":
                    await createLobby(arg);
                    break;
                case "lobby-list":
                    await seeLobbyList(arg);
                    break;
                case "lobby-join":
                    await joinLobby(arg);
                    break;
                case "profile":
                    await seeProfile(arg);
                    break;
                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in slachcommand: {ex}");
        }
    }
    private static async Task _client_ButtonExecuted(SocketMessageComponent arg)
    {
        try
        {
            //Console.WriteLine($"{arg.Data.CustomId}");
            if (arg.Data.CustomId.Split('#')[0] == "next" || arg.Data.CustomId.Split('#')[0] == "previous")
            {
                arg.DeferAsync();
                changeCurrentQuestionNumber(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString())); return;
            }
            else if (arg.Data.CustomId == "save")
            {
                arg.DeferAsync();
                User.SaveAllToJson(_users, "TriviasData.json");
            }
            else if (arg.Data.CustomId.Split('#')[0] == "ready")
            {
                await changeUserReady(arg);
            }
            else if (arg.Data.CustomId.Split('#')[0] == "leave")
            {
                await leaveLobby(arg);
                await arg.DeferAsync();
            }
            else if (arg.Data.CustomId.Split('#')[0] == "start")
            {
                startLobby(arg.Data.CustomId.Split('#')[1], arg);return;
            }
            else
            {
                arg.DeferAsync();
                if (arg.Data.CustomId.Split('#').Count()==3)
                {
                    Console.WriteLine(arg.Data.CustomId + " ilk");
                    correctControl(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString()), ulong.Parse(arg.Data.CustomId.Split('#')[3].ToString())); return;
                }
                else
                {
                    Console.WriteLine(arg.Data.CustomId + " ikinci");
                    correctControl(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString()),null); return;
                }
            }
        }
        catch (Exception ex)
        {
            if (ex.ToString() != "The server responded with error 50006: Cannot send an empty message")
            {
                Console.WriteLine($"Error in buttonexecuted: {ex}");
            }
        }
    }
    private static async Task _client_ModalSubmitted(SocketModal arg)
    {
        try
        {
            Console.WriteLine(arg.Data.CustomId.Split('#')[0].ToString());
            switch (arg.Data.CustomId.Split('#')[0].ToString())
            {
                case "amount":

                    await changeLobbyQuestionAmount(arg, arg.Data.CustomId.Split('#')[1].ToString());
                    break;
                case "time":
                    await changeLobbySecondsPerQuestion(arg, arg.Data.CustomId.Split('#')[1].ToString());
                    break;
                default:
                    break;
            }
            await arg.DeferAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in modalsubmitted {ex}");
        }
       
    }
    private static async Task _client_SelectMenuExecuted(SocketMessageComponent arg)
    {
        try
        {
            switch (arg.Data.CustomId.Split('#')[0].ToString())
            {
                case "questions":
                    await selectMenuHandlerQuestion(arg, ulong.Parse(arg.Data.CustomId.Split('#')[1].ToString()), ulong.Parse(arg.Data.CustomId.Split('#')[2].ToString()));
                    break;
                case "trivias":
                    await selectMenuHandlerPastTrivias(arg);
                    break;
                case "lobbies":
                    await selectMenuHandlerLobbiesList(arg);
                    break;
                case "configure":
                    await selectMenuHandlerConfigureLobbyParameters(arg, arg.Data.CustomId.Split('#')[1].ToString());
                    break;
                case "kick":
                    await selectMenuHandlerKickUsersFromLobby(arg);
                    break;
                case "host":
                    await selectMenuHandlerMakeHostTheUser(arg);
                    break;
                case "profile-processes":
                    await selectMenuHandlerProfileProcesses(arg);
                    break;

                default:
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in selectmenuexecuted: {ex}");

        }
        await arg.DeferAsync();
    }
    #endregion

    #region Lobby
    public static async Task changeLobbyQuestionAmount(SocketModal arg, string id)
    {
        Lobby lobby = lobies.Find(c => c.lobbyId == id);
        var amountComponent = arg.Data.Components?.FirstOrDefault(x => x.CustomId == "amount");


        
            if (amountComponent != null && int.TryParse(amountComponent.Value.ToString(), out int amount))
            { 
                 if (amount < 1 || amount > 25)
                 {
                await arg.RespondAsync("please enter values ​​within the appropriate range",ephemeral:true);
                 }
                      
            else
            {
                Console.WriteLine(amount);
                lobby.amount = amount;
                Console.WriteLine(lobby.amount);
                lobby.embed = UpdateLobbyEmbed(lobby);
                lobby.builder = UpdateLobbyBuilder(lobby);

                foreach (var user in lobby.users)
                {
                    if (!user.isCreator)
                    {
                        foreach (var a in user.args)
                        {
                            await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                            {
                                res.Embed = lobby.embed.Build();
                            });
                        }
                    }
                }
                await lobby.arg.ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = lobby.embed.Build();
                    res.Components = lobby.builder.Build();
                });
            }

        }
            else
            {
                Console.WriteLine("Failed to retrieve amount component from modal.");
            }
        

       
    }
    public static async Task changeLobbySecondsPerQuestion(SocketModal arg, string id)
    {
        Lobby lobby = lobies.Find(c => c.lobbyId == id);

        var timeComponent = arg.Data.Components?.FirstOrDefault(x => x.CustomId == "time");
       
      
            if (timeComponent != null && int.TryParse(timeComponent.Value.ToString(), out int time))
            {
            if (time < 1 || time > 25)
            {
                await arg.RespondAsync("please enter values ​​within the appropriate range", ephemeral: true);
            }
            else
            {
                lobby.time = time;
                lobby.embed = UpdateLobbyEmbed(lobby);
                lobby.builder = UpdateLobbyBuilder(lobby);
                await lobby.arg.ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = lobby.embed.Build();
                    res.Components = lobby.builder.Build();
                });

                foreach (var user in lobby.users)
                {
                    if (!user.isCreator)
                    {
                        foreach (var a in user.args)
                        {
                            await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                            {
                                res.Embed = lobby.embed.Build();
                            });
                        }
                    }

                }
            }
                
            }
            else
            {
                // Handle the case where timeComponent is null or parsing fails
                Console.WriteLine("Failed to retrieve time component from modal.");
                // You might want to throw an exception or handle this case according to your application logic.
            }
        
      
    }
    public static async Task selectMenuHandlerLobbiesList(SocketMessageComponent arg)
    {
        string lobbyId = arg.Data.Values.FirstOrDefault().ToString();
        LobbyUser lobbyUser = new LobbyUser();
        foreach (var item in lobies)
        {
            if (lobbyId == item.lobbyId && item.users.Count < item.MaxPeople)
            {
                if (!item.users.Exists(c => c.userId == arg.User.Id.ToString()))
                {
                    lobbyUser.userId = arg.User.Id.ToString();
                    lobbyUser.userName = arg.User.Mention;
                    item.users.Add(lobbyUser);
                    lobbyUser.args.Add(arg);
                    await arg.RespondAsync($"lobiye katıldınız ", ephemeral: true);
                    item.embed = UpdateLobbyEmbed(item);
                    item.builder = UpdateLobbyBuilder(item);
                    await item.arg.ModifyOriginalResponseAsync(res =>
                    {
                        res.Embed = item.embed.Build();
                        res.Components = item.builder.Build();
                    });
                    await arg.RespondAsync(components: item.builder.Build(), embed: item.embed.Build(), ephemeral: true);

                    foreach (var user in item.users)
                    {
                        if (!user.isCreator)
                        {
                            foreach (var a in user.args)
                            {
                                if (!user.isCreator)
                                {
                                    await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                                    {
                                        res.Embed = item.embed.Build();
                                    });
                                }

                            }
                        }

                    }
                    return;
                }
                else
                {
                    await arg.RespondAsync($"lobide zaten varsınız ", ephemeral: true);
                }
            }
            else
            {
                await arg.RespondAsync("uygun lobi bulunamadı", ephemeral: true);
            }
        }
    }
    public static async Task selectMenuHandlerConfigureLobbyParameters(SocketMessageComponent arg, string id)
    {
        try
        {
            string selectedOption = arg.Data.Values.FirstOrDefault().ToString();
            Lobby lobby = lobies.Find(c => c.lobbyId == id);
            switch (selectedOption)
            {
                case "amount":
                    ModalBuilder changeQuestionAmount = new ModalBuilder()
                         .WithTitle("Change Question Amount")
                         .WithCustomId("amount#" + id)
                         .AddTextInput("Only integer between 1 and 25 !!", "amount", placeholder: "Input question amount")
                        ;
                    if (lobby.amount < 1 || lobby.amount > 25 )
                    {
                        await arg.RespondAsync("please enter values ​​within the appropriate range", ephemeral: true);
                    }
                    else
                    {
                        await arg.RespondWithModalAsync(modal: changeQuestionAmount.Build());
                    }
                    break;
                case "time":
                    ModalBuilder changeTime = new ModalBuilder()
                        .WithTitle("Change Seconds per question")
                        .WithCustomId("time#" + id)
                        .AddTextInput("Only integer between 1 and 30 !!", "time", placeholder: "Input seconds per question")
                       ;
                    if (lobby.time < 1 || lobby.time > 30)
                    {
                        await arg.RespondAsync("please enter values ​​within the appropriate range", ephemeral: true);
                    }
                    else
                    {
                        await arg.RespondWithModalAsync(modal: changeTime.Build());
                    }

                    break;
                case "type":
                    lobby.isPrivate = !lobby.isPrivate;
                    break;
                case "mode":
                    lobby.type = lobby.type == "firstanswer" ? "maxpoint" : "firstanswer";
                    break;
                default:
                    break;
            }
            lobby.embed = UpdateLobbyEmbed(lobby);
            lobby.builder = UpdateLobbyBuilder(lobby);


            foreach (var user in lobby.users)
            {
                if (!user.isCreator)
                {
                    foreach (var a in user.args)
                    {
                        if (!user.isCreator)
                        {
                            await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                            {
                                res.Embed = lobby.embed.Build();
                            });
                        }
                    }
                }

            }

            await lobby.arg.ModifyOriginalResponseAsync(res =>
            {
                res.Embed = lobby.embed.Build();
                res.Components = lobby.builder.Build();
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in selectmenuhandlerconfigureparameters: {ex.Message}");
        }
    }
    public static async Task selectMenuHandlerKickUsersFromLobby(SocketMessageComponent arg)
    {
        string selectedOption = arg.Data.Values.FirstOrDefault().ToString();
        //selectedOption.Split('#')[1]
        Lobby lobby = lobies.Find(c => c.lobbyId == arg.Data.CustomId.Split('#')[1]);
        LobbyUser user = lobby.users.Find(u => u.userId == selectedOption.Split('#')[1].ToString());
        LobbyUser admin = lobby.users.Find(u => u.isCreator);
        lobby.users.RemoveAll(u => u.userId == selectedOption.Split('#')[1].ToString());
        ComponentBuilder builder = new ComponentBuilder();
        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle($"You kickedd from lobby by {admin}")
            .WithDescription("See you next lobbies")
            .WithColor(Discord.Color.Gold);
        foreach (var u in lobby.users)
        {
            if (!u.isCreator)
            {
                foreach (var a in user.args)
                {
                    await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                    {
                        res.Embed = embed.Build();
                        res.Components = builder.Build();
                    });
                }
            }
        }
        EmbedBuilder lobbyEmbed = UpdateLobbyEmbed(lobby);
        ComponentBuilder lobbyBuilder = UpdateLobbyBuilder(lobby);
        await lobby.arg.ModifyOriginalResponseAsync(res =>
        {
            res.Embed = lobbyEmbed.Build();
            res.Components = lobbyBuilder.Build();
        });
    }
    public static async Task selectMenuHandlerMakeHostTheUser(SocketMessageComponent arg)
    {
        string selectedOption = arg.Data.Values.FirstOrDefault().ToString();
        Lobby lobby = lobies.Find(c => c.lobbyId == arg.Data.CustomId.Split('#')[1]);
        LobbyUser user = lobby.users.Find(u => u.userId == selectedOption.Split('#')[1].ToString());
        LobbyUser admin = lobby.users.Find(u => u.isCreator);
        admin.isCreator = false;
        admin.isReady = false;
        user.isCreator = true;
        user.isReady = true;
        EmbedBuilder lobbyEmbed = UpdateLobbyEmbed(lobby);
        ComponentBuilder lobbyBuilder = UpdateLobbyBuilder(lobby);
        ComponentBuilder builder = UpdateJoinLobbyBuilder(admin, lobby);
        foreach (var u in lobby.users)
        {
            if (!u.isCreator)
            {
                foreach (var a in user.args)
                {
                    await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                    {
                        res.Embed = lobbyEmbed.Build();
                        res.Components = lobbyBuilder.Build();

                    });
                }
            }
        }
        await lobby.arg.ModifyOriginalResponseAsync(res =>
        {
            res.Embed = lobbyEmbed.Build();
            res.Components = builder.Build();
        });
        foreach (var u in user.args)
        {
            lobby.arg = u as SocketSlashCommand;
        }
    }
    public static async Task startLobby(string lobbyId, SocketMessageComponent arg)
    {
        Lobby lobby = lobies.Find(c => c.lobbyId == lobbyId);

        var (myTrivia, jObject) = await createTriviaForLobby(true, lobby);
        createQuestions(myTrivia, jObject);

        foreach (var u in lobby.users)
        {
            //Console.WriteLine($"{u._userName} {u.trivia.id} {u.trivia.forLobby}");
            u.trivia.questions = myTrivia.questions;
            u.trivia.questions.ForEach(c => u.trivia.answersCorrectness.Add("a"));
            u.trivia.questions.ForEach(c => u.trivia.userAnswers.Add("ğ"));
            u.trivia.questions.ForEach(c=>c.time=u.trivia.time);
            //Console.WriteLine(u.userId + " nin quesitons sayısı " + u.trivia.questions.Count);
            u.trivia.questions.ForEach(c => Console.WriteLine(u.trivia.id + "  " + c.time));


            foreach (var a in u.args)
            {
                await Console.Out.WriteLineAsync("arg : "+ a);
                Console.WriteLine("b : " + u.trivia.id);
                showQuestions(a, u.trivia.id, false,ulong.Parse(lobby.lobbyId));
            }
        }
    }
    public static async Task leaveLobby(SocketMessageComponent arg)
    {
        Lobby lobby = lobies.Find(c => c.lobbyId == arg.Data.CustomId.Split('#')[1]);
        LobbyUser user = lobby.users.Find(u => u.userId == arg.User.Id.ToString());
        lobby.users.RemoveAll(u => u.userId == arg.User.Id.ToString());


        Console.WriteLine(lobby.users.Count);
        EmbedBuilder embed = new EmbedBuilder()
            .WithTitle("You leaved from lobby")
            .WithDescription("See you next lobbies")
            .WithColor(Discord.Color.Gold);
        ComponentBuilder builder = new ComponentBuilder();
        EmbedBuilder lobbyEmbed = UpdateLobbyEmbed(lobby);
        ComponentBuilder lobbyBuilder = UpdateLobbyBuilder(lobby);

        if (user.isCreator)
        {
            user.isCreator = false;
            Console.WriteLine(lobby.users.Count);
            if (lobby.users.Count > 1)
            {
                lobby.users[0].isCreator = true;
                foreach (var u in lobby.users)
                {
                    if (!u.isCreator)
                    {
                        foreach (var a in u.args)
                        {
                            await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                            {
                                res.Embed = lobbyEmbed.Build();
                            });
                        }
                    }
                }
                await lobby.arg.ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = embed.Build();
                    res.Components = builder.Build();
                });
            }
            else if (lobby.users.Count == 1)
            {
                lobby.users[0].isCreator = true;
                EmbedBuilder lobbyEmbed1 = UpdateLobbyEmbed(lobby);

                foreach (var u in lobby.users)
                {
                    if (u.isCreator)
                    {
                        Console.WriteLine(u._userName);
                        foreach (var a in u.args)
                        {
                            await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                            {
                                res.Embed = lobbyEmbed1.Build();
                                res.Components = lobbyBuilder.Build();
                            });
                        }
                    }
                    await lobby.arg.ModifyOriginalResponseAsync(res =>
                    {
                        res.Embed = embed.Build();
                        res.Components = builder.Build();
                    });
                }

            }


        }
        else
        {
            foreach (var a in user.args)
            {
                await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = embed.Build();
                    res.Components = builder.Build();
                });
            }

            foreach (var u in lobby.users)
            {

                if (!u.isCreator)
                {
                    foreach (var a in u.args)
                    {
                        await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = lobbyEmbed.Build();
                        });
                    }
                }
                else
                {
                    foreach (var a in u.args)
                    {
                        await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = lobbyEmbed.Build();
                            res.Components = lobbyBuilder.Build();
                        });
                    }
                }


            }
        }


    }
    public static async Task changeUserReady(SocketMessageComponent arg)
    {
        LobbyUser user = lobies.SelectMany(l => l.users).FirstOrDefault(u => u.userId.ToString() == arg.Data.CustomId.Split('#')[1].ToString());
        Lobby item = lobies.Find(c => c.lobbyId == arg.Data.CustomId.Split('#')[2].ToString());
        user.isReady = !user.isReady;
        Console.WriteLine($"{user._userName} isReady is {user.isReady}");

        await arg.DeferAsync();
        item.embed = UpdateLobbyEmbed(item);
        item.builder = UpdateLobbyBuilder(item);
        await item.arg.ModifyOriginalResponseAsync(res =>
        {
            res.Embed = item.embed.Build();
            res.Components = item.builder.Build();
        });

        ComponentBuilder builder = UpdateJoinLobbyBuilder(user, item);
        foreach (var u in item.users)
        {
            if (!u.isCreator)
            {
                foreach (var a in user.args)
                {
                    Console.WriteLine(a.User.Username);
                    await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                    {

                        res.Embed = item.embed.Build();
                        res.Components = builder.Build();
                    });
                }
            }
        }
    }
    private static async Task<(Trivia, JObject)> createTriviaForLobby(bool isTimed, Lobby lobby)
    {
        int amount = lobby.amount;
        Trivia myTrivia = new Trivia
        {
            amount = amount,
            currentQuestionNumber = 0,
            forLobby = true
        };

        if (isTimed)
        {
            int time = lobby.time;
            myTrivia.time = time;
            myTrivia.gameType = "timed";
        }
        else
        {
            myTrivia.gameType = "normal";
        }


        foreach (LobbyUser lobbyUser in lobby.users)
        {
            User existingUser = _users.FirstOrDefault(u => u.userId == lobbyUser.userId);
            if (existingUser == null)
            {
                _users.Add(new User
                {
                    userId = lobbyUser.userId,
                });
            }

            // Create a new instance of Trivia for each user with a unique ID
            Trivia userTrivia = new Trivia
            {
                amount = myTrivia.amount,
                currentQuestionNumber = 0,
                forLobby = myTrivia.forLobby,
                gameType = myTrivia.gameType,
                time = myTrivia.time
            };
           
            lobbyUser.trivia = userTrivia;
            lobbyUser.myTrivias.Add(userTrivia);
            _users.Find(c => c.userId == lobbyUser.userId).myTrivias.Add(userTrivia);
            Console.WriteLine(lobbyUser.userId+" nın trivia id : " + lobbyUser.trivia.id);
        }

        string url = $"https://opentdb.com/api.php?amount={myTrivia.amount}";
        RestClient client = new RestClient(url);
        RestRequest request = new RestRequest();
        RestResponse response = client.Get(request);
        string decodedResponse = WebUtility.HtmlDecode(response.Content.ToString().Replace("&quot;", "'"));
        JObject jObject = JObject.Parse(decodedResponse);

        return (myTrivia, jObject);
    }
    public static async Task seeLobbyList(SocketSlashCommand arg)
    {

        EmbedBuilder lobbyListEmbed = new EmbedBuilder()
            .WithTitle("Here is a list of all lobies which you can join")
            .WithColor(Discord.Color.Gold);
        ;
        SelectMenuBuilder lobbyMenu = new SelectMenuBuilder()
            .WithPlaceholder("Select a lobby")
            .WithCustomId("lobbies#")
            .WithMinValues(1)
            .WithMaxValues(1)
            ;


        if (lobies.Count==0)
        {
            lobbyListEmbed.WithDescription("There are currently no lobbies you can join 😔");
            await arg.RespondAsync(embed: lobbyListEmbed.Build(), ephemeral: true);
        }
        else
        {
            Console.WriteLine($"{lobies.Count}");
            if (lobies.Count(c => !c.isPrivate)==0)
            {
                lobbyListEmbed.WithDescription("There are currently no lobbies you can join 😔");
                await arg.RespondAsync(embed: lobbyListEmbed.Build(), ephemeral: true);
            }
            else 
            {
                lobies.FindAll(c=>!c.isPrivate).ForEach(c => lobbyMenu.AddOption($"{c.lobbyName}", $"{c.lobbyId}"));
                ComponentBuilder builder = new ComponentBuilder().WithSelectMenu(lobbyMenu);
                await arg.RespondAsync(components: builder.Build(), embed: lobbyListEmbed.Build(), ephemeral: true);
            }
          
        }

    }
    public static async Task joinLobby(SocketSlashCommand arg)
    {
        LobbyUser lobbyUser = new LobbyUser();

        if (arg.Data.Options.ToList().Exists(c => c.Name == "id"))
        {
            string lobbyId = arg.Data.Options.FirstOrDefault(option => option.Name == "id").Value.ToString();

            foreach (var item in lobies)
            {
                if (lobbyId == item.lobbyId)
                {
                    if (item.users.Count < item.MaxPeople)
                    {
                        if (!item.users.Exists(c => c.userId == arg.User.Id.ToString()))
                        {
                            lobbyUser.userId = arg.User.Id.ToString();
                            lobbyUser.userName = arg.User.Mention;
                            lobbyUser._userName = arg.User.Username;
                            item.users.Add(lobbyUser);
                            item.embed = UpdateLobbyEmbed(item);
                            item.builder = UpdateLobbyBuilder(item);
                            lobbyUser.args.Add(arg);
                            await item.arg.ModifyOriginalResponseAsync(res =>
                            {
                                res.Embed = item.embed.Build();
                                res.Components = item.builder.Build();
                            });

                            ButtonBuilder ready = new ButtonBuilder()
                                .WithLabel("READY")
                                .WithCustomId("ready#" + lobbyUser.userId + "#" + item.lobbyId)
                                .WithStyle(ButtonStyle.Success);
                            ButtonBuilder leave = new ButtonBuilder()
                                .WithLabel("LEAVE")
                                .WithCustomId("leave#" + item.lobbyId + "#" + lobbyUser.userId)
                                .WithStyle(ButtonStyle.Danger);
                            ComponentBuilder builder = UpdateJoinLobbyBuilder(lobbyUser, item);
                            await arg.RespondAsync(components: builder.Build(), embed: item.embed.Build(), ephemeral: true);

                            foreach (var user in item.users)
                            {
                                if (user.userId != arg.User.Id.ToString() && !user.isCreator)
                                {
                                    foreach (var a in user.args)
                                    {
                                        await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                                        {
                                            res.Embed = item.embed.Build();
                                            res.Components = builder.Build();
                                        });
                                    }
                                }
                            }

                            return;
                        }
                        else
                        {
                            await arg.RespondAsync($"Lobide zaten varsınız.", ephemeral: true);
                        }
                    }
                    else
                    {
                        await arg.RespondAsync($"Lobi dolu, yeni kişiler katılamaz.", ephemeral: true);
                    }
                }
            }

            await arg.RespondAsync("Belirtilen ID'ye sahip lobi bulunamadı", ephemeral: true);
        }
        else
        {
            foreach (var item in lobies)
            {
                if (item.users.Count < item.MaxPeople)
                {
                    if (!item.users.Exists(c => c.userId == arg.User.Id.ToString()))
                    {
                        lobbyUser.userId = arg.User.Id.ToString();
                        lobbyUser.userName = arg.User.Mention;
                        lobbyUser._userName = arg.User.Username;
                        item.users.Add(lobbyUser);
                        item.embed = UpdateLobbyEmbed(item);
                        item.builder = UpdateLobbyBuilder(item);
                        lobbyUser.args.Add(arg);
                        await item.arg.ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = item.embed.Build();
                            res.Components = item.builder.Build();
                        });

                        ButtonBuilder ready = new ButtonBuilder()
                            .WithLabel("READY")
                            .WithCustomId("ready#" + lobbyUser.userId + "#" + item.lobbyId)
                            .WithStyle(ButtonStyle.Success);
                        ButtonBuilder leave = new ButtonBuilder()
                            .WithLabel("LEAVE")
                            .WithCustomId("leave#" + item.lobbyId + "#" + lobbyUser.userId)
                            .WithStyle(ButtonStyle.Danger);
                        ComponentBuilder builder = UpdateJoinLobbyBuilder(lobbyUser, item);
                        await arg.RespondAsync(components: builder.Build(), embed: item.embed.Build(), ephemeral: true);

                        foreach (var user in item.users)
                        {
                            if (user.userId != arg.User.Id.ToString() && !user.isCreator)
                            {
                                foreach (var a in user.args)
                                {
                                    await (a as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                                    {
                                        res.Embed = item.embed.Build();
                                        res.Components = builder.Build();
                                    });
                                }
                            }
                        }

                        return;
                    }
                    else
                    {
                        await arg.RespondAsync($"Lobide zaten varsınız.", ephemeral: true);
                    }
                }
                else
                {
                    await arg.RespondAsync($"Lobi dolu, yeni kişiler katılamaz.", ephemeral: true);
                }
            }
        }
    }
    public static async Task createLobby(SocketSlashCommand arg)
    {
        try
        {
            Lobby myLobby = new Lobby();
            LobbyUser user = new LobbyUser
            {
                userId = arg.User.Id.ToString(),
                userName = arg.User.Mention,
                _userName = arg.User.Username,
                isCreator = true,
            };
            user.args.Add(arg);
            int time = 10;
            string name = arg.Data.Options.FirstOrDefault(option => option.Name == "name").Value.ToString();
            string type = arg.Data.Options.FirstOrDefault(option => option.Name == "type").Value.ToString();
            int maxParticipantCount = int.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "max-participant-count").Value.ToString());
            bool isPrivate = bool.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "is-private").Value.ToString());
            int amount = int.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "amount").Value.ToString());
            if (arg.Data.Options.ToList().Exists(c => c.Name == "time"))
            {
                time = int.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "time").Value.ToString());
            }
            myLobby.time = time;

            myLobby.lobbyName = name;
            myLobby.type = type;
            myLobby.MaxPeople = maxParticipantCount;
            myLobby.amount = amount;
            myLobby.isPrivate = isPrivate;
            myLobby.users.Add(user);

            if (myLobby.amount < 1 || myLobby.amount > 25 || myLobby.time<1 || myLobby.time>30)
            {
                await arg.RespondAsync("please enter values ​​within the appropriate range", ephemeral: true);
            }
            else 
            {
                lobies.Add(myLobby);
                await showLobby(arg, myLobby.lobbyId);
            }

           
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in createLobby: {ex.Message}");
        }
    }
    public static async Task showLobby(IDiscordInteraction arg, string id)
    {
        Lobby lobby = lobies.Find(c => c.lobbyId == id);

        string lobbyType = lobby.isPrivate ? "private" : "public";
        string participantNames = string.Join("\n", lobby.users.Select(c => c.isCreator ? $"{c.userName} 👑" : c.userName));
        string lobbyTypeEmoji = lobby.isPrivate ? "🕵️" : "👨‍👩‍👧";
        string gameModeEmoji = lobby.type == "firstanswer" ? "⏱️" : "💯";


        EmbedBuilder lobbyEmbed = new EmbedBuilder()
            .WithTitle($"{lobby.lobbyName}")
            .WithDescription($"Game Type : {lobby.type}\nQuestion Count : {lobby.amount}\nLobby Type : {lobbyType} \n Participants :\n {participantNames}")
            .WithFooter($"\nPlayer Count : {lobby.users.Count}/{lobby.MaxPeople}\nLobby Id : {lobby.lobbyId}")
            .WithAuthor(_client.CurrentUser)
            .WithColor(Discord.Color.Gold);
        ButtonBuilder startButton = new ButtonBuilder()
            .WithLabel($"Start {lobby.users.Count}/{lobby.MaxPeople}")
            .WithCustomId("start#" + id)
            .WithStyle(ButtonStyle.Primary)
            .WithDisabled(true);
        ButtonBuilder leaveButton = new ButtonBuilder()
            .WithLabel("Leave")
            .WithCustomId("leave#" + id)
            .WithStyle(ButtonStyle.Danger);
        SelectMenuBuilder configure = new SelectMenuBuilder()
             .WithPlaceholder("Choose setting you want to configure")
             .WithCustomId("configure#" + id)
             .WithMinValues(1)
             .WithMaxValues(1)
             .AddOption($"📝 Question Count : {lobby.amount}", "amount")
             .AddOption($"⏱️ Seconds per Question : {lobby.time}", "time")
             .AddOption($"{lobbyTypeEmoji} Lobby Type : {lobbyType}", "type")
             .AddOption($"{gameModeEmoji} Game Mode : {lobby.type}", "mode");


        ComponentBuilder builder = new ComponentBuilder()
            .WithSelectMenu(configure)
            .WithButton(startButton)
            .WithButton(leaveButton);

        lobby.builder = builder;
        lobby.embed = lobbyEmbed;
        lobby.arg = arg as SocketSlashCommand;

        if (arg is SocketMessageComponent)
        {
            await (arg as SocketMessageComponent).RespondAsync(components: lobby.builder.Build(), embed: lobby.embed.Build(), ephemeral: true);
        }
        else
        {
            await (arg as SocketSlashCommand).RespondAsync(components: lobby.builder.Build(), embed: lobby.embed.Build(), ephemeral: true);
        }
    }
    private static EmbedBuilder UpdateLobbyEmbed(Lobby lobby)
    {

        string lobbyType = lobby.isPrivate ? "private" : "public";
        string participantNames = string.Join("\n", lobby.users.Select(c =>
        {
            string userNameWithStatus = c.isCreator ? $"{c.userName} 👑" : c.userName;
            if (c.isReady && !c.isCreator)
            {
                userNameWithStatus += " ✅";
            }
            else if (!c.isReady && !c.isCreator)
            {
                userNameWithStatus += "🚫";
            }
            return userNameWithStatus;
        }));

        return new EmbedBuilder()
            .WithTitle($"{lobby.lobbyName}")
            .WithDescription($"Game Type : {lobby.type}\nQuestion Count : {lobby.amount}\nLobby Type : {lobbyType} \n Participants :\n {participantNames}")
            .WithFooter($"\nPlayer Count : {lobby.users.Count}/{lobby.MaxPeople}\nLobby Id : {lobby.lobbyId}")
            .WithAuthor(_client.CurrentUser)
            .WithColor(Discord.Color.Gold);
    }
    private static ComponentBuilder UpdateLobbyBuilder(Lobby lobby)
    {
        string lobbyType = lobby.isPrivate ? "private" : "public";
        string lobbyTypeEmoji = lobby.isPrivate ? "🕵️" : "👨‍👩‍👧";
        string gameModeEmoji = lobby.type == "firstanswer" ? "⏱️" : "💯";
        ButtonBuilder startButton = new ButtonBuilder()
            .WithLabel($"Start {lobby.users.Count}/{lobby.MaxPeople}")
            .WithCustomId("start#" + lobby.lobbyId)
            .WithStyle(ButtonStyle.Primary)
            .WithDisabled(!lobby.users.All(u => u.isReady));
        ButtonBuilder leaveButton = new ButtonBuilder()
            .WithLabel("Leave")
            .WithCustomId("leave#" + lobby.lobbyId)
            .WithStyle(ButtonStyle.Danger);
        SelectMenuBuilder configure = new SelectMenuBuilder()
            .WithPlaceholder("Choose setting you want to configure")
            .WithCustomId("configure#" + lobby.lobbyId)
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption($"📝 Question Count : {lobby.amount}", "amount")
            .AddOption($"⏱️ Seconds per Question : {lobby.time}", "time")
            .AddOption($"{lobbyTypeEmoji} Lobby Type : {lobbyType}", "type")
            .AddOption($"{gameModeEmoji} Game Mode : {lobby.type}", "mode");
        if (lobby.users.Count > 1)
        {
            SelectMenuBuilder kick = new SelectMenuBuilder()
             .WithPlaceholder("which user do you want to kick")
             .WithCustomId("kick#" + lobby.lobbyId)
             .WithMinValues(1)
             .WithMaxValues(1);
            SelectMenuBuilder host = new SelectMenuBuilder()
             .WithPlaceholder("which user do you want to make host")
             .WithCustomId("host#" + lobby.lobbyId)
             .WithMinValues(1)
             .WithMaxValues(1);
            foreach (var user in lobby.users)
            {
                if (!user.isCreator)
                {
                    kick.AddOption($"{user._userName}", $"kick#{user.userId}");
                    host.AddOption($"{user._userName}", $"host#{user.userId}");
                }
            }
            return new ComponentBuilder().WithSelectMenu(configure).WithSelectMenu(host).WithSelectMenu(kick).WithButton(startButton).WithButton(leaveButton);
        }
        return new ComponentBuilder().WithSelectMenu(configure).WithButton(startButton).WithButton(leaveButton);
    }
    private static ComponentBuilder UpdateJoinLobbyBuilder(LobbyUser lobbyUser, Lobby item)
    {

        if (item.users.Exists(c => c.userId == lobbyUser.userId))
        {
            ButtonBuilder ready = new ButtonBuilder()
                          .WithLabel("READY")
                          .WithCustomId("ready#" + lobbyUser.userId + "#" + item.lobbyId)
                          .WithStyle(lobbyUser.isReady ? ButtonStyle.Secondary : ButtonStyle.Success);
            ButtonBuilder leave = new ButtonBuilder()
                                 .WithLabel("LEAVE")
                              .WithCustomId("leave#" + item.lobbyId + "#" + lobbyUser.userId)
                              .WithStyle(ButtonStyle.Danger);
            return new ComponentBuilder()
                .WithButton(ready)
                .WithButton(leave);
        }
        else
        {
            return new ComponentBuilder();
        }
    }
    public static async Task updateQuestionsForLobby(ulong id, IDiscordInteraction arg, bool isAnswered,ulong? lobbyId)
    {
        try
        {
            Lobby lobby = lobies.Find(c=>c.lobbyId==lobbyId.Value.ToString());
            User user = _users.Find(c => c.userId == arg.User.Id.ToString());
            Trivia trivia = user.myTrivias.Find(c => c.id == id);
            Console.WriteLine("ucfl çalıştırıldı");
            //Console.WriteLine(user.userId + " nin trivia id : " + trivia.id);
            //Console.WriteLine(user.userId + " nin soru sayısı : " + trivia.currentQuestionNumber);
            
            #region güncelleme
            string[] answers = new string[trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.Count + 1];
            answers[0] = trivia.questions[trivia.currentQuestionNumber].correctAnswer;
            Array.Copy(trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.ToArray(), 0, answers, 1, trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.Count);
            //Console.WriteLine(trivia.amount);
            // Shuffle the answers array
            if (!isAnswered)
            {
                Random random = new Random();
                int n = answers.Length;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    (answers[n], answers[k]) = (answers[k], answers[n]);
                }
                trivia.questions[trivia.currentQuestionNumber].answers.AddRange(answers);
            }

            if (trivia.answeredQuestionsCount == trivia.amount)
            {
                trivia.isFinished = true;
            }

            string situation = trivia.isFinished ? $"You finished all the questions \n Your Score is {trivia.score}/{trivia.amount}" : "";

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Trivia Question")
                .WithDescription($"Question {trivia.currentQuestionNumber + 1} : {trivia.questions[trivia.currentQuestionNumber].question}")
                .WithFooter($"Category : {trivia.questions[trivia.currentQuestionNumber].category}\nType : {trivia.questions[trivia.currentQuestionNumber].type}\nDifficulty : {trivia.questions[trivia.currentQuestionNumber].difficulty}\n{situation}\"")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Discord.Color.Magenta);
            SelectMenuBuilder questionsMenu = new SelectMenuBuilder()
                 .WithPlaceholder("Select an question")
                 .WithCustomId("questions#" + trivia.id+"#"+lobbyId)
                 .WithMinValues(1)
                 .WithMaxValues(1);

            for (int i = 1, j; i <= trivia.amount; i++)
            {
                Emoji emoji = new Emoji("");
                if (trivia.answersCorrectness[i - 1].ToString() == "correct")
                {
                    emoji = new Emoji("\u2705");
                }
                else if (trivia.answersCorrectness[i - 1].ToString() == "uncorrect")
                {
                    emoji = new Emoji("\u274C");
                }

                questionsMenu.AddOption($"{emoji} Question {i}  ", $"{i - 1}", isDefault: i == (trivia.currentQuestionNumber + 1));
            }

            ComponentBuilder builder;
            //Console.WriteLine(trivia.questions[trivia.currentQuestionNumber].correctAnswer);
            //trivia.questions[trivia.currentQuestionNumber].answers.ForEach(c=>Console.WriteLine(c));
            int correctIndex = Array.IndexOf(trivia.questions[trivia.currentQuestionNumber].answers.ToArray(), trivia.questions[trivia.currentQuestionNumber].correctAnswer);
            //Console.WriteLine(correctIndex);
            ButtonBuilder saveButton = new ButtonBuilder()
                .WithLabel("Save")
                .WithCustomId("save")
                .WithStyle(ButtonStyle.Secondary);

            if (trivia.type.ToString() == "multiple" || trivia.questions[trivia.currentQuestionNumber].type.ToString() == "multiple")
            {

                ButtonBuilder button1 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[0]}")
                    .WithCustomId("Choice1#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[0]}#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary)
                   ;

                ButtonBuilder button2 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[1]}")
                    .WithCustomId("Choice2#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[1]}#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary);

                ButtonBuilder button3 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[2]}")
                    .WithCustomId("Choice3#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[2]}#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary);

                ButtonBuilder button4 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[3]}")
                    .WithCustomId("Choice4#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[3]}#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary);


                //Console.WriteLine(trivia.answersCorrectness[trivia.currentQuestionNumber]);

                switch (correctIndex)
                {
                    case 0:
                        _ = button1.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[0]}#" + lobbyId);
                        break;
                    case 1:
                        _ = button2.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[1]}#" + lobbyId);
                        break;
                    case 2:
                        _ = button3.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[2]}#" + lobbyId);
                        break;
                    case 3:
                        _ = button4.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[3]}#" + lobbyId);
                        break;
                    default:
                        break;
                }
                if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "a")
                {
                    button1.WithStyle(ButtonStyle.Primary);
                    button2.WithStyle(ButtonStyle.Primary);
                    button3.WithStyle(ButtonStyle.Primary);
                    button4.WithStyle(ButtonStyle.Primary);
                }
                else if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "correct" || trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                {
                    button1.IsDisabled = true;
                    button2.IsDisabled = true;
                    button3.IsDisabled = true;
                    button4.IsDisabled = true;

                    if (button1.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button1.CustomId.Split('#')[0]);
                        button1.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button1.CustomId.Split('#')[2])
                            {
                                button1.WithStyle(ButtonStyle.Danger);

                            }

                        }
                    }

                    if (button2.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button2.CustomId.Split('#')[0]);
                        button2.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button2.CustomId.Split('#')[2])
                            {
                                button2.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }

                    if (button3.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button3.CustomId.Split('#')[0]);
                        button3.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button3.CustomId.Split('#')[2])
                            {
                                button3.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }

                    if (button4.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button4.CustomId.Split('#')[0]);
                        button4.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button4.CustomId.Split('#')[2])
                            {
                                button4.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }
                }

                builder = new ComponentBuilder()
                   .WithButton(button1)
                   .WithButton(button2)
                   .WithButton(button3)
                   .WithButton(button4)
                   ;
                //Console.WriteLine( $"{button1.CustomId},{button2.CustomId},{button3.CustomId},{button4.CustomId}");

            }
            else if (trivia.type.ToString() == "boolean" || trivia.questions[trivia.currentQuestionNumber].type.ToString() == "boolean")
            {
                ButtonBuilder buttonTrue = new ButtonBuilder()
                    .WithLabel("True")
                    .WithCustomId("True#" + trivia.id + "#True#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary)
                    ;

                ButtonBuilder buttonFalse = new ButtonBuilder()
                    .WithLabel("False")
                    .WithCustomId("False#" + trivia.id + "#False#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary)
                   ;



                switch (correctIndex)
                {
                    case 0:
                        _ = buttonTrue.WithCustomId("correct#" + trivia.id + "#True#" + lobbyId);
                        break;
                    case 1:
                        _ = buttonFalse.WithCustomId("correct#" + trivia.id + "#False#" + lobbyId);
                        break;
                }

                //Console.WriteLine(trivia.answersCorrectness[trivia.currentQuestionNumber]);
                if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "a")
                {
                    buttonTrue.WithStyle(ButtonStyle.Primary);
                    buttonFalse.WithStyle(ButtonStyle.Primary);
                }
                else if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "correct" || trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                {
                    buttonTrue.IsDisabled = true;
                    buttonFalse.IsDisabled = true;
                    if (buttonTrue.CustomId.Split('#')[0] == "correct")
                    {
                        buttonTrue.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == buttonTrue.CustomId.Split('#')[2])
                            {
                                buttonTrue.WithStyle(ButtonStyle.Danger);

                            }
                        }

                    }

                    if (buttonFalse.CustomId.Split('#')[0] == "correct")
                    {
                        buttonFalse.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == buttonFalse.CustomId.Split('#')[2])
                            {
                                buttonFalse.WithStyle(ButtonStyle.Danger);

                            }
                        }
                    }
                }


                builder = new ComponentBuilder()
                   .WithButton(buttonTrue)
                   .WithButton(buttonFalse)
                   ;
            }
            else
            {
                Console.WriteLine($"Unsupported question type");
                return;
            }
            //Console.WriteLine(answers[correctIndex]);

            #endregion

            if (arg is SocketMessageComponent)
            {

                await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = embed.Build();
                    res.Components = builder.Build();
                });
            }
            if (arg is SocketSlashCommand)
            {
                await (arg as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = embed.Build();
                    res.Components = builder.Build();
                });
            }
            if (trivia.gameType == "timed")
            {

                for (int i = trivia.questions[trivia.currentQuestionNumber].time; i > 0; i--)
                {
                    var nba = DateTime.Now;
                    Console.WriteLine("o anki soru için kalan zaman : "+ trivia.questions[trivia.currentQuestionNumber].time);
                    embed.Title = trivia.answersCorrectness[trivia.currentQuestionNumber] != "a" ? $"Time:{i}" : "Waiting for other participants";
                    Console.WriteLine($"Time:{i}");

                    if (trivia.answersCorrectness[trivia.currentQuestionNumber] != "a")
                    {
                        if (lobby.isEveryoneAnswered)
                        {
                            Console.WriteLine("herkes çözdü");
                            embed.Title = "Last 2 seconds for other questions";
                            //await Task.Delay(2000);
                            return;
                        }
                        else
                        {
                            Console.WriteLine("çözmeyenler var");
                            embed.Title = $"Wait for other participants.\nTime:{i}";
                        }
                    }



                    if (arg is SocketMessageComponent)
                    {
                        Console.WriteLine("arg bir smc");
                        await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                        {
                            Console.WriteLine("arg bir smc olarak güncelleniyor");
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");

                        });

                    }
                    else if (arg is SocketSlashCommand)
                    {
                        await (arg as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");
                        });

                    }
                    // 1 saniye bekleyerek zamanı azalt
                    await Task.Delay(950);
                  
                    trivia.questions[trivia.currentQuestionNumber].time--;
                    Console.WriteLine("gecen zaman:" + (DateTime.Now-nba));

                }

                // Zaman sıfıra ulaştığında

                if (trivia.userAnswers[trivia.currentQuestionNumber] == "ğ")
                {
                    trivia.answersCorrectness[trivia.currentQuestionNumber] = "uncorrect";
                }

                if (trivia.currentQuestionNumber < (trivia.amount - 1))
                {
                    changeCurrentQuestionNumberWithTime(arg, trivia.id, lobbyId);
                    Console.WriteLine(user.userId + " nin soru sayısı : " + trivia.currentQuestionNumber);
                }
                else
                {
                    Console.WriteLine("Sorular bitti");
                    Console.WriteLine($" son sorunun doğruluk değeri :  {trivia.answersCorrectness[trivia.currentQuestionNumber]}");
                    correctControl(arg as SocketMessageComponent, trivia.id,lobbyId);
                    return;
                }           

            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in updaateQuestions: {ex}");
        }

    }


    #endregion

    #region Past
    public static async Task selectMenuHandlerPastTrivias(SocketMessageComponent arg)
    {

        ulong id = ulong.Parse(arg.Data.Values.FirstOrDefault());
        //Trivia trivia = trivias.Find(c => c.id == id);
        //Console.WriteLine(trivias.Count);
        await showQuestions(arg, id, false, null);

    }
    public static async Task pastTrivias(IDiscordInteraction arg)
    {
        try
        {
            List<User> _pastTrivias = LoadTriviasFromJson("TriviasData.json");
            EmbedBuilder embed = new EmbedBuilder().WithTitle("All of your trivias you solved").WithDescription($"You solved {_pastTrivias.Count} trivias");
            SelectMenuBuilder triviasMenu = new SelectMenuBuilder()
               .WithPlaceholder("Select a trivia")
               .WithCustomId("trivias#")
               .WithMinValues(1)
               .WithMaxValues(1)
               ;
            for (int i = 0; i < _pastTrivias.Count; i++)
            {
                for (int j = 0; j < _pastTrivias[i].myTrivias.Count; j++)
                {
                    triviasMenu.AddOption(_pastTrivias[i].myTrivias[j].id.ToString(), _pastTrivias[i].myTrivias[j].id.ToString());
                }
            }

            ComponentBuilder builder = new ComponentBuilder();
            builder.WithSelectMenu(triviasMenu);
            if (arg is SocketSlashCommand)
            {
                await (arg as SocketSlashCommand).RespondAsync(embed:embed.Build(),components: builder.Build(), ephemeral: true);
            }
            else if (arg is SocketMessageComponent)
            {
                await (arg as SocketMessageComponent).RespondAsync(embed: embed.Build(), components: builder.Build(), ephemeral: true);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in pasttrivias: {ex.Message}");
        }


    }
    static List<User> LoadTriviasFromJson(string filePath)
    {
        if (File.Exists(filePath))
        {

            string json = File.ReadAllText(filePath);
            if (json.Length != 0)
            {
                return JsonConvert.DeserializeObject<List<User>>(json);
            }
            return new List<User>();
        }
        else
        {
            Console.WriteLine("dosya bulunamadı");
        }
        return new List<User>();
    }

    #endregion

    #region Trivia
    public static async Task selectMenuHandlerQuestion(SocketMessageComponent arg, ulong id,ulong? lobbyId)
    {
            User user = _users.Find(c => c.userId == arg.User.Id.ToString());
            Trivia trivia = user.myTrivias.Find(c => c.id == id);
            int selectedOptionIndex = int.Parse(arg.Data.Values.FirstOrDefault());
            trivia.currentQuestionNumber = selectedOptionIndex;
        if (trivia.forLobby)
        {
            updateQuestionsForLobby(id, arg, false,lobbyId); return;
        }
        else
        {
            updateQuestions(id, arg, false); return;

        }

    }
    public static async Task correctControl(SocketMessageComponent arg, ulong id, ulong? lobbyId)
    {
        try
        {
            User user = _users.Find(c => c.userId == arg.User.Id.ToString());
            Trivia trivia = user.myTrivias.Find(c => c.id == id);

            Console.WriteLine("id : "+trivia.id);
            var isCorrect = arg.Data.CustomId.Split('#')[0] == "correct";
                if (isCorrect)
                {
                    //await Console.Out.WriteLineAsync("Your Answer is Correct");
                    trivia.answersCorrectness[trivia.currentQuestionNumber] = trivia.answersCorrectness[trivia.currentQuestionNumber] == "a" ? "correct" : trivia.answersCorrectness[trivia.currentQuestionNumber];
                    if (trivia.userAnswers[trivia.currentQuestionNumber] == "ğ")
                    {
                        trivia.answeredQuestionsCount++;
                        trivia.score++;
                        user.userStats.userAchievements.categories[trivia.questions[trivia.currentQuestionNumber].category]++;
                    foreach (var categoryName in user.userStats.userAchievements.categories.Keys)
                    {
                        if (!user.userStats.examinedCategories.Contains(categoryName) && user.userStats.userAchievements.categories[categoryName] > 5)
                        {
                            user.userStats.unlockedAchivementsCount++;

                            user.userStats.examinedCategories.Add(categoryName);
                        }
                    }
                    switch (trivia.questions[trivia.currentQuestionNumber].difficulty)
                    {
                        case "easy":
                            user.userStats.xp += 5;
                            break;
                        case "medium":                           
                            user.userStats.xp += 10;
                            break;
                        case "hard":
                            user.userStats.xp += 15; 
                            break;                  
                        default:
                            break;
                    }

                }
                }
                else
                {
                    //await Console.Out.WriteLineAsync("Your Answer is UnCorrect");
                    trivia.answersCorrectness[trivia.currentQuestionNumber] = trivia.answersCorrectness[trivia.currentQuestionNumber] == "a" ? "uncorrect" : trivia.answersCorrectness[trivia.currentQuestionNumber];
                    if (trivia.userAnswers[trivia.currentQuestionNumber] == "ğ")
                    {
                        trivia.answeredQuestionsCount++;
                    }
                }
                if (arg.Data.CustomId.Split('#')[0] == "correct" || arg.Data.CustomId.Split('#')[0] == "Choice1" || arg.Data.CustomId.Split('#')[0] == "Choice2" || arg.Data.CustomId.Split('#')[0] == "Choice3" || arg.Data.CustomId.Split('#')[0] == "Choice4" || arg.Data.CustomId.Split('#')[0] == "True" || arg.Data.CustomId.Split('#')[0] == "False")
                {
                    trivia.userAnswers[trivia.currentQuestionNumber] = trivia.userAnswers[trivia.currentQuestionNumber] == "ğ" ? arg.Data.CustomId.Split('#')[2] : trivia.userAnswers[trivia.currentQuestionNumber];
                }

            if (trivia.forLobby)
            {
                    Console.WriteLine("lobby id : "+lobbyId);
                    Lobby lobby = lobies.Find(c => c.lobbyId == lobbyId.Value.ToString());
                    if (lobby.users.All(u => u.trivia.answersCorrectness[trivia.currentQuestionNumber] != "a"))
                    {
                        lobby.isEveryoneAnswered = true;
                    }
                                
                updateQuestionsForLobby(id, arg, false,lobbyId);
                return;
            }
            else
            {
                Console.WriteLine("lobi değil");
                updateQuestions(id, arg, false); return;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in correctcontrol: {ex}");
        }
    }
    public static async Task changeCurrentQuestionNumberWithTime(IDiscordInteraction arg, ulong id,ulong? lobbyId )
    {
        User user = _users.Find(c => c.userId == arg.User.Id.ToString());
        Trivia trivia = user.myTrivias.Find(c => c.id == id);
        try
        {
            trivia.currentQuestionNumber = Math.Min(int.Parse(trivia.amount.ToString()), trivia.currentQuestionNumber + 1);
            if (trivia.forLobby)
            {
                updateQuestionsForLobby(id, arg, false,lobbyId); return;
            }
            else
            {
                updateQuestions(id, arg, false); return;

            }
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in changeCurrentQuestionNumber: {ex.Message}");
        }
    }
    public static async Task changeCurrentQuestionNumber(SocketMessageComponent arg, ulong id)
    {
        try
        {
            User user = _users.Find(c => c.userId == arg.User.Id.ToString());
            Trivia trivia = user.myTrivias.Find(c => c.id == id);
            if (arg.Data.CustomId.Split('#')[0] == "next")
            {
                trivia.currentQuestionNumber = Math.Min(int.Parse(trivia.amount.ToString()), trivia.currentQuestionNumber + 1);
            }
            if (arg.Data.CustomId.Split('#')[0] == "previous")
            {
                trivia.currentQuestionNumber = Math.Max(0, trivia.currentQuestionNumber - 1);
            }

            Console.WriteLine(user.userId + " nin soru sayısı : " + trivia.currentQuestionNumber);
            if (trivia.forLobby)
            {
                updateQuestionsForLobby(id, arg, false,null); return;
            }
            else
            {
                updateQuestions(id, arg, false); return;

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in changeCurrentQuestionNumber: {ex.Message}");
        }
    }
    private static async Task<(Trivia, JObject)> createTrivia(SocketSlashCommand arg, bool isTimed)
    {
        User user = _users.FirstOrDefault(u => u.userId == arg.User.Id.ToString());
        if (user == null)
        {
            user = new User
            {
                userId = arg.User.Id.ToString(),
            };

            _users.Add(user);
        }
        

        Trivia myTrivia = new Trivia{};
        int amount = int.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "amount").Value.ToString());
        if (isTimed)
        {
            int time = int.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "time").Value.ToString());
            myTrivia.time = time;
            myTrivia.gameType = "timed";
        }
        else
        {
            myTrivia.gameType = "normal";
        }
        int currentQuestionNumber = 0;
       
        if (arg.Data.Options.ToList().Exists(c => c.Name == "category"))
        {
            int category = int.Parse(arg.Data.Options.FirstOrDefault(option => option.Name == "category").Value.ToString());
            myTrivia.category = category;
        }
        if (arg.Data.Options.ToList().Exists(c => c.Name == "difficulty"))
        {
            string difficulty = arg.Data.Options.FirstOrDefault(option => option.Name == "difficulty").Value.ToString();
            myTrivia.difficulty = difficulty;
        }
        if (arg.Data.Options.ToList().Exists(c => c.Name == "type"))
        {
            string type = arg.Data.Options.FirstOrDefault(option => option.Name == "type").Value.ToString() ?? "multiple";
            myTrivia.type = type;
        }
       
        myTrivia.amount = amount;
        myTrivia.currentQuestionNumber = currentQuestionNumber;

        if (myTrivia.amount < 1 && myTrivia.amount > 25 && myTrivia.time < 1 && myTrivia.time > 30)
        {
            await arg.RespondAsync("please enter values ​​within the appropriate range", ephemeral: true);
        }
        else
        {
            user.myTrivias.Add(myTrivia);
        }

        string url = $"https://opentdb.com/api.php?amount={myTrivia.amount}&category={myTrivia.category}&difficulty={myTrivia.difficulty}&type={myTrivia.type}";
        RestClient client = new RestClient(url);
        RestRequest request = new RestRequest();
        RestResponse response = client.Get(request);
        string decodedResponse = WebUtility.HtmlDecode(response.Content.ToString().Replace("&quot;", "'"));
        JObject jObject = JObject.Parse(decodedResponse);
        return (myTrivia, jObject);
    }
    public static async Task changeEmbedTitle(EmbedBuilder embed,int sayac)
    {
        embed.WithTitle($"\nTime : {sayac}");
    }
    public static void createQuestions(Trivia trivia, JObject jObject)
    {
        try
        {
            JToken data = jObject["results"];
            //User user = _users.Find(c => c.userId == arg.User.Id.ToString());
            //Trivia trivia = user.myTrivias.Find(c => c.id == id);
            for (int questionNumber = 0; questionNumber < trivia.amount; questionNumber++)
            {
                Question question = new Question();
                string q = data[questionNumber]["question"].ToString();
                string correctAnswer = data[questionNumber]["correct_answer"].ToString();
                string[] incorrectAnswers = data[questionNumber]["incorrect_answers"].ToObject<string[]>();
                string type = data[questionNumber]["type"].ToString();  // Get the question type
                string category = data[questionNumber]["category"].ToString();
                string difficulty = data[questionNumber]["difficulty"].ToString();
                question.question = q; question.correctAnswer = correctAnswer; question.inCorrectAnswers = incorrectAnswers.ToList(); question.questionNumber = questionNumber; question.type = type; question.category = category; question.difficulty = difficulty;            
                trivia.questions.Add(question);

            }
            trivia.questions.ForEach(c => trivia.answersCorrectness.Add("a"));
            trivia.questions.ForEach(c => trivia.userAnswers.Add("ğ"));
            Console.WriteLine("sorular yaratıldı");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in createquestions: {ex.Message}");
        }
    }
    public static async Task showQuestions(IDiscordInteraction arg, ulong id, bool isAnswered, ulong? lobbyId)
    {
        try
        {
            User user = _users.Find(c => c.userId == arg.User.Id.ToString());
            Console.WriteLine(user.userId+" nın triviası gösteriliyor");
            Console.WriteLine("u :"+user.userId);
            Trivia trivia = user.myTrivias.Find(c => c.id == id);
            #region güncelleme
            string[] answers = new string[trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.Count + 1];
            answers[0] = trivia.questions[trivia.currentQuestionNumber].correctAnswer;
            Array.Copy(trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.ToArray(), 0, answers, 1, trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.Count);

            //Console.WriteLine(trivia.amount);
            // Shuffle the answers array
            if (!isAnswered)
            {
                Random random = new Random();
                int n = answers.Length;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    (answers[n], answers[k]) = (answers[k], answers[n]);
                }
                trivia.questions[trivia.currentQuestionNumber].answers.AddRange(answers);
            }
            if (trivia.answeredQuestionsCount == trivia.amount)
            {
                trivia.isFinished = true;
            }

            string situation = trivia.isFinished ? $"You finished all the questions \n Your Score is {trivia.score}/{trivia.amount}" : "";

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Trivia Question")
                .WithDescription($"Question {trivia.currentQuestionNumber + 1} : {trivia.questions[trivia.currentQuestionNumber].question}")
                .WithFooter($"Category : {trivia.questions[trivia.currentQuestionNumber].category}\nType : {trivia.questions[trivia.currentQuestionNumber].type}\nDifficulty : {trivia.questions[trivia.currentQuestionNumber].difficulty}\n{situation}\"")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Discord.Color.Magenta);
            SelectMenuBuilder questionsMenu = new SelectMenuBuilder()
                 .WithPlaceholder("Select an question")
                 .WithCustomId("questions#" + id+"#"+lobbyId)
                 .WithMinValues(1)
                 .WithMaxValues(1);
            //trivia.answersCorrectness.ForEach(a=>Console.WriteLine(a));

            for (int i = 1, j; i <= trivia.amount; i++)
            {
                Console.WriteLine(trivia.answersCorrectness[i-1]);
                Emoji emoji = new Emoji("");
                if (trivia.answersCorrectness[i - 1].ToString() == "correct")
                {
                    emoji = new Emoji("\u2705");
                }
                else if (trivia.answersCorrectness[i - 1].ToString() == "uncorrect")
                {
                    emoji = new Emoji("\u274C");
                }

                questionsMenu.AddOption($"{emoji} Question {i}  ", $"{i - 1}", isDefault: i == (trivia.currentQuestionNumber + 1));
            }

            ComponentBuilder builder;
            int correctIndex = Array.IndexOf(trivia.questions[trivia.currentQuestionNumber].answers.ToArray(), trivia.questions[trivia.currentQuestionNumber].correctAnswer);
            ButtonBuilder saveButton = new ButtonBuilder()
                .WithLabel("Save")
                .WithCustomId("save")
                .WithStyle(ButtonStyle.Secondary);

            if (trivia.type.ToString() == "multiple" || trivia.questions[trivia.currentQuestionNumber].type.ToString() == "multiple")
            {

                ButtonBuilder button1 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[0]}")
                    .WithCustomId("Choice1#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[0]}#"+lobbyId)
                    .WithStyle(ButtonStyle.Primary)
                   ;

                ButtonBuilder button2 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[1]}")
                    .WithCustomId("Choice2#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[1]}#"+lobbyId)
                    .WithStyle(ButtonStyle.Primary);

                ButtonBuilder button3 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[2]}")
                    .WithCustomId("Choice3#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[2]}#"+lobbyId)
                    .WithStyle(ButtonStyle.Primary);

                ButtonBuilder button4 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[3]}")
                    .WithCustomId("Choice4#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[3]}#"+lobbyId)
                    .WithStyle(ButtonStyle.Primary);


                //Console.WriteLine(trivia.answersCorrectness[trivia.currentQuestionNumber]);

                switch (correctIndex)
                {
                    case 0:
                        _ = button1.WithCustomId("correct#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[0]}#" + lobbyId);
                        break;
                    case 1:
                        _ = button2.WithCustomId("correct#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[1]}#" + lobbyId);
                        break;
                    case 2:
                        _ = button3.WithCustomId("correct#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[2]}#" + lobbyId);
                        break;
                    case 3:
                        _ = button4.WithCustomId("correct#" + id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[3]}#" + lobbyId);
                        break;
                    default:
                        break;
                }
                if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "a")
                {
                    button1.WithStyle(ButtonStyle.Primary);
                    button2.WithStyle(ButtonStyle.Primary);
                    button3.WithStyle(ButtonStyle.Primary);
                    button4.WithStyle(ButtonStyle.Primary);
                }
                else if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "correct" || trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                {
                    button1.IsDisabled = true;
                    button2.IsDisabled = true;
                    button3.IsDisabled = true;
                    button4.IsDisabled = true;

                    if (button1.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button1.CustomId.Split('#')[0]);
                        button1.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button1.CustomId.Split('#')[2])
                            {
                                button1.WithStyle(ButtonStyle.Danger);

                            }

                        }
                    }

                    if (button2.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button2.CustomId.Split('#')[0]);
                        button2.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button2.CustomId.Split('#')[2])
                            {
                                button2.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }

                    if (button3.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button3.CustomId.Split('#')[0]);
                        button3.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button3.CustomId.Split('#')[2])
                            {
                                button3.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }

                    if (button4.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button4.CustomId.Split('#')[0]);
                        button4.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button4.CustomId.Split('#')[2])
                            {
                                button4.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }
                }

                builder = new ComponentBuilder()
                   .WithButton(button1)
                   .WithButton(button2)
                   .WithButton(button3)
                   .WithButton(button4)
                   ;
                //Console.WriteLine( $"{button1.CustomId},{button2.CustomId},{button3.CustomId},{button4.CustomId}");

            }
            else if (trivia.type.ToString() == "boolean" || trivia.questions[trivia.currentQuestionNumber].type.ToString() == "boolean")
            {
                ButtonBuilder buttonTrue = new ButtonBuilder()
                    .WithLabel("True")
                    .WithCustomId("True#" + id + "#True#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary)
                    ;

                ButtonBuilder buttonFalse = new ButtonBuilder()
                    .WithLabel("False")
                    .WithCustomId("False#" + id + "#False#" + lobbyId)
                    .WithStyle(ButtonStyle.Primary)
                   ;



                switch (correctIndex)
                {
                    case 0:
                        _ = buttonTrue.WithCustomId("correct#" + id + "#True#" + lobbyId);
                        break;
                    case 1:
                        _ = buttonFalse.WithCustomId("correct#" + id + "#False#" + lobbyId);
                        break;
                }

                //Console.WriteLine(trivia.answersCorrectness[trivia.currentQuestionNumber]);
                if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "a")
                {
                    buttonTrue.WithStyle(ButtonStyle.Primary);
                    buttonFalse.WithStyle(ButtonStyle.Primary);
                }
                else if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "correct" || trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                {
                    buttonTrue.IsDisabled = true;
                    buttonFalse.IsDisabled = true;
                    if (buttonTrue.CustomId.Split('#')[0] == "correct")
                    {
                        buttonTrue.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == buttonTrue.CustomId.Split('#')[2])
                            {
                                buttonTrue.IsDisabled = true;

                            }
                        }

                    }

                    if (buttonFalse.CustomId.Split('#')[0] == "correct")
                    {
                        buttonFalse.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == buttonFalse.CustomId.Split('#')[2])
                            {
                                buttonFalse.WithStyle(ButtonStyle.Danger);

                            }
                        }
                    }
                }


                builder = new ComponentBuilder()
                   .WithButton(buttonTrue)
                   .WithButton(buttonFalse)
                   ;
            }
            else
            {
                Console.WriteLine($"Unsupported question type");
                return;
            }
            //Console.WriteLine(answers[correctIndex]);

            ButtonBuilder nextButton = new ButtonBuilder()
                .WithLabel("Next Question")
                .WithCustomId("next#" + id)
                .WithStyle(ButtonStyle.Success)
                ;
            ButtonBuilder backButton = new ButtonBuilder()
                .WithLabel("Previous Question")
                .WithCustomId("previous#" + id)
                .WithStyle(ButtonStyle.Success);
            ButtonBuilder passButton = new ButtonBuilder()
               .WithLabel("Previous Question")
               .WithCustomId("previous#" + id)
               .WithStyle(ButtonStyle.Success);

            if (trivia.currentQuestionNumber == 0)
            {
                backButton.IsDisabled = true;
            }
            if (trivia.currentQuestionNumber == (trivia.amount - 1))
            {
                nextButton.IsDisabled = true;
            }
            if (trivia.gameType == "normal" || trivia.answersCorrectness[trivia.currentQuestionNumber] != "a")
            {
                _ = builder
              .WithSelectMenu(questionsMenu, row: 0)
              .WithButton(backButton, row: 1)
              .WithButton(nextButton, row: 1)
              .WithButton(saveButton, row: 3);
            }

            #endregion
            
            if (arg is SocketMessageComponent)
            {

                if (trivia.forLobby)
                {
                    await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                    {
                        res.Embed = embed.Build();
                        res.Components = builder.Build();
                    });
                }
                else
                {
                    await (arg as SocketMessageComponent).RespondAsync(components: builder.Build(), embed: embed.Build(), ephemeral: true);
                }
            }
            else
            {
                if (trivia.forLobby)
                {
                    await (arg as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                    {
                        res.Embed = embed.Build();
                        res.Components = builder.Build();
                    });
                }
                else
                {
                    await (arg as SocketSlashCommand).RespondAsync(components: builder.Build(), embed: embed.Build(), ephemeral: true);
                }
            }
            Console.WriteLine("sorular gösterildi");

            if (trivia.gameType == "timed")
            {

                for (int i = trivia.time; i > 0; i--)
                {
                    //Console.WriteLine("zamanı azalan trivianın idsi"+trivia.id);
                    Console.WriteLine("zamanı azalan trivianın idsi" + trivia.id+" o anki soru için kalan zaman : " + trivia.questions[trivia.currentQuestionNumber].time);
                    trivia.questions.ForEach(c => Console.WriteLine(trivia.id + "  " + c.time));
                    // Eğer kullanıcı soruya cevap verdiyse döngüden çık
                    if (trivia.userAnswers[trivia.currentQuestionNumber] != "ğ")
                    {
                        return;
                    }

                    embed.Title = $"Time:{i}";
                    Console.WriteLine($"Time:{i}");

                    // Mesajı güncelle
                    if (arg is SocketMessageComponent)
                    {
                        await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");

                        });

                    }
                    else if (arg is SocketSlashCommand)
                    {
                        await (arg as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");
                        });

                    }
                    // 1 saniye bekleyerek zamanı azalt
                    await Task.Delay(1000);
                }

                // Zaman sıfıra ulaştığında

                trivia.answersCorrectness[trivia.currentQuestionNumber] = trivia.answersCorrectness[trivia.currentQuestionNumber] == "a" ? "uncorrect" : trivia.answersCorrectness[trivia.currentQuestionNumber];
                if (trivia.currentQuestionNumber<(trivia.amount-1))
                {
                    changeCurrentQuestionNumberWithTime(arg, trivia.id,lobbyId);
                    Console.WriteLine(user.userId+" nin soru sayısı : "+trivia.currentQuestionNumber);

                }
                else
                {
                    Console.WriteLine("Sorular bitti");
                    if (arg is SocketMessageComponent)
                    {
                        await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");

                        });

                    }
                    else if (arg is SocketSlashCommand)
                    {
                        await (arg as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");
                        });

                    }

                    return; 
                }

                return;
            }

           
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in showQuestions: {ex}");
        }
    }
    public static async Task updateQuestions(ulong id,IDiscordInteraction arg,bool isAnswered, ulong? lobbyId = null)
    {
        try
        {
            User user = _users.Find(c => c.userId == arg.User.Id.ToString());
            Trivia trivia = user.myTrivias.Find(c => c.id == id);
            Console.WriteLine(user.userId + " nin trivia id : " + trivia.id);
            Console.WriteLine(user.userId + " nin soru sayısı : " + trivia.currentQuestionNumber);
            #region güncelleme
            string[] answers = new string[trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.Count + 1];
            answers[0] = trivia.questions[trivia.currentQuestionNumber].correctAnswer;
            Array.Copy(trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.ToArray(), 0, answers, 1, trivia.questions[trivia.currentQuestionNumber].inCorrectAnswers.Count);
            //Console.WriteLine(trivia.amount);
            // Shuffle the answers array
            if (!isAnswered)
            {
                Random random = new Random();
                int n = answers.Length;
                while (n > 1)
                {
                    n--;
                    int k = random.Next(n + 1);
                    (answers[n], answers[k]) = (answers[k], answers[n]);
                }
                trivia.questions[trivia.currentQuestionNumber].answers.AddRange(answers);
            }
            if (trivia.answeredQuestionsCount == trivia.amount)
            {
                trivia.isFinished = true;
            }

            string situation = trivia.isFinished ? $"You finished all the questions \n Your Score is {trivia.score}/{trivia.amount}" : "";

            EmbedBuilder embed = new EmbedBuilder()
                .WithTitle("Trivia Question")
                .WithDescription($"Question {trivia.currentQuestionNumber + 1} : {trivia.questions[trivia.currentQuestionNumber].question}")
                .WithFooter($"Category : {trivia.questions[trivia.currentQuestionNumber].category}\nType : {trivia.questions[trivia.currentQuestionNumber].type}\nDifficulty : {trivia.questions[trivia.currentQuestionNumber].difficulty}\n{situation}\"")
                .WithAuthor(_client.CurrentUser)
                .WithColor(Discord.Color.Magenta);
            SelectMenuBuilder questionsMenu = new SelectMenuBuilder()
                 .WithPlaceholder("Select an question")
                 .WithCustomId("questions#" + trivia.id)
                 .WithMinValues(1)
                 .WithMaxValues(1);
            //trivia.answersCorrectness.ForEach(a=>Console.WriteLine(a));

            for (int i = 1, j; i <= trivia.amount; i++)
            {
                Emoji emoji = new Emoji("");
                //Console.WriteLine(trivia.answersCorrectness[i - 1].ToString());
                if (trivia.answersCorrectness[i - 1].ToString() == "correct")
                {
                    emoji = new Emoji("\u2705");
                }
                else if (trivia.answersCorrectness[i - 1].ToString() == "uncorrect")
                {
                    emoji = new Emoji("\u274C");
                }

                questionsMenu.AddOption($"{emoji} Question {i}  ", $"{i - 1}", isDefault: i == (trivia.currentQuestionNumber + 1));
            }

            ComponentBuilder builder;
            //Console.WriteLine(trivia.questions[trivia.currentQuestionNumber].correctAnswer);
            //trivia.questions[trivia.currentQuestionNumber].answers.ForEach(c=>Console.WriteLine(c));
            int correctIndex = Array.IndexOf(trivia.questions[trivia.currentQuestionNumber].answers.ToArray(), trivia.questions[trivia.currentQuestionNumber].correctAnswer);
            //Console.WriteLine(correctIndex);
            ButtonBuilder saveButton = new ButtonBuilder()
                .WithLabel("Save")
                .WithCustomId("save")
                .WithStyle(ButtonStyle.Secondary);

            if (trivia.type.ToString() == "multiple" || trivia.questions[trivia.currentQuestionNumber].type.ToString() == "multiple")
            {

                ButtonBuilder button1 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[0]}")
                    .WithCustomId("Choice1#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[0]}")
                    .WithStyle(ButtonStyle.Primary)
                   ;

                ButtonBuilder button2 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[1]}")
                    .WithCustomId("Choice2#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[1]}")
                    .WithStyle(ButtonStyle.Primary);

                ButtonBuilder button3 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[2]}")
                    .WithCustomId("Choice3#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[2]}")
                    .WithStyle(ButtonStyle.Primary);

                ButtonBuilder button4 = new ButtonBuilder()
                    .WithLabel($"{trivia.questions[trivia.currentQuestionNumber].answers[3]}")
                    .WithCustomId("Choice4#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[3]}")
                    .WithStyle(ButtonStyle.Primary);


                //Console.WriteLine(trivia.answersCorrectness[trivia.currentQuestionNumber]);

                switch (correctIndex)
                {
                    case 0:
                        _ = button1.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[0]}");
                        break;
                    case 1:
                        _ = button2.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[1]}");
                        break;
                    case 2:
                        _ = button3.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[2]}");
                        break;
                    case 3:
                        _ = button4.WithCustomId("correct#" + trivia.id + $"#{trivia.questions[trivia.currentQuestionNumber].answers[3]}");
                        break;
                    default:
                        break;
                }
                if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "a")
                {
                    button1.WithStyle(ButtonStyle.Primary);
                    button2.WithStyle(ButtonStyle.Primary);
                    button3.WithStyle(ButtonStyle.Primary);
                    button4.WithStyle(ButtonStyle.Primary);
                }
                else if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "correct" || trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                {
                    button1.IsDisabled = true;
                    button2.IsDisabled = true;
                    button3.IsDisabled = true;
                    button4.IsDisabled = true;

                    if (button1.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button1.CustomId.Split('#')[0]);
                        button1.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button1.CustomId.Split('#')[2])
                            {
                                button1.WithStyle(ButtonStyle.Danger);

                            }

                        }
                    }

                    if (button2.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button2.CustomId.Split('#')[0]);
                        button2.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button2.CustomId.Split('#')[2])
                            {
                                button2.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }

                    if (button3.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button3.CustomId.Split('#')[0]);
                        button3.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button3.CustomId.Split('#')[2])
                            {
                                button3.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }

                    if (button4.CustomId.Split('#')[0] == "correct")
                    {
                        //Console.WriteLine(button4.CustomId.Split('#')[0]);
                        button4.WithStyle(ButtonStyle.Success);
                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == button4.CustomId.Split('#')[2])
                            {
                                button4.WithStyle(ButtonStyle.Danger);
                            }
                        }
                    }
                }

                builder = new ComponentBuilder()
                   .WithButton(button1)
                   .WithButton(button2)
                   .WithButton(button3)
                   .WithButton(button4)
                   ;
                //Console.WriteLine( $"{button1.CustomId},{button2.CustomId},{button3.CustomId},{button4.CustomId}");

            }
            else if (trivia.type.ToString() == "boolean" || trivia.questions[trivia.currentQuestionNumber].type.ToString() == "boolean")
            {
                ButtonBuilder buttonTrue = new ButtonBuilder()
                    .WithLabel("True")
                    .WithCustomId("True#" + trivia.id + "#True")
                    .WithStyle(ButtonStyle.Primary)
                    ;

                ButtonBuilder buttonFalse = new ButtonBuilder()
                    .WithLabel("False")
                    .WithCustomId("False#" + trivia.id + "#False")
                    .WithStyle(ButtonStyle.Primary)
                   ;



                switch (correctIndex)
                {
                    case 0:
                        _ = buttonTrue.WithCustomId("correct#" + trivia.id + "#True");
                        break;
                    case 1:
                        _ = buttonFalse.WithCustomId("correct#" + trivia.id + "#False");
                        break;
                }

                //Console.WriteLine(trivia.answersCorrectness[trivia.currentQuestionNumber]);
                if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "a")
                {
                    buttonTrue.WithStyle(ButtonStyle.Primary);
                    buttonFalse.WithStyle(ButtonStyle.Primary);
                }
                else if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "correct" || trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                {
                    buttonTrue.IsDisabled = true;
                    buttonFalse.IsDisabled = true;
                    if (buttonTrue.CustomId.Split('#')[0] == "correct")
                    {
                        buttonTrue.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == buttonTrue.CustomId.Split('#')[2])
                            {
                                buttonTrue.WithStyle(ButtonStyle.Danger);

                            }
                        }

                    }

                    if (buttonFalse.CustomId.Split('#')[0] == "correct")
                    {
                        buttonFalse.WithStyle(ButtonStyle.Success);

                    }
                    else
                    {
                        if (trivia.answersCorrectness[trivia.currentQuestionNumber] == "uncorrect")
                        {
                            if (trivia.userAnswers[trivia.currentQuestionNumber] == buttonFalse.CustomId.Split('#')[2])
                            {
                                buttonFalse.WithStyle(ButtonStyle.Danger);

                            }
                        }
                    }
                }


                builder = new ComponentBuilder()
                   .WithButton(buttonTrue)
                   .WithButton(buttonFalse)
                   ;
            }
            else
            {
                Console.WriteLine($"Unsupported question type");
                return;
            }
            //Console.WriteLine(answers[correctIndex]);

            ButtonBuilder nextButton = new ButtonBuilder()
                .WithLabel("Next Question")
                .WithCustomId("next#" + trivia.id)
                .WithStyle(ButtonStyle.Success)
                ;
            ButtonBuilder backButton = new ButtonBuilder()
                .WithLabel("Previous Question")
                .WithCustomId("previous#" + trivia.id)
                .WithStyle(ButtonStyle.Success);
            ButtonBuilder passButton = new ButtonBuilder()
               .WithLabel("Previous Question")
               .WithCustomId("previous#" + trivia.id)
               .WithStyle(ButtonStyle.Success);

            if (trivia.currentQuestionNumber == 0)
            {
                backButton.IsDisabled = true;
            }
            if (trivia.currentQuestionNumber == (trivia.amount - 1))
            {
                nextButton.IsDisabled = true;
            }
            if (trivia.gameType == "normal" || trivia.answersCorrectness[trivia.currentQuestionNumber] != "a")
            {
                _ = builder
              .WithSelectMenu(questionsMenu, row: 0)
              .WithButton(backButton, row: 1)
              .WithButton(nextButton, row: 1)
              .WithButton(saveButton, row: 3);
            }

            #endregion
           
            if (arg is SocketMessageComponent)
            {

                await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = embed.Build();
                    res.Components = builder.Build();
                });
            }
            if (arg is SocketSlashCommand)
            {
                await (arg as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = embed.Build();
                    res.Components = builder.Build();
                });
            }
            if (trivia.gameType == "timed")
            {

                for (int i = trivia.time; i > 0; i--)
                {
               
                    // Eğer kullanıcı soruya cevap verdiyse döngüden çık
                    if (trivia.userAnswers[trivia.currentQuestionNumber] != "ğ")
                    {
                        return;
                    }

                    embed.Title = $"Time:{i}";
                    Console.WriteLine($"Time:{i}");

                    // Mesajı güncelle
                    if (arg is SocketMessageComponent)
                    {
                        Console.WriteLine("arg bir smc");
                        await (arg as SocketMessageComponent).ModifyOriginalResponseAsync(res =>
                        {
                            Console.WriteLine("arg bir smc olarak güncelleniyor");
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");

                        });

                    }
                    else if (arg is SocketSlashCommand)
                    {
                        await (arg as SocketSlashCommand).ModifyOriginalResponseAsync(res =>
                        {
                            res.Embed = embed.Build();
                            res.Components = builder.Build();
                            Console.WriteLine("Mesaj güncellendi");
                        });

                    }
                    // 1 saniye bekleyerek zamanı azalt
                    await Task.Delay(1000);
                }

                // Zaman sıfıra ulaştığında

                if (trivia.userAnswers[trivia.currentQuestionNumber] == "ğ")
                {
                    trivia.answersCorrectness[trivia.currentQuestionNumber] = "uncorrect";
                }

                if (trivia.currentQuestionNumber < (trivia.amount-1))
                {
                    await changeCurrentQuestionNumberWithTime(arg, trivia.id,lobbyId);
                    Console.WriteLine(user.userId + " nin soru sayısı : " + trivia.currentQuestionNumber);
                }
                else
                {
                    Console.WriteLine("Sorular bitti");
                    Console.WriteLine($" son sorunun doğruluk değeri :  {trivia.answersCorrectness[trivia.currentQuestionNumber]}");
                    correctControl(arg as SocketMessageComponent,trivia.id,null);
                    return;
                }


                // Buradan çıkış yap, return

               
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in updaateQuestions: {ex}");
        }
       
    }
    #endregion

    #region Profile 

    #region ProfileFuncs
    private static async Task seeProfile(IDiscordInteraction arg)
    {
        User user = _users.Find(c => c.userId == arg.User.Id.ToString());

        EmbedBuilder profileEmbed = new EmbedBuilder()
            .WithTitle($"{arg.User.Username}'s Profile")
            .WithDescription($"Level: {user.userStats.level}\nTotal XP: {user.userStats.xp}\nLevel up progress: {user.userStats.xp}/{user.userStats.neededXpForNextLevel}")
            .WithThumbnailUrl(arg.User.GetAvatarUrl())
            .WithColor(Discord.Color.Green);
        SelectMenuBuilder profileProcesses = new SelectMenuBuilder()
            .WithPlaceholder("Select an option")
            .WithCustomId("profile-processes")
            .AddOption("Trivias", "trivias#")
            .AddOption("Badges", "badges#");
        ComponentBuilder builder = new ComponentBuilder().WithSelectMenu(profileProcesses);

        if (arg is SocketSlashCommand)
        {
            await (arg as SocketSlashCommand).RespondAsync(embed: profileEmbed.Build(), components: builder.Build(), ephemeral: true);
        }
        else if (arg is SocketMessageComponent)
        {
            await (arg as SocketMessageComponent).RespondAsync(embed: profileEmbed.Build(), components: builder.Build(), ephemeral: true);
        }
    }
    private static async Task selectMenuHandlerProfileProcesses(SocketMessageComponent arg)
    {
        string selectedOption = arg.Data.Values.FirstOrDefault().ToString();
        switch (selectedOption.Split('#')[0].ToString())
        {
            case "profile":
                await seeProfile(arg);
                break;
            case "badges":
                await arg.DeferAsync(ephemeral: true);
                await seeBadges(arg);
                break;
            case "trivias":
                await pastTriviasForProfile(arg);
                break;
            default:
                break;
        }
    }
    private static async Task seeBadges(IDiscordInteraction arg)
    {
        try
        {
            User user = _users.Find(c => c.userId == arg.User.Id.ToString());



            Console.WriteLine(user.userId);
            EmbedBuilder badgesEmbed = new EmbedBuilder()
                   .WithTitle($"All badges of {arg.User.Username}")
                   .WithDescription($"You have successfully unlocked {user.userStats.unlockedAchivementsCount}/24");
            SelectMenuBuilder profileProcesses = new SelectMenuBuilder()
                .WithPlaceholder("Select an option")
                 .WithCustomId("profile-processes")
                 .AddOption("Profile", "profile#")
                 .AddOption("Trivias", "trivias#");
            ComponentBuilder componentBuilder = new ComponentBuilder().WithSelectMenu(profileProcesses);
            string[,] imageUrls = new string[,]
     {
        {"https://cdn.discordapp.com/attachments/1064977711543619595/1185562606623260702/9.png?ex=65901038&is=657d9b38&hm=8f63cddfa0016e3d8783a1efdb3ab071583f8ee106943135aca4ddb0b3139213&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562606942048437/10.png?ex=65901038&is=657d9b38&hm=28c5b02c730c916c1d9f26f27057cff582f730e039117ca0b5da462f5fb3b766&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562607382433802/11.png?ex=65901038&is=657d9b38&hm=13694ab28deac58288f2cb0e944d9d6b990aa4ce6c1f564aee950fa88f2306a1&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562607705391165/12.png?ex=65901038&is=657d9b38&hm=099e256b6c13c79ee835aac707db379a802e690340ae69d90152dcf5323d0cb0&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562608120647841/13.png?ex=65901038&is=657d9b38&hm=a8c0448c1f3f466d6085c61d8cc7862c30a4652d763bbe5272696f3caebb5f51&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562608544256130/14.png?ex=65901038&is=657d9b38&hm=fc7b5198bafb75357ec6903a237e8b6c266d1b6b4d1a806549cee4dd114342ae&"},
        {"https://cdn.discordapp.com/attachments/1064977711543619595/1185562608854650930/15.png?ex=65901038&is=657d9b38&hm=a7ed5096bc7061f002be52c1631347adf2ae90172e8b4260e22bbe9b3dd4ed26&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562609458614302/16.png?ex=65901038&is=657d9b38&hm=33f3a8431180235a2782cddc2e41044f16bff14071bdfd468dfa031182f65b0a&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562609848680519/17.png?ex=65901038&is=657d9b38&hm=83e645cd00ac5d7626f9a952ac817d402e0862f88b8b83044637c7fbc84c717d&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562606308691988/18.png?ex=65901038&is=657d9b38&hm=72aa36d6f5606b9913c404f7a413bcd35690c422c7b016a57a1ba718b48dc501&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562832125841458/19.png?ex=6590106d&is=657d9b6d&hm=53043d5596ad9c8fd2db4490bff644e37577eca55e64f5b8084843b97eb7b1ea&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562832444588032/20.png?ex=6590106d&is=657d9b6d&hm=a53783eef6995fcd6c44d245bb917897dd8c78f3c1a52e67a12c917bda15f15b&"},
        {"https://cdn.discordapp.com/attachments/1064977711543619595/1185562832671088671/21.png?ex=6590106e&is=657d9b6e&hm=14b2512956f1925705128c5ecd85c14fe664052cbfb0f9ca1ad6a7c2d21c4e53&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562832918564964/22.png?ex=6590106e&is=657d9b6e&hm=d98141444372225d481aeec44723ddcc0f0db680bf9884c6401ecf89dce1a7b8&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562833178591362/23.png?ex=6590106e&is=657d9b6e&hm=7073833974c01d4e65aa62b34d1074912426a48a5ff44d10ed19482bee416141&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562833388314775/24.png?ex=6590106e&is=657d9b6e&hm=50d977c4627aa80b27c0e8d9c3d28e5719862beb81bd4241dc92ba6c5f65885b&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562833644179496/25.png?ex=6590106e&is=657d9b6e&hm=776e20e9d48aaa19cfd9d4602100c14ccf494bcf8c8ba3b2d71f2e16ef2c0420&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562833895817276/26.png?ex=6590106e&is=657d9b6e&hm=98b83329175ff83e76818e437679cce6bfba04a9cc5207dc3d8aa9bc0de82326&"},
        {"https://cdn.discordapp.com/attachments/1064977711543619595/1185562834126512128/27.png?ex=6590106e&is=657d9b6e&hm=dd11954d99fc872c1088aa74656505c8616856dbf6ae59f75636e35091f665a7&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562831895142491/28.png?ex=6590106d&is=657d9b6d&hm=49d1d59f6958506009909e20d2ce3a3644ea3ba5e8cf6ad159e34903b21a9a35&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562905681334312/29.png?ex=6590107f&is=657d9b7f&hm=64286b7c0c25fa618267491e287821f05215db3a8e22895600d3abf29a5612d4&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562905899446383/30.png?ex=6590107f&is=657d9b7f&hm=e930d91a216e7787f68f4c93500be6f358cf9bcbcb6499aac26d4a55ef02cc22&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562906234994729/31.png?ex=6590107f&is=657d9b7f&hm=9f440f6ec744f9227c8e1b7fb4329abf52a6435531536f09ba54cefbe7103fc0&", "https://cdn.discordapp.com/attachments/1064977711543619595/1185562905438076948/32.png?ex=6590107f&is=657d9b7f&hm=ef19f62ced2d85acede547e76bc3fb7c022741d5b89e7cad228a42b1597ecc41&"}
     };

            Bitmap combinedImage = CombineImages(imageUrls, user);
            Console.WriteLine("resim birleştirildi");

            ulong userId = 1064253803706208397;

            var _user = _client.GetUser(userId);

            // Bitmap'i MemoryStream'e dönüştür
            using (var stream = new MemoryStream())
            {
                combinedImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Position = 0;

                // MemoryStream'i Discord'a dosya olarak gönder
                var sentMessage = await _user.SendFileAsync(stream, "combined_image.png", "Here is your combined image:");

                // Dosyanın URL'sini al
                var fileUrl = sentMessage.Attachments.FirstOrDefault()?.Url;
                // Elde edilen URL'yi kullanabilirsinizc
                Console.WriteLine($"File URL: {fileUrl}");
                badgesEmbed.WithImageUrl(fileUrl);

                await arg.ModifyOriginalResponseAsync(res =>
                {
                    res.Embed = badgesEmbed.Build();
                    res.Components = componentBuilder.Build();
                });
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in seeBadges: {ex}");
        }
    }
    public static async Task pastTriviasForProfile(IDiscordInteraction arg)
    {
        try
        {
            List<User> _pastTrivias = LoadTriviasFromJson("TriviasData.json");
            EmbedBuilder embed = new EmbedBuilder().WithTitle("All of your trivias you solved").WithDescription($"You solved {_pastTrivias.Count} trivias");
            SelectMenuBuilder triviasMenu = new SelectMenuBuilder()
               .WithPlaceholder("Select a trivia")
               .WithCustomId("trivias#")
               .WithMinValues(1)
               .WithMaxValues(1)
               ;
            SelectMenuBuilder profileProcesses = new SelectMenuBuilder()
                .WithPlaceholder("Select an option")
                .WithCustomId("profile-processes")
                .AddOption("Profile", "profiles#")
                .AddOption("Badges", "badges#");
            for (int i = 0; i < _pastTrivias.Count; i++)
            {
                for (int j = 0; j < _pastTrivias[i].myTrivias.Count; j++)
                {
                    triviasMenu.AddOption(_pastTrivias[i].myTrivias[j].id.ToString(), _pastTrivias[i].myTrivias[j].id.ToString());
                }
            }

            ComponentBuilder builder = new ComponentBuilder();
            builder.WithSelectMenu(triviasMenu);
            builder.WithSelectMenu(profileProcesses);
            if (arg is SocketSlashCommand)
            {
                await (arg as SocketSlashCommand).RespondAsync(embed: embed.Build(), components: builder.Build(), ephemeral: true);
            }
            else if (arg is SocketMessageComponent)
            {
                await (arg as SocketMessageComponent).RespondAsync(embed: embed.Build(), components: builder.Build(), ephemeral: true);

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in pasttrivias: {ex.Message}");
        }


    }
    #endregion

    #region Bitmap
    private static Bitmap CombineImages(string[,] imageUrls, User user)
    {
        List<Bitmap> images = new List<Bitmap>();

        foreach (var categoryName in user.userStats.userAchievements.categories.Keys)
        {
            int index = (int.Parse(categoriesDic[categoryName].ToString()) - 9);
            int row = (int)Math.Floor(index / 6.0);
            int col = index % 6;

            if (row >= 0 && row < imageUrls.GetLength(0))
            {
                string imageUrl = imageUrls[row, col];
                Bitmap image = DownloadImage(imageUrl);
                int trueCount = user.userStats.userAchievements.categories[categoryName];

                if (user.userStats.userAchievements.categories[categoryName] > 5)
                {
                    int number = 0;

                    for (int i = 5; i < 55; i += 5)
                    {
                        if (i <= trueCount && trueCount < i + 5)
                        {
                            number = (i / 5);
                            break;
                        }
                    }

                    string romanNumeral = ConvertToRoman(number);
                    DrawRomanNumeral(image, romanNumeral);
                }
                else
                {
                    Bitmap bwImage = ConvertToBlackAndWhite(image);
                    images.Add(bwImage);
                    continue;
                }

                images.Add(image);
            }
            else
            {
                Console.WriteLine("Invalid row index");
            }
        }
        return CombineImages(images, 20, 10);
    }
    private static void DrawRomanNumeral(Bitmap image, string romanNumeral)
    {
        using (Graphics g = Graphics.FromImage(image))
        {
            Font font = new Font("Calisto MT", 120);
            System.Drawing.Color textColor = System.Drawing.Color.White;
            System.Drawing.Color shadowColor = System.Drawing.Color.Gray;

            SizeF textSize = g.MeasureString(romanNumeral, font);
            float x = image.Width - textSize.Width;
            float y = image.Height - textSize.Height + 10;

            // Metni çiz
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.DrawString(romanNumeral, font, new SolidBrush(shadowColor), x + 9, y + 9);
            g.DrawString(romanNumeral, font, new SolidBrush(textColor), x, y);
        }
    }
    private static string ConvertToRoman(int number)
    {
        string romenNumber = "";

        if (number >= 1 && number <= 10)
        {
            string[] romanNumerals = { "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X" };
            romenNumber = romanNumerals[number - 1];
        }
        return romenNumber;
    }
    private static Bitmap ConvertToBlackAndWhite(Bitmap image)
    {
        // Resmi siyah beyaz yap
        Bitmap bwImage = new Bitmap(image.Width, image.Height);

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                // Her pikselin renk değerlerini al
                System.Drawing.Color pixelColor = image.GetPixel(x, y);
                int avgColor = (pixelColor.R + pixelColor.G + pixelColor.B) / 3; // Ortalama rengi hesapla

                // Yeni siyah beyaz pikseli oluştur
                System.Drawing.Color newPixel = System.Drawing.Color.FromArgb(avgColor, avgColor, avgColor);
                bwImage.SetPixel(x, y, newPixel);
            }
        }

        return bwImage;
    }
    private static Bitmap DownloadImage(string imageUrl)
    {
        // Resmi internetten indir
        WebClient webClient = new WebClient();
        byte[] data = webClient.DownloadData(new Uri(imageUrl));
        return new Bitmap(new System.IO.MemoryStream(data));
    }
    private static Bitmap CombineImages(List<Bitmap> images, int spacing, int borderSize)
    {
        // Calculate dimensions considering spacing and borders
        int width = images[0].Width * 6 + (spacing * 5) + (borderSize * 2);
        int height = images[0].Height * 4 + (spacing * 3) + (borderSize * 2);

        Bitmap combinedImage = new Bitmap(width, height);

        using (Graphics g = Graphics.FromImage(combinedImage))
        {
            g.FillRectangle(Brushes.Black, 0, 0, width, height); // Fill background with black color

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 6; col++)
                {
                    int index = row * 6 + col;
                    Bitmap image = images[index];

                    int x = col * (image.Width + spacing) + borderSize; // Adjust x-coordinate for spacing and border
                    int y = row * (image.Height + spacing) + borderSize; // Adjust y-coordinate for spacing and border

                    // Draw a black border around each image
                    g.FillRectangle(Brushes.Black, x - borderSize, y - borderSize, image.Width + 2 * borderSize, image.Height + 2 * borderSize);

                    // Draw the image on top of the border
                    g.DrawImage(image, x, y, image.Width, image.Height);
                }
            }
        }

        return combinedImage;
    }
    #endregion

    #endregion
}
