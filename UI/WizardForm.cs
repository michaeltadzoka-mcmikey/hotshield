using System;
using System.Drawing;
using System.Windows.Forms;
using Hotshield.Data;
using Hotshield.Helpers;
using Hotshield.Models;

namespace Hotshield.UI
{
    public partial class WizardForm : Form
    {
        private Label _stepLabel;
        private Button _btnNext;
        private int _step = 0;
        private string _detectedSsid = "";

        public WizardForm()
        {
            Text = "Welcome to Hotshield";
            Size = new Size(500, 320);
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.White;

            _stepLabel = new Label
            {
                Location = new Point(20, 20),
                Size = new Size(460, 180),
                Font = new Font("Segoe UI", 10F),
                Text = "Loading..."
            };
            Controls.Add(_stepLabel);

            _btnNext = new Button
            {
                Text = "Next",
                Location = new Point(200, 220),
                Size = new Size(90, 30)
            };
            _btnNext.Click += BtnNext_Click;
            Controls.Add(_btnNext);

            ShowStep0();
        }

        private void ShowStep0()
        {
            _detectedSsid = WlanHelper.GetCurrentSsid();
            
            string welcomeText = "🛡️ Welcome to Hotshield - Your Data Guardian!\n\n" +
                                "Hotshield helps you save mobile data by automatically blocking apps " +
                                "that waste data in the background (like Windows Update, OneDrive, etc.) " +
                                "when you're on a limited connection.\n\n";
            
            if (string.IsNullOrEmpty(_detectedSsid))
            {
                welcomeText += "📡 No WiFi detected right now.\n\n" +
                              "Connect to your mobile hotspot or limited WiFi, then open the Dashboard " +
                              "to mark it as 'metered' and choose which apps to block.";
                _btnNext.Text = "Get Started";
            }
            else
            {
                welcomeText += $"📡 You're connected to: \"{_detectedSsid}\"\n\n" +
                              "Is this a mobile hotspot or limited data connection that you want to protect?";
                _btnNext.Text = "Yes, Protect This Network";
            }
            
            _stepLabel.Text = welcomeText;
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            if (_step == 0)
            {
                if (!string.IsNullOrEmpty(_detectedSsid))
                {
                    // Add as metered with Hotspot Mode
                    var repo = new MeteredNetworkRepo();
                    if (repo.FindBySsid(_detectedSsid) == null)
                    {
                        var presets = PresetRepo.GetAll();
                        var hotspot = presets.Find(p => p.Name == "Hotspot Mode");
                        repo.Add(new MeteredNetwork { Ssid = _detectedSsid, PresetId = hotspot?.Id ?? 1 });
                    }
                }
                // Check USB tether
                if (AdapterDetector.IsUsbTetherActive())
                {
                    string adapter = AdapterDetector.GetUsbTetherAdapterName() ?? "USB Tether";
                    var repo = new MeteredNetworkRepo();
                    if (repo.FindBySsid(adapter) == null)
                    {
                        var presets = PresetRepo.GetAll();
                        var hotspot = presets.Find(p => p.Name == "Hotspot Mode");
                        repo.Add(new MeteredNetwork { Ssid = adapter, PresetId = hotspot?.Id ?? 1 });
                    }
                }
                _stepLabel.Text = "Hotshield is now protecting your connection.\n\nYou can manage rules and networks from the dashboard.\nThank you for using Hotshield!";
                _btnNext.Text = "Start";
                _step = 1;
            }
            else
            {
                SettingsRepo.Set("first_run_complete", "true");
                Close();
            }
        }
    }
}
