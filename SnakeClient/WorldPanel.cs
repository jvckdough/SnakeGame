using IImage = Microsoft.Maui.Graphics.IImage;
#if MACCATALYST
using Microsoft.Maui.Graphics.Platform;
#else
using Microsoft.Maui.Graphics.Win2D;
#endif
using System.Reflection;
using Models;
using System.Diagnostics;

namespace SnakeGame;
/// <summary>
/// Class that will be used to draw objects in the client's view
/// </summary>
public class WorldPanel : IDrawable
{
    private IImage wall;
    private IImage background;
    private bool initializedForDrawing = false;
    public delegate void ObjectDrawer(object o, ICanvas canvas);
    public World theWorld;
    private int clientID;

    private IImage loadImage(string name)
    {
        Assembly assembly = GetType().GetTypeInfo().Assembly;
        string path = "SnakeClient.Resources.Images";
        using (Stream stream = assembly.GetManifestResourceStream($"{path}.{name}"))
        {
#if MACCATALYST
            return PlatformImage.FromStream(stream);
#else
            return new W2DImageLoadingService().FromStream(stream);
#endif
        }
    }


    /// <summary>
    /// Set the world from the information from the gameController
    /// </summary>
    /// <param name="world"></param>
    public void setWorld(World world)
    {
        theWorld = world;
    }

    /// <summary>
    /// Load images to be used for drawing
    /// </summary>
    private void InitializeDrawing()
    {
        wall = loadImage( "wallsprite.png" );
        background = loadImage( "background.png" );
        initializedForDrawing = true;
    }

    /// <summary>
    /// Draw all objects in the client view based on updated world information
    /// </summary>
    /// <param name="canvas"></param>
    /// <param name="dirtyRect"></param>
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        //Check if images are loaded or not
        if (!initializedForDrawing)
            InitializeDrawing();


