using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VenturaBot.Services;
using VenturaBot.Handlers;
using VenturaBot.Builders;
using VenturaBot.TaskDefinitions;
using Microsoft.Extensions.Configuration;
using VenturaBot.Commands;

namespace VenturaBot
{
    public class Bot
    {
        private readonly DiscordSocketClient _client;
        private readonly InteractionService _interactions;
        private readonly IServiceProvider _services;

        public Bot()
        {
            Console.WriteLine("[Bot] Constructor");
            _services = BuildServiceProvider();
            _client = _services.GetRequiredService<DiscordSocketClient>();
            _interactions = _services.GetRequiredService<InteractionService>();
        }

        public async Task RunAsync()
        {
            SubscribeToLogs();
            SubscribeEventHandlers();
            await StartClientAsync();
            LoadData();
            Console.WriteLine("[Bot] Running");
            await Task.Delay(-1);
        }

        #region DI Setup

        private IServiceProvider BuildServiceProvider()
        {
            Console.WriteLine("[Bot] Building DI container...");
            var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();
            var services = new ServiceCollection()
                // Discord client
                .AddSingleton(provider =>
                {
                    Console.WriteLine("[Bot][DI] Configuring DiscordSocketClient");
                    return new DiscordSocketClient(new DiscordSocketConfig
                    {
                        LogLevel = LogSeverity.Info,
                        GatewayIntents =
                            GatewayIntents.AllUnprivileged
                            | GatewayIntents.MessageContent
                            | GatewayIntents.GuildMembers
                    });
                })
                // Interaction service
                .AddSingleton(provider =>
                {
                    Console.WriteLine("[Bot][DI] Configuring InteractionService");
                    return new InteractionService(provider.GetRequiredService<DiscordSocketClient>());
                })

                // Core services
                .AddSingleton<ITaskService, TaskService>()
                .AddSingleton<IGuildMemberService, GuildMemberService>()

                // Economy & Store
                .AddSingleton<IEconomyService, EconomyService>()
                .AddSingleton<StoreService>()
                .AddSingleton<StoreEmbedBuilder>()
                .AddSingleton<IStoreService>(sp => sp.GetRequiredService<StoreService>())

                // Hall of Fame
                .AddSingleton<ILeaderboardService, LeaderboardService>()
                .AddSingleton<HallOfFameEmbedBuilder>()
                .AddSingleton<HallOfFameUpdater>()
                .AddHostedService(provider => provider.GetRequiredService<HallOfFameUpdater>())

                // Event services & builders
                .AddSingleton<EventEmbedBuilder>()
                .AddSingleton<IEventService, EventService>()
                .AddSingleton<EventComponentBuilder>()

                // Task builders & handlers
                .AddSingleton<TaskEmbedBuilder>()
                .AddSingleton<TaskComponentBuilder>()
                .AddSingleton<ButtonHandler>()
                .AddSingleton<SelectMenuHandler>()
                .AddSingleton<ModalHandler>()
                .AddSingleton<ProfileEmbedBuilder>()
                //raffle
                .AddSingleton<RaffleService>()
                .AddSingleton<RaffleEmbedBuilder>()

            //level up service 
                .AddSingleton<LevelUpService>()
                .AddSingleton<IConfiguration>(configuration)
                // Recurring‐task system
                .AddSingleton<IRecurringRepo, FileBasedRecurringRepo>()
                .AddHostedService<RecurringTaskScheduler>();

            // Auto‐register all ITaskDefinition implementations
            var asm = Assembly.GetExecutingAssembly();
            foreach (var defType in asm
                .GetTypes()
                .Where(t => typeof(ITaskDefinition).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract))
            {
                services.AddSingleton(typeof(ITaskDefinition), defType);
            }

            var provider = services.BuildServiceProvider();
            Console.WriteLine("[Bot] DI container built");
            return provider;
        }

        #endregion

        #region Lifecycle

        private async Task StartClientAsync()
        {
            Console.WriteLine("[Bot] Loading environment variables");
            DotNetEnv.Env.Load();
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN")!;
            Console.WriteLine("[Bot] Logging in");
            await _client.LoginAsync(TokenType.Bot, token);
            Console.WriteLine("[Bot] Starting client");
            await _client.StartAsync();
        }

