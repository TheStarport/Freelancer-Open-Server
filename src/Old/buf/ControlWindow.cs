using System;
using System.Windows.Forms;
using FLServer.AppDomain;

namespace FLServer
{
    public partial class ControlWindow : Form
    {
        private readonly CustomContext _context;

        public ControlWindow()
        {
            InitializeComponent();
            _context = (new CustomContext()).CurrentContext;
            _context.GetLogger.OnMessage += GetLogger_OnMessage;
            Old.CharacterDB.Database.AddCallback(_context.GetLogger);
        }

        private void GetLogger_OnMessage(string msg)
        {
            if (InvokeRequired)
            {
                var d = new GotMessageInvoke(GetLogger_OnMessage);
                Invoke(d, new object[] {msg});
                return;
            }

            listBoxLog.Items.Add(msg);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void buttonStartServer_Click(object sender, EventArgs e)
        {
            _context.StartServer();
            button1.Enabled = false;
            button2.Enabled = true;
        }

        private void buttonStopServer_Click(object sender, EventArgs e)
        {
            _context.StopServer();
            button1.Enabled = true;
            button2.Enabled = false;
        }

        private void checkBoxLogDPlay_CheckedChanged(object sender, EventArgs e)
        {
            _context.GetLogger.LogMask[(int) LogType.DPLAY_MSG] = checkBoxLogDPlay.Checked;
        }

        private void checkBoxLogFLMsg_CheckedChanged(object sender, EventArgs e)
        {
            _context.GetLogger.LogMask[(int) LogType.FL_MSG] = checkBoxLogFLMsg.Checked;
        }

        private void checkBoxLogPositionUpdates_CheckedChanged(object sender, EventArgs e)
        {
            _context.GetLogger.LogMask[(int) LogType.FL_MSG2] = checkBoxLogPositionUpdates.Checked;
        }

        private delegate void GotMessageInvoke(string msg);

        private void ControlWindow_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void listBoxLog_SelectedIndexChanged(object sender, EventArgs e)
        {
            string copyBuffer = "";
            foreach (var item in listBoxLog.SelectedItems)
            {
                copyBuffer += item.ToString() + "\n";
            }
            if (copyBuffer != "")
            {
                Clipboard.SetText(copyBuffer);
            }
        }
    }
}