
using SnakeGame;
using System.Text.Json.Serialization;


namespace Models
{
    /// <summary>
    /// This class represents a powerup object in the game's world.
    /// </summary>
    public class Powerup

    {
       // powerup property for when adding to World dictionary 
        [JsonPropertyName("power")]
        public int power { get; private set; }

        //Property for location of the powerup
        [JsonPropertyName("loc")]
        public Vector2D loc { get; private set; }


        //Represents if powerup was connected or not
        [JsonPropertyName("died")]
        public bool died { get; set; } = true;

        public uint lastDeath = 0;
        /// <summary>
        /// Default constructor so JSON can properly deserialize
        /// </summary>
        public Powerup() 
        {
            loc = new Vector2D();
        }


        /// <summary>
        /// JSON constructor for deserializing Powerups from Server messages.
        /// </summary>
        /// <param name="power"></param>
        /// <param name="loc"></param>
        /// <param name="died"></param>
        [JsonConstructor]
        public Powerup(int power, Vector2D loc, bool died)
        {
            this.power = power;
            this.loc = loc;
            this.died = died;
        }

        /// <summary>
        /// Check if a powerup has been collected by a snake
        /// </summary>
        /// <param name="time"></param>
        public void crashed(uint time)
        {
            this.died = true;
            lastDeath = time;
        }

        /// <summary>
        /// Set location of a powerup 
        /// </summary>
        /// <param name="vector"></param>
        public void setLoc(Vector2D vector)
        {
            this.loc = vector;
        }
    }
}
