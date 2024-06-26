﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;

using ComponentFactory.Krypton.Toolkit;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Fiddler;
using System.Net;
using DiscordRPC;

namespace Pochita
{
    public partial class Pochita : KryptonForm
    {
        class Variables
        {
            public settings.Profile.Rootobject profile = null;
            public Dictionary<string, Color> status = new Dictionary<string, Color>()
            {
                { "running", Color.LimeGreen },
                { "paused", Color.Orange },
                { "stopped", Color.Red },
            };
            public components.Manager manager = new components.Manager();
            public BackgroundWorker worker_save = new BackgroundWorker();
            public BackgroundWorker worker_update = new BackgroundWorker();
            public BackgroundWorker worker_run = new BackgroundWorker();
            public BackgroundWorker worker_cookie = new BackgroundWorker();
            public BackgroundWorker worker_logger = new BackgroundWorker();
            public string cookie = null;
            public string market = null;
            public string response = null;
            public string playername = null;
            public string server_ip = null;
            public string server_region = null;
            public string token = null;
            public List<string> banned = new List<string>()
            {
                "InvalidTokenException",
                "NotAllowedException",
                "localizationCode"
            };
            public string previous = null;
        }

        Variables variables = new Variables();

        public Pochita(settings.Profile.Rootobject settings_profile)
        {
            InitializeComponent();
            initialize_variables(settings_profile);
            initialize_workers();
            RPC();
        }

        private void initialize_workers()
        {
            variables.worker_save.DoWork += new DoWorkEventHandler(saver);
            variables.worker_update.DoWork += new DoWorkEventHandler(update_market);
            variables.worker_cookie.DoWork += new DoWorkEventHandler(save_cookie);
        }

        static async Task RPC()
        {
            var client = new DiscordRpcClient("1230928127614783488");
            client.Initialize();
            var buttons = new DiscordRPC.Button[]
            {
                new DiscordRPC.Button() { Label = "Get Pochita", Url = "https://github.com/0xsharkboy/Pochita" }
            };
            var presence = new RichPresence()
            {
                Details = "Unlocking everything...",
                State = "Fuck TCSM <3",
                Timestamps = new Timestamps(DateTime.UtcNow),
                Assets = new Assets()
                {
                    LargeImageKey = "pochita",
                    LargeImageText = "Fuck TCSM",
                },
                Buttons = buttons
            };
            client.SetPresence(presence);
            await Task.Delay(-1);
        }

        private void initialize_variables(settings.Profile.Rootobject settings_profile)
        {
            variables.profile = settings_profile;

            switch_autorun.Checked = settings_profile.autorun;
            switch_auto_update.Checked = settings_profile.market.autoupdate;
            switch_active_market.Checked = settings_profile.market.activate;
            switch_active_streamer.Checked = settings_profile.streamer;
            switch_disable_telemetry.Checked = settings_profile.telemetry;
            label_market_path.Text = settings_profile.market.path;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            event_worker(variables.worker_update);
        }

        private void eventer(string function, string message)
        {
            if (variables.previous != function)
            {
                variables.previous = function;
                send_log($"{function}:", message);
            } else
            {
                send_log(get_space(variables.previous.Length + 3), message);
            }
        }

        private string get_space(int size)
        {
            string spaces = "";

            for (int i = 0; i < size; i++)
                spaces += " ";

            return (spaces);
        }

        private void event_worker(BackgroundWorker worker)
        {
            worker.RunWorkerAsync();

            while (worker.IsBusy == true)
            {
                Application.DoEvents();
            }
        }

        private void send_log(string sender, string message)
        {

        }

        private void update_market(object sender, EventArgs e)
        {
            string output = "market\\market.json";

            Console.WriteLine($"updating market");
            if (variables.manager.get_switch_button(switch_auto_update) == true)
            {
                Console.WriteLine($"loading {output}");
                load_market(output);
            }
            else
            {
                Console.WriteLine($"loading {variables.profile.market.path}");
                load_market(variables.profile.market.path);
            }
        }

