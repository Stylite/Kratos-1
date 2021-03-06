﻿using System;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Kratos.Services;
using Kratos.Configs;

namespace Kratos
{
    class Program
    {
        static void Main(string[] args) => new Program().Start().GetAwaiter().GetResult();

        #region Private fields
        private DiscordSocketClient _client;
        private BlacklistService _blacklist;
        private PermissionsService _permissions;
        private UsernoteService _usernotes;
        private RecordService _records;
        private TagService _tags;
        private UnpunishService _unpunish;
        private LogService _log;
        private SlowmodeService _slowmode;
        private RatelimitService _ratelimit;
        private AliasTrackingService _aliases;
        private ModerationService _mod;
        private CommandHandler _commands;
        private CoreConfig _config;
        #endregion

        public async Task Start()
        {
            Console.Title = "Kratos";

            // Set up our Discord client
            _client = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                MessageCacheSize = 100
            });

            _client.Log += Log;

            // Set up the config directory and core config
            if (!Directory.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config")))
                Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "config"));

            if (File.Exists(Path.Combine(Directory.GetCurrentDirectory(), "config", "core.json")))
                _config = await CoreConfig.UseCurrentAsync();
            else
                _config = await CoreConfig.CreateNewAsync();

            // Set up services
            _mod = new ModerationService();
            await _mod.LoadConfigurationAsync();

            _usernotes = new UsernoteService();

            _records = new RecordService();

            _tags = new TagService();

            _log = new LogService(_client);
            await _log.LoadConfigurationAsync();

            _unpunish = new UnpunishService(_client, _blacklist, _log, _records, _config);
            await _unpunish.GetRecordsAsync();

            _slowmode = new SlowmodeService(_client, _log, _unpunish, _records, _config);

            _ratelimit = new RatelimitService(_client, _config, _records, _unpunish, _log);
            await _ratelimit.LoadConfigurationAsync();
            if (_ratelimit.IsEnabled)
                _ratelimit.Enable(_ratelimit.Limit);

            _blacklist = new BlacklistService(_client, _unpunish, _records, _log, _config);

            _aliases = new AliasTrackingService(_client);
            await _aliases.LoadConfigurationAsync();

            _permissions = new PermissionsService();
            _permissions.LoadPermissions(Assembly.GetEntryAssembly());
            await _permissions.LoadConfigurationAsync();

            // Instantiate the dependency map and add our services and client to it.
            var serviceProvider = ConfigureServices();

            // Set up command handler
            _commands = new CommandHandler(serviceProvider);
            await _commands.InstallAsync();

            // Set up an event handler to execute some state-reliant startup tasks
            _client.Ready += async () =>
            {
                await _blacklist.LoadConfigurationAsync();
            };
            // Connect to Discord
            await _client.LoginAsync(TokenType.Bot, _config.Token);
            await _client.StartAsync();

            // Start unpunisher loop
            await _unpunish.StartAsync();

            // Hang indefinitely
            await Task.Delay(-1);
        }

        private Task Log(LogMessage m)
        {
            switch (m.Severity)
            {
                case LogSeverity.Warning: Console.ForegroundColor = ConsoleColor.Yellow; break;
                case LogSeverity.Error: Console.ForegroundColor = ConsoleColor.Red; break;
                case LogSeverity.Critical: Console.ForegroundColor = ConsoleColor.DarkRed; break;
                case LogSeverity.Verbose: Console.ForegroundColor = ConsoleColor.White; break;
            }

            Console.WriteLine(m.ToString());
            if (m.Exception != null)
                Console.WriteLine(m.Exception);
            Console.ForegroundColor = ConsoleColor.Gray;

            return Task.CompletedTask;
        }

        private IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();
            services.AddSingleton(_mod);
            services.AddSingleton(_blacklist);
            services.AddSingleton(_aliases);
            services.AddSingleton(_permissions);
            services.AddSingleton(_usernotes);
            services.AddSingleton(_records);
            services.AddSingleton(_tags);
            services.AddSingleton(_unpunish);
            services.AddSingleton(_log);
            services.AddSingleton(_slowmode);
            services.AddSingleton(_ratelimit);
            services.AddSingleton(_client);
            services.AddSingleton(_config);
            services.AddSingleton(new CommandService());

            return new DefaultServiceProviderFactory().CreateServiceProvider(services);
        }
    }
}
