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
using System.IO;
using System.Windows.Input;
using System.Text.RegularExpressions;

namespace Mission_Mkaer
{
    public static class SharedVariables
    {
        public static List<MissionSeries> MissionSeries = new List<MissionSeries>();

    }


    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            MainForm = this;
            MainForm.Height = 700;
            MainForm.Width = 900;
            Block.BlockHeight = this.CreateGraphics().MeasureString("P", defaultfont).Height;
            Thread DrawingThread = new Thread(Drawing);
            this.DoubleBuffered = true;
            Main.Area.Y = 0;
            Main.Area.X = 200;

            VisualizerWindow = new MissionVisualizer();
            VisualizerWindow.Show();

            DrawingThread.Start();
            FormClosing += ClosingHandler;
            MouseDown += UserMouseInput;
            MouseUp += UserRelease;
            KeyPress += UserKeyboardInputChar;
            KeyDown += UserKeyboardInputCode;

            MouseWheel += UserWheelInput;

            NodeFile nf = new NodeFile("file.txt");
            bool done = false;

            Texts.Add(new TextOnScreen() { BoundingArea = new RectangleF(20, 20, 170, 50), Text = "Basic triggers" });

            SideBarBlocks.AddBlock(new OpenBlock("AND") { StaticPosition = new PointF(20, 70), Color = Color.LightBlue });
            SideBarBlocks.AddBlock(new OpenBlock("OR") { StaticPosition = new PointF(20, 100), Color = Color.Green });
            SideBarBlocks.AddBlock(new OpenBlock("NOT") { StaticPosition = new PointF(20, 130), Color = Color.Red });

            //SideBarBlocks.AddBlock(new CommentBlock("AND:\nType: trigger\nNeeds all of the blocks directly inside it to evaluate to true"));
            //SideBarBlocks.AddBlock(new OpenBlock("OR"));
            //SideBarBlocks.AddBlock(new OpenBlock("NOT"));

