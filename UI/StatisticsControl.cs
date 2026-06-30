using System;
using System.Drawing;
using System.Windows.Forms;

namespace Hotshield.UI
{
    public class StatisticsControl : UserControl
    {
        public StatisticsControl()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(30)
            };
            panel.Controls.Add(new Label
            {
                Text = "📊 Data Usage Statistics",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 0);

            panel.Controls.Add(new Label
            {
                Text = "Coming in a future update.\n\nYou'll be able to see how much data each blocked app tried to use,\nhelping you understand where your limited data goes.",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.DimGray,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            }, 0, 1);

            var placeholderIcon = new Label
            {
                Text = "📈",
                Font = new Font("Segoe UI", 48F, FontStyle.Regular),
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panel.Controls.Add(placeholderIcon, 0, 2);

            Controls.Add(panel);
        }
    }
}