        private void LoadData()
        {
            Console.WriteLine("[Bot] Loading data services");
            DataService.Load();
            DuneFactService.LoadFacts();

            var taskService = _services.GetRequiredService<ITaskService>();
            taskService.Load();

            var memberService = _services.GetRequiredService<IGuildMemberService>();
            memberService.Load();
        }

        #endregion

        #region Event Handlers

        private void SubscribeToLogs()
        {
            _client.Log += LogAsync;
            _interactions.Log += LogAsync;
        }

        private void SubscribeEventHandlers()
        {
            _client.Ready += OnReadyAsync;
            _client.ButtonExecuted += OnButtonExecutedAsync;
            _client.SelectMenuExecuted += OnSelectMenuExecutedAsync;
            _client.ModalSubmitted += OnModalSubmittedAsync;
            _client.InteractionCreated += OnInteractionCreatedAsync;
        }

        private async Task OnReadyAsync()
        {
            Console.WriteLine("[Bot][Ready] Registering commands");

            // 1) Load all your slash-command modules
            var asm = Assembly.GetExecutingAssembly();
            await _interactions.AddModulesAsync(asm, _services);

            // 2) Register them into your test guild
            const ulong guildId = 1377887213764874423;
            Console.WriteLine($"[Bot][Ready] Registering to guild {guildId}");
            await _interactions.RegisterCommandsToGuildAsync(guildId);
            Console.WriteLine("[Bot][Ready] Commands registered");

            // 3) Download every guild member so GetUser(id) will return real SocketGuildUser
            //    (requires GatewayIntents.GuildMembers enabled in your DiscordSocketConfig)
            if (_client.GetGuild(guildId) is SocketGuild guild)
            {
                Console.WriteLine("[Bot][Ready] Downloading guild members...");
                await guild.DownloadUsersAsync();
                Console.WriteLine("[Bot][Ready] Guild members cached");
            }

            // 4) Force an immediate Hall of Fame update now that names can resolve
            var updater = _services.GetRequiredService<HallOfFameUpdater>();
            await updater.ForceUpdateAsync();
            Console.WriteLine("[Bot][Ready] Hall of Fame forced update");
        }


        private async Task OnButtonExecutedAsync(SocketMessageComponent component)
        {
            Console.WriteLine($"[Bot][ButtonExecuted] CustomId={component.Data.CustomId}");
            var handler = _services.GetRequiredService<ButtonHandler>();
            try
            {
                await handler.HandleButtonAsync(component);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot][ButtonExecuted] Exception: {ex}");
            }
        }

        private async Task OnSelectMenuExecutedAsync(SocketMessageComponent component)
        {
            Console.WriteLine($"[Bot][SelectMenuExecuted] CustomId={component.Data.CustomId}");
            var handler = _services.GetRequiredService<SelectMenuHandler>();
            try
            {
                await handler.HandleSelectMenuAsync(component);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot][SelectMenuExecuted] Exception: {ex}");
            }
        }

        private async Task OnModalSubmittedAsync(SocketModal modal)
        {
            Console.WriteLine($"[Bot][ModalSubmitted] CustomId={modal.Data.CustomId}");
            var handler = _services.GetRequiredService<ModalHandler>();
            try
            {
                await handler.HandleModalAsync(modal);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot][ModalSubmitted] Exception: {ex}");
            }
        }

        private async Task OnInteractionCreatedAsync(SocketInteraction interaction)
        {
            Console.WriteLine($"[Bot][InteractionCreated] Type={interaction.Type}");
            var context = new SocketInteractionContext(_client, interaction);
            try
            {
                await _interactions.ExecuteCommandAsync(context, _services);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Bot][InteractionCreated] Exception: {ex}");
            }
        }

        #endregion

        #region Utilities

        private Task LogAsync(LogMessage msg)
        {
            if (msg.Exception != null)
                Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}\n{msg.Exception}");
            else
                Console.WriteLine($"[{msg.Severity}] {msg.Source}: {msg.Message}");
            return Task.CompletedTask;
        }

        #endregion
    }
}
