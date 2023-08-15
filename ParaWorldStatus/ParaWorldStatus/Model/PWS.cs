using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace ParaWorldStatus.Model
{
    public class PWS : PWSModel
    {
        private static readonly DateTime _startTime = DateTime.UtcNow;
        private static readonly string _mirageExe = AppDomain.CurrentDomain.BaseDirectory + "../MIRAGE Launcher/MIRAGE Launcher.exe";
        private static readonly string _partyID = new String(Enumerable.Range(0, 32).Select(n => (Char)(new Random().Next(32, 127))).ToArray());

        public PWS()
        {
            if (Process.GetProcessesByName("ParaWorldStatus").Length > 1)
            {
                MessageBox.Show("ParaWorldStatus in already running", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                Application.Current.Shutdown();
            }
            else if (InitRPC())
            {
                PWSUpdate(true);
                pwState.StateChanged += PWSUpdate;
                pwState.PWStateFileWatcher();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }

        private string _title;
        public string Title { get { return _title; } set { _title = value; RaisePropertyChangedEvent("Title"); } }

        public static PWState pwState = new();

        private static RichPresence presence = new RichPresence
        {
            Details = "Starting PWS...",
            Assets = new Assets()
            {
                LargeImageKey = "mod_basegame",
                LargeImageText = "ParaWorld",
            },
            Timestamps = new Timestamps
            {
                Start = _startTime,
            }
        };

        public void UpdatePWS()
        {
            PWSUpdate(true);
        }

        //ParaWorld AppId: 723292670327390248
        private static DiscordRpcClient rpcClient;
        static bool InitRPC()
        {
            rpcClient = new DiscordRpcClient("723292670327390248", pipe: -1, logger: new ConsoleLogger(LogLevel.Trace, true), autoEvents: true, client: new ManagedNamedPipeClient());
            rpcClient.RegisterUriScheme();
            rpcClient.SkipIdenticalPresence = true;

            rpcClient.OnReady += OnReady;                                      //Called when the rpcClient is ready to send presences
            rpcClient.OnClose += OnClose;                                      //Called when connection to discord is lost
            rpcClient.OnError += OnError;                                      //Called when discord has a error
            rpcClient.OnConnectionEstablished += OnConnectionEstablished;      //Called when a pipe connection is made, but not ready
            rpcClient.OnConnectionFailed += OnConnectionFailed;                //Called when a pipe connection failed.
            rpcClient.OnPresenceUpdate += OnPresenceUpdate;                    //Called when the presence is updated
            rpcClient.OnSubscribe += OnSubscribe;                              //Called when a event is subscribed too
            rpcClient.OnUnsubscribe += OnUnsubscribe;                          //Called when a event is unsubscribed from.
            rpcClient.OnJoin += OnJoin;                                        //Called when the rpcClient wishes to join someone else. Requires RegisterUriScheme to be called.
            rpcClient.OnSpectate += OnSpectate;                                //Called when the rpcClient wishes to spectate someone else. Requires RegisterUriScheme to be called.
            //rpcClient.OnJoinRequested += OnJoinRequested;                      //Called when someone else has requested to join this rpcClient.
            rpcClient.SetSubscription(EventType.Join | EventType.Spectate);    //This will alert us if discord wants to join a game
            rpcClient.SetPresence(presence);

            if (!rpcClient.Initialize())
            {
                MessageBox.Show("ParaWorldStatus init failed", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }

        public void PWSUpdate(bool b)
        {
            presence = new RichPresence
            {
                Details = "Loading...",
                Assets = new Assets()
                {
                    LargeImageKey = "mod_basegame",
                    LargeImageText = "ParaWorld",
                },
                Timestamps = new Timestamps
                {
                    Start = _startTime,
                },
                Buttons = new DiscordRPC.Button[]
                {
                    new DiscordRPC.Button() { Label = "Visit ParaWelt", Url = "https://para-welt.com/" },
                    new DiscordRPC.Button() { Label = "Download MIRAGE mod", Url = "https://www.moddb.com/mods/paraworld-mirage" }
                },
            };

            if (pwState.StateType == "StateSmall")
            {
                presence.Details = pwState.Details;
                presence.Assets = new Assets()
                {
                    LargeImageKey = GetModImage(pwState.Mods, "").Key,
                    LargeImageText = GetModImage(pwState.Mods, "").Text,
                };
            }
            else if (pwState.StateType == "StateFull")
            {
                if (pwState.Details == "In game as spectator")
                {
                    presence.Details = pwState.Details;
                }
                else
                {
                    presence.Details = pwState.Details + GetTribeIcon(pwState.Tribe);
                }
                presence.State = GetState(pwState.GameMode, pwState.Additional);

                var MapInfo = new MapInfo(pwState.MapFile, pwState.MapName, pwState.MapSetting);
                presence.Assets = new Assets()
                {
                    LargeImageKey = GetMapImage(MapInfo).Key,
                    LargeImageText = GetMapImage(MapInfo).Text,
                    SmallImageKey = GetModImage(pwState.Mods, pwState.Additional).Key,
                    SmallImageText = GetModImage(pwState.Mods, pwState.Additional).Text,
                };

                presence.Party = new Party
                {
                    ID = _partyID,
                    Privacy = Party.PrivacySetting.Public,
                    Max = pwState.MaxPlayers,
                    Size = pwState.NumPlayers,
                };

                if (pwState.Details == "In multiplayer" && pwState.Additional == "Lobby" && !String.IsNullOrEmpty(pwState.Server))
                {
                    presence.Buttons = null;
                    if (pwState.MaxPlayers > pwState.NumPlayers)
                    {
                        //127.0.0.1:1111#BoosterPack1|MIRAGE...
                        string joinSecret = pwState.Server + "#" + pwState.Mods.Replace(" ", "|");
                        presence.Secrets = new Secrets() { JoinSecret = joinSecret };
                    }
                    else
                    {
                        //maxPlayer++;
                        string spectateSecret = pwState.Server + "#" + pwState.Mods.Replace(" ", "|");
                        presence.Secrets = new Secrets() { SpectateSecret = spectateSecret };
                    }
                }
            }
            rpcClient.SetPresence(presence);
        }

        private static string GetState(string details, string additional)
        {
            if (additional == "Lobby")
            {
                return additional;
            }
            return details;
        }
        private static string GetTribeIcon(string tribe)
        {
            return tribe switch
            {
                "Hu" => " as \U0001f417 tribe",
                "Aje" => " as \U0001f996 tribe",
                "Ninigi" => " as \U0001f432 tribe",
                "SEAS" => " as \U0001f9be tribe",
                "Random" => " as \u2753 tribe",
                //Mirage tribes 
                "Aje_Ninigi_SEAS" => " as \U0001f996|\U0001f432|\U0001f9be tribe",
                "Ninigi_Hu_SEAS" => " as \U0001f432|\U0001f417|\U0001f9be tribe",
                "Aje_Hu_SEAS" => " as \U0001f996|\U0001f417|\U0001f9be tribe",
                "Aje_Ninigi_Hu" => " as \U0001f996|\U0001f432|\U0001f417 tribe",
                "Aje_Ninigi" => " as \U0001f996|\U0001f432 tribe",
                "Ninigi_Hu" => " as \U0001f432|\U0001f417 tribe",
                "Hu_SEAS" => " as \U0001f417|\U0001f9be tribe",
                "SEAS_Aje" => " as \U0001f9be|\U0001f996 tribe",
                "Aje_Hu" => " as \U0001f996|\U0001f417 tribe",
                "Ninigi_SEAS" => " as \U0001f432|\U0001f9be tribe",
                _ => "",
            };
        }
        private static ImageData GetModImage(string mods, string additional)
        {
            string key = "mod_basegame";
            string text = String.Join(",", mods);

            if (mods.Contains("LevelEd"))
            {
                return new ImageData { Key = "state_editor", Text = String.Join("\n", mods) };
            }
            else if (mods.Contains("MIRAGE"))
            {
                key = "mod_mirage";
            }

            if (additional != "Lobby" && !String.IsNullOrEmpty(additional))
            {
                key = "game_paused";
                text = "Paused: " + additional;
            }

            return new ImageData { Key = key, Text = text };
        }

        private static readonly IDictionary<string, string> _mapNameTranslations = TextFileReader.ReadMapTranslations();
        private static ImageData GetMapImage(MapInfo map)
        {
            return new ImageData
            {
                Key = ImageService.GetImage(map),
                Text = _mapNameTranslations.TryGetValue(map.Filename, out var translatedMapName)
                    ? translatedMapName
                    : map.Name
            };
        }

        #region Events

        #region State Events
        private static void OnReady(object sender, ReadyMessage args)
        {
            //This is called when we are all ready to start receiving and sending discord events. 
            // It will give us some basic information about discord to use in the future.
            //It can be a good idea to send a inital presence update on this event too, just to setup the inital game state.
            Console.WriteLine("On Ready. RPC Version: {0}", args.Version);

        }
        private static void OnClose(object sender, CloseMessage args)
        {
            //This is called when our rpcClient has closed. The rpcClient can no longer send or receive events after this message.
            // Connection will automatically try to re-establish and another OnReady will be called (unless it was disposed).
            Console.WriteLine("Lost Connection with rpcClient because of '{0}'", args.Reason);
        }
        private static void OnError(object sender, ErrorMessage args)
        {
            //Some error has occured from one of our messages. Could be a malformed presence for example.
            // Discord will give us one of these events and its upto us to handle it
            Console.WriteLine("Error occured within discord. ({1}) {0}", args.Message, args.Code);
        }
        #endregion

        #region Pipe Connection Events
        private static void OnConnectionEstablished(object sender, ConnectionEstablishedMessage args)
        {
            //This is called when a pipe connection is established. The connection is not ready yet, but we have at least found a valid pipe.
            Console.WriteLine("Pipe Connection Established. Valid on pipe #{0}", args.ConnectedPipe);
        }
        private static void OnConnectionFailed(object sender, ConnectionFailedMessage args)
        {
            //This is called when the rpcClient fails to establish a connection to discord. 
            // It can be assumed that Discord is unavailable on the supplied pipe.
            Console.WriteLine("Pipe Connection Failed. Could not connect to pipe #{0}", args.FailedPipe);
        }
        #endregion

        private static void OnPresenceUpdate(object sender, PresenceMessage args)
        {
            //This is called when the Rich Presence has been updated in the discord rpcClient.
            // Use this to keep track of the rich presence and validate that it has been sent correctly.
            Console.WriteLine("Rich Presence Updated. Playing {0}", args.Presence == null ? "Nothing (NULL)" : args.Presence.State);
        }

        #region Subscription Events
        private static void OnSubscribe(object sender, SubscribeMessage args)
        {
            //This is called when the subscription has been made succesfully. It will return the event you subscribed too.
            Console.WriteLine("Subscribed: {0}", args.Event);
        }
        private static void OnUnsubscribe(object sender, UnsubscribeMessage args)
        {
            //This is called when the unsubscription has been made succesfully. It will return the event you unsubscribed from.
            Console.WriteLine("Unsubscribed: {0}", args.Event);
        }
        #endregion

        #region Join / Spectate feature
        private static void OnJoin(object sender, JoinMessage args)
        {
            if (File.Exists(_mirageExe))
            {
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "../MIRAGE Launcher/MIRAGE Launcher.exe", $"-PWSJoin -{args.Secret}");
                Console.WriteLine("OnJoin");
            }
            else
            {
                MessageBox.Show(Path.GetFullPath(_mirageExe) + " not found.", null, MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("OnJoin failed");
            }
        }

        private static void OnSpectate(object sender, SpectateMessage args)
        {
            if (File.Exists(_mirageExe))
            {
                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "../MIRAGE Launcher/MIRAGE Launcher.exe", $"-PWSJoin -{args.Secret}");
                Console.WriteLine("OnSpectate");
            }
            else
            {
                MessageBox.Show(Path.GetFullPath(_mirageExe) + " not found.", null, MessageBoxButton.OK, MessageBoxImage.Error);
                Console.WriteLine("OnSpectate failed");
            }
        }

        private static void OnJoinRequested(object sender, JoinRequestMessage args)
        {
            //MessageBox.Show("OnJoinRequested", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            /*
             * This is called when the Discord Client has received a request from another external Discord User to join your game.
             * You should trigger a UI prompt to your user sayings 'X wants to join your game' with a YES or NO button. You can also get
             *  other information about the user such as their avatar (which this library will provide a useful link) and their nickname to
             *  make it more personalised. You can combine this with more API if you wish. Check the Discord API documentation.
             *  
             *  Once a user clicks on a response, call the Respond function, passing the message, to respond to the request.
             *  A example is provided below.
             *  
             * This feature requires the RegisterURI to be true on the rpcClient.


            //We have received a request, dump a bunch of information for the user
            Console.WriteLine("'{0}' has requested to join our game.", args.User.Username);
            Console.WriteLine(" - User's Avatar: {0}", args.User.GetAvatarURL(User.AvatarFormat.GIF, User.AvatarSize.x2048));
            Console.WriteLine(" - User's Descrim: {0}", args.User.Discriminator);
            Console.WriteLine(" - User's Snowflake: {0}", args.User.ID);
            Console.WriteLine();

            //Ask the user if they wish to accept the join request.
            Console.Write("Do you give this user permission to join? [Y / n]: ");
            bool accept = Console.ReadKey().Key == ConsoleKey.Y; Console.WriteLine();

            //Tell the rpcClient if we accept or not.
            DiscordRpcClient client = (DiscordRpcClient)sender;
            client.Respond(args, accept);

            //All done.
            Console.WriteLine(" - Sent a {0} invite to the rpcClient {1}", accept ? "ACCEPT" : "REJECT", args.User.Username);
            */
        }
        #endregion

        #endregion
    }
}
