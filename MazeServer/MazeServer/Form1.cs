using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;

namespace MazeServer
{
    public partial class MazeServer : Form
    {
        public MazeServer()
        {
            InitializeComponent();
        }
                       
        private TcpListener listener; // listen for client connection        
        public int currentPlayer; // keep track of whose turn it is         
        private Thread getPlayers; // Thread for acquiring client connections
        internal bool disconnected = false; // true if the server closes  
        private Player[] players;
        private Thread[] playerThreads;



        private void Form1_Load(object sender, EventArgs e)
        {
            players = new Player[2];
            playerThreads = new Thread[2];
            currentPlayer = 1;
            getPlayers = new Thread(new ThreadStart(SetUp));
            getPlayers.Start();
        }
        private delegate void DisplayDelegate(string message);
        internal void DisplayMessage(string message)
        {
            if (displayTextBox.InvokeRequired)
            {                                      
                Invoke(new DisplayDelegate(DisplayMessage),
                   new object[] { message });
            }
            else displayTextBox.Text += message;
        }
        public void SetUp()
        {
            DisplayMessage("Waiting for players...\r\n");
            listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 50000);
            listener.Start();

            players[0] = new Player(listener.AcceptSocket(), this, 1);
            playerThreads[0] =
               new Thread(new ThreadStart(players[0].Run));
            playerThreads[0].Start();

            // accept second player and start another player thread       
            players[1] = new Player(listener.AcceptSocket(), this, 2);
            playerThreads[1] =
               new Thread(new ThreadStart(players[1].Run));
            playerThreads[1].Start();

            // let the first player know that the other player has connected
            lock (players[0])
            {
                players[0].threadSuspended = false;
                Monitor.Pulse(players[0]);
            } // end lock   
        }
        public bool PlayerMove(int location, int player)
        {
            lock (this)
            {
                while (player != currentPlayer)
                    Monitor.Wait(this);


            }
            return true;
        }
        private void MazeServer_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Environment.Exit(System.Environment.ExitCode);
        }
        public bool ValidMove(int location, int player)
        {
            // prevent another thread from making a move
            lock (this)
            {
                // while it is not the current player's turn, wait
                while (player != currentPlayer)
                    Monitor.Wait(this);


                // set the currentPlayer to be the other player
                currentPlayer = (currentPlayer + 1) % 2;

                // notify the other player of the move                
                players[currentPlayer].OtherPlayerMoved(location);

                    // alert the other player that it's time to move
                    Monitor.Pulse(this);
                    return true;
            } // end lock
        }
        public void SendMove(int number, int location_X, int location_Y)
        {
            
            int currentplayer = (number == 1 ? 1 : 0);
            players[currentplayer].ReseveLocation(location_X, location_Y);
        }
        public void SendEndMove(int number)
        {
            int currentplayer = (number == 1 ? 1 : 0);
            players[currentplayer].ReseveEndMove();
        }
        public void SendFinishGame(int number)
        {
            int currentplayer = (number == 1 ? 1 : 0);
            players[currentplayer].ReseveFinishGame();
        }
    }
    public class Player
    {
        internal Socket connection;  
        private NetworkStream socketStream;        
        private MazeServer server;      
        private BinaryWriter writer;  
        private BinaryReader reader; 
        private int number;                 
        internal bool threadSuspended = true;
        public Player(Socket socket, MazeServer serverValue,
           int newNumber)
        {
            connection = socket;
            server = serverValue;
            number = newNumber;
   
            socketStream = new NetworkStream(connection);
            
            writer = new BinaryWriter(socketStream);
            reader = new BinaryReader(socketStream);
        } 

        public void OtherPlayerMoved(int location)
        {
            // signal that opponent moved                     
            writer.Write("Opponent moved.");
            writer.Write(location); // send location of move
        }
        public void Run()
        {
            bool done = false;

            // display on the server that a connection was made
            server.DisplayMessage((number == 1 ? "Player1" : "Player2")
               + " connected\r\n");
            writer.Write(number);

            writer.Write((number == 1 ?
        "Player1 connected.\r\n" : "Player2 connected, please wait.\r\n"));

            if (number == 1)
            {
                writer.Write("Waiting for player2.");

                lock (this)
                {
                    while (threadSuspended)
                        Monitor.Wait(this);
                } // end lock               

                writer.Write("player2 connected. Your move.");
            } // end if

            int count = 0;
            // play game
            while (!done)
            {
                // wait for data to become available
                while (connection.Available == 0)
                {
                    Thread.Sleep(1000);

                    if (server.disconnected)
                        return;
                } // end while
                

                count++;
                int location_X = reader.ReadInt32();
                int location_Y = reader.ReadInt32();
                server.DisplayMessage((number == 1 ? "Player1 Moving (" + count + ")" : "Player2 Moving(" + count + ")"));
                if (location_X==9999 && location_Y==9999)
                {
                    server.DisplayMessage(" \r\n End Game, " + (number == 1 ? "Mario Win" : "Luigi Win"));
                    server.SendFinishGame(number);
                    done = true;
                    break;
                }
                server.DisplayMessage("Loc X:" + location_X +" , Loc Y:"+location_Y+ "\r\n");
                server.SendMove(number, location_X, location_Y);

                if(count == 50)
                {
                    server.DisplayMessage("End Moving \r\n");
                    server.SendEndMove(number);
                    writer.Write("Other Player Turn.");
                    count = 0;
                }



            } 
            
        }

        public void ReseveLocation(int location_X,int location_Y)
        {
            writer.Write("Receve Location");
            writer.Write(location_X);
            writer.Write(location_Y);
        }
        public void ReseveEndMove()
        {
            writer.Write("End Moving");
        }
        public void ReseveFinishGame()
        {
            writer.Write("Win");
        }
    }
}


