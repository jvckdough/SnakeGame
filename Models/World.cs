
using System.Diagnostics;
using System.Net.NetworkInformation;
using NetworkController;
using SnakeGame;
using static System.Net.Mime.MediaTypeNames;

using System.Text.Json;
using System.Xml.Linq;


namespace Models
{
    /// <summary>
    /// This class represents all the objects that a present in each frame of the Client's game.
    /// </summary>
    public class World
    {
        //All the Snakes present in the game
        public Dictionary<int, Snake> Snakes;

        //All the Powerups present in the game
        public Dictionary<int, Powerup> Powerups;

        //All the Walls present in the game
        public Dictionary<int, Wall> Walls;

        //The Size of the game world
        public int Size;

        //The clientID that represents the clients snake
        public int clientID;

        //Object used to generate random numbers
        public Random random = new Random();

        //The rate at which a dead snake respawns
        public int RespawnRate;

        //How many miliseconds pass between each frame
        public int MSPerFrame;

        //A measurement of how many frames have passed
        public uint time;

        //A measurement of the last time a powerup was eaten
        public uint timeFromLastPW;

        //Object used to measure time
        private Stopwatch stopwatch = new Stopwatch();


        /// <summary>
        /// Constructor for a world object, takes an integer that represents the size of the game world.
        /// </summary>
        public World(int size)
        {
            this.Snakes = new Dictionary<int, Snake>();
            this.Powerups = new Dictionary<int, Powerup>();
            this.Walls = new Dictionary<int, Wall>();
            Size = size;
            this.RespawnRate = 10;
        }

        /// <summary>
        /// Constructor to pass XML settings to the World
        /// </summary>
        /// <param name="size"></param>
        /// <param name="Filewalls"></param>
        /// <param name="RespawnRate"></param>
        /// <param name="MSPerFrame"></param>
        public World(int size, List<Wall> Filewalls, int RespawnRate, int MSPerFrame)
        {
            this.Snakes = new Dictionary<int, Snake>();
            this.Powerups = new Dictionary<int, Powerup>();
            this.Walls = new Dictionary<int, Wall>();
            foreach (Wall wall in Filewalls)
            {
                this.Walls[wall.wall] = wall;
            }
            this.AddPowerUps(100);
            Size = size;
            this.RespawnRate = RespawnRate;
            this.MSPerFrame = MSPerFrame;
            this.time = 0;
            timeFromLastPW = 0;

        }

        /// <summary>
        /// Move the direction of the snake based on the requests sent from the client
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="message"></param>
        public void UpdateSnakeOrientation(int ID, String message)
        {
            //Create a substring from the user request to process it
            string str = "";
            if (message.Count<char>() != 0)
            {
                str = message.Substring(message.IndexOf(':') + 1);
                str = str.Substring(1, str.Length - 4);
            }

            //Check if the command is any of the 4 expected commands, if not, just ignore
            // and don't update direction
            if (str.Equals("up"))
            {
                Snakes[ID].ClientChangedDirection("up");
            }

            else if (str.Equals("right"))
            {
                Snakes[ID].ClientChangedDirection("right");
            }

            else if (str.Equals("left"))
            {
                Snakes[ID].ClientChangedDirection("left");
            }

            else if (str.Equals("down"))
            {
                Snakes[ID].ClientChangedDirection("down");
            }
        }
        
