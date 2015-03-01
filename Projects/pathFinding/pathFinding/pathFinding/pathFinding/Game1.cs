using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;
using System.Collections;

namespace pathFinding
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;
        Texture2D pixelSprite;
        Vector2 tempVec;

        MouseState prevMouse;
        KeyboardState prevKeyboard;
        Queue<Point> queue = new Queue<Point>();//Queue to hold the coordinates of the start and end
        Point p1, p2, temp, tempTemp;//Points to hold start/end coordinates
        PriorityQueue pQ = new PriorityQueue();//Priority queue based on its heuristic
        Queue<Point> tempQueue = new Queue<Point>();//Queue to add to the priority queue after neighbors have been checked
        bool [,] closedArray = new bool[ARRAYLENGTH, ARRAYLENGTH];//Closed set

        float pixelScale = 0.15f;
        const int HEIGHT = 550, WIDTH = 550, ARRAYLENGTH = 97;
        const double EDGE_COST = 1;//, DIAGONAL_COST = 1.414213562373095;
        int[,] pixelArray = new int[ARRAYLENGTH, ARRAYLENGTH];
        int numberOfEdgeSteps = 0, /*numberOfDiagonalSteps = 0,*/ queuePos = 0;
        double heuristic, fOfN, cost;
        bool gameStart = false, draw = false, setStart = false;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = HEIGHT;
            graphics.PreferredBackBufferWidth = WIDTH;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixelSprite = Content.Load<Texture2D>(@"sprites\lifeCell");//Loads sprite
            spriteFont = Content.Load<SpriteFont>(@"fonts\SpriteFont1");//Loads font
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                this.Exit();//Allows the game to exit
            if (Keyboard.GetState().IsKeyDown(Keys.D)
                && prevKeyboard.IsKeyUp(Keys.D))
                draw = !draw;//Activates draw obstruction mode
            if (Keyboard.GetState().IsKeyDown(Keys.G)
                && prevKeyboard.IsKeyUp(Keys.G))
                setStart = !setStart;//Activates set start/goal mode
            if (Keyboard.GetState().IsKeyDown(Keys.Space)
                && prevKeyboard.IsKeyUp(Keys.Space))
            {
                gameStart = true;//Starts game
                if (queue.Count > 1)
                {
                    aStar();//Activates A* if a start/goal has been chosen
                }
            }
            
            mickey();//Tracks mouse movement
            prevKeyboard = Keyboard.GetState();//Sets current keyboard state to previous
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            spriteBatch.Begin();
            
            if (draw || gameStart)//Draws pixelSprites on the display
            {
                for (int i = 0; i < ARRAYLENGTH; i++)
                    for (int j = 0; j < ARRAYLENGTH; j++)
                    {
                        if (pixelArray[i, j] == 1)//Draws black sprite
                        {
                            tempVec = new Vector2((i * (pixelSprite.Width * pixelScale)),
                                    (j * (pixelSprite.Height * pixelScale)));
                            spriteBatch.Draw(pixelSprite, tempVec, null, Color.Black, 0f,
                                Vector2.Zero, pixelScale, SpriteEffects.None, 0f);
                        }
                        else if (pixelArray[i, j] == 2)//Draws pink sprite
                        {
                            tempVec = new Vector2((i * (pixelSprite.Width * pixelScale)),
                                    (j * (pixelSprite.Height * pixelScale))); 
                            spriteBatch.Draw(pixelSprite, tempVec, null, Color.DeepPink, 0f,
                                Vector2.Zero, pixelScale, SpriteEffects.None, 0f);
                        }
                        else if (pixelArray[i, j] == 3)//Draws brown sprite
                        {
                            tempVec = new Vector2((i * (pixelSprite.Width * pixelScale)),
                                    (j * (pixelSprite.Height * pixelScale)));
                            spriteBatch.Draw(pixelSprite, tempVec, null, Color.Brown, 0f,
                                Vector2.Zero, pixelScale, SpriteEffects.None, 0f);
                        }
                        else if (pixelArray[i, j] == 4)//Draws green sprite
                        {
                            tempVec = new Vector2((i * (pixelSprite.Width * pixelScale)),
                                    (j * (pixelSprite.Height * pixelScale)));
                            spriteBatch.Draw(pixelSprite, tempVec, null, Color.Green, 0f,
                                Vector2.Zero, pixelScale, SpriteEffects.None, 0f);
                        }
                    }
            }
            else if (!gameStart)//Displays text/instructions before the game starts
            {
                spriteBatch.DrawString(spriteFont, "Welcome to Pathfinding! Press \"D\" " +
                "to \ndraw on the board, \"enter\" to quit, \"G\" \nto set start/end, and the spacebar\n" +
                "to start.", Vector2.Zero, Color.Black);
            }
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public void aStar()
        {
            p1 = pQ.Dequeue();//Start
            p2 = pQ.Dequeue();//Goal
            temp = p1;//Temporary point from the priority queue
            tempTemp = temp;//Temporary point to check neighbors of the other temporary point
            pQ.Enqueue(p1, 0);//Add to the priority queue

            while (!pQ.IsEmpty())//While the priority queue is not empty
            {
                if (temp.X == p2.X && temp.Y == p2.Y)//If the goal's array position is the same as the current position
                {
                    while (!pQ.IsEmpty())
                    {
                        pQ.Dequeue();
                    }
                    goalIsFound();//Change the sprite colors and exit the method
                    break;
                }
                else if (pixelArray[temp.X, temp.Y] == 1)//Do nothing if the point is an obstruction
                {
                    ;
                }
                else if (!closedArray[temp.X, temp.Y])//If the point isn't in the closed set...
                {
                    numberOfEdgeSteps = 1;//Calculates the amount of edges
                    numberOfEdgeSteps += Math.Abs(p1.X - temp.X);
                    numberOfEdgeSteps += Math.Abs(p1.Y - temp.Y);
                    
                    closedArray[temp.X, temp.Y] = true;

                    for (int i = -1; i < 2; i++)//Enqueue all its neighbors in the temporary queue
                        for (int j = -1; j < 2; j++)
                        {
                            if (i + temp.X > -1 && i + temp.X < ARRAYLENGTH//If it's within the screen
                                && j + temp.Y > -1 && j + temp.Y < ARRAYLENGTH)
                            {
                                if (pixelArray[i + temp.X, j + temp.Y] == 1)//If its neighbor is not an obstruction
                                    continue;
                                else if (i == 0 && j == 0)//Don't add itself to the temporary queue
                                    continue;
                                else if (i == j || i == -j)//Don't check diagonals
                                    continue;
                                else
                                {
                                    temp.X = temp.X + i;
                                    temp.Y = temp.Y + j;
                                    tempQueue.Enqueue(temp);//Add it to the temporary queue
                                    temp = tempTemp;//Reset the temporary point to the original
                                }
                            }
                        }

                    for (int l = 0; l < tempQueue.Count; l++)//Add all neighbors to the priority queue
                    {
                        Point tempPoint = tempQueue.Dequeue();
                        pQ.Enqueue(tempPoint, getF(tempPoint));
                    }
                }

                temp = pQ.Dequeue();//Pop from the priority queue
                tempTemp = temp;
            }
        }

        public void goalIsFound()
        {
            for (int i = 0; i < ARRAYLENGTH; i++)//Change the color of the visited sprites
                for (int j = 0; j < ARRAYLENGTH; j++)
                {
                    if (closedArray[i, j])
                        pixelArray[i, j] = 3;//Three represents visited sprite color
                }

            int o = p1.X;
            int p = p1.Y;
            pixelArray[o, p] = 2;//Change the start sprite to pink
        }

        public void hOfN(Point p)//Calculate heuristic
        {
            int x, y, z, w;

            x = p.X;
            y = p.Y;
            /* Diagonal calculations
            x -= p2.X;
            y -= p2.Y;
            x *= x;
            y *= y;
            heuristic = Math.Sqrt(x + y);
            */
            z = Math.Abs(x - p2.X);
            w = Math.Abs(y - p2.Y);

            heuristic = z + w;
        }

        public void gOfN()//Calculate cost
        {
            cost = numberOfEdgeSteps * EDGE_COST;// +numberOfDiagonalSteps * DIAGONAL_COST;
        }

        public double getF(Point p)//Add cost and the heuristic
        {
            hOfN(p);
            gOfN();
            return fOfN = cost + heuristic;
        }

        public void mickey()//Converts mouse clicks to moved positions
        {
            MouseState nowMouse = Mouse.GetState();

            if (IsActive && nowMouse.X != prevMouse.X && draw && !setStart
                && nowMouse.Y != prevMouse.Y && gameStart)
            {
                if ((Mouse.GetState().X > 0 && Mouse.GetState().X < WIDTH)//Is the mouse in the display?
                    && Mouse.GetState().Y > 0 && Mouse.GetState().Y < HEIGHT)
                {
                    int xPos = (int)(((Mouse.GetState().X)) / (pixelSprite.Width * pixelScale));//Converts mouse position to an
                    int yPos = (int)(((Mouse.GetState().Y)) / (pixelSprite.Height * pixelScale));//integer within the array

                    pixelArray[xPos, yPos] = 1;//One represents black sprites
                }
            }
            else if (IsActive && nowMouse.LeftButton == ButtonState.Pressed && !draw &&
                    prevMouse.LeftButton == ButtonState.Released && setStart && gameStart)
            {
                if ((Mouse.GetState().X > 0 && Mouse.GetState().X < WIDTH)
                    && Mouse.GetState().Y > 0 && Mouse.GetState().Y < HEIGHT)
                {
                    int xPos = (int)(((Mouse.GetState().X)) / (pixelSprite.Width * pixelScale));
                    int yPos = (int)(((Mouse.GetState().Y)) / (pixelSprite.Height * pixelScale));

                    pixelArray[xPos, yPos] = 2;//Two represents pink sprites
                    
                    p1 = new Point(xPos, yPos);
                    if (queue.Count == 0)
                    {
                        queue.Enqueue(p1);//Adds both x and y coordinates to a Queue
                        pQ.Enqueue(p1, queuePos);
                        queuePos--;
                    }
                    else if (queue.Count == 1)
                    {
                        queue.Enqueue(p1);
                        pQ.Enqueue(p1, 1);
                        queuePos--;
                    }
                    else// if (queue.Count > 2)//If there are more than two points, pop and push
                    {
                        queue.Enqueue(p1);
                        pQ.Enqueue(p1, queuePos);
                        p1 = queue.Dequeue();
                        pQ.Dequeue();
                        xPos = p1.X;
                        yPos = p1.Y;
                        pixelArray[xPos, yPos] = 1;//Changes oldest start/end sprite to black
                        queuePos--;
                    }
                }
            }
            prevMouse = nowMouse;//Sets the current mouse state to the previous one
        }
    }

    public class PriorityQueue
    {
        int size;
        SortedDictionary<double, Queue<Point>> pQ;

        public PriorityQueue()
        {
            this.pQ = new SortedDictionary<double, Queue<Point>>();
            this.size = 0;
        }

        public bool IsEmpty()
        {
            return (size == 0);
        }

        public Point Dequeue()
        {
            if (!IsEmpty())
                foreach (Queue<Point> q in pQ.Values)
                {
                    if (q.Count > 0)
                    {
                        size--;
                        return q.Dequeue();
                    }
                }

            return Point.Zero;
        }

        public object Dequeue(int priority)
        {
            size--;
            return pQ[priority].Dequeue();
        }

        public void Enqueue(Point item, double priority)//Double because heuristic is a double
        {
            if (!pQ.ContainsKey(priority))
            {
                pQ.Add(priority, new Queue<Point>());
                Enqueue(item, priority);
            }

            else
            {
                pQ[priority].Enqueue(item);
                size++;
            }
        }
    }
}
