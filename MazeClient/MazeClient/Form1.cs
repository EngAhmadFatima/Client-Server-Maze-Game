using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace MazeClient
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private Thread outputThread; // Thread for receiving data from server
        private TcpClient connection; // client to establish connection      
        private NetworkStream stream; // network data stream                 
        private BinaryWriter writer; // facilitates writing to the stream    
        private BinaryReader reader; // facilitates reading from the stream  
        private int myMark; // player's mark on the board                   
        private bool myTurn; // is it this player's turn?                             
        private bool done = false; // true when game is over     
        private string PlayerName;
        private int MoveCounter = 50;
        private int PlayerPower = 0;

        private void Form1_Load(object sender, EventArgs e)
        {
            connection = new TcpClient("127.0.0.1", 50000);
            stream = connection.GetStream();
            writer = new BinaryWriter(stream);
            reader = new BinaryReader(stream);

            outputThread = new Thread(new ThreadStart(RunClient));
            outputThread.Start();
        }
        public void sendPlayerMove(int x,int y)
        {
            if (myTurn)
            {
                writer.Write(x);
                writer.Write(y);
            }
        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }
        private delegate void DisplayDelegate(string message);
        private delegate void DisableInputDelegate(bool value);
        private delegate void ChangeIdLabelDelegate(string message);
        private void ChangeIdLabel(string label)
        {
            // if modifying idLabel is not thread safe
            if (idLabel.InvokeRequired)
            {
                // use inherited method Invoke to execute ChangeIdLabel
                // via a delegate                                       
                Invoke(new ChangeIdLabelDelegate(ChangeIdLabel),
                   new object[] { label });
            } // end if
            else // OK to modify idLabel in current thread
                idLabel.Text = label;
        }
        private void DisplayMessage(string message)
        {
            // if modifying displayTextBox is not thread safe
            if (displayTextBox.InvokeRequired)
            {
                // use inherited method Invoke to execute DisplayMessage
                // via a delegate                                       
                Invoke(new DisplayDelegate(DisplayMessage),
                   new object[] { message });
            } // end if
            else // OK to modify displayTextBox in current thread
                displayTextBox.Text = message;
        }
        public void RunClient()
        {
            myMark = reader.ReadInt32();
            PlayerName = (myMark == 1 ? "mario" : "luigi");
            ChangeIdLabel((PlayerName =="mario"? "MARIO":"LUIGI"));
            myTurn = (myMark == 1 ? true : false);
            if (PlayerName == "mario")
            {
                idLabel.ForeColor = Color.Red;
            }
            else idLabel.ForeColor = Color.Green;
            try
            {     
                while (!done)
                {
                    ProcessMessage(reader.ReadString());
                }
            } 
            catch (IOException)
            {
                MessageBox.Show("Server is down, game over", "Error",
                   MessageBoxButtons.OK, MessageBoxIcon.Error);
            } 

        }
        public void ProcessMessage(string message)
        {
            if(message == "Waiting for player2.")
            {
                DisplayMessage(message+"\r\n");
                mario.Enabled = false;
            }
            else if (message == "player2 connected. Your move.")
            {
                DisplayMessage(message + "\r\n");
                mario.Enabled = true;
            }
           else if (message == "Receve Location")
            {
                int location_X = reader.ReadInt32();
                int location_Y = reader.ReadInt32();
                OtherPlayerMoving(location_X, location_Y);
            }
            else if (message == "End Moving")
            {
                DisplayMessage("Other Player End Moving. Your Turn .. \r\n");
                myTurn = true;
                MoveCounter = 50;
                lbl_MoveCounter.Text = MoveCounter.ToString();
            }
            else if (message == "Other Player Turn.")
            {
                myTurn = false;
                DisplayMessage("Other Player Turn. Please Wait .. \r\n");
            }
           else if(message == "Win")
            {
                DisplayMessage((PlayerName=="mario"?"LUIGI":"MARIO")+" Win the Game. \r\n");
            }
            else
            {
                DisplayMessage(message + "\r\n");
            }
        }
        public void OtherPlayerMoving(int location_X, int location_Y)
        {
            if(PlayerName == "luigi")
            {
                mario.Location = new Point(location_X, location_Y);
            }
            else
            {
                luigi.Location = new Point(location_X, location_Y);
            }
           
        }
        private void mario_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if(e.KeyData == Keys.Right)
            {
                if (myTurn)
                {
                    if (PlayerName == "mario")
                    {
                        if (MarioMoveRight())
                        {
                            sendPlayerMove(mario.Location.X,mario.Location.Y);
                            MoveCounter--; lbl_MoveCounter.Text = MoveCounter.ToString();
                            EatPlayerFromRight(mario, luigi);
                            for (int i = 1; i < 5; i++)
                            {
                                Control co = ((PictureBox)panel1.Controls["Jew" + i]);
                                if ((mario.Location.X + mario.Size.Width) > (co.Location.X) && mario.Location.X < (co.Location.X + co.Size.Width))
                                {
                                    co.Visible = false;
                                    co.Location = new Point(0, 0);
                                    PlayerPower = PlayerPower + 50;
                                    lbl_power.Text = PlayerPower.ToString();
                                }
                            }
                            if ((mario.Location.X + mario.Size.Width) > finish.Location.X && (mario.Location.X + mario.Size.Width) < (finish.Location.X + finish.Size.Width))
                            {
                                sendPlayerMove(9999,9999);
                                mario.Enabled = false;
                                MessageBox.Show("You Win");
                                DisplayMessage("You Win the Game.");
                            }
                        }
                    }
                }
            }
            else if(e.KeyData == Keys.Left)
            {
                if(myTurn)
                {
                    if (PlayerName == "mario")
                    {
                        if (MarioMoveLeft()) { sendPlayerMove(mario.Location.X, mario.Location.Y); MoveCounter--; lbl_MoveCounter.Text = MoveCounter.ToString();}
                    }
                }
            }
            else if(e.KeyData == Keys.Up)
            {
                if (myTurn)
                {
                    if (PlayerName == "mario")
                    {
                        if (MarioMoveUp())
                        {
                            sendPlayerMove(mario.Location.X, mario.Location.Y);
                            MoveCounter--;
                            lbl_MoveCounter.Text = MoveCounter.ToString();
                            
                            for (int i=1; i<5; i++)
                            {
                                Control co = ((PictureBox)panel1.Controls["mas"+i]);
                                if (mario.Location.Y < (co.Location.Y + co.Size.Height) && mario.Location.Y > (co.Location.Y))
                                {
                                    co.Visible = false;
                                    co.Location = new Point(0, 0);
                                    PlayerPower = PlayerPower + 50;
                                    lbl_power.Text = PlayerPower.ToString();
                                }
                            }
                        }
                    }
                }
            }
            else if(e.KeyData == Keys.Down)
            {
                if (myTurn)
                {
                    if (PlayerName == "mario")
                    {
                        if (MarioMoveDown()) { sendPlayerMove(mario.Location.X, mario.Location.Y); MoveCounter--; lbl_MoveCounter.Text = MoveCounter.ToString(); }
                    }
                }
            }
        }
        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }
        public int MoveRight(PictureBox Player, Control Wall)
        {
            float WX1 = Wall.Location.X;
            float WX2 = Wall.Location.X + Wall.Size.Width;
            float WY1 = Wall.Location.Y;
            float WY2 = Wall.Location.Y + Wall.Size.Height;

            float PX1 = Player.Location.X;
            float PX2 = Player.Location.X + Player.Size.Width;
            float PY1 = Player.Location.Y;
            float PY2 = Player.Location.Y + Player.Size.Height;

            if ((PX2 < WX1 -1) || (PX1 > WX2 +1))
            {
                return 1;
            }
            else if ((PY2 < WY1 -1) || (PY1 > WY2 +1))
            {
                return 1;
            }
            else if (PX2 == WX1) { return 2; }
            else return 0;
        }
        public int MoveLeft(PictureBox Player, Control Wall)
        {
            float WX1 = Wall.Location.X;
            float WX2 = Wall.Location.X + Wall.Size.Width;
            float WY1 = Wall.Location.Y;
            float WY2 = Wall.Location.Y + Wall.Size.Height;

            float PX1 = Player.Location.X;
            float PX2 = Player.Location.X + Player.Size.Width;
            float PY1 = Player.Location.Y;
            float PY2 = Player.Location.Y + Player.Size.Height;

            if ((PX2 < WX1 -1) || (PX1 > WX2 +1))
            {
                return 1;
            }
            else if ((PY2 < WY1 -1) || (PY1 > WY2 +1))
            {
                return 1;
            }
            else if (PX1 == WX2) { return 2; }
            else return 0;
        }
        public int MoveUp(PictureBox Player, Control Wall)
        {
            float WX1 = Wall.Location.X;
            float WX2 = Wall.Location.X + Wall.Size.Width;
            float WY1 = Wall.Location.Y;
            float WY2 = Wall.Location.Y + Wall.Size.Height;

            float PX1 = Player.Location.X;
            float PX2 = Player.Location.X + Player.Size.Width;
            float PY1 = Player.Location.Y;
            float PY2 = Player.Location.Y + Player.Size.Height;

            if ((PY1 > WY2 +1) || (PY2 < WY1 -1))
            {
                return 1;
            }
            else if ((PX1 > WX2 +1) || (PX2 < WX1 -1))
            {
                return 1;
            }
            else if (PY1 == WY2)
            {
                return 2;
            }
            else return 0;
        }
        public int MoveDown(PictureBox Player, Control Wall)
        {
            float WX1 = Wall.Location.X;
            float WX2 = Wall.Location.X + Wall.Size.Width;
            float WY1 = Wall.Location.Y;
            float WY2 = Wall.Location.Y + Wall.Size.Height;

            float PX1 = Player.Location.X;
            float PX2 = Player.Location.X + Player.Size.Width;
            float PY1 = Player.Location.Y;
            float PY2 = Player.Location.Y + Player.Size.Height;

            if ((PY1 > WY2 +1) || (PY2 < WY1-1))
            {
                return 1;
            }
            else if ((PX1 > WX2+1) || (PX2 < WX1-1))
            {
                return 1;
            }
            else if (PY2 == WY1) { return 2; }
            else return 0;
        }

        public bool MarioMoveRight()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveRight(mario, Cont));
                }
            }
            if (list.Contains(2)) { mario.Left = mario.Location.X - 3; return true; }
            else if (list.Contains(0)) { mario.Left = mario.Location.X - 1; return false;  }
            else { mario.Left = mario.Location.X + 3; return true; }
        }
        public bool MarioMoveLeft()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveLeft(mario, Cont));
                }
            }
            if (list.Contains(2)) { mario.Location = new Point(mario.Location.X + 3, mario.Location.Y); return true; }
            else if (list.Contains(0)) { mario.Location = new Point(mario.Location.X + 1, mario.Location.Y); return false; }
            else { mario.Location = new Point(mario.Location.X - 3, mario.Location.Y); return true; }
        }
        public bool MarioMoveUp()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveUp(mario, Cont));
                }
            }
            if (list.Contains(2)) { mario.Location = new Point(mario.Location.X, mario.Location.Y + 3); return true; }
            else if (list.Contains(0)) { mario.Location = new Point(mario.Location.X, mario.Location.Y + 1); return false; }
            else { mario.Location = new Point(mario.Location.X, mario.Location.Y - 3); return true; }
        }
        public bool MarioMoveDown()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveDown(mario, Cont));
                }
            }
            if (list.Contains(2)) { mario.Location = new Point(mario.Location.X, mario.Location.Y - 3); return true; }
            else if (list.Contains(0)) { mario.Location = new Point(mario.Location.X, mario.Location.Y - 1); return false; }
            else { mario.Location = new Point(mario.Location.X, mario.Location.Y + 3); return true; }
        }
        public bool LuigiMoveRight()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveRight(luigi, Cont));
                }
            }
            if (list.Contains(2)) { luigi.Left = luigi.Location.X - 3; return true; }
            else if (list.Contains(0)) { luigi.Left = luigi.Location.X - 1; return false; }
            else { luigi.Left = luigi.Location.X + 3; return true; }
        }
        public bool LuigiMoveLeft()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveLeft(luigi, Cont));
                }
            }
            if (list.Contains(2)) { luigi.Location = new Point(luigi.Location.X + 3, luigi.Location.Y); return true; }
            else if (list.Contains(0)) { luigi.Location = new Point(luigi.Location.X + 1, luigi.Location.Y); return false; }
            else { luigi.Location = new Point(luigi.Location.X - 3, luigi.Location.Y); return true; }
        }
        public bool LuigiMoveUp()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveUp(luigi, Cont));
                }
            }
            if (list.Contains(2)) { luigi.Location = new Point(luigi.Location.X, luigi.Location.Y + 3); return true; }
            else if (list.Contains(0)) {luigi.Location = new Point(luigi.Location.X, luigi.Location.Y + 1); return false; }
            else { luigi.Location = new Point(luigi.Location.X, luigi.Location.Y - 3); return true; }
        }
        public bool LuigiMoveDown()
        {
            List<int> list = new List<int>();
            foreach (Control Cont in panel1.Controls.Cast<Control>())
            {
                if (Cont is Label)
                {
                    list.Add(MoveDown(luigi, Cont));
                }
            }
            if (list.Contains(2)) { luigi.Location = new Point(luigi.Location.X, luigi.Location.Y - 3); return true; }
            else if (list.Contains(0)) { luigi.Location = new Point(luigi.Location.X, luigi.Location.Y - 1); return false; }
            else { luigi.Location = new Point(luigi.Location.X, luigi.Location.Y + 3); return true; }
        }

        public void EatPlayerFromRight(Control player1 , Control player2)
        {
            if ((player1.Location.X+player1.Size.Width) > (player2.Location.X) && (player1.Location.X)<(player2.Location.X+player2.Size.Width) )
            {
                MessageBox.Show("You Have Ate All LUIGI Power ...");
                player1.Location = new Point(player1.Location.X +player1.Size.Width+player2.Size.Width,player1.Location.Y);
                writer.Write(8888);
                writer.Write(8888);
            }
        }
        private void panel1_MouseClick(object sender, MouseEventArgs e)
        {
            if (PlayerName == "mario")
            {
                mario.Focus();
            }
            else luigi.Focus();
        }

        private void luigi_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyData == Keys.Right)
            {
                if (myTurn)
                {
                    if (PlayerName == "luigi")
                    {
                        if (LuigiMoveRight())
                        {
                            MoveCounter--;
                            lbl_MoveCounter.Text = MoveCounter.ToString();
                            sendPlayerMove(luigi.Location.X, luigi.Location.Y);

                            for (int i = 1; i < 5; i++)
                            {
                                Control co = ((PictureBox)panel1.Controls["Jew" + i]);
                                if ((luigi.Location.X+luigi.Size.Width) > (co.Location.X) && luigi.Location.X < (co.Location.X + co.Size.Width))
                                {
                                    co.Visible = false;
                                    co.Location = new Point(0, 0);
                                    PlayerPower = PlayerPower + 50;
                                    lbl_power.Text = PlayerPower.ToString();
                                }
                            }
                        }
                        if ((luigi.Location.X + luigi.Size.Width) > finish.Location.X && (luigi.Location.X + luigi.Size.Width) < (finish.Location.X + finish.Size.Width))
                        {
                            sendPlayerMove(9999, 9999);
                            luigi.Enabled = false;
                            MessageBox.Show("You Win");
                            DisplayMessage("You Win the Game.");
                        }
                    }
                }
            }
            else if (e.KeyData == Keys.Left)
            {
                if (myTurn)
                {
                   if (PlayerName == "luigi")
                    {
                        if (LuigiMoveLeft()) { sendPlayerMove(luigi.Location.X, luigi.Location.Y); MoveCounter--; lbl_MoveCounter.Text = MoveCounter.ToString(); }
                    }
                }
            }
            else if (e.KeyData == Keys.Up)
            {
                if (myTurn)
                {
                    if (PlayerName == "luigi")
                    {
                        if (LuigiMoveUp())
                        {
                            sendPlayerMove(luigi.Location.X, luigi.Location.Y);
                            MoveCounter--;
                            lbl_MoveCounter.Text = MoveCounter.ToString();
                            for (int i = 1; i < 5; i++)
                            {
                                Control co = ((PictureBox)panel1.Controls["mas" + i]);
                                if (luigi.Location.Y < (co.Location.Y + co.Size.Height) && luigi.Location.Y > (co.Location.Y))
                                {
                                    co.Visible = false;
                                    co.Location = new Point(0, 0);
                                    PlayerPower = PlayerPower + 50;
                                    lbl_power.Text = PlayerPower.ToString();
                                }
                            }
                        }
                    }
                }
            }
            else if (e.KeyData == Keys.Down)
            {
                if (myTurn)
                {
                    if (PlayerName == "luigi")
                    {
                        if (LuigiMoveDown()) { sendPlayerMove(luigi.Location.X, luigi.Location.Y); MoveCounter--; lbl_MoveCounter.Text = MoveCounter.ToString(); }
                    }
                }
            }
        }

        private void lbl_MoveCounter_Click(object sender, EventArgs e)
        {

        }
    }

}
