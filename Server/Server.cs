using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using Models;
using NetworkController;

/// <summary>
/// This class represents our server for our Snake Game
/// </summary>
class Server
{
    // A map of clients that are connected, each with an ID
    private Dictionary<long, SocketState> clients;
    
    //The game world object
    private World theWorld;

    //Gamsettings object that will read xml settings file
    private GameSettings gs;

    //Stopwatch to time when to send world updates
    public Stopwatch stopwatch;

    //boolean for running update infinite loop
    public bool ServerOn = false;

    //variable for MSPerFrame from settings file
    private long MSPerFrame;


    /// <summary>
    /// Main method to initialize and start server, as well as start infinite loop for 
    /// sending updates.
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        Server server = new Server();
        server.stopwatch.Start();
        server.ServerOn = true;
        server.StartServer();
        // Sleep to prevent the program from closing,
        // since all the real work is done in separate threads.
        // StartServer is non-blocking.
        Console.Read();
        server.ServerOn = false;
    }

    /// <summary>
    /// Initialize the server's state
    /// </summary>
    public Server()
    {
        clients = new Dictionary<long, SocketState>();
        DataContractSerializer ser = new(typeof(GameSettings));
        XmlReader reader = XmlReader.Create("GameSettings.xml");
        gs = (GameSettings)ser.ReadObject(reader)!;
        theWorld = new World(gs.getUniverse(), gs.GetWalls(), gs.RespawnRate, gs.MSPerFrame);
        this.MSPerFrame = gs.MSPerFrame;
        stopwatch = new Stopwatch();
    }

    /// <summary>
    /// Start accepting Tcp sockets connections from clients and start update loop
    /// </summary>
    public void StartServer()
    {
        // This begins an "event loop"
        Networking.StartServer(NewClientConnected, 11000);

        Console.WriteLine("Server is running. Accepting new clients.");

        //update the world and broadcast to clients every MSPerFrame
        while (ServerOn)
        {
            Update();
        }
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a new client connects (see line 41)
    /// </summary>
    /// <param name="state">The SocketState representing the new client</param>
    private void NewClientConnected(SocketState state)
    {
        if (state.ErrorOccurred)
            return;

        Console.WriteLine("New client connection. ID: " + state.ID);



        // change the state's network action to the 
        // receive handler so we can process data when something
        // happens on the network
        state.OnNetworkAction = ReceiveMessage;

        Networking.GetData(state);
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a network action occurs (see lines 64-66)
    /// </summary>
    /// <param name="state"></param>
    private void ReceiveMessage(SocketState state)
    {
        // Remove the client if they aren't still connected
        if (state.ErrorOccurred)
        {
            return;
        }

        ProcessMessage(state);
        // Continue the event loop that receives messages from this client
        Networking.GetData(state);
    }


    /// <summary>
    /// Process requests from clients
    /// </summary>
    /// <param name="sender">The SocketState that represents the client</param>
    private void ProcessMessage(SocketState state)
    {
        string totalData = state.GetData();
        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        //
        string p = parts[0];


            //if its a clients first time sending a message, it needs to be added to the dictionary
            if (!clients.ContainsKey((int)state.ID))
            {
                //send startup info
                RecieveNewClientName(state, p);
                lock (clients)
                {
                    //Add to dictionary
                    clients.Add(state.ID, state);
                }
            }

            //otherwise process the request
            else
                theWorld.UpdateSnakeOrientation((int)state.ID, p);
           
            // Remove data from the SocketState's growable buffer
            state.RemoveData(0, totalData.Length);
            

            // We also need to remove any disconnected clients.
            HashSet<long> disconnectedClients = new HashSet<long>();

        
    }

    /// <summary>
    /// Private method for when its a clients first time joining a server. Startup info of walls
    /// and world info will be sent, as well as the clients id.
    /// </summary>
    /// <param name="state"></param>
    /// <param name="name"></param>
    private void RecieveNewClientName(SocketState state, string name)
    {
        //Get player name
        name = name.TrimEnd('\n');
        Console.WriteLine("Player " + name + " joined the server.");

        //Send startup info (d and world size).
        Networking.Send(state.TheSocket, state.ID.ToString() + "\n" + theWorld.Size.ToString() + "\n");
        string objString;


        //Send all walls
        foreach (Wall wall in theWorld.Walls.Values)
        {
            objString = JsonSerializer.Serialize(wall) + "\n";
            Networking.Send(state.TheSocket, objString);
        }
        
        //Lock the world while adding snake to it
        lock (theWorld)
        {
            //Create new snake object and add it to the world
            theWorld.SpawnSnake((int)state.ID, name);

        }
    }

    /// <summary>
    /// Removes a client from the clients dictionary
    /// </summary>
    /// <param name="id">The ID of the client</param>
    private void RemoveClient(long id)
    {
        Console.WriteLine("Client " + id + " disconnected");

        lock (clients)
        {
            clients.Remove(id);
 
        }
    }


    /// <summary>
    /// This method will send world updates to all connected clients at the specified frame rate
    /// in the XML settings file.
    /// </summary>
    private void Update()
    {

        if (stopwatch.IsRunning)
        {
            //Spin until the stopwatch has reached the set MSPerFrame
            while (stopwatch.ElapsedMilliseconds < MSPerFrame)
            {
                //Do Nothing
            }
        }
        else
            stopwatch.Start();

        //Restart stopwatch everytime we update the world
        stopwatch.Restart();

        //Update world
        theWorld.Update();

        //Stringbuilder for sending updates
        StringBuilder world = new StringBuilder();


        lock (theWorld)
        {
            //Update powerups
            foreach (Powerup powerup in theWorld.Powerups.Values)
            {
                world.Append(JsonSerializer.Serialize(powerup) + "\n");
            }

            //Update Snakes
            foreach (Snake snake in theWorld.Snakes.Values)
            {
                world.Append(JsonSerializer.Serialize(snake) + "\n");
            }
        }


        lock (clients)
        {
            //Loop through clients
            foreach (SocketState client in clients.Values)
            {
                //Check if client is disconnected before sending
                if (!client.TheSocket.Connected)
                {
                    lock (theWorld)
                    {
                        //If disconnected, let all other clients know
                        if (theWorld.Snakes.ContainsKey((int)client.ID))
                        {
                            theWorld.Snakes[(int)client.ID].disconnected();
                        }
                    }

                    //RemoveClient from server list
                    RemoveClient(client.ID);
                }

                //Otherwise, send the client updated world
                else
                {
                    Networking.Send(client.TheSocket, world.ToString());
                }
            }
        }
    }


    /// <summary>
    /// This class is a basic internal class that will be used to read the settings from a XML file
    /// and serialize them into an object.
    /// </summary>
    [DataContract(Name = "GameSettings", Namespace = "")]
    internal class GameSettings
    {
        //Size of the game world
        [DataMember]
        public int UniverseSize;

        //Amount of frames to wait before sendng updates
        [DataMember]
        public int MSPerFrame;

        //Respawn rate for the game
        [DataMember]
        public int RespawnRate;

        //List of walls in the game
        [DataMember]
        public List<Wall> Walls = new List<Wall>();

        public GameSettings()
        {
            //Default sizes needed for XML serialization
            this.UniverseSize = 10;
            this.MSPerFrame = 10;
            this.MSPerFrame += 10;
            this.Walls = new List<Wall>();
        }

        //Get universe size
        public int getUniverse()
        {
            return UniverseSize;
        }

        //Get Frame per MS
        public int getFrames()
        {
            return MSPerFrame;
        }

        //Get Respawn Rate
        public int getRespawnRate()
        {
            return RespawnRate;

        }

        //Get List of Walls
        public List<Wall> GetWalls()
        {
            return Walls;
        }
    }
}