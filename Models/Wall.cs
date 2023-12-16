
using SnakeGame;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Models
{

    /// <summary>
    /// This class represents a single wall object in the game's world
    /// </summary>
    
    [DataContract(Namespace = "")]

    public class Wall
    {
        
        //ID that represents the wall
        [JsonPropertyName("wall")]
        [DataMember(Name = "ID")]
        public int wall { get; private set; }

        //One endpoint of the wall
        [JsonPropertyName("p1")]
        [DataMember(Name = "p1")]
        public Vector2D p1 { get; private set; }

        //Second endpoint of the walla
        [JsonPropertyName("p2")]
        [DataMember(Name = "p2")]
        public Vector2D p2 { get; private set; }


        /// <summary>
        /// Default Constructor needed for proper JSON  deserialization
        /// </summary>
        public Wall()
        {
            p1 = new Vector2D();
            p2 = new Vector2D();
        }

        /// <summary>
        /// JSON constructor to properly deserialize a JSON object to a wall object
        /// </summary>
        [JsonConstructor]
        public Wall(int wall, Vector2D p1, Vector2D p2)
        {
            this.wall = wall;
            this.p1 = p1;
            this.p2 = p2;
        }
}
}
