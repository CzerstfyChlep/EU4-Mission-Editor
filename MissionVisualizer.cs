using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Mission_Mkaer
{
    public partial class MissionVisualizer : Form
    {
        public MissionVisualizer()
        {
            InitializeComponent();
            Thread DrawingThread = new Thread(Drawing);
            DrawingThread.Start();
            this.DoubleBuffered = true;
            FormClosing += ClosingHandler;
        }

        public bool FormClosed = false;
        Font defaultfont = new Font(FontFamily.GenericMonospace, 20, FontStyle.Regular, GraphicsUnit.Pixel);
        public void ClosingHandler(object sender, EventArgs e)
        {
            FormClosed = true;
        }

        public void Drawing()
        {
            while (!FormClosed)
            {
                Thread.Sleep(15);
                Invalidate();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            foreach(MissionSeries ms in SharedVariables.MissionSeries)
            {
                foreach(Mission m in ms.Missions)
                {
                    Rectangle r = new Rectangle();
                    r.X = 40 + 100 * ms.Slot;
                    r.Y = 40 + 100 * m.Position;
                    r.Width = 60;
                    r.Height = 60;
                    e.Graphics.DrawRectangle(Pens.Black, r);
                    e.Graphics.DrawString(m.Name, defaultfont, Brushes.Black, r);
                }
            }
        }

    }
}
