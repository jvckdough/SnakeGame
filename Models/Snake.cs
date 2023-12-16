using SnakeGame;
using System.Text.Json.Serialization;
using NetworkController;
using System.Text.Json;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;
using System;

namespace Models
{
    /// <summary>
    /// This class represents a Player's snake in the game's world.
    /// </summary>
    public class Snake
    {
        // ClientID number for the snake.
        [JsonPropertyName("snake")]
        public int snake { get; private set; }

        //Plauer name associated with this snake
        [JsonPropertyName("name")]
        public string name { get; private set; }

        //list of segments of the snake body
        [JsonPropertyName("body")]
        public List<Vector2D> body { get; private set; }

        //direction the snake is facing
        [JsonPropertyName("dir")]
        public Vector2D dir { get; private set; }

        //the score of the plauer
        [JsonPropertyName("score")]
        public int score { get; private set; }

        //Boolean value if the snake has died or not
        [JsonPropertyName("died")]
        public bool died { get; private set; }

        // Represents if the snake is alive or not
        [JsonPropertyName("alive")]
        public bool alive { get; private set; }

        //Represents if the player has disconnected or not
        [JsonPropertyName("dc")]
        public bool dc { get; private set; }

        //Represents if the player joined on this frame or not
        [JsonPropertyName("join")]
        public bool join { get; private set; }

        //Speed of Snake
        public int speed = 6;

        //Time when snake died
        public uint lastDeath = 0;

        //Length of snake
        public int length = 0;

        //Check if snake is currently growing or not
        public bool growing = false;

        //Last Powerup of Snake
        public uint lastPower = 0;

        //the last direction of the snakes movement 
        public Vector2D lastdirection = new Vector2D();

        //Direction of snake tail
        public Vector2D tailDirection = new Vector2D();


        /// JSON constructor to deserialize a JSON object into a snake object
        /// </summary>
        [JsonConstructor]
        public Snake(int snake, string name, List<Vector2D> body, Vector2D dir, int score, bool died, bool alive, bool dc, bool join)
        {
            this.snake = snake;
            this.name = name;
            this.body = body;
            this.dir = dir;
            this.score = score;
            this.died = died;
            this.alive = alive;
            this.dc = dc;
            this.join = join;
        }

        /// Default constructor, needed for JSON to deserialize
        /// </summary>
        public Snake()
        {
            body = new List<Vector2D>();
            dir = new Vector2D();
            name = "";
        }

        /// <summary>
        /// Method for when a snake crashes
        /// </summary>
        /// <param name="time"></param>
        public void crashed(uint time)
        {
            this.died = true;
            this.alive = false;
            this.speed = 0;
            lastDeath = time;
        }

        /// <summary>
        /// Grow the Snake
        /// </summary>
        /// <param name="time"></param>
        public void grow(uint time)
        {
            lastPower = time;
            growing = true;
            this.score += 1;
        }

        /// <summary>
        /// Method to indicate snake has died
        /// </summary>
        /// <param name="dead"></param>
        public void setDied(bool dead)
        {
            this.died = dead;
        }

        /// <summary>
        /// Change the direction of the snake's based on the client request
        /// </summary>
        /// <param name="direction"></param>
        /// <param name="time"></param>
        public void ClientChangedDirection(string direction)
        {
            //Save old direction
            Vector2D olddir = new Vector2D(this.dir);
            
            if (direction.Equals("right"))
            {
                this.dir = new Vector2D(1.0, 0.0);
            }
            else if (direction.Equals("left"))
                this.dir = new Vector2D(-1.0, 0.0);

            else if (direction.Equals("up"))
                this.dir = new Vector2D(0.0, -1.0);

            else if(direction.Equals("down"))
                this.dir = new Vector2D(0.0, 1.0);

            //If the direction is the opposite of the current , this movement is not allowed and 
           // the head direction is not changed.
            if (this.dir.IsOppositeCardinalDirection(olddir))
                this.dir = olddir;


        }

        /// <summary>
        /// Update the snake
        /// </summary>
        /// <param name="command"></param>
        /// <param name="time"></param>
        /// <param name="olddirection"></param>
        public void Update(string command, uint time, Vector2D olddirection)
        {
            // Check if Snake is alive
            if (!this.alive)
                return;

            // Update the direction based on request
            Vector2D newDirection = GetNewDirection(command)!;

            //If movement is allowed and it is a new direction, add a new head to the snake
            if (newDirection != null && !newDirection.IsOppositeCardinalDirection(this.dir) && !newDirection.Equals(olddirection))
            {
                this.dir = newDirection!;
                // Add a new vertex for the new head position
                Vector2D newHead = new Vector2D(this.body[body.Count()-1].X + this.dir.X * speed, this.body[body.Count() - 1].Y + this.dir.Y * speed);
                 this.body.Add(newHead);
                
            }

            //Otherwise just move the snake
            else
            {
                 this.body[body.Count()-1] = new Vector2D(this.body[body.Count() - 1].X + this.dir.X * speed, this.body[body.Count() - 1].Y + this.dir.Y * speed);
            }

            // If snake is growing, don't update the tail
            if (this.growing)
            {
                if(time - this.lastPower >= 24) this.growing = false; // Reset the growing state after 24 frames
            }

            else
            {

                // Move the tail
                
                    Vector2D tail = this.body[0];
                    Vector2D nextVertex = this.body[1];
                    this.tailDirection = new Vector2D(nextVertex.X - tail.X, nextVertex.Y - tail.Y);
                    tailDirection.Normalize();
                    this.body[0] = new Vector2D(tail.X + tailDirection.X * speed, tail.Y + tailDirection.Y * speed);

                    // Check if the tail reached the next vertex
                    if (this.body[0].Equals(nextVertex))
                    {
                    // Remove the old tail vertex when it does
                    this.body.RemoveAt(0);
                    }
            }
        }

        /// <summary>
        /// Get the direction of the snake head based on the user request
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        private Vector2D? GetNewDirection(string request)
        {
            // Determine the new direction based on request
            switch (request)
            {
                case "right":
                    return new Vector2D(1.0, 0.0);
                case "left":
                    return new Vector2D(-1.0, 0.0);
                case "up":
                    return new Vector2D(0.0, -1.0);
                case "down":
                    return new Vector2D(0.0, 1.0);
                default:
                    
                    // No direction change
                    return null;
            }
        }

        /// <summary>
        /// Method to update snake when a client disconnects.
        /// </summary>
        public void disconnected()
        {
            this.dc = true;
            this.alive = false;
            this.died = true;
        }
    }

}
