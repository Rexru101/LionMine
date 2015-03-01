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

namespace Prototype2
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        //check for else if 3
        //check if hex is valid (not in a safe zone)

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;
        Texture2D hexBoard, square, larg, goltana;
        MouseState currentMouse, prevMouse;
        
        const int HEIGHT = 790;
        const int WIDTH = 750;
        bool newGame = false;
        bool player1 = true, player2;
        bool setup = true;
        bool destinationIsSelected;
        bool gameOver;
        String winner;
        int player1Mines = 9, player2Mines = 9;//9
        int p1ActionPoints = 5, p2ActionPoints = 0;
        int explosionTime = 2000, detectTime = 3000, defusionTime = 5000;
        int numOfStuff, flagDistance;
        
        Point[] p1Stuff = new Point[9], p2Stuff = new Point[9];
        Point player1Indices = new Point(0, 2), player2Indices = new Point(20, 2);
        Point playerDestination = Point.Zero;

        int[,] boardState = new int[21, 5];
        Rectangle[,] rectArr = new Rectangle[21, 5];

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            graphics.PreferredBackBufferHeight = HEIGHT;
            graphics.PreferredBackBufferWidth = WIDTH;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            hexBoard = Content.Load<Texture2D>(@"Sprites/hexBoard");
            larg = Content.Load<Texture2D>(@"Sprites/LargFlag");
            goltana = Content.Load<Texture2D>(@"Sprites/GoltanaFlag");
            square = Content.Load<Texture2D>(@"Sprites/pixel");

            spriteFont = Content.Load<SpriteFont>(@"Fonts/SpriteFont1");
            this.IsMouseVisible = true;

            for (int i = 0; i < 21; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (i % 2 == 0)
                        rectArr[i, j] = new Rectangle(18 + (int)((square.Width + 23.7) * j),
                            13 + (int)((square.Height / 2 + 21.5) * (i / 2)), square.Width / 2, square.Height / 2);
                    else
                    {
                        if (j > 3)
                            continue;
                        rectArr[i, j] = new Rectangle((square.Width / 2 + 30) + (int)((square.Width + 23.7) * j),
                            (int)(square.Height / 2) + (int)(square.Height / 2 + 21.9999999) * ((i - 1) / 2),
                            (int)(square.Width * 0.5f), (int)(square.Height * 0.5f));
                    }
                }
            }
        }

        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected override void Update(GameTime gameTime)
        {
            explosionTime += gameTime.ElapsedGameTime.Milliseconds;
            detectTime += gameTime.ElapsedGameTime.Milliseconds;
            defusionTime += gameTime.ElapsedGameTime.Milliseconds;
            prevMouse = currentMouse;
            currentMouse = Mouse.GetState();
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                this.Exit();
            if (IsActive && currentMouse.LeftButton == ButtonState.Released
                && prevMouse.LeftButton == ButtonState.Pressed)
            {
                checkClick();
                
                if (setup && isSetupValid())
                {
                    if (player1 && rectArr[player1Indices.X, player1Indices.Y].Contains(new Point(currentMouse.X, currentMouse.Y)))
                    {
                        player1 = false;
                        player2 = true;
                    }
                    else if (player2 && (rectArr[player2Indices.X, player2Indices.Y].Contains(new Point(currentMouse.X, currentMouse.Y))))
                    {
                        player1 = true;
                        player2 = false;
                        setup = false;
                    }
                }
            }

            if (IsActive && currentMouse.RightButton == ButtonState.Released
                && prevMouse.RightButton == ButtonState.Pressed && !setup)
            {
                if (destinationIsSelected)
                    defuse();
                else
                    detect();
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            spriteBatch.Begin();

            spriteBatch.Draw(hexBoard, Vector2.Zero, Color.White);
            spriteBatch.Draw(goltana, new Vector2(rectArr[player1Indices.X, player1Indices.Y].X,
                rectArr[player1Indices.X, player1Indices.Y].Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            spriteBatch.Draw(larg, new Vector2(rectArr[player2Indices.X, player2Indices.Y].X + 2,
                rectArr[player2Indices.X, player2Indices.Y].Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            
            if (newGame)
                drawNewGame();
            spriteBatch.DrawString(spriteFont, "" + player1Mines, new Vector2(WIDTH - 75, HEIGHT / 2 - 20), Color.Red);
            spriteBatch.DrawString(spriteFont, "" + player2Mines, new Vector2(WIDTH - 75, HEIGHT / 2 + 20), Color.Blue);
            spriteBatch.DrawString(spriteFont, "" + p1ActionPoints, new Vector2(WIDTH - 115, HEIGHT / 2 - 20), Color.Red);
            spriteBatch.DrawString(spriteFont, "" + p2ActionPoints, new Vector2(WIDTH - 115, HEIGHT / 2 + 20), Color.Blue);
            
            if (gameOver)
                spriteBatch.DrawString(spriteFont, winner, new Vector2(WIDTH - 150, 40), Color.Green);
            if (explosionTime < 2000)
                spriteBatch.DrawString(spriteFont, "KABOOM!", new Vector2(WIDTH - 150, 40), Color.Green);
            if (detectTime < 3000)
                spriteBatch.DrawString(spriteFont, "" + numOfStuff, new Vector2(WIDTH - 150, 40), Color.Green);
            if (defusionTime < 5000)
                spriteBatch.DrawString(spriteFont, "" + flagDistance, new Vector2(WIDTH - 150, 40), Color.Green);
            drawSquare();
            
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public void drawNewGame()
        {
            for (int i = 0; i < 21; i++)//21
            {
                for (int j = 0; j < 5; j++)//5
                {
                    if (i % 2 == 1 && j == 4)
                        continue;
                    spriteBatch.Draw(square, new Vector2(rectArr[i, j].X, rectArr[i, j].Y), rectArr[i, j], Color.Black,
                        0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
                }
            }
        }

        public void drawSquare()
        {
            for (int i = 0; i < 21; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if ((boardState[i, j] == 1 || boardState[i, j] == 3)
                        && player1 && setup)
                        spriteBatch.Draw(square, new Vector2(rectArr[i, j].X, rectArr[i, j].Y), rectArr[i, j], Color.Black,
                            0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
                    else if ((boardState[i, j] == 2 || boardState[i, j] == 3)
                        && player2 && setup)
                        spriteBatch.Draw(square, new Vector2(rectArr[i, j].X, rectArr[i, j].Y), rectArr[i, j], Color.Black,
                            0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
                    else if (!setup && boardState[i, j] > 3 && boardState[i, j] < 8)
                    {
                        spriteBatch.Draw(square, new Vector2(rectArr[i, j].X, rectArr[i, j].Y), rectArr[i, j], Color.Black,
                            0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
                    }
                }
            }
        }

        public void checkClick()
        {
            for (int i = 0; i < 21; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (rectArr[i, j].Contains(new Point(currentMouse.X, currentMouse.Y)))
                    {
                        if (setup && player1)
                        {
                            if (boardState[i, j] == 1)
                            {
                                boardState[i, j] = 0;
                                player1Mines++;
                            }
                            else if (boardState[i, j] == 0 && player1Mines > 0)
                            {
                                if (player1Mines == 9)
                                {
                                    p1Stuff[0] = new Point(i, j);
                                }
                                else
                                {
                                    p1Stuff[player1Mines] = new Point(i, j);
                                }
                                boardState[i, j] = 1;
                                player1Mines--;
                            }
                            else if (boardState[i, j] == 2 && player1Mines > 0)
                            {
                                boardState[i, j] = 3;
                                player1Mines--;
                            }
                        }
                        else if (setup && player2)
                        {
                            if (boardState[i, j] == 2)
                            {
                                boardState[i, j] = 0;
                                player2Mines++;
                            }
                            else if (boardState[i, j] == 0 && player2Mines > 0)
                            {
                                if (player2Mines == 9)
                                {
                                    p2Stuff[0] = new Point(i, j);
                                }
                                else
                                {
                                    p2Stuff[player2Mines] = new Point(i, j);
                                }
                                boardState[i, j] = 2;
                                player2Mines--;
                            }
                            else if (boardState[i, j] == 1 && player2Mines > 0)
                            {
                                if (player2Mines == 9)
                                {
                                    p2Stuff[0] = new Point(i, j);
                                }
                                else
                                {
                                    p2Stuff[player2Mines] = new Point(i, j);
                                }
                                boardState[i, j] = 3;
                                player2Mines--;
                            }
                        }
                        else if (!setup)
                        {
                            if (isMovementValid(new Point(i, j)))
                            {
                                if (destinationIsSelected && boardState[i, j] > 3 && boardState[i, j] < 8)
                                {
                                    boardState[i, j] -= 4;
                                    destinationIsSelected = false;
                                    playerDestination.X = i;
                                    playerDestination.Y = j;
                                }
                                else if (!destinationIsSelected)
                                {
                                    boardState[i, j] += 4;
                                    destinationIsSelected = true;
                                    playerDestination.X = i;
                                    playerDestination.Y = j;
                                }
                            }
                            else if (destinationIsSelected)
                            {
                                if (player1 && player1Indices.X == i && player1Indices.Y == j)
                                {
                                    movePlayer(playerDestination.X, playerDestination.Y);
                                    boardState[playerDestination.X, playerDestination.Y] -= 4;
                                    playerDestination = new Point(-1, -1);
                                    destinationIsSelected = false;
                                }
                                else if (player2 && player2Indices.X == i && player2Indices.Y == j)
                                {
                                    movePlayer(playerDestination.X, playerDestination.Y);
                                    boardState[playerDestination.X, playerDestination.Y] -= 4;
                                    playerDestination = new Point(-1, -1);
                                    destinationIsSelected = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        public bool isSetupValid()
        {
            if (player1)
            {
                if (player1Mines == 0)
                    return true;
                else
                    return false;
            }
            else
            {
                if (player2Mines == 0)
                {
                    return true;
                }
                else
                    return false;
            }
        }

        public bool isMovementValid(Point p)
        {
            if (player1)
            {
                if (player1Indices.X % 2 == 0)
                {
                    if (p.Y - player1Indices.Y == 0)
                    {
                        if (Math.Abs(p.X - player1Indices.X) < 3
                            && p.X - player1Indices.X != 0)
                            return true;
                    }
                    else if (p.Y - player1Indices.Y == -1)
                    {
                        if (Math.Abs(p.X - player1Indices.X) < 2
                            && p.X - player1Indices.X != 0)
                            return true;
                    }
                }
                else if (player1Indices.X % 2 == 1)
                {
                    if (p.Y - player1Indices.Y == 0)
                    {
                        if (Math.Abs(p.X - player1Indices.X) < 3
                            && p.X - player1Indices.X != 0)
                            return true;
                    }
                    else if (p.Y - player1Indices.Y == 1)
                    {
                        if (Math.Abs(p.X - player1Indices.X) < 2
                            && p.X - player1Indices.X != 0)
                            return true;
                    }
                }
            }
            else if (player2)
            {
                if (player2Indices.X % 2 == 0)
                {
                    if (p.Y - player2Indices.Y == 0)
                    {
                        if (Math.Abs(p.X - player2Indices.X) < 3
                            && p.X - player2Indices.X != 0)
                            return true;
                    }
                    else if (p.Y - player2Indices.Y == -1)
                    {
                        if (Math.Abs(p.X - player2Indices.X) < 2
                            && p.X - player2Indices.X != 0)
                            return true;
                    }
                }
                else if (player2Indices.X % 2 == 1)
                {
                    if (p.Y - player2Indices.Y == 0)
                    {
                        if (Math.Abs(p.X - player2Indices.X) < 3
                            && p.X - player2Indices.X != 0)
                            return true;
                    }
                    else if (p.Y - player2Indices.Y == 1)
                    {
                        if (Math.Abs(p.X - player2Indices.X) < 2
                            && p.X - player2Indices.X != 0)
                            return true;
                    }
                }
            }
            return false;
        }

        public void movePlayer(int row, int col)
        {
            if (player1)
            {
                player1Indices.X = row;
                player1Indices.Y = col;
                p1ActionPoints--;
            }
            else if (player2)
            {
                player2Indices.X = row;
                player2Indices.Y = col;
                p2ActionPoints--;
            }
            checkAndEndTurn();
            checkForContact();
        }

        public void checkAndEndTurn()
        {
            if (p1ActionPoints == 0 && player1)
            {
                p2ActionPoints = 5;
                player2 = true;
                player1 = false;
            }
            else if (p2ActionPoints == 0 && player2)
            {
                p1ActionPoints = 5;
                player1 = true;
                player2 = false;
            }
        }

        public void endTurn()
        {
            if (player1)
            {
                player1 = false;
                player2 = true;
                p1ActionPoints = 0;
                p2ActionPoints = 5;
            }
            else if (player2)
            {
                player2 = false;
                player1 = true;
                p2ActionPoints = 0;
                p1ActionPoints = 5;
            }
        }

        public void checkForContact()
        {
            for (int i = 0; i < p1Stuff.Length; i++)
            {
                if (p2Stuff[i].X == player1Indices.X && p2Stuff[i].Y == player1Indices.Y)
                {
                    if (i == 0)
                    {
                        gameOver = true;
                        winner = "Player 1 \nWins";
                    }
                    else
                    {
                        explosionTime = 0;
                        player1Indices.X = 0;
                        player1Indices.Y = 2;
                        p2Stuff[i].X = -1;
                        p2Stuff[i].Y = -1;
                        endTurn();
                    }
                }
                else if (p1Stuff[i].X == player2Indices.X && p1Stuff[i].Y == player2Indices.Y)
                {
                    if (i == 0)
                    {
                        gameOver = true;
                        winner = "Player 2 \nWins";
                    }
                    else
                    {
                        explosionTime = 0;
                        player2Indices.X = 20;
                        player2Indices.Y = 2;
                        p1Stuff[i].X = -1;
                        p1Stuff[i].Y = -1;
                        endTurn();
                    }
                }
            }
        }

        public void detect()
        {
            if (player1 && p1ActionPoints > 1)
            {
                p1ActionPoints -= 2;
                numOfStuff = 0;
                for (int i = 0; i < p2Stuff.Length; i++)
                    if (isMovementValid(p2Stuff[i]))
                        numOfStuff++;
                if (numOfStuff > 0)
                    detectTime = 0;
                if (p1ActionPoints == 0)
                    endTurn();
            }
            else if (player2 && p2ActionPoints > 1)
            {
                p2ActionPoints -= 2;
                numOfStuff = 0;
                for (int i = 0; i < p1Stuff.Length; i++)
                    if (isMovementValid(p1Stuff[i]))
                        numOfStuff++;
                if (numOfStuff > 0)
                    detectTime = 0;
                if (p2ActionPoints == 0)
                    endTurn();
            }
        }

        public void defuse()
        {
            if (player1 && p1ActionPoints > 2)
            {
                p1ActionPoints -= 3;
                for (int i = 1; i < p2Stuff.Length; i++)
                    if (p2Stuff[i].X == playerDestination.X && p2Stuff[i].Y == playerDestination.Y)
                    {
                        p2Stuff[i] = new Point(-1, -1);
                        defusionTime = 0;
                        calculateFlagDistance();
                    }

                destinationIsSelected = false;
                boardState[playerDestination.X, playerDestination.Y] -= 4;
                playerDestination = new Point(-1, -1);

                if (p1ActionPoints == 0)
                    endTurn();
            }
            else if (player2 && p2ActionPoints > 2)
            {
                p2ActionPoints -= 3;
                for (int i = 1; i < p1Stuff.Length; i++)
                    if (p1Stuff[i].X == playerDestination.X && p1Stuff[i].Y == playerDestination.Y)
                    {
                        p1Stuff[i] = new Point(-1, -1);
                        defusionTime = 0;
                        calculateFlagDistance();
                    }

                destinationIsSelected = false;
                boardState[playerDestination.X, playerDestination.Y] -= 4;
                playerDestination = new Point(-1, -1);

                if (p2ActionPoints == 0)
                    endTurn();
            }
        }

        public void calculateFlagDistance()
        {
            if (player1)
            {
                int dx = Math.Abs(player1Indices.X - p2Stuff[0].X);
                int dy = Math.Abs(player1Indices.Y - p2Stuff[0].Y);

                if (isMovementValid(p2Stuff[0]))
                    flagDistance = 1;
                else if (dy < 3 && dx < 3)
                    flagDistance = 2;
                else if (dy < 3)
                    flagDistance = dx * 4 / 3;
                else if (dx < 3)
                    flagDistance = dy / 2 + 1;
                else
                    flagDistance = dx * 2 - dy + 1;
            }
            else if (player2)
            {
                int dx = Math.Abs(player2Indices.X - p1Stuff[0].X);
                int dy = Math.Abs(player2Indices.Y - p1Stuff[0].Y);

                if (isMovementValid(p1Stuff[0]))
                    flagDistance = 1;
                else if (dy < 3 && dx < 3)
                    flagDistance = 2;
                else if (dy < 3)
                    flagDistance = dx * 4 / 3;
                else if (dx < 3)
                    flagDistance = dy / 2 + 1;
                else
                    flagDistance = dx * 2 - dy + 1;
            }
        }
    }
}