using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ComponentFactory.Krypton.Toolkit;
using Newtonsoft.Json;

using Fiddler;
using System.Net;

namespace Ana_kata
{
    public partial class Ana_kata : KryptonForm
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
            public settings.Queue.Rootobject queue = null;
            public string cookie = null;
            public string market = null;
            public string response = null;
        }

        Variables variables = new Variables();

        public Ana_kata(settings.Profile.Rootobject settings_profile)
        {
            InitializeComponent();
            initialize_variables(settings_profile);
            initialize_workers();
        }

        private void initialize_workers()
        {
            variables.worker_save.DoWork += new DoWorkEventHandler(saver);
            variables.worker_update.DoWork += new DoWorkEventHandler(update_market);
            variables.worker_cookie.DoWork += new DoWorkEventHandler(save_cookie);
        }

        private void initialize_variables(settings.Profile.Rootobject settings_profile)
        {
            variables.profile = settings_profile;

            switch_autorun.Checked = settings_profile.autorun;
            switch_auto_update.Checked = settings_profile.market.autoupdate;
            switch_active_market.Checked = settings_profile.market.activate;
            label_market_path.Text = settings_profile.market.path;
            switch_save_cookie.Checked = settings_profile.cookie.autosave;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            event_worker(variables.worker_update);
        }

        private void event_worker(BackgroundWorker worker)
        {
            worker.RunWorkerAsync();

            while (worker.IsBusy == true)
            {
                Application.DoEvents();
            }
        }

        private void update_market(object sender, EventArgs e)
        {
            string output = "market\\market.json";

            if (variables.manager.get_switch_button(switch_auto_update) == true)
            {
                load_market(output);
            }
        }

        private void saver(object sender, EventArgs e)
        {
            variables.profile.autorun = variables.manager.get_switch_button(switch_autorun);
            variables.profile.market.activate = variables.manager.get_switch_button(switch_active_market);
            variables.profile.market.autoupdate = variables.manager.get_switch_button(switch_auto_update);
            variables.profile.market.path = variables.manager.get_label(label_market_path);
            variables.profile.cookie.autosave = variables.manager.get_switch_button(switch_save_cookie);

            File.WriteAllText("settings\\profile.json", JsonConvert.SerializeObject(variables.profile));
        }

        private void button_exit_Click(object sender, EventArgs e)
        {
            event_worker(variables.worker_save);

            Close();
        }

        private void button_reduce_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
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
            variables.market = File.ReadAllText(path);
            variables.manager.label(label_market_path, path, Color.Violet);
        }

        private void button_play_Click(object sender, EventArgs e)
        {
            InstallCertificate();
            Start();
        }

        private void button_stop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        public bool InstallCertificate()
        {
            Cursor.Current = Cursors.WaitCursor;

            if (!CertMaker.rootCertExists())
            {
                if (!CertMaker.createRootCert())
                {
                    Cursor.Current = Cursors.Default;
                    return (false);
                }

                if (!CertMaker.trustRootCert())
                {
                    Cursor.Current = Cursors.Default;
                    return (false);
                }
            }
            Cursor.Current = Cursors.Default;
            return (true);
        }

        public bool UninstallCertificate()
        {
            Cursor.Current = Cursors.WaitCursor;

            if (CertMaker.rootCertExists())
            {
                if (!CertMaker.removeFiddlerGeneratedCerts(true))
                {
                    Cursor.Current = Cursors.Default;
                    return (false);
                }
            }
            Cursor.Current = Cursors.Default;
            return (true);
        }

        private void label_cookie_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(variables.manager.get_label(label_cookie));
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
            if (FiddlerApplication.IsStarted() == false)
                FiddlerApplication.Startup(startupSettings);

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
            File.WriteAllText("cookie.txt", variables.manager.get_label(label_cookie));
        }

        private void update_cookie(string future)
        {
            if (variables.cookie != future)
            {
                variables.cookie = future;
                variables.manager.label(label_cookie, variables.cookie, Color.Violet);
                if (variables.manager.get_switch_button(switch_save_cookie) == true)
                {
                    event_worker(variables.worker_cookie);
                }
            }
        }

        private void Bypasser(Session sess)
        {
            sess.bBufferResponse = true;

            if (sess != null && sess.oRequest != null && sess.oRequest.headers != null)
            {
                if (sess.fullUrl.Contains("bhvrdbd") == true)
                {
                    if (sess.RequestHeaders.ToString().Contains("bhvrSession=") == true)
                    {
                        update_cookie(sess.RequestHeaders["Cookie"].Replace("bhvrSession=", ""));
                    }
                    if (sess.fullUrl.Contains("/v1/inventories") == true && variables.manager.get_switch_button(switch_active_market) == true)
                    {
                        sess.utilDecodeResponse();
                        sess.utilSetResponseBody(variables.market);
                    }
                    if (sess.fullUrl.Contains("/v1/queue") == true)
                    {
                        variables.response = sess.GetResponseBodyAsString();
                        if (variables.response.Contains("queueData") == true && variables.response.Contains("position") == true)
                        {
                            variables.queue = JsonConvert.DeserializeObject<settings.Queue.Rootobject>(
                                variables.response
                            );
                            if (variables.queue.status == "QUEUED")
                            {
                                variables.manager.label(label_queue, $"{variables.queue.queueData.position}", Color.Violet);
                            }
                        }
                    }
                }
            }
        }

        private void Ana_kata_Shown(object sender, EventArgs e)
        {
            if (variables.profile.autorun == true)
            {
                InstallCertificate();
                Start();
            }
        }

        private void Ana_kata_FormClosing(object sender, FormClosingEventArgs e)
        {
            Stop();
            UninstallCertificate();
        }
    }
}
