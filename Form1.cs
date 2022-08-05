using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace Mission_Mkaer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            MainForm = this;
            Block.BlockHeight = this.CreateGraphics().MeasureString("P", defaultfont).Height;
            Thread DrawingThread = new Thread(Drawing);
            this.DoubleBuffered = true;
            Main.Area.Y = 0;
            Main.AddBlock(new OpenBlock("ok"));
            OpenBlock MissionBlock = new OpenBlock();
            MissionBlock.Type = BlockType.MissionMainBlock;
            MissionBlock.Variable = "mission";
            MissionBlock.AddBlock(new ClosedBlock("slot") { Color = Color.Brown });
            MissionBlock.AddBlock(new OpenBlock("something"));
            (MissionBlock.Contents[1] as OpenBlock).AddBlock(new ClosedBlock("idk") { Color = Color.Pink });
            Main.AddBlock(MissionBlock);
            
            DrawingThread.Start();
            FormClosing += ClosingHandler;
            MouseDown += UserMouseInput;
            MouseUp += UserRelease;
            KeyDown += UserKeyboardInput;
            
        }
        public static Form MainForm;
        public void ClosingHandler(object sender, EventArgs e)
        {
            FormClosed = true;
        }

        public OpenBlock Main = new OpenBlock();
        public Point MainPosition = new Point(30, 30);

        public EditableArea FocusedOn = null;

        public Block CurrentlyHeld = null;

        public bool FormClosed = false;

        public void UserKeyboardInput(object sender, KeyEventArgs e)
        {
            if(FocusedOn != null)
            {
                if (e.KeyCode == Keys.Back)
                    FocusedOn.Text = FocusedOn.Text.Substring(0, FocusedOn.Text.Length - 1);
                else if(char.IsLetterOrDigit((char)e.KeyCode))
                {
                    FocusedOn.Text += (char)e.KeyCode;
                }
            }
        }

        public void UserMouseInput(object sender, MouseEventArgs e)
        {
            foreach(Block block in Block.AllBlocks)
            {
                if (block.Area.Contains(e.Location))
                {
                    if(block.EditableArea != null)
                    {
                        if (block.EditableArea.Container.Contains(e.Location)){

                            FocusedOn = block.EditableArea;
                            break;
                        }
                    }
                    CurrentlyHeld = block;
                    block.Parent.Contents.Remove(block);
                    block.Parent = null;
                    break;
                }
            }
        }

        public void UserRelease(object sender, MouseEventArgs e)
        {
            if (CurrentlyHeld != null) {
                bool found = false;
                for (int y = e.Location.Y; y > 0; y--)
                {
                    foreach (Block b in Block.AllBlocks)
                    {
                        if (b != CurrentlyHeld)
                        {
                            if (b is OpenBlock)
                            {
                                OpenBlock ob = (OpenBlock)b;
                                if ((int)ob.Area.Y == y)
                                {
                                    ob.AddBlock(CurrentlyHeld, 0);
                                    found = true;
                                    break;
                                }
                                if ((int)ob.ClosingArea.Y == y)
                                {
                                    ob.Parent.AddBlock(CurrentlyHeld, ob.Parent.Contents.IndexOf(ob)+1);
                                    found = true;
                                    break;
                                }
                            }
                            else if (b is ClosedBlock)
                            {
                                if ((int)b.Area.Y == y)
                                {
                                    b.Parent.Contents.Insert(b.Parent.Contents.IndexOf(b)+1, CurrentlyHeld);
                                    CurrentlyHeld.Parent = b.Parent;
                                    found = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (found)
                        break;
                }
                CurrentlyHeld = null;
            }
        }

        public void Drawing()
        {
            while (!FormClosed)
            {
                Thread.Sleep(15);
                Invalidate();
            }
        }

        public void AddControl(Control toadd)
        {
            this.Controls.Add(toadd);
        }

        Font defaultfont = new Font(FontFamily.GenericMonospace,20, FontStyle.Regular, GraphicsUnit.Pixel);
        protected override void OnPaint(PaintEventArgs e)
        {
            List<OpenBlock> ParentList = new List<OpenBlock>();
            ParentList.Add(Main);
            bool exited = false;
            int currentLine = 0;
            int currentID = 0;
            using (Pen pen = new Pen(Color.Black, 1))
            {                
                do
                {
                    SizeF size;
                    SizeF editablesize = new SizeF(0, 0);

                    if (ParentList.Last().Contents.Count >= currentID + 1)
                    {
                        for(int a = 1; a < ParentList.Count(); a++)
                        {
                            e.Graphics.FillRectangle(new SolidBrush(ParentList[a].Color), MainPosition.X + 6 * (a-1), MainPosition.Y + Block.BlockHeight * currentLine, 6, Block.BlockHeight);
                        }
                                          

                        Block b = ParentList.Last().Contents[currentID];
                        if(b is OpenBlock)
                            size = e.Graphics.MeasureString(b.Variable, defaultfont);
                        else
                            size = e.Graphics.MeasureString(b.Variable + " = ", defaultfont);
                        b.TextSize = size;
                        RectangleF BoxRectangle = new RectangleF(MainPosition.X + 6 * (ParentList.Count() - 1), MainPosition.Y + Block.BlockHeight * currentLine, size.Width, Block.BlockHeight);
                        if (b.EditableArea != null)
                        {
                            editablesize = e.Graphics.MeasureString(b.EditableArea.Text, defaultfont);
                            if (editablesize.Width < b.EditableArea.MinWidth)
                                editablesize.Width = b.EditableArea.MinWidth;
                            BoxRectangle.Width += editablesize.Width + 10;
                        }
                        b.Area = BoxRectangle;
                        e.Graphics.FillRectangle(new SolidBrush(b.Color), BoxRectangle);
                        if(b is OpenBlock)
                            e.Graphics.DrawString(b.Variable, defaultfont, Brushes.Black, BoxRectangle);
                        else
                            e.Graphics.DrawString(b.Variable + " = ", defaultfont, Brushes.Black, BoxRectangle);
                        if (b.EditableArea != null)
                        {
                            b.EditableArea.Container = new RectangleF(b.Area.X + size.Width + 5, b.Area.Y + 2, editablesize.Width, Block.BlockHeight - 4);
                            e.Graphics.FillRectangle(new SolidBrush(GetLighterColor(b.Color)), b.EditableArea.Container);
                            e.Graphics.DrawString(b.EditableArea.Text, defaultfont, Brushes.Black, b.Area.X + size.Width + 5, b.Area.Y);
                            if(b.EditableArea == FocusedOn)
                            {
                                e.Graphics.DrawRectangle(pen,new Rectangle((int)b.EditableArea.Container.X-1, (int)b.EditableArea.Container.Y-1, (int)b.EditableArea.Container.Width+2, (int)b.EditableArea.Container.Height+2));
                            }
                        }
                        if (b is OpenBlock)
                        {                                                                                                                                   
                            currentID = -1;
                            ParentList.Add((OpenBlock)b);
                        }
                    }
                    else
                    {
                        if (ParentList.Last() == Main)
                            exited = true;
                        else
                        {
                            for (int a = 1; a < ParentList.Count()-1; a++)
                            {
                                e.Graphics.FillRectangle(new SolidBrush(ParentList[a].Color), MainPosition.X + 6 * (a - 1), MainPosition.Y + Block.BlockHeight * currentLine, 6, Block.BlockHeight);
                            }
                            OpenBlock ob = ParentList.Last();
                            size = e.Graphics.MeasureString("/" + ob.Variable, defaultfont);
                            RectangleF BoxRectangle = new RectangleF(MainPosition.X + 6 * (ParentList.Count() - 2), MainPosition.Y + Block.BlockHeight * currentLine, size.Width, Block.BlockHeight);



                            e.Graphics.FillRectangle(new SolidBrush(ob.Color), BoxRectangle);
                            e.Graphics.DrawString("/" + ob.Variable, defaultfont, Brushes.Black, BoxRectangle);
                            ob.ClosingArea = BoxRectangle;
                            
                            currentID = ParentList.Last().Parent.Contents.IndexOf(ParentList.Last());
                            ParentList.Remove(ParentList.Last());
                        }
                    }
                    currentID++;
                    currentLine++;
                } while (!exited);

                base.OnPaint(e);
                if (CurrentlyHeld != null)
                {
                    SizeF heldsize = e.Graphics.MeasureString(CurrentlyHeld.Variable, defaultfont);
                    Point mouse = this.PointToClient(Cursor.Position);
                    RectangleF HeldBoxRectangle = new RectangleF(mouse.X, mouse.Y, heldsize.Width, Block.BlockHeight);
                    CurrentlyHeld.Area = HeldBoxRectangle;
                    e.Graphics.FillRectangle(new SolidBrush(CurrentlyHeld.Color), HeldBoxRectangle);
                    e.Graphics.DrawString(CurrentlyHeld.Variable, defaultfont, Brushes.Black, HeldBoxRectangle);
                }
            }
            
        }
        public Color GetLighterColor(Color original)
        {
            int R = original.R + 25;
            int G = original.G + 25;
            int B = original.G + 25;
            return Color.FromArgb(R > 255 ? 255 : R, G > 255 ? 255 : G, B > 255 ? 255 : B);
        }
    }


    public enum BlockType { MissionMainBlock, ProvinceScoper };



    public class Block
    {
        public static List<Block> AllBlocks = new List<Block>();
        public string Variable = "";
        public BlockType Type = BlockType.ProvinceScoper;
        public Color Color = Color.Red;
        public OpenBlock Parent = null;
        public RectangleF Area = new RectangleF();
        public SizeF TextSize = new SizeF();
        public Block()
        {
            AllBlocks.Add(this);
        }
        public Block(string var)
        {
            AllBlocks.Add(this);
            Variable = var;
        }
        public void SetParent(OpenBlock parent)
        {
            if (Parent != null)
                Parent.Contents.Remove(this);
            Parent = parent;
            Parent.Contents.Add(this);
        }
        public static Random BlockRandom = new Random();
        // public TextBox EditableArea = null;
        public EditableArea EditableArea = null;
        public static float BlockHeight = 0;
        public void CreateTextBox(string exampletext = "value")
        {
            EditableArea ea = new EditableArea();
            ea.Text = "15";
            EditableArea = ea;
        }
    }
    public class ClosedBlock : Block
    {
        public string Value = "";
        public ClosedBlock()
        {
        }
        public ClosedBlock(string var)
        {
            CreateTextBox();
            Variable = var;           
        }
    }
    public class OpenBlock : Block
    {
        public List<Block> Contents = new List<Block>();
        public void AddBlock(Block toadd)
        {
            if (toadd.Parent != null)
                toadd.Parent.Contents.Remove(toadd);
            toadd.Parent = this;
            Contents.Add(toadd);
        }
        public void AddBlock(Block toadd, int where)
        {
            if (toadd.Parent != null)
                toadd.Parent.Contents.Remove(toadd);
            toadd.Parent = this;
            Contents.Insert(where, toadd);
        }
        public static Color GenerateGreenColor()
        {
            return Color.FromArgb(BlockRandom.Next(15, 160), BlockRandom.Next(150, 255), BlockRandom.Next(15, 160));
        }
        public OpenBlock()
        {
            Color = GenerateGreenColor();
        }
        public OpenBlock(string var)
        {
            Variable = var;
            Color = GenerateGreenColor();
        }
        public OpenBlock(string var, Color col)
        {
            Variable = var;
            Color = col;
        }
        public RectangleF ClosingArea = new RectangleF();
    }


    public class EditableArea
    {
        public string Text = "";
        public int MinWidth = 30;
        public Position Alignment = EditableArea.Position.Right;
        public enum Position { Left, Right };
        public RectangleF Container = new RectangleF();
    }


    public class Trigger
    {

    }

    public enum ConnectorType { OR = 1, AND = 2, NOT = 3 };

    public class TriggerConnector : Trigger
    {
        ConnectorType Type = ConnectorType.OR;
        List<Trigger> Inside = new List<Trigger>();
    }



    public class BasicTrigger : Trigger
    {
        string Name = "";
        string Value = "";
    }

    public class MissionGroup
    {
        public string Name = "";
        public int Slot = 1;
        public bool AI = false;
        public bool Shield = false;
        public List<Trigger> Potential = new List<Trigger>();
    }

    public class Mission
    {

    }
}