            List<Node> ParentList = new List<Node>() { nf.MainNode };
            OpenBlock BlockParent = Main;
            int currentID = 0;
            do
            {
                if (ParentList.Last().ItemOrder.Count >= currentID + 1)
                {
                    NodeItem ni = ParentList.Last().ItemOrder[currentID];
                    if (ni is Node)
                    {
                        OpenBlock ob = new OpenBlock((ni as Node).Name);
                        BlockParent.AddBlock(ob);
                        BlockParent = ob;
                        currentID = -1;
                        ParentList.Add((Node)ni);

                        if((ni as Node).PureValues.Any())
                        {
                            foreach (PureValue pv in (ni as Node).PureValues)
                            {
                                BlockParent.AddBlock(new ValueBlock(pv.Name));
                            }
                        }

                        if ((ni as Node).Parent == nf.MainNode)
                            ob.Type = BlockTypes.MissionSeriesBlock;
                        else
                        {
                            switch (ni.Name.ToLower())
                            {
                                case "and":
                                    ob.Type = BlockTypes.AND;
                                    ob.Color = Color.Blue;
                                    break;
                                case "or":
                                    ob.Type = BlockTypes.OR;
                                    ob.Color = Color.Green;
                                    break;
                                case "not":
                                    ob.Type = BlockTypes.NOT;
                                    ob.Color = Color.Red;
                                    break;
                                case "potential":
                                    ob.Type = BlockTypes.MissionSeriesPotential;
                                    break;
                                case "required_missions":
                                    ob.Type = BlockTypes.MissionRequiredMissions;
                                    break;
                                case "trigger":
                                    ob.Type = BlockTypes.Trigger;
                                    break;
                                case "effect":
                                    ob.Type = BlockTypes.Effect;
                                    break;
                                case "custom_trigger_tooltip":
                                    ob.Type = BlockTypes.CustomTriggerTooltip;
                                    break;
                                default:
                                    if (ob.Parent.Type == BlockTypes.MissionSeriesBlock)
                                        ob.Type = BlockTypes.MissionContainerBlock;
                                break;
                            }
                        }
                    }
                    else if(ni is Variable)
                    {
                        ClosedBlock cb = new ClosedBlock((ni as Variable).Name, (ni as Variable).Value);
                        BlockParent.AddBlock(cb);
                        switch((ni as Variable).Name.ToLower())
                        {
                            case "slot":
                                cb.EditableArea.Type = EditableArea.VariableType.MissionSlotNumber;
                                cb.Type = BlockTypes.MissionSlot;
                                goto case "_NUMBER";
                            case "generic":
                                cb.Type = BlockTypes.MissionGeneric;
                                goto case "_YESNO";
                            case "ai":
                                cb.Type = BlockTypes.MissionAI;
                                goto case "_YESNO";
                            case "always":
                                cb.Type = BlockTypes.Always;
                                goto case "_YESNO";
                            case "position":
                                cb.EditableArea.Type = EditableArea.VariableType.AnyNumber;
                                cb.Type = BlockTypes.Position;
                                goto case "_NUMBER";

                            case "icon":
                                cb.Type = BlockTypes.Icon;
                                goto case "_BOTH";
                            case "tooltip":
                                cb.Type = BlockTypes.Tooltip;
                                goto case "_BOTH";

                            case "_YESNO":
                                cb.EditableArea.Type = EditableArea.VariableType.YesNo;
                                goto case "_TEXT";
                             

                            case "_TEXT":
                                cb.EditableArea.TextInput = true;
                                cb.EditableArea.NumberInput = false;
                                break;
                            case "_NUMBER":
                                cb.EditableArea.TextInput = false;
                                cb.EditableArea.NumberInput = true;
                                break;
                            case "_BOTH":
                                cb.EditableArea.TextInput = true;
                                cb.EditableArea.NumberInput = true;
                                break;

                        }

                    }
                }
                else
                {
                    if (ParentList.Last() == nf.MainNode)
                        done = true;
                    else
                    {
                        currentID = ParentList.Last().Parent.ItemOrder.IndexOf(ParentList.Last());
                        BlockParent = BlockParent.Parent;
                        ParentList.Remove(ParentList.Last());
                        
                    }
                }
                currentID++;
            } while (!done);
            InterpretIntoMissions();

        }
        public static Form MainForm;
        public void ClosingHandler(object sender, EventArgs e)
        {
            FormClosed = true;
        }

        public OpenBlock Main = new OpenBlock();
        public Point MainPosition = new Point(230, 30);

        public OpenBlock SideBarBlocks = new OpenBlock();
        public Point SideBarPosition = new Point(20, 30);

        public List<TextOnScreen> Texts = new List<TextOnScreen>();

        public MissionVisualizer VisualizerWindow;

        public int ScrollY = 0;
        public int MaxScroll = 1;

        public int SideBarScrollY = 0;
        public int SideBarMaxScroll = 1;

        public EditableArea FocusedOn = null;

        public Block CurrentlyHeld = null;
        public InvisibleBlock CurrentInvisible = null;

        public bool FormClosed = false;

        public int MeasureLines(Block tomeasure)
        {
            return 0;
            if (tomeasure is ClosedBlock)
                return 0;
            else if(tomeasure is OpenBlock)
            {
                if ((tomeasure as OpenBlock).Collapsed)
                    return 0;

                List<OpenBlock> ParentList = new List<OpenBlock>();
                ParentList.Add((OpenBlock)tomeasure);
                bool exited = false;
                int currentLine = 0;
                int currentID = 0;
                do
                {
                    if (ParentList.Last().Contents.Count >= currentID + 1)
                    {
                        Block b = ParentList.Last().Contents[currentID];
                        if (b is OpenBlock)
                        {
                            currentID = -1;
                            ParentList.Add((OpenBlock)b);
                        }
                    }
                    else
                    {
                        if (ParentList.Last() == tomeasure)
                            exited = true;
                        else
                        {
                            OpenBlock ob = ParentList.Last();
                            currentID = ParentList.Last().Parent.Contents.IndexOf(ParentList.Last());
                            ParentList.Remove(ParentList.Last());
                        }
                    }
                    currentID++;
                    currentLine++;
                } while (!exited);
                return currentLine;
            }
            else
            {
                return 0;
            }
        }

        public void UserWheelInput(object sender, MouseEventArgs e)
        {
            ScrollY -= e.Delta;
            if (ScrollY < 0)
                ScrollY = 0;
            else if (ScrollY > MaxScroll && MaxScroll > 0)
                ScrollY = MaxScroll;
        }


        public void UserKeyboardInputCode(object sender, KeyEventArgs e)
        {
            if (FocusedOn != null)
            {
                if (e.KeyCode == Keys.Back && FocusedOn.Text.Any())
                    FocusedOn.Text = FocusedOn.Text.Substring(0, FocusedOn.Text.Length - 1);
            }
        }

        public void UserKeyboardInputChar(object sender, KeyPressEventArgs e)
        {
            if(FocusedOn != null)
            {              
                if (char.IsDigit(e.KeyChar) && FocusedOn.NumberInput)
                    FocusedOn.Text += (e.KeyChar).ToString();
                else if (!char.IsControl(e.KeyChar) && FocusedOn.TextInput)                
                    FocusedOn.Text += (e.KeyChar).ToString();
                
            }
        }

        public void UserMouseInput(object sender, MouseEventArgs e)
        {
            
            FocusedOn = null;
            foreach(Block block in Block.AllBlocks)
            {
                if (block.Area.Contains(e.Location))
                {
                    if (CheckIfAnyParentCollapsed(block))
                        continue;

                    if (block is CommentBlock)
                        continue;

                    if (e.Button == MouseButtons.Left)
                    {
                        if (block.Parent != SideBarBlocks)
                        {
                            if (block.EditableArea != null)
                            {
                                if (block.EditableArea.Container.Contains(e.Location))
                                {
                                    FocusedOn = block.EditableArea;
                                    break;
                                }
                            }
                            CurrentlyHeld = block;
                            CurrentInvisible = new InvisibleBlock(MeasureLines(block));
                            block.Parent.AddBlock(CurrentInvisible, block.Parent.Contents.IndexOf(block));
                            block.Parent.Contents.Remove(block);
                            block.Parent = null;
                        }
                        else
                        {
                            CurrentlyHeld = block.Copy();
                        }
                    }
                    else if(e.Button == MouseButtons.Right)
                    {
                        if(block is OpenBlock)
                        {
                            (block as OpenBlock).Collapsed = !(block as OpenBlock).Collapsed;
                        }
                    }
                    break;
                }
                else if(block is OpenBlock)
                {
                    OpenBlock ob = (OpenBlock)block;
                    if (ob.ClosingArea.Contains(e.Location) && !(block as OpenBlock).Collapsed)
                    {
                        if (CheckIfAnyParentCollapsed(block))
                            continue;

                        if (e.Button == MouseButtons.Left)
                        {
                            if (block.Parent != SideBarBlocks)
                            {
                                CurrentlyHeld = block;
                                CurrentInvisible = new InvisibleBlock(MeasureLines(block));
                                block.Parent.AddBlock(CurrentInvisible, block.Parent.Contents.IndexOf(block));
                                block.Parent.Contents.Remove(block);
                                block.Parent = null;
                            }
                            else
                            {
                                CurrentlyHeld = block.Copy();
                            }
                        }
                        else if (e.Button == MouseButtons.Right)
                        {
                            if (block is OpenBlock)
                            {
                                (block as OpenBlock).Collapsed = !(block as OpenBlock).Collapsed;
                            }
                        }
                        break;
                    }
                }
            }
        }

        public void UserRelease(object sender, MouseEventArgs e)
        {
            if (CurrentlyHeld == null)
                return;

            if (e.Button == MouseButtons.Right)
                return;

            if (e.Location.X <= 200)
            {
                Block.AllBlocks.Remove(CurrentlyHeld);
                CurrentlyHeld = null;
                if (CurrentInvisible != null)
                {
                    CurrentInvisible.Parent.Contents.Remove(CurrentInvisible);
                    Block.AllBlocks.Remove(CurrentInvisible);
                    CurrentInvisible = null;
                }
                return;
            }

            bool skip = false;
            if (CurrentInvisible != null)
            {
                if ((e.Y >= CurrentInvisible.Area.Y && e.Y <= CurrentInvisible.Area.Y + CurrentInvisible.Area.Height))
                {
                    CurrentInvisible.Parent.AddBlock(CurrentlyHeld, CurrentInvisible.Parent.Contents.IndexOf(CurrentInvisible));
                    skip = true;
                }
            }

            if (!skip)
            {
                foreach (Block b in Block.AllBlocks)
                {
                    if (b != CurrentlyHeld)
                    {
                        if (b.Parent == SideBarBlocks)
                            continue;
                        if (CheckIfAnyParentCollapsed(b))
                            continue;

                        if (b is InvisibleBlock)
                        {
                            InvisibleBlock ib = (InvisibleBlock)b;
                            if ((e.Y >= ib.Area.Y && e.Y <= ib.Area.Y + ib.Area.Height))
                            {
                                b.Parent.AddBlock(CurrentlyHeld, b.Parent.Contents.IndexOf(b));
                                break;
                            }
                        }
                        int diff = (int)(b.Area.Y + Block.BlockHeight / 2) - e.Y;
                        if (Math.Abs(diff) <= Block.BlockHeight / 2)
                        {
                            if (diff >= 0)
                            {
                                b.Parent.AddBlock(CurrentlyHeld, b.Parent.Contents.IndexOf(b));
                                break;
                            }
                            else
                            {
                                if (b is OpenBlock)
                                {
                                    if ((b as OpenBlock).Collapsed)
                                    {
                                        b.Parent.AddBlock(CurrentlyHeld, b.Parent.Contents.IndexOf(b) + 1);
                                        break;
                                    }
                                    else
                                    {
                                        OpenBlock ob = (OpenBlock)b;
                                        ob.AddBlock(CurrentlyHeld, 0);
                                        break;
                                    }
                                }
                                else if (b is ClosedBlock || b is InvisibleBlock)
                                {
                                    b.Parent.AddBlock(CurrentlyHeld, b.Parent.Contents.IndexOf(b) + 1);
                                    break;
                                }
                            }
                        }
                        else if (b is OpenBlock)
                        {
                            OpenBlock ob = (OpenBlock)b;
                            diff = (int)(ob.ClosingArea.Y + Block.BlockHeight / 2) - e.Y;
                            if (Math.Abs(diff) < Block.BlockHeight / 2)
                            {
                                if (diff >= 0)
                                {
                                    ob.AddBlock(CurrentlyHeld, ob.Contents.Count);
                                    break;
                                }
                                else
                                {
                                    ob.Parent.AddBlock(CurrentlyHeld, ob.Parent.Contents.IndexOf(ob) + 1);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            CurrentlyHeld = null;
            if (CurrentInvisible != null)
            {
                CurrentInvisible.Parent.Contents.Remove(CurrentInvisible);
                Block.AllBlocks.Remove(CurrentInvisible);
                CurrentInvisible = null;
            }
            
        }

        public void ValidateAllBlocks()
        {
            foreach(Block b in Block.AllBlocks.Where(x=>!SideBarBlocks.Contents.Contains(x)))
            {
                ValidateBlock(b, true);
            }
        }

        public void ValidateBlock(Block b, bool notsideblock = false)
        {
            if (!notsideblock)
            {
                if (SideBarBlocks.Contents.Contains(b))
                    return;
            }

            switch (b.Type)
            {
                case BlockTypes.MissionContainerBlock:
                    break;

                case BlockTypes.MissionGeneric:
                    break;
            }

        }

        public void InterpretOneBlock(Block b)
        {

        }

        public void InterpretIntoMissions()
        {
            int[] Positions = new int[] { 0, 0, 0, 0, 0 };
            SharedVariables.MissionSeries.Clear();
            foreach(Block b in Main.Contents)
            {
                MissionSeries ms = new MissionSeries();
                ms.Name = b.Variable;
                Block Slot = (b as OpenBlock).Contents.Find(x => x.Type == BlockTypes.MissionSlot);
                if (Slot != null)
                {
                    int.TryParse(Slot.EditableArea.Text, out ms.Slot);
                }

                foreach (OpenBlock mission in (b as OpenBlock).Contents.Where(x=>x is OpenBlock))
                {
                    if (mission.Variable == "potential")
                        continue;
                    Mission m = new Mission() { Name = mission.Variable };
                    Block Position = (mission as OpenBlock).Contents.Find(x => x.Type == BlockTypes.Position);
                    if (Position != null)
                    {
                        int.TryParse(Position.EditableArea.Text, out m.Position);
                    }
                    else
                    {
                        m.Position = Positions[ms.Slot];
                        Positions[ms.Slot]++;
                    }


                    ms.Missions.Add(m);
                    m.Series = ms;
                }
                SharedVariables.MissionSeries.Add(ms);
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
        
        public bool CheckIfAnyParentCollapsed(Block toCheck)
        {
            if(toCheck.Parent != null)
            {
                if (toCheck.Parent == Main)
                    return false;
                if (toCheck.Parent is OpenBlock)
                {
                    if ((toCheck.Parent as OpenBlock).Collapsed)
                        return true;
                    else
                        return CheckIfAnyParentCollapsed(toCheck.Parent);
                }
                else
                    return CheckIfAnyParentCollapsed(toCheck.Parent);
            }
            return false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            List<OpenBlock> ParentList = new List<OpenBlock>();
            ParentList.Add(Main);
            bool exited = false;
            int currentLine = 0;
            int currentID = 0;
            Block lastdrawn = null;

            //Texts

            foreach(TextOnScreen tos in Texts)
            {
                //if(tos.BoundingArea.Y + tos.BoundingArea.Height > ScrollY && tos.BoundingArea.Y < ScrollY + Height)
                //{
                    e.Graphics.DrawString(tos.Text, defaultfont, Brushes.Black, tos.BoundingArea);
                //}
            }



            //Left side

            foreach(Block b in SideBarBlocks.Contents)
            {
                SizeF size;
                SizeF editablesize = new SizeF(0, 0);
                if (b is OpenBlock || b is CommentBlock)
                    size = e.Graphics.MeasureString(b.Variable, defaultfont);
                else if (b is ValueBlock)
                    size = new SizeF(0, 0);
                else
                    size = e.Graphics.MeasureString(b.Variable + " = ", defaultfont);
                b.TextSize = size;
                RectangleF BoxRectangle;
                if (b.StaticPosition != null)
                    BoxRectangle = new RectangleF(b.StaticPosition.Value.X + 6, b.StaticPosition.Value.Y - SideBarScrollY, size.Width, Block.BlockHeight);
                else
                    BoxRectangle = new RectangleF(SideBarPosition.X + 6, SideBarPosition.Y + Block.BlockHeight * currentLine - SideBarScrollY, size.Width, Block.BlockHeight);
                if (b.EditableArea != null)
                {
                    editablesize = e.Graphics.MeasureString(b.EditableArea.Text, defaultfont);
                    if (editablesize.Width < b.EditableArea.MinWidth)
                        editablesize.Width = b.EditableArea.MinWidth;
                    BoxRectangle.Width += editablesize.Width + 10;
                }
                b.Area = BoxRectangle;
                e.Graphics.FillRectangle(new SolidBrush(b.Color), BoxRectangle);
                if (b is OpenBlock || b is CommentBlock)                
                    e.Graphics.DrawString(b.Variable, defaultfont, Brushes.Black, BoxRectangle);               
                else
                    e.Graphics.DrawString(b.Variable + " = ", defaultfont, Brushes.Black, BoxRectangle);
                if (b.EditableArea != null)
                {
                    b.EditableArea.Container = new RectangleF(b.Area.X + size.Width + 5, b.Area.Y + 2, editablesize.Width, Block.BlockHeight - 4);
                    e.Graphics.FillRectangle(Brushes.White/*new SolidBrush(GetLighterColor(b.Color))*/, b.EditableArea.Container);
                    e.Graphics.DrawString(b.EditableArea.Text, defaultfont, Brushes.Black, b.Area.X + size.Width + 5, b.Area.Y);
                }
                currentLine++;
            }
            currentLine = 0;
            //Right side
            using (Pen pen = new Pen(Color.Black, 1))
            {
                e.Graphics.DrawLine(new Pen(Color.Black, 4), new Point((int)Main.Area.X - 10, 0), new Point((int)Main.Area.X - 10, Height));
                //e.Graphics.DrawString(Block.AllBlocks.Count() + "", defaultfont, Brushes.Black, new Point(0,0));
                do
                {
                    SizeF size;
                    SizeF editablesize = new SizeF(0, 0);                   
                    if (ParentList.Last().Contents.Count >= currentID + 1)
                    {
                        Block b = ParentList.Last().Contents[currentID];
                        int repeats = 1;
                        bool ib = false;
                        if(b is InvisibleBlock)
                        {
                            repeats = ((InvisibleBlock)b).LineSize+1;
                            b.Area = new RectangleF(0, MainPosition.Y + Block.BlockHeight * currentLine - ScrollY, 0, repeats * Block.BlockHeight);
                            ib = true;
                        }
                        for (int c = 0; c < repeats; c++)
                        {
                            for (int a = 1; a < ParentList.Count(); a++)
                            {
                                e.Graphics.FillRectangle(new SolidBrush(ParentList[a].Color), MainPosition.X + 6 * (a - 1), MainPosition.Y + Block.BlockHeight * currentLine - ScrollY, 6, Block.BlockHeight);                               
                            }
                            if (ib)
                                currentLine++;
                        }
                        if (ib)
                        {
                            currentID++;
                            continue;
                        }

                        if (b is OpenBlock)
                        {
                            if ((b as OpenBlock).Collapsed)
                                size = e.Graphics.MeasureString(b.Variable + "⮟", defaultfont);
                            else
                                size = e.Graphics.MeasureString(b.Variable, defaultfont);
                        }
                        else if (b is ValueBlock)
                            size = e.Graphics.MeasureString("Value: ", defaultfont);
                        else
                            size = e.Graphics.MeasureString(b.Variable + " = ", defaultfont);
                        b.TextSize = size;
                        RectangleF BoxRectangle = new RectangleF(MainPosition.X + 6 * (ParentList.Count() - 1), MainPosition.Y + Block.BlockHeight * currentLine - ScrollY, size.Width, Block.BlockHeight);
                        if (b.EditableArea != null)
                        {
                            editablesize = e.Graphics.MeasureString(b.EditableArea.Text, defaultfont);
                            if (editablesize.Width < b.EditableArea.MinWidth)
                                editablesize.Width = b.EditableArea.MinWidth;
                            BoxRectangle.Width += editablesize.Width + 10;
                        }
                        b.Area = BoxRectangle;
                        e.Graphics.FillRectangle(new SolidBrush(b.Color), BoxRectangle);
                        if (b is OpenBlock)
                        {
                            if((b as OpenBlock).Collapsed)
                                e.Graphics.DrawString(b.Variable + "⮟", defaultfont, Brushes.Black, BoxRectangle);
                            else
                                e.Graphics.DrawString(b.Variable, defaultfont, Brushes.Black, BoxRectangle);
                        }
                        else if(b is ValueBlock)
                            e.Graphics.DrawString("Value: ", defaultfont, Brushes.Black, BoxRectangle);
                        else
                            e.Graphics.DrawString(b.Variable + " = ", defaultfont, Brushes.Black, BoxRectangle);
                        if (b.EditableArea != null)
                        {
                            b.EditableArea.Container = new RectangleF(b.Area.X + size.Width + 5, b.Area.Y + 2, editablesize.Width, Block.BlockHeight - 4);
                            e.Graphics.FillRectangle(Brushes.White/*new SolidBrush(GetLighterColor(b.Color))*/, b.EditableArea.Container);
                            e.Graphics.DrawString(b.EditableArea.Text, defaultfont, Brushes.Black, b.Area.X + size.Width + 5, b.Area.Y);
                            if(b.EditableArea == FocusedOn)
                            {
                                e.Graphics.DrawRectangle(pen,new Rectangle((int)b.EditableArea.Container.X-1, (int)b.EditableArea.Container.Y-1, (int)b.EditableArea.Container.Width+2, (int)b.EditableArea.Container.Height+2));
                            }
                        }
                        if (b is OpenBlock)
                        {
                            if (!((OpenBlock)b).Collapsed)
                            {
                                currentID = -1;
                                ParentList.Add((OpenBlock)b);
                            }
                        }
                        lastdrawn = b;
                    }
                    else
                    {
                        if (ParentList.Last() == Main)
                            exited = true;
                        else
                        {
                            for (int a = 1; a < ParentList.Count()-1; a++)
                            {
                                e.Graphics.FillRectangle(new SolidBrush(ParentList[a].Color), MainPosition.X + 6 * (a - 1), MainPosition.Y + Block.BlockHeight * currentLine - ScrollY, 6, Block.BlockHeight);
                            }
                            OpenBlock ob = ParentList.Last();
                            size = e.Graphics.MeasureString("/" + ob.Variable, defaultfont);
                            RectangleF BoxRectangle = new RectangleF(MainPosition.X + 6 * (ParentList.Count() - 2), MainPosition.Y + Block.BlockHeight * currentLine - ScrollY, size.Width, Block.BlockHeight);



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
                    if(CurrentlyHeld is ValueBlock)
                        heldsize = e.Graphics.MeasureString("Value", defaultfont);
                    Point mouse = this.PointToClient(Cursor.Position);
                    RectangleF HeldBoxRectangle = new RectangleF(mouse.X, mouse.Y, heldsize.Width, Block.BlockHeight);
                    CurrentlyHeld.Area = HeldBoxRectangle;
                    e.Graphics.FillRectangle(new SolidBrush(CurrentlyHeld.Color), HeldBoxRectangle);
                    if(CurrentlyHeld is ValueBlock)
                        e.Graphics.DrawString("Value", defaultfont, Brushes.Black, HeldBoxRectangle);
                    else
                        e.Graphics.DrawString(CurrentlyHeld.Variable, defaultfont, Brushes.Black, HeldBoxRectangle);
                }
                MaxScroll = (int)(lastdrawn.Area.Y + ScrollY - 80 + Block.BlockHeight)+1;
                //e.Graphics.DrawString(MaxScroll + "", defaultfont, Brushes.Black, 0, 0);//(int)((currentLine) * Block.BlockHeight);
                //e.Graphics.DrawString(ScrollY + "", defaultfont, Brushes.Black, 0, 20);
                //e.Graphics.DrawString(lastdrawn.Variable, defaultfont, Brushes.Black, 0, 40);
                //e.Graphics.DrawString(lastdrawn.Area.Y + "", defaultfont, Brushes.Black, 0, 60);
                //Rectangle ScrollBar = new Rectangle(Width - 8,(ScrollY/MaxScroll)*Height, 8, 30);
                Rectangle ScrollBar = new Rectangle(Width - 24, (int)(((float)ScrollY / MaxScroll) * (Height-64)), 8, 30);
                e.Graphics.FillRectangle(Brushes.Black,ScrollBar);
            }
            

        }
        public Color GetLighterColor(Color original)
        {
            int R = (int)(original.R * 0.9);
            int G = (int)(original.G * 0.9);
            int B = (int)(original.G * 0.9);
            return Color.FromArgb(R > 255 ? 255 : R, G > 255 ? 255 : G, B > 255 ? 255 : B);
        }

        

    }


    public class TextOnScreen
    {
        public RectangleF BoundingArea = new RectangleF(0,0,0,0);
        public string Text = "";
    }

    public enum BlockTypes
    {
        AND,
        OR,
        NOT,
        MissionSlot,
        MissionGeneric,
        MissionAI,
        MissionContainerBlock,
        MissionSeriesBlock,
        MissionSeriesPotential,
        MissionRequiredMissions,
        Position,
        CustomTriggerTooltip,
        Tooltip,
        Trigger,
        Effect,
        Icon,
        Always,
        __DEFAULT,
    }

    public class Block
    {
        public static Color GetPredictableColor(Block b)
        {
            int R = 0;
            int G = 0;
            int B = 0;

            foreach (char c in b.Variable)
            {
                switch (c)
                {
                    case 'a':
                    case 'A':
                        R += 23;
                        G += 17;
                        B += 13;
                        break;
                    case 'b':
                    case 'B':
                        R += 101;
                        G += 59;
                        B += 83;
                        break;
                    case 'c':
                    case 'C':
                        R += 151;
                        G += 97;
                        B += 11;
                        break;
                    case 'd':
                    case 'D':
                        R += 179;
                        G += 23;
                        B += 73;
                        break;
                    case 'e':
                    case 'E':
                        R += 47;
                        G += 2;
                        B += 29;
                        break;
                    case 'f':
                    case 'F':
                        R += 3;
                        G += 47;
                        B += 199;
                        break;
                    case 'g':
                    case 'G':
                        R += 137;
                        G += 61;
                        B += 167;
                        break;
                    case 'h':
                    case 'H':
                        R += 29;
                        G += 89;
                        B += 179;
                        break;
                    case 'i':
                    case 'I':
                        R += 43;
                        G += 41;
                        B += 37;
                        break;
                    case 'j':
                    case 'J':
                        R += 17;
                        G += 23;
                        B += 5;
                        break;
                    case 'k':
                    case 'K':
                        R += 137;
                        G += 139;
                        B += 157;
                        break;
                    case 'l':
                    case 'L':
                        R += 2;
                        G += 149;
                        B += 53;
                        break;
                    case 'm':
                    case 'M':
                        R += 11;
                        G += 61;
                        B += 137;
                        break;
                    case 'n':
                    case 'N':
                        R += 59;
                        G += 71;
                        B += 31;
                        break;
                    case 'o':
                    case 'O':
                        R += 17;
                        G += 19;
                        B += 23;
                        break;
                    case 'p':
                    case 'P':
                        R += 193;
                        G += 103;
                        B += 37;
                        break;
                    case 'q':
                    case 'Q':
                        R += 7;
                        G += 193;
                        B += 199;
                        break;
                    case 'r':
                    case 'R':
                        R += 79;
                        G += 61;
                        B += 19;
                        break;
                    case 's':
                    case 'S':
                        R += 13;
                        G += 47;
                        B += 97;
                        break;
                    case 't':
                    case 'T':
                        R += 107;
                        G += 163;
                        B += 137;
                        break;
                    case 'u':
                    case 'U':
                        R += 43;
                        G += 151;
                        B += 97;
                        break;
                    case 'v':
                    case 'V':
                        R += 103;
                        G += 157;
                        B += 163;
                        break;
                    case 'w':
                    case 'W':
                        R += 41;
                        G += 43;
                        B += 47;
                        break;
                    case 'x':
                    case 'X':
                        R += 59;
                        G += 137;
                        B += 5;
                        break;
                    case 'y':
                    case 'Y':
                        R += 19;
                        G += 199;
                        B += 97;
                        break;
                    case 'z':
                    case 'Z':
                        R += 151;
                        G += 157;
                        B += 167;
                        break;
                }
            }
            R = R % 255;
            G = G % 255;
            B = B % 255;
            if (R + G + B < 200)
            {
                R += 30;
                B += 30;
                G += 30;
            }
            return Color.FromArgb(R, G, B);
        }
        public static List<Block> AllBlocks = new List<Block>();
        public string Variable = "";
        public BlockTypes Type = BlockTypes.__DEFAULT;
        public Color Color = Color.Red;
        public OpenBlock Parent = null;
        public RectangleF Area = new RectangleF();
        public SizeF TextSize = new SizeF();

        public PointF? StaticPosition = null;

        public Block Copy()
        {
            if(this is OpenBlock)
            {
                return new OpenBlock(this.Variable, this.Color);
            }
            else if(this is ClosedBlock)
            {
                return new ClosedBlock(this.Variable, this.Color, "");
            }
            else
            {
                return new Block(this.Variable, this.Color);
            }
        }

        public Block()
        {
            AllBlocks.Add(this);
        }
        public Block(string var)
        {
            AllBlocks.Add(this);
            Variable = var;
        }
        public Block(string var, Color col)
        {
            AllBlocks.Add(this);
            Variable = var;
            Color = col;
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
        public void CreateTextBox(string value = "")
        {
            EditableArea ea = new EditableArea();
            ea.Text = value;
            EditableArea = ea;
        }
    }
    public class ClosedBlock : Block
    {
        //public string Value = "";
        public ClosedBlock() : base()
        {
            Color = GetPredictableColor(this); //GenerateRedColor();
        }
        public ClosedBlock(string var) : base(var)
        {
            CreateTextBox();
            Color = GetPredictableColor(this); //GenerateRedColor();         
        }
        public ClosedBlock(string var, string val) : base(var)
        {
            CreateTextBox(val);
            Color = GetPredictableColor(this); //GenerateRedColor();
        }
        public ClosedBlock(string var, Color col, string val) : base(var)
        {
            CreateTextBox(val);
            Color = col; //GenerateRedColor();
        }
        public static Color GenerateRedColor()
        {
            return Color.FromArgb(BlockRandom.Next(150, 255), BlockRandom.Next(15, 160), BlockRandom.Next(15, 160));
        }
    }
    public class OpenBlock : Block
    {
        public List<Block> Contents = new List<Block>();
        public bool Collapsed = false;
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
        public OpenBlock() : base()
        {
            Color = GetPredictableColor(this); //GenerateGreenColor();
        }
        public OpenBlock(string var) : base(var)
        {
            Color = GetPredictableColor(this); //GenerateGreenColor();
        }
        public OpenBlock(string var, Color col) : base (var)
        {
            Color = col;
        }
        public RectangleF ClosingArea = new RectangleF();
    }
    public class ValueBlock : Block
    {
        public ValueBlock(string value)
        {
            CreateTextBox(value);
        }
    }
    public class InvisibleBlock : Block
    {
        public int LineSize = 0;
        public InvisibleBlock(int size)
        {
            LineSize = size;
        }
    }
    public class CommentBlock : Block
    {
        public int WrapLength = 140;
        public CommentBlock(string var)
        {
            Variable = var;
        }
    }

    public class EditableArea
    {
        public string Text = "";
        public int MinWidth = 30;
        public Position Alignment = EditableArea.Position.Right;
        public enum Position { Left, Right };
        public RectangleF Container = new RectangleF();
        public bool TextInput = true;
        public bool NumberInput = true;
        public enum VariableType
        {
            AnyNumber,
            MissionSlotNumber,
            YesNo
        }
        public VariableType Type = VariableType.AnyNumber;
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

    public class MissionSeries
    {
        public string Name = "";
        public int Slot = 1;
        public bool AI = false;
        public bool Shield = false;
        public List<Trigger> Potential = new List<Trigger>();
        public List<Mission> Missions = new List<Mission>();
    }

    public class Mission
    {
        public MissionSeries Series;
        public string Name = "";
        public int Position = 1;

    }

    public class NodeItem
    {
        public string Name = "";
        public string Comment = "";
    }
    public class NodeFile
    {
        public Node MainNode;
        public List<Node> AllNodes = new List<Node>();
        public List<Variable> Allvariables = new List<Variable>();
        public bool ReadOnly = false;
        public bool CreatedByEditor = false;
        public string FileName = "";
        public string Path = "";
        public NodeFile()
        {
            MainNode = new Node("__MainNode");
        }
        public NodeFile(string file, bool readonl = false)
        {
            ReadFile(file);
            ReadOnly = readonl;
            Path = file;
        }


        char[] specialChars = { '=', '{', '}', '#' };

        public void ReadFile(string path)
        {
            Path = path;
            FileName = path.Split('\\').Last().Replace(".txt", "");
            Node CurrentNode = new Node("__MainNode");
            MainNode = CurrentNode;
            if (!Directory.Exists(System.IO.Path.GetDirectoryName(Path)))
            {
                if(System.IO.Path.GetDirectoryName(Path) != "")
                    Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            }
            if (!File.Exists(path))
            {
                CreatedByEditor = true;
                return;
            }

            bool comment = false;
            bool commentline = false;
            bool equals = false;
            string pretxt = "";
            bool insideAp = false;

            foreach (string line in File.ReadAllLines(path, Encoding.Default))
            {
                string nospaces = "";
                int n = 0;
                foreach (char c in line)
                {
                    if (specialChars.Contains(c) && !insideAp)
                        nospaces += $" {c} ";
                    else if (c == '"' && n + 1 < line.Length)
                    {
                        if (line[n + 1] == '"')
                            nospaces += $"{c} ";
                        else
                            nospaces += c;
                    }
                    else
                        nospaces += c;
                    n++;
                    if (c == '"')
                        insideAp = !insideAp;

                }

                nospaces = Regex.Replace(nospaces.Replace("\t", " "), @"\s+", " ").Trim();

                //Console.WriteLine(nospaces);

                List<string> contentsl = nospaces.Split(' ').ToList();
                contentsl.RemoveAll(x => x == " " || x == "");
                string[] contents = contentsl.ToArray();

                for (int a = 0; a < contents.Length; a++)
                {
                    if (!comment)
                    {
                        switch (contents[a])
                        {
                            case "#":
                                if (pretxt != "")
                                    CurrentNode.AddPureValue(pretxt, contents.Length == 1);
                                comment = true;
                                equals = false;
                                pretxt = "";
                                if (a == 0)
                                    commentline = true;
                                break;
                            case "=":
                                equals = true;
                                break;
                            case "{":
                                CurrentNode = CurrentNode.AddNode(pretxt);
                                pretxt = "";
                                equals = false;
                                break;
                            case "}":
                                if (pretxt != "")
                                    CurrentNode.AddPureValue(pretxt, contents.Length == 1);
                                CurrentNode = CurrentNode.Parent;
                                pretxt = "";
                                break;
                            default:
                                string textmem = "";
                                if (contents[a].Count(x => x == '"') == 1)
                                {
                                    textmem += contents[a] + " ";
                                    do
                                    {
                                        a++;
                                        textmem += contents[a] + " ";
                                    } while (a + 1 < contents.Length && !contents[a].Contains("\""));
                                    textmem.Trim();
                                }

                                if (!equals && pretxt != "")
                                {
                                    CurrentNode.AddPureValue(pretxt, contents.Length == 1);
                                    CurrentNode.AddPureValue(textmem == "" ? contents[a] : textmem, contents.Length == 1);
                                    pretxt = "";
                                }
                                else if (!equals && pretxt == "")
                                {
                                    if (a + 1 == contents.Length)
                                        CurrentNode.AddPureValue(contents[a], contents.Length == 1);
                                    else
                                        pretxt = textmem == "" ? contents[a] : textmem;

                                }
                                else if (equals)
                                {
                                    Variable v = new Variable(pretxt, textmem == "" ? contents[a] : textmem);
                                    CurrentNode.AddVariable(v);
                                    equals = false;
                                    pretxt = "";
                                }
                                break;
                        }
                    }
                    else
                    {
                        string textleft = "";
                        for (; a < contents.Length; a++)
                        {
                            textleft += contents[a] + " ";
                        }

                        if (commentline)
                        {
                            if (CurrentNode.ItemOrder.Any())
                            {
                                CommentLine cl = new CommentLine(textleft, CurrentNode.ItemOrder.Last());
                                CurrentNode.Comments.Add(cl);
                            }
                            else if (CurrentNode.PureValues.Any())
                            {
                                CommentLine cl = new CommentLine(textleft, CurrentNode.PureValues.Last());
                                CurrentNode.Comments.Add(cl);
                            }
                            else
                            {
                                NodeItem nl = null;
                                CommentLine cl = new CommentLine(textleft, nl);
                                CurrentNode.Comments.Add(cl);
                            }


                        }
                        else
                        {
                            if (CurrentNode.ItemOrder.Any())
                                CurrentNode.ItemOrder.Last().Comment = textleft;
                            else
                                CurrentNode.FirstBracketComment = textleft;
                        }
                    }
                }
                comment = false;
                commentline = false;
                pretxt = "";
            }
        }
        public void SaveFile(string path)
        {
            File.WriteAllText(path, Node.NodeToText(MainNode));
        }
        public void SaveFile()
        {
            File.WriteAllText(Path, Node.NodeToText(MainNode));
        }
    }
    public class CommentLine
    {
        public string Text = "";
        public NodeItem Below = null;
        public PureValue BelowPureValue = null;
        public CommentLine(string text, NodeItem below)
        {
            Text = text;
            Below = below;
        }
        public CommentLine(string text, PureValue below)
        {
            Text = text;
            BelowPureValue = below;
        }
    }

    public class PureValue
    {
        public string Name = "";
        public bool SeparateLine = false;
        public override string ToString()
        {
            return Name;
        }
        public PureValue(string val, bool sep = false)
        {
            Name = val;
            SeparateLine = sep;
        }
    }

    public class Node : NodeItem
    {
        public string PureInnerText = "";
        public bool UseInnerText = false;
        public string FirstBracketComment = "";
        public Node Parent = null;
        public List<Node> Nodes = new List<Node>();
        public List<Variable> Variables = new List<Variable>();
        public List<PureValue> PureValues = new List<PureValue>();
        public List<CommentLine> Comments = new List<CommentLine>();
        public List<NodeItem> ItemOrder = new List<NodeItem>();
        public Node(string name)
        {
            Name = name;
        }
        public Node(string name, Node parent)
        {
            Name = name;
            Parent = parent;
        }
        public static string NodeToText(Node n)
        {
            string text = "";
            foreach (CommentLine cl in n.Comments)
            {
                if (cl.Below == null && cl.BelowPureValue == null)
                    text += "#" + cl.Text + "\n";
            }

            if (n.PureValues.Any())
            {
                int count = 0;
                bool lastwassep = false;
                foreach (PureValue s in n.PureValues)
                {
                    if (s.SeparateLine)
                    {
                        if (!lastwassep)
                            text += "\n";
                        text += s.Name + "\n";
                        lastwassep = true;
                        count = 0;
                    }
                    else
                    {
                        count++;
                        if (count == 10)
                        {
                            count = 0;
                            text += s + "\n";
                        }
                        else
                        {
                            text += s + " ";
                        }
                        lastwassep = false;
                    }
                    foreach (CommentLine cl in n.Comments.FindAll(x => x.BelowPureValue == s))
                    {
                        if (!lastwassep)
                            text += "\n";
                        text += "#" + cl.Text + "\n";
                        lastwassep = true;
                    }
                }
                text += "\n";
            }

            foreach (NodeItem ni in n.ItemOrder)
            {
                if (ni is Variable)
                {
                    Variable v = (Variable)ni;
                    text += v.Name + " = " + v.Value;
                    if (v.Comment != "")
                        text += "#" + v.Comment;
                    text += "\n";
                    foreach (CommentLine cl in n.Comments)
                    {
                        if (cl.Below == v)
                            text += "#" + cl.Text + "\n";
                    }
                }
                else if (ni is Node)
                {
                    Node inner = (Node)ni;
                    text += inner.Name + " = {";
                    if (inner.FirstBracketComment != "")
                        text += " #" + inner.FirstBracketComment + "\n";
                    if (inner.UseInnerText && false)
                    {
                        text += " " + inner.PureInnerText + " ";
                    }
                    else
                    {
                        text += "\n";
                        string innertext = NodeToText(inner);
                        string tabbedtext = "";
                        foreach (string line in innertext.Split('\n'))
                        {
                            if (line != "")
                            {
                                tabbedtext += "\t" + line + "\n";
                            }
                        }
                        text += tabbedtext;
                    }
                    text += "}";
                    if (ni.Comment != "")
                        text += "#" + ni.Comment;
                    text += "\n";

                    foreach (CommentLine cl in n.Comments)
                    {
                        if (cl.Below == inner)
                            text += "#" + cl.Text + "\n";
                    }
                }
            }

            return text;
        }
        public bool ChangeVariable(string name, string value, bool forceadd = false)
        {
            Variable v = Variables.Find(x => x.Name == name);
            if (v != null)
            {
                v.Value = value;
                return true;
            }
            else
            {
                if (forceadd)
                {
                    v = new Variable(name, value);
                    Variables.Add(v);
                    ItemOrder.Add(v);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        public Node AddNode(string name)
        {
            Node n = new Node(name, this);
            Nodes.Add(n);
            ItemOrder.Add(n);
            return n;
        }
        public void AddNode(Node node)
        {
            Nodes.Add(node);
            ItemOrder.Add(node);
            node.Parent = this;
        }
        public PureValue AddPureValue(string name, bool sep = false)
        {
            PureValue pv = new PureValue(name, sep);
            PureValues.Add(pv);
            return pv;
        }
        public string[] GetPureValuesAsArray()
        {
            List<string> s = new List<string>();
            foreach (PureValue pv in PureValues)
                s.Add(pv.Name);
            return s.ToArray();
        }
        public Variable AddVariable(string name, string value)
        {
            Variable v = new Variable(name, value);
            Variables.Add(v);
            ItemOrder.Add(v);
            return v;
        }
        public void AddVariable(Variable v)
        {
            Variables.Add(v);
            ItemOrder.Add(v);
        }

        public void RemoveVariable(Variable v)
        {
            Variables.Remove(v);
            ItemOrder.Remove(v);
        }
        public void RemoveNode(Node n)
        {
            Nodes.Remove(n);
            ItemOrder.Remove(n);
        }
    }
    public class Variable : NodeItem
    {
        public string Value = "";

        public Variable(string name, string value)
        {
            Name = name;
            Value = value;
        }
    }
}