        /// <summary>
        /// This method will add a snake to the world and choose a random location to spawn into 
        /// the world that is "safe" to spawn into.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public void SpawnSnake(int id, string name)
        {
            int randNum = random.Next(0, 2);

            int randX;
            int randY;

            if (randNum == 1)
            {
                randX = random.Next(-850, -450);
                randY = random.Next(-850, -450);
            }
            else
            {
                randX = random.Next(450, 850);
                randY = random.Next(450, 850);
            }

            int randDirX = random.Next(-1, 2);
            int randDirY = 0;

            if (randDirX == 0)
            {
                randDirY = random.Next(0, 2) == 0 ? -1 : 1;
            }

            Vector2D head;
            Vector2D tail;

            //if snake is going up 
            if (randDirY == -1)
            {
                head = new Vector2D(randX, randY);
                tail = new Vector2D(randX, randY - 120);
            }

            // if snake is going down
            else if (randDirY == 1)
            {
                head = new Vector2D(randX, randY);
                tail = new Vector2D(randX, randY + 120);
            }
            // if snake is going left 
            else if (randDirX == -1)
            {
                head = new Vector2D(randX, randY);
                tail = new Vector2D(randX - 120, randY);
            }
            // if snake is going right
            else
            {
                head = new Vector2D(randX, randY);
                tail = new Vector2D(randX + 120, randY);
            }

            //Create list for snake body
            List<Vector2D> body = new List<Vector2D> { head, tail };

            //Chose snake direction
            Vector2D dir = new Vector2D(randDirX, randDirY);

            //if the world does not contain a snake, add it 
            if (!this.Snakes.ContainsKey(id))
            {
                this.Snakes.Add(id, new Snake(id, name, body, dir, 0, false, true, false, true));
            }

            //otherwise, just update the snakes position
            else
            {
                this.Snakes[id] = new Snake(id, name, body, dir, 0, false, true, false, true);
                this.Snakes[id].speed = 6;
            }

            //Set the snakes lastdirection to its current direction
            Snakes[id].lastdirection = Snakes[id].dir;
        }

        /// <summary>
        /// Method to add powerups into the world
        /// </summary>
        /// <param name="count"></param>
        public void AddPowerUps(int count)
        {
            for (int i = 0; i < count; i++)
            {
                int randX = random.Next(-97, 97) * 10;
                int randY = random.Next(-97, 97) * 10;
                Vector2D loc = new Vector2D(randX, randY);

                Powerup powerup = new Powerup(this.Powerups.Count, loc, false);
                this.Powerups.Add(this.Powerups.Count, powerup);
            }
        }

        /// <summary>
        /// Method to update the state of the game world.
        /// </summary>
        public void Update()
        {
            //Updaate snakes 
            foreach (Snake snake in Snakes.Values)
            {
                snake.setDied(false);
                if (time - snake.lastDeath >= RespawnRate && snake.alive == false && snake.dc == false)
                {
                    SpawnSnake(snake.snake, snake.name);
                }

                //Check if the snake is goig left
                if (snake.dir.Equals(new Vector2D(-1, 0)))
                {
                    snake.Update("left", time, snake.lastdirection);

                }
                //Check if the snake is going right 
                else if (snake.dir.Equals(new Vector2D(1, 0)))
                {
                    snake.Update("right", time, snake.lastdirection);
                }
                //Check if the snake is going up 
                else if (snake.dir.Equals(new Vector2D(0, 1)))
                {
                    snake.Update("down", time, snake.lastdirection);
                }
                //Check if the snake is going down 
                else if (snake.dir.Equals(new Vector2D(0, -1)))
                {
                    snake.Update("up", time, snake.lastdirection);
                }

                //Set snakes last direction to its current direction for next world update
                snake.lastdirection = snake.dir;

                //Check Snake collisions 
                CheckCollision(snake);

            }

            //Update powerups
            foreach (Powerup powerup in this.Powerups.Values)
            {
                if (time - powerup.lastDeath >= 75 && powerup.died == true)
                {
                    powerup.died = false;
                }
            }

            time++;
        }