        //If the world is not null, start drawing objects
        if (theWorld != null)
        {
            //Critical Section: Lock the world to handle race conditions 
            lock (theWorld)
            {
                //Only draw when the snakes list is not empty
                if(theWorld.Snakes.Count() != 0) 
                { 

                //Get client's X and Y values
                float clientX = (float)theWorld.Snakes[theWorld.clientID].body[theWorld.Snakes[theWorld.clientID].body.Count - 1].X; //"(the player's world-space X coordinate)"
                float clientY = (float)theWorld.Snakes[theWorld.clientID].body[theWorld.Snakes[theWorld.clientID].body.Count - 1].Y; //"(the player's world-space Y coordinate)"

                canvas.DrawImage(background, -clientX - 550, -clientY - 550, theWorld.Size, theWorld.Size);

                // undo previous transformations from last frame
                canvas.ResetState();

                //Center world on client's snake
                float playerX = clientX;
                float playerY = clientY;
                canvas.Translate(-playerX + (900 / 2), -playerY + (900 / 2));

                //Draw walls
                foreach (Wall wall in theWorld.Walls.Values)
                {
                    // Find if the wall is vertical or horizontal and draw in the correct direction
                    Vector2D difference = wall.p1 - wall.p2;

                    double yMax = Math.Max(wall.p1.GetY(), wall.p2.GetY());
                    double yMin = Math.Min(wall.p1.GetY(), wall.p2.GetY());
                    double xMax = Math.Max(wall.p1.GetX(), wall.p2.GetX());
                    double xMin = Math.Min(wall.p1.GetX(), wall.p2.GetX());

                    // If vertical
                    if (difference.GetX() == 0)
                    {
                        for (double y = yMin; y <= yMax; y += 50)
                        {
                            DrawObjectWithTransform(canvas, wall, wall.p1.GetX(), y, 0, WallSegmentDrawer);
                        }
                    }

                    // If horizontal
                    else
                    {
                        for (double x = xMin; x <= xMax; x += 50)
                        {
                            DrawObjectWithTransform(canvas, wall, x, wall.p1.GetY(), 0, WallSegmentDrawer);
                        }
                    }
                }

                //Draw powerups
                foreach (Powerup powerup in theWorld.Powerups.Values)
                {
                    if (powerup.died == false)
                    {
                        DrawObjectWithTransform(canvas, powerup, powerup.loc.GetX(), powerup.loc.GetY(), 0, PowerupSegmentDrawer);
                    }
                }
                    //Draw snakes
                    foreach (Snake snake in theWorld.Snakes.Values.ToList())
                    {
                        if (!snake.dc)
                        {
                            // Loop through snake segments, calculate segment length and segment direction
                            // Set the Stroke Color, etc, based on s's ID
                            if (!snake.alive)
                            {
                                clientID = -1;
                            }
                            else
                            {
                                clientID = snake.snake;
                            }
                            //Score + name
                            canvas.DrawString(snake.name + ": " + snake.score.ToString(), (float)snake.body[snake.body.Count - 1].GetX() - 250, (float)snake.body[snake.body.Count - 1].GetY() - 30, 500, 500, HorizontalAlignment.Center, VerticalAlignment.Top);

                            //head
                            DrawObjectWithTransform(canvas, snake, snake.body[snake.body.Count - 1].GetX(), snake.body[snake.body.Count - 1].GetY(), 0, SnakeHeadDrawer);

                            //tail
                            DrawObjectWithTransform(canvas, snake, snake.body[0].GetX(), snake.body[0].GetY(), 0, SnakeHeadDrawer);

                            //body
                            for (int i = snake.body.Count - 1; i > 0; i--)
                            {
                                Debug.WriteLine(snake.body.Count());
                                Vector2D vector = snake.body[i] - snake.body[i - 1];
                                double length = vector.Length();
                                vector.Normalize();
                                DrawObjectWithTransform(canvas, length, snake.body[i].GetX(), snake.body[i].GetY(), vector.ToAngle(), SnakeSegmentDrawer);

                            }
                        }
                    }

                }
            }
        }
    }


    /// <summary>
    /// Method that will draw a single wall segment
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void WallSegmentDrawer(object o, ICanvas canvas)
    {
        canvas.DrawImage(wall, -30, -30, wall.Width, wall.Height);
    }

    /// <summary>
    /// Method that will draw a single powerup 
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void PowerupSegmentDrawer(object o, ICanvas canvas)
    {
        Powerup p = o as Powerup;
        int width = 8;
        canvas.FillColor = Colors.Green;

        // Ellipses are drawn starting from the top-left corner.
        // So if we want the circle centered on the powerup's location, we have to offset it
        // by half its size to the left (-width/2) and up (-height/2)
        canvas.FillCircle(-(width / 2), -(width / 2), 10);
    }
    
    /// <summary>
    /// Draw the head of a snake object
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void SnakeHeadDrawer(object o, ICanvas canvas)
    {
        Snake s = o as Snake;
        ClientColor(canvas);
        canvas.FillCircle(0, 0, 5);
    }

    /// <summary>
    /// Draw the body of a snake object
    /// </summary>
    /// <param name="o"></param>
    /// <param name="canvas"></param>
    private void SnakeSegmentDrawer(object o, ICanvas canvas)
    { 
        int snakeSegmentLength = int.Parse(o.ToString());
        ClientColor(canvas);
        canvas.StrokeSize = 10;
        canvas.DrawLine(0, 0, 0, snakeSegmentLength);
    }

    /// <summary>
    /// This method performs a translation and rotation to draw an object.
    /// </summary>
    /// <param name="canvas">The canvas object for drawing onto</param>
    /// <param name="o">The object to draw</param>
    /// <param name="worldX">The X component of the object's position in world space</param>
    /// <param name="worldY">The Y component of the object's position in world space</param>
    /// <param name="angle">The orientation of the object, measured in degrees clockwise from "up"</param>
    /// <param name="drawer">The drawer delegate. After the transformation is applied, the delegate is invoked to draw whatever it wants</param>
    private void DrawObjectWithTransform(ICanvas canvas, object o, double worldX, double worldY, double angle, ObjectDrawer drawer)
    {
        // "push" the current transform
        canvas.SaveState();

        canvas.Translate((float)worldX, (float)worldY);
        canvas.Rotate((float)angle);
        drawer(o, canvas);

        // "pop" the transform
        canvas.RestoreState();
    }

    /// <summary>
    /// Change the color of each snake based on its ClientID number
    /// </summary>
    /// <param name="canvas"></param>
    private void ClientColor(ICanvas canvas)
    {
        if (clientID == -1)
        {
            canvas.StrokeColor = Colors.Red;
            canvas.FillColor = Colors.Red;
        }
        else if (clientID == 0)
        {
            canvas.StrokeColor = Colors.Purple;
            canvas.FillColor = Colors.Purple;
        }
        else if (clientID == 1)
        {
            canvas.StrokeColor = Colors.Blue;
            canvas.FillColor = Colors.Blue;
        }
        else if (clientID == 2)
        {
            canvas.StrokeColor = Colors.Yellow;
            canvas.FillColor = Colors.Yellow;

        }
        else if (clientID == 3)
        {
            canvas.StrokeColor = Colors.Orange;
            canvas.FillColor = Colors.Orange;

        }
        else if (clientID == 4)
        {
            canvas.StrokeColor = Colors.Cyan;
            canvas.FillColor = Colors.Cyan;

        }
        else if (clientID == 5)
        {
            canvas.StrokeColor = Colors.Brown;
            canvas.FillColor = Colors.Brown;

        }
        else if (clientID == 6)
        {
            canvas.StrokeColor = Colors.White;
            canvas.FillColor = Colors.White;

        }
        else if (clientID == 7)
        {
            canvas.StrokeColor = Colors.Pink;
            canvas.FillColor = Colors.Pink;

        }
        else
        {
            canvas.StrokeColor = Colors.Black;
            canvas.FillColor = Colors.Black;
        }
    }

}