        private void saver(object sender, EventArgs e)
        {
            variables.profile.autorun = variables.manager.get_switch_button(switch_autorun);
            variables.profile.market.activate = variables.manager.get_switch_button(switch_active_market);
            variables.profile.market.autoupdate = variables.manager.get_switch_button(switch_auto_update);
            variables.profile.streamer = variables.manager.get_switch_button(switch_active_streamer);
            variables.profile.telemetry = variables.manager.get_switch_button(switch_disable_telemetry);
            variables.profile.market.path = variables.manager.get_label(label_market_path);

            Console.WriteLine($"saving current profile:");
            Console.WriteLine($"variables.profile.autorun: {variables.profile.autorun}");
            Console.WriteLine($"variables.profile.market.activate: {variables.profile.market.activate}");
            Console.WriteLine($"variables.profile.market.autoupdate: {variables.profile.market.autoupdate}");
            Console.WriteLine($"variables.profile.market.path: {variables.profile.market.path}");
            Console.WriteLine($"variables.profile.cookie.autosave: {variables.profile.cookie.autosave}");

            Console.WriteLine($"writting profile");
            File.WriteAllText("settings\\profile.json", JsonConvert.SerializeObject(variables.profile));
            Console.WriteLine($"profile written");
        }

        private void button_exit_Click(object sender, EventArgs e)
        {
            event_worker(variables.worker_save);

            Close();
        }

        private void button_reduce_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"reducing window");
            WindowState = FormWindowState.Minimized;
            Console.WriteLine($"window reduced");
        }

        private void label_market_path_Click(object sender, EventArgs e)
        {
            update_market();
        }

        private void label3_Click(object sender, EventArgs e)
        {
            update_market();
        }

        private void update_market()
        {
            open_market.ShowDialog();

            if (open_market.FileName != string.Empty)
            {
                load_market(open_market.FileName);
                variables.manager.set_switch_button(switch_auto_update, false);
            }
        }

        private void load_market(string path)
        {
            Console.WriteLine($"loading market");

            if (File.Exists(path) == true)
            {
                Console.WriteLine($"market path found: {path}");
                Console.WriteLine($"reading market file");
                variables.market = File.ReadAllText(path);
                Console.WriteLine($"market file read");
                Console.WriteLine($"updating label");
                variables.manager.label(label_market_path, path, Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(137)))), ((int)(((byte)(86))))));
                Console.WriteLine($"label updated");
            }
            else
            {
                Console.WriteLine($"market path not found");
                Console.WriteLine($"updating path");
                variables.manager.label(label_market_path, "path not found", Color.Red);
                Console.WriteLine($"path updated");
            }
        }

        private void button_play_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"starting pochita catch");
            InstallCertificate();
            Start();
            Console.WriteLine($"pochita catch started");
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            Console.WriteLine($"stopping pochita catch");
            Stop();
            Console.WriteLine($"pochita catch stopped");
        }

        public bool InstallCertificate()
        {
            Console.WriteLine($"installing certificate");
            Cursor.Current = Cursors.WaitCursor;

            if (!CertMaker.rootCertExists())
            {
                Console.WriteLine($"root certificate doesn't exists");
                if (!CertMaker.createRootCert())
                {
                    Console.WriteLine($"failed to create root certificate");
                    Cursor.Current = Cursors.Default;
                    return (false);
                }
                else
                {
                    Console.WriteLine($"successfully to create root certificate");
                }

                if (!CertMaker.trustRootCert())
                {
                    Console.WriteLine($"failed to trust root certificate");
                    Cursor.Current = Cursors.Default;
                    return (false);
                }
                else
                {
                    Console.WriteLine($"successfully to trust root certificate");
                }
            } else
            {
                Console.WriteLine($"certificate root exists");
            }

            Cursor.Current = Cursors.Default;
            return (true);
        }

        public bool UninstallCertificate()
        {
            Cursor.Current = Cursors.WaitCursor;

            if (CertMaker.rootCertExists())
            {
                Console.WriteLine($"root certificate exists");
                if (!CertMaker.removeFiddlerGeneratedCerts(true))
                {
                    Console.WriteLine($"certificate root removed");
                    Cursor.Current = Cursors.Default;
                    return (false);
                }
                else
                {
                    Console.WriteLine($"failed to remove certificate root");
                }
            }
            else
            {
                Console.WriteLine($"no certificate root to remove");
            }
            Cursor.Current = Cursors.Default;
            return (true);
        }

        private void label_cookie_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(variables.manager.get_label(label_cookie));
        }

        private void label_token_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(variables.manager.get_label(label_token));
        }

        private void FiddlerApplication_BeforeRequest(Session oSession)
        {
            oSession.bBufferResponse = true;
        }

        public void Start()
        {
            FiddlerCoreStartupSettings startupSettings = new FiddlerCoreStartupSettingsBuilder()
                .RegisterAsSystemProxy()
                .DecryptSSL()
                .Build();
            Console.WriteLine($"checking proxy status");
            Console.WriteLine($"proxy running: {FiddlerApplication.IsStarted()}");
            if (FiddlerApplication.IsStarted() == false)
            {
                Console.WriteLine($"starting proxy");
                FiddlerApplication.Startup(startupSettings);
                Console.WriteLine($"proxy started");
            }

            FiddlerApplication.BeforeRequest += FiddlerApplication_BeforeRequest;
            FiddlerApplication.BeforeResponse += Bypasser;

            variables.manager.button(button_play, false);
            variables.manager.button(button_stop, true);
        }

        public void Stop()
        {
            FiddlerApplication.BeforeResponse -= Bypasser;

            if (FiddlerApplication.IsStarted())
                FiddlerApplication.Shutdown();

            variables.manager.button(button_play, true);
            variables.manager.button(button_stop, false);
        }

        private void save_cookie(object sender, EventArgs e)
        {
            List<string> credentials = new List<string>()
            {
                $"playername: {variables.playername}",
                $"token: {variables.token}",
                $"cookie: {variables.cookie}"
            };

            if (variables.playername != null && variables.token != null && variables.cookie != null)
            {
                Console.WriteLine($"credentials:");
                foreach (string data in credentials)
                {
                    Console.WriteLine($"\t{data}");
                }
                Console.WriteLine($"dumping credentials in 'cookie.txt'");
                File.WriteAllLines("cookie.txt", credentials);
                Console.WriteLine($"credentials dumped");
            }
        }

        private void update_cookie(string future)
        {
            if (variables.cookie != future)
            {
                variables.cookie = future;
                variables.manager.label(label_cookie, variables.cookie, Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(137)))), ((int)(((byte)(86))))));
                Console.WriteLine($"cookie: {variables.cookie}");
            }
        }

        private void update_token(string future)
        {
            if (variables.token != future)
            {
                variables.token = future;
                variables.manager.label(label_token, variables.token, Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(137)))), ((int)(((byte)(86))))));
                Console.WriteLine($"token: {variables.token}");
            }
        }

        private void update_playername(string future)
        {
            if (variables.playername != future)
            {
                variables.playername = future;
                variables.manager.label(label_playername, variables.playername, Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(137)))), ((int)(((byte)(86))))));
                Console.WriteLine($"playername: {variables.playername}");
            }
        }

        private void update_server_ip(string future)
        {
            if (variables.server_ip != future)
            {
                variables.server_ip = future;
                variables.manager.label(label_server_ip, variables.server_ip, Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(137)))), ((int)(((byte)(86))))));
                Console.WriteLine($"playername: {variables.server_ip}");
            }
        }
        private void update_server_region(string future)
        {
            if (variables.server_ip != future)
            {
                variables.server_region = future;
                variables.manager.label(label_server_region, variables.server_region, Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(137)))), ((int)(((byte)(86))))));
                Console.WriteLine($"playername: {variables.server_region}");
            }
        }

        private void Bypasser(Session sess)
        {
            sess.bBufferResponse = true;

            if (sess != null && sess.oRequest != null && sess.oRequest.headers != null)
            {
                if (sess.fullUrl.Contains("playfabapi") == true)
                {
                    if (sess.fullUrl.Contains("LoginWithSteam") && variables.playername == null)
                    {
                        Console.WriteLine($"checking playername");
                        update_playername(JObject.Parse(sess.GetResponseBodyAsString())["data"]["InfoResultPayload"]["AccountInfo"]["SteamInfo"]["SteamName"].ToString(Formatting.None).Replace("\"", ""));
                        Console.WriteLine($"playername checked");
                    }
                    if (sess.fullUrl.Contains("LoginWithSteam") && variables.manager.get_switch_button(switch_active_streamer) == true)
                    {
                        JObject login_infos = JObject.Parse(sess.GetResponseBodyAsString());
                        login_infos["data"]["InfoResultPayload"]["AccountInfo"]["SteamInfo"]["SteamName"] = "Pochita";
                        string modifiedResponse = login_infos.ToString(Formatting.None);
                        sess.utilSetResponseBody(modifiedResponse);
                        Console.WriteLine($"playername changed");
                    }
                    if (sess.fullUrl.Contains("GetEntityToken") && variables.token == null)
                    {
                        Console.WriteLine($"checking token");
                        update_token(JObject.Parse(sess.GetResponseBodyAsString())["data"]["EntityToken"].ToString(Formatting.None).Replace("\"", ""));
                        Console.WriteLine($"token checked");
                    }
                    if (sess.fullUrl.Contains("GetUserData") && variables.cookie == null)
                    {
                        Console.WriteLine($"checking ID");
                        update_cookie(JObject.Parse(sess.GetRequestBodyAsString())["PlayFabId"].ToString(Formatting.None).Replace("\"", ""));
                        Console.WriteLine($"ID checked");
                    }
                    if (sess.fullUrl.Contains("GetUserInventory") == true && variables.manager.get_switch_button(switch_active_market) == true)
                    {
                        Console.WriteLine($"bypassing inventory limitations");
                        sess.utilDecodeResponse();
                        sess.utilSetResponseBody(variables.market);
                        Console.WriteLine($"inventory limitations bypassed");
                    }
                    if (sess.fullUrl.Contains("ExecuteFunction") && sess.GetRequestBodyAsString().Contains("v4_FindServer"))
                    {
                        Console.WriteLine($"checking server ip");
                        update_server_ip(JObject.Parse(sess.GetResponseBodyAsString())["data"]["FunctionResult"]["ipAddress"].ToString(Formatting.None).Replace("\"", ""));
                        update_server_region(JObject.Parse(sess.GetRequestBodyAsString())["FunctionParameter"]["Region"].ToString(Formatting.None).Replace("\"", ""));
                        Console.WriteLine($"server ip checked");
                    }
                    if (sess.fullUrl.Contains("/Event/WriteTelemetryEvents") && variables.manager.get_switch_button(switch_disable_telemetry) == true)
                    {
                        sess.oRequest.FailSession(403, "Blocked by Pochita <3", "Fuck you");
                        Console.WriteLine("Blocked Telemetry request");
                    }
                    if (sess.fullUrl.Contains("LoginWithSteam") == true)
                    {
                        Console.WriteLine($"provided called");
                        variables.response = JObject.Parse(sess.GetResponseBodyAsString())["data"]["InfoResultPayload"]["AccountInfo"]["TitleInfo"]["isBanned"].ToString(Formatting.None);
                        if (variables.response != null && variables.response != string.Empty)
                        {
                            Console.WriteLine($"checking current ban status");
                            Console.WriteLine(variables.response);
                            if (variables.response == "false")
                            {
                                variables.manager.label(label_banned, "not banned", Color.LimeGreen);
                            }
                            else
                            {
                                variables.manager.label(label_banned, "banned", Color.Red);
                            }
                            Console.WriteLine($"ban status checked");
                        }
                    }
                }
            }
        }

        private bool is_banned(List<string> messages, string body)
        {
            foreach (string message in messages)
            {
                if (body.Contains(message) == true)
                {
                    Console.WriteLine($"client banned");
                    return (true);
                }
            }
            Console.WriteLine($"client not banned");
            return (false);
        }

        private void Pochita_Shown(object sender, EventArgs e)
        {
            if (variables.profile.autorun == true)
            {
                InstallCertificate();
                Start();
            }
        }

        private void Pochita_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
            UninstallCertificate();
        }
    }
}