        /// <summary>
        /// Method to CheckCollsions that happen in the world
        /// </summary>
        /// <param name="snake"></param>
        private void CheckCollision(Snake snake)
        {
            //Check if snake hits other snake
            foreach (Snake enemy in this.Snakes.Values)
            {
                if (enemy.snake != snake.snake)
                {
                    // Find if the snake segmenet is vertical or horizontal 
                    for (int i = 0; i < enemy.body.Count - 1; i++)
                    {
                        Vector2D difference = enemy.body[i] - enemy.body[i + 1];

                        double yMax = Math.Max(enemy.body[i].GetY(), enemy.body[i + 1].GetY());
                        double yMin = Math.Min(enemy.body[i].GetY(), enemy.body[i + 1].GetY());
                        double xMax = Math.Max(enemy.body[i].GetX(), enemy.body[i + 1].GetX());
                        double xMin = Math.Min(enemy.body[i].GetX(), enemy.body[i + 1].GetX());

                        // If vertical
                        if (difference.GetX() == 0)
                        {
                            for (double y = yMin; y <= yMax; y += 1)
                            {
                                if (Math.Abs(snake.body[snake.body.Count - 1].GetX() - xMax) <= 5 && Math.Abs(snake.body[snake.body.Count - 1].GetY() - y) <= 5)
                                {
                                    if (snake.alive && enemy.alive) snake.crashed(time);
                                }
                            }
                        }

                        // If horizontal
                        else
                        {
                            for (double x = xMin; x <= xMax; x += 1)
                            {
                                if (Math.Abs(snake.body[snake.body.Count - 1].GetY() - yMax) <= 5 && Math.Abs(snake.body[snake.body.Count - 1].GetX() - x) <= 5)
                                {
                                    if (snake.alive) snake.crashed(time);
                                }
                            }
                        }
                    }

                }
                //check self collisions
                else
                {
                    // Find if the snake segmenet is vertical or horizontal 
                    for (int i = 0; i < enemy.body.Count - 2; i++)
                    {
                        Vector2D difference = enemy.body[i] - enemy.body[i + 1];

                        double yMax = Math.Max(enemy.body[i].GetY(), enemy.body[i + 1].GetY());
                        double yMin = Math.Min(enemy.body[i].GetY(), enemy.body[i + 1].GetY());
                        double xMax = Math.Max(enemy.body[i].GetX(), enemy.body[i + 1].GetX());
                        double xMin = Math.Min(enemy.body[i].GetX(), enemy.body[i + 1].GetX());

                        // If vertical
                        if (difference.GetX() == 0)
                        {
                            for (double y = yMin; y <= yMax; y += 1)
                            {
                                if (Math.Abs(snake.body[snake.body.Count - 1].GetX() - xMax) <= 5 && Math.Abs(snake.body[snake.body.Count - 1].GetY() - y) <= 5)
                                {
                                    if (snake.alive) snake.crashed(time);
                                }
                            }
                        }

                        // If horizontal
                        else
                        {
                            for (double x = xMin; x <= xMax; x += 1)
                            {
                                if (Math.Abs(snake.body[snake.body.Count - 1].GetY() - yMax) <= 5 && Math.Abs(snake.body[snake.body.Count - 1].GetX() - x) <= 5)
                                {
                                    if (snake.alive) snake.crashed(time);
                                }
                            }
                        }
                    }
                }
            } 
            //check if snake collides with any powerups
            foreach (Powerup pw in this.Powerups.Values)
            {
                if (Math.Abs(pw.loc.GetX() - snake.body[snake.body.Count - 1].GetX()) <= 10 && Math.Abs(pw.loc.GetY() - snake.body[snake.body.Count - 1].GetY()) <= 10)
                {
                    int randX = random.Next(-96, 96) * 10;
                    int randY = random.Next(-96, 96) * 10;
                    Vector2D loc = new Vector2D(randX, randY);

                    pw.crashed(time);
                    snake.grow(time);
                    pw.setLoc(loc);
                }
            }

            //check if snake hit wall
            foreach (Wall wall in this.Walls.Values)
            {
                // Find if the wall is vertical or horizontal and draw in the correct direction
                Vector2D difference = wall.p1 - wall.p2;

                double yMax = Math.Max(wall.p1.GetY(), wall.p2.GetY());
                double yMin = Math.Min(wall.p1.GetY(), wall.p2.GetY());
                double xMax = Math.Max(wall.p1.GetX(), wall.p2.GetX());
                double xMin = Math.Min(wall.p1.GetX(), wall.p2.GetX());

                //If vertical
                if (difference.GetX() == 0)
                {
                    for (double y = yMin; y <= yMax; y += 50)
                    {
                        if (Math.Abs(snake.body[snake.body.Count - 1].GetX() - xMax) <= 25 && Math.Abs(snake.body[snake.body.Count - 1].GetY() - y) <= 25)
                        {
                            if (snake.alive) snake.crashed(time);
                        }
                    }
                }

                // If horizontal
                else
                {
                    for (double x = xMin; x <= xMax; x += 50)
                    {
                        if (Math.Abs(snake.body[snake.body.Count - 1].GetY() - yMax) <= 25 && Math.Abs(snake.body[snake.body.Count - 1].GetX() - x) <= 25)
                        {
                            if (snake.alive) snake.crashed(time);
                        }
                    }
                }
            }
        }
    }   
}