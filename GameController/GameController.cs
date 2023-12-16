using System.Text.Json;
using System.Text.RegularExpressions;
using Models;
using NetworkController;

namespace GameController
{
    public class GameController
    {
        
        //Delegates for View to subcribe to get updates from controller.
        public delegate void DataHandler();
        public event DataHandler? DataArrived;

        public delegate void ConnectedHandler();
        public event ConnectedHandler? Connected;

        public delegate void ErrorHandler(string err);
        public event ErrorHandler? Error;

        //Represents if the server's 1st message has been sent
        private bool firstMessage = true;

        //Represents the clients snake in the World
        private int ClientID;

        //Set world to default size.
        private World theWorld = new World(10);


        /// <summary>
        /// State representing the connection with the server
        /// </summary>
        SocketState? theServer = null;

        /// <summary>
        /// Begins the process of connecting to the server
        /// </summary>
        /// <param name="addr"></param>
        public void Connect(string addr)
        {
            try
            {
                Networking.ConnectToServer(OnConnect, addr, 11000);
            }
            catch(Exception) 
            {
                Error?.Invoke("Server not found");
            }

        }

        /// <summary>
        /// Method to be invoked by the networking library when a connection is made
        /// </summary>
        /// <param name="state"></param>
        private void OnConnect(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // inform the view
                Error?.Invoke("Server not found");
                return;
            }

            theServer = state;

            // inform the view
            Connected?.Invoke();

            // Start an event loop to receive messages from the server
            state.OnNetworkAction = ReceiveMessage;
            Networking.GetData(state);
        }

        /// <summary>
        /// Method to be invoked by the networking library when
        /// data is available
        /// </summary>
        /// <param name="state"></param>
        private void ReceiveMessage(SocketState state)
        {
            if (state.ErrorOccurred)
            {
                // inform the view
                Error?.Invoke("Lost connection to server");
                return;
            }
            ProcessMessages(state);

            // Continue the event loop
            // state.OnNetworkAction has not been changed,
            // so this same method (ReceiveMessage)
            // will be invoked when more data arrives
            Networking.GetData(state);
        }

        /// <summary>
        /// Process any buffered messages separated by '\n'
        /// Then inform the view
        /// </summary>
        /// <param name="state"></param>
        private void ProcessMessages(SocketState state)
        {
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");

            // Loop until we have processed all messages.
            // We may have received more than one.

            List<string> newMessages = new List<string>();

            lock (this)
            {
                foreach (string p in parts)
                {
                    // Ignore empty strings added by the regex splitter
                    if (p.Length == 0)
                        continue;
                    // The regex splitter will include the last string even if it doesn't end with a '\n',
                    // So we need to ignore it if this happens.
                    if (p[p.Length - 1] != '\n')
                        break;

                    // build a list of messages to send to the view
                    newMessages.Add(p);

                    // Then remove it from the SocketState's growable buffer
                    state.RemoveData(0, p.Length);
                }
            }
            //break newMessages down for deserialization and update model
            ProcessData(newMessages);

            // inform the view and redraw
            DataArrived?.Invoke();
        }

        /// <summary>
        /// Closes the connection with the server
        /// </summary>
        public void Close()
        {
            theServer?.TheSocket.Close();
        }

        /// <summary>
        /// Send a message to the server
        /// </summary>
        /// <param name="message"></param>
        public void MessageEntered(string message)
        {
            if (theServer is not null)
                Networking.Send(theServer.TheSocket, message + "\n");
        }


        /// <summary>
        /// Getter method for the view to get the current state of the world.
        /// </summary>
        /// <returns></returns>
        public World getWorld()
        {
            return theWorld;
        }

        /// <summary>
        /// Private method to set world sieze and deserialize new messages sent from server into
        /// json objects in the world.
        /// </summary>
        /// <param name="data"></param>
        private void ProcessData(List<string> data)
        {
            
            //if first message from server, set the client id and world size.
            if (firstMessage)
            { 
                ClientID = int.Parse(data.First());
                this.theWorld = new World(int.Parse(data[1]));
                theWorld.clientID = ClientID;
                //If the Server sends the walls here, deserialize them.
                for(int i = 2; i < data.Count; i++)
                {
                    Wall wall = JsonSerializer.Deserialize<Wall>(data[i])!;
                    theWorld.Walls.Add(wall.wall, wall);
                }                    
                firstMessage = false;
            }
            
            else
            {
                //Critical section: Lock world to negate any race conditions that might occur during deserialization
                lock (theWorld)
                {
                    //Loop through dataa
                    for (int j = 0; j < data.Count(); j++)
                    {
                        //parse message as json
                        JsonDocument doc = JsonDocument.Parse(data[j]);


                        //If snake object,deserialize as snake and add to theWorld's snake dictionary.
                        if (doc.RootElement.TryGetProperty("snake", out _))
                        {
                            Snake snake = JsonSerializer.Deserialize<Snake>(data[j])!;

                            //If snake is not in dictionary, add it.
                            if (!theWorld.Snakes.ContainsKey(snake.snake))
                            {
                                theWorld.Snakes.Add(snake.snake, snake);
                            }

                            //otherwise, just update the snake information.
                            else 
                                theWorld.Snakes[snake.snake] = snake;
                        }

                        //If wall object, deserialize as wall.
                        else if (doc.RootElement.TryGetProperty("wall", out _))
                        {
                            Wall wall = JsonSerializer.Deserialize<Wall>(data[j])!;
                            theWorld.Walls.Add(wall.wall, wall);

                        }

                        //If not wall or snake, deserialize as powerup.
                        else
                        {
                            Powerup powerup = JsonSerializer.Deserialize<Powerup>(data[j])!;

                            //if powerup not in dictionary, add it. If it is, update its properties.
                            if (!theWorld.Powerups.ContainsKey(powerup.power))
                            {
                                theWorld.Powerups.Add(powerup.power, powerup);
                            }
                            else 
                                theWorld.Powerups[powerup.power] = powerup;
                        }
                    }
                }
            }
        }
    }
}