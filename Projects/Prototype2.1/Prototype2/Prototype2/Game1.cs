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
using System.Collections;

namespace Prototype2
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        //check if hex is valid (cannot move on opponent's square)
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont spriteFont;
        Texture2D hexBoard, square, larg, goltana, redFlag, blueFlag;
        MouseState currentMouse, prevMouse;
        Color p1PathColor, p2PathColor, flagAreaColor1, flagAreaColor2, flagAreaColor3;

        Player p1, p2;

        const int HEIGHT = 790;
        const int WIDTH = 750;
        bool newGame = false;
        bool setup = true;
        bool destinationIsSelected;
        bool gameOver;
        String winner;
        int explosionTime = 2000, detectTime = 3000, defusionTime = 5000;
        int numOfStuff, flagDistance;
        int bitValueP1Mine = 1, bitValueP2Mine = 2, bitValueP1Flag = 4, bitValueP2Flag = 8;

        Point playerDestination = Point.Zero;
        Queue<Point> queue = new Queue<Point>();
        Queue<Point> discovered = new Queue<Point>();
        Queue<Point> frontier = new Queue<Point>();

        int[,] xyBoard = new int[11, 9];
        Rectangle[,] xyRect = new Rectangle[11, 9];
        
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
            redFlag = Content.Load<Texture2D>(@"Sprites/redFlag");
            blueFlag = Content.Load<Texture2D>(@"Sprites/blueFlag");
            spriteFont = Content.Load<SpriteFont>(@"Fonts/SpriteFont1");

            this.IsMouseVisible = true;

            p1 = new Player(larg, redFlag, new Point(0, 4), bitValueP1Mine, bitValueP1Flag);
            p2 = new Player(goltana, blueFlag, new Point(10, 4), bitValueP2Mine, bitValueP2Flag);
            p1.resetActionPoints();

            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (j % 2 == 0)
                    {
                        xyRect[i, j] = new Rectangle(18 + (int)((square.Width + 23.7) * (j/2)),
                            13 + (int)((square.Height / 2 + 21.5) * (i)), square.Width / 2, square.Height / 2);
                    }
                    else
                    {
                        if (i == 10)
                            continue;
                        xyRect[i, j] = new Rectangle((square.Width / 2 + 30) + (int)((square.Width + 23.7) * (j/2)),
                            (int)(square.Height / 2) + (int)(square.Height / 2 + 21.9999999) * ((i)),
                            (int)(square.Width * 0.5f), (int)(square.Height * 0.5f));
                    }
                }
            }

            p1PathColor = Color.Red;
            p1PathColor.A = 128;
            p2PathColor = Color.Blue;
            p2PathColor.A = 128;
            flagAreaColor1 = Color.Purple;
            flagAreaColor1.A = 32;
            flagAreaColor2 = Color.Purple;
            flagAreaColor2.A = 64;
            flagAreaColor3 = Color.Purple;
            flagAreaColor3.A = 128;
        }

        public void drawNewGame()
        {
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (i == 10 && j % 2 == 1)
                        continue;
                    spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), xyRect[i, j], Color.Black,
                        0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
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
                if (Player.Player1Turn)
                    checkClick(p1);
                else
                    checkClick(p2);
                
                if (setup && isSetupValid())
                {
                    if (Player.Player1Turn && xyRect[p1.getSpawn().X, p1.getSpawn().Y].Contains
                        (new Point(currentMouse.X, currentMouse.Y)))
                    {
                        Player.Player1Turn = false;
                    }
                    else if (!Player.Player1Turn && xyRect[p2.getSpawn().X, p2.getSpawn().Y].Contains
                        (new Point(currentMouse.X, currentMouse.Y)))
                    {
                        Player.Player1Turn = true;
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

            if (newGame)
            {
                drawNewGame();
                spriteBatch.End();
                return;
            }

            spriteBatch.DrawString(spriteFont, "" + p1.getMines(), new Vector2(WIDTH - 75, HEIGHT / 2 - 20), Color.Red);
            spriteBatch.DrawString(spriteFont, "" + p2.getMines(), new Vector2(WIDTH - 75, HEIGHT / 2 + 20), Color.Blue);
            spriteBatch.DrawString(spriteFont, "" + p1.getActionPoints(), new Vector2(WIDTH - 115, HEIGHT / 2 - 20), Color.Red);
            spriteBatch.DrawString(spriteFont, "" + p2.getActionPoints(), new Vector2(WIDTH - 115, HEIGHT / 2 + 20), Color.Blue);
            
            if (gameOver)
                spriteBatch.DrawString(spriteFont, winner, new Vector2(WIDTH - 150, 40), Color.Green);
            if (explosionTime < 2000)
                spriteBatch.DrawString(spriteFont, "KABOOM!", new Vector2(WIDTH - 150, 40), Color.Green);
            if (detectTime < 3000)
                spriteBatch.DrawString(spriteFont, "" + numOfStuff, new Vector2(WIDTH - 150, 40), Color.Green);
            if (defusionTime < 5000)
                spriteBatch.DrawString(spriteFont, "" + flagDistance, new Vector2(WIDTH - 150, 40), Color.Green);
            drawSquare();
            
            spriteBatch.Draw(goltana, new Vector2(xyRect[p1.getPosition().X, p1.getPosition().Y].X,
                xyRect[p1.getPosition().X, p1.getPosition().Y].Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            spriteBatch.Draw(larg, new Vector2(xyRect[p2.getPosition().X, p2.getPosition().Y].X,
                xyRect[p2.getPosition().X, p2.getPosition().Y].Y), null, Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0);
            
            spriteBatch.End();
            base.Draw(gameTime);
        }

        public void drawSquare()
        {
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (setup && Player.Player1Turn && (xyBoard[i, j] == 1))
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, Color.Black,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
                    else if (setup && Player.Player1Turn && (xyBoard[i, j] == 4))
                        spriteBatch.Draw(redFlag, new Vector2(xyRect[i, j].X + 2, xyRect[i, j].Y), null, Color.White, 0f,
                            Vector2.Zero, 1f, SpriteEffects.None, 0);
                    else if (setup && !Player.Player1Turn && (xyBoard[i, j] == 2 || xyBoard[i, j] == 3 || xyBoard[i, j] == 6))
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, Color.Black,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 1);
                    else if (setup && !Player.Player1Turn && (xyBoard[i, j] == 8 || xyBoard[i, j] == 9 || xyBoard[i, j] == 12))
                        spriteBatch.Draw(blueFlag, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, Color.White, 0f,
                            Vector2.Zero, 1f, SpriteEffects.None, 0);
                    else if (!setup && xyBoard[i, j] > 15 && xyBoard[i, j] < 32)
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), xyRect[i, j], Color.Black,
                            0f, Vector2.Zero, 1f, SpriteEffects.None, 1);

                    if (Player.Player1Turn && !setup && p1.getFlagArea()[i, j] == 1)
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, flagAreaColor1,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }
                    else if (Player.Player1Turn && !setup && p1.getFlagArea()[i, j] == 2)
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, flagAreaColor2,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }
                    else if (Player.Player1Turn && !setup && p1.getFlagArea()[i, j] > 2)
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, flagAreaColor3,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }
                    else if (!Player.Player1Turn && !setup && p2.getFlagArea()[i, j] == 1)
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, flagAreaColor1,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }
                    else if (!Player.Player1Turn && !setup && p2.getFlagArea()[i, j] == 2)
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, flagAreaColor2,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }
                    else if (!Player.Player1Turn && !setup && p2.getFlagArea()[i, j] > 2)
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, flagAreaColor3,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }

                    if (!setup && Player.Player1Turn && p1.getPath()[i, j])
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, p1PathColor,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }
                    else if (!setup && !Player.Player1Turn && p2.getPath()[i, j])
                    {
                        spriteBatch.Draw(square, new Vector2(xyRect[i, j].X, xyRect[i, j].Y), null, p2PathColor,
                            0f, Vector2.Zero, 0.5f, SpriteEffects.None, 0.1f);
                    }
                }
            }
        }

        public void checkClick(Player p)
        {
            for (int i = 0; i < 11; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    if (xyRect[i, j].Contains(new Point(currentMouse.X, currentMouse.Y)))
                    {
                        if (setup)
                        {
                            if (((i < 2 && j > 1 && j < 7) || (i == 2 && j == 4)
                                    || (i > 8 && j > 1 && j < 7) || (i == 8 && (j > 2 && j < 6)))
                                && (p.getMines() > 0))
                            {
                                continue;
                            }
                            if (Player.Player1Turn)
                            {
                                if (p.getFlagIsSet())
                                {
                                    if (xyBoard[i, j] == 0 && p.getMines() > 0)
                                    {
                                        xyBoard[i, j] = p.getBitM();
                                        p.appendInventory(new Point(i, j));
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == p.getBitM())
                                    {
                                        xyBoard[i, j] = 0;
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                    else if (xyBoard[i, j] == p.getBitF())
                                    {
                                        xyBoard[i, j] = 0;
                                        p.changeFlagIsSet();
                                        p.incMine();
                                        p.removeFlagFromInventory();
                                    }
                                }
                                else if (!p.getFlagIsSet())
                                {
                                    if (xyBoard[i, j] == 0)
                                    {
                                        xyBoard[i, j] = p.getBitF();
                                        p.appendFlag(new Point(i, j));
                                        p.changeFlagIsSet();
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == p.getBitM())
                                    {
                                        xyBoard[i, j] = 0;
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                }
                            }
                            else if (!Player.Player1Turn)
                            {
                                if (p.getFlagIsSet())
                                {
                                    if (xyBoard[i, j] == 0 && p.getMines() > 0)
                                    {
                                        xyBoard[i, j] = p.getBitM();
                                        p.appendInventory(new Point(i, j));
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == 1)
                                    {
                                        xyBoard[i, j] += p.getBitM();
                                        p.appendInventory(new Point(i, j));
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == p.getBitM())
                                    {
                                        xyBoard[i, j] = 0;
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                    else if (xyBoard[i, j] == 3)
                                    {
                                        xyBoard[i, j] -= p.getBitM();
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                    else if (xyBoard[i, j] == 4)
                                    {
                                        xyBoard[i, j] += p.getBitM();
                                        p.appendInventory(new Point(i, j));
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == 6)
                                    {
                                        xyBoard[i, j] -= p.getBitM();
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                    else if (xyBoard[i, j] == p.getBitF())
                                    {
                                        xyBoard[i, j] = 0;
                                        p.changeFlagIsSet();
                                        p.incMine();
                                        p.removeFlagFromInventory();
                                    }
                                    else if (xyBoard[i, j] == 9)
                                    {
                                        xyBoard[i, j] -= p.getBitF();
                                        p.changeFlagIsSet();
                                        p.incMine();
                                        p.removeFlagFromInventory();
                                    }
                                    else if (xyBoard[i, j] == 12)
                                    {
                                        xyBoard[i, j] -= p.getBitF();
                                        p.changeFlagIsSet();
                                        p.incMine();
                                        p.removeFlagFromInventory();
                                    }
                                }
                                else if (!p.getFlagIsSet())
                                {
                                    if (xyBoard[i, j] == 0)
                                    {
                                        xyBoard[i, j] = p.getBitF();
                                        p.appendFlag(new Point(i, j));
                                        p.changeFlagIsSet();
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == 1)
                                    {
                                        xyBoard[i, j] += p.getBitF();
                                        p.appendFlag(new Point(i, j));
                                        p.changeFlagIsSet();
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == p.getBitM())
                                    {
                                        xyBoard[i, j] = 0;
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                    else if (xyBoard[i, j] == 3)
                                    {
                                        xyBoard[i, j] -= p.getBitM();
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                    else if (xyBoard[i, j] == 4)
                                    {
                                        xyBoard[i, j] += p.getBitF();
                                        p.appendFlag(new Point(i, j));
                                        p.changeFlagIsSet();
                                        p.decMine();
                                    }
                                    else if (xyBoard[i, j] == 6)
                                    {
                                        xyBoard[i, j] -= p.getBitM();
                                        p.removeInventory();
                                        p.incMine();
                                    }
                                }
                            }
                        }
                        else if (!setup)
                        {
                            if (isMovementValid(p.getPosition(), i, j))
                            {
                                if (destinationIsSelected && xyBoard[i, j] > 15)
                                {
                                    xyBoard[i, j] -= 16;
                                    destinationIsSelected = false;
                                    playerDestination.X = i;
                                    playerDestination.Y = j;
                                }
                                else if (!destinationIsSelected)
                                {
                                    xyBoard[i, j] += 16;
                                    destinationIsSelected = true;
                                    playerDestination.X = i;
                                    playerDestination.Y = j;
                                }
                            }
                            else if (destinationIsSelected)
                            {
                                if (p.getPosition().X == i && p.getPosition().Y == j)
                                {
                                    xyBoard[playerDestination.X, playerDestination.Y] -= 16;
                                    movePlayer(p, playerDestination.X, playerDestination.Y);
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
            if (Player.Player1Turn)
            {
                if (p1.getMines() == 0)
                    return true;
                else
                    return false;
            }
            else
            {
                if (p2.getMines() == 0)
                {
                    return true;
                }
                else
                    return false;
            }
        }

        public bool isMovementValid(Point p, int x, int y)
        {
            if (p.Y % 2 == 0)
            {
                if (Math.Abs(p.Y - y) == 0)
                {
                    if (Math.Abs(p.X - x) == 1)
                    {
                        return true;
                    }
                }
                else if (Math.Abs(p.X - x) == 0)
                {
                    if (Math.Abs(p.Y - y) == 1)
                    {
                        return true;
                    }
                }
                else if (p.X - x == 1)
                {
                    if (Math.Abs(p.Y - y) == 1)
                    {
                        return true;
                    }
                }
            }
            else if (p.Y % 2 == 1)
            {
                if (Math.Abs(p.Y - y) == 0)
                {
                    if (Math.Abs(p.X - x) == 1)
                    {
                        return true;
                    }
                }
                else if (Math.Abs(p.X - x) == 0)
                {
                    if (Math.Abs(p.Y - y) == 1)
                    {
                        return true;
                    }
                }
                else if (p.X - x == -1)
                {
                    if (Math.Abs(p.Y - y) == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void movePlayer(Player p, int row, int col)
        {
            p.setPosition(row, col);
            p.subActionPoints(1);
            p.move(row, col);

            checkAndEndTurn();
            checkForContact();
        }

        public void checkAndEndTurn()
        {
            if (Player.Player1Turn && p1.getActionPoints() == 0)
            {
                p2.resetActionPoints();
                Player.Player1Turn = !Player.Player1Turn;
            }
            else if (!Player.Player1Turn && p2.getActionPoints() == 0)
            {
                p1.resetActionPoints();
                Player.Player1Turn = !Player.Player1Turn;
            }
        }

        public void endTurn()
        {
            if (Player.Player1Turn)
            {
                p1.subActionPoints(p1.getActionPoints());
                p2.resetActionPoints();
            }
            else if (!Player.Player1Turn)
            {
                p2.subActionPoints(p2.getActionPoints());
                p1.resetActionPoints();
            }
            Player.Player1Turn = !Player.Player1Turn;
        }

        public void checkForContact()
        {
            for (int i = 0; i < p1.getInventory().Length; i++)
            {
                if (p2.getInventory()[i].X == p1.getPosition().X && p2.getInventory()[i].Y == p1.getPosition().Y)
                {
                    if (i == 0)
                    {
                        gameOver = true;
                        winner = "Player 1 \nWins";
                    }
                    else
                    {
                        explosionTime = 0;
                        xyBoard[p1.getPosition().X, p1.getPosition().Y] -= p2.getBitM();
                        p1.setPosition(p1.getSpawn());
                        p2.getInventory()[i].X = -1;
                        p2.getInventory()[i].Y = -1;
                        endTurn();
                    }
                }
                else if (p1.getInventory()[i].X == p2.getPosition().X && p1.getInventory()[i].Y == p2.getPosition().Y)
                {
                    if (i == 0)
                    {
                        gameOver = true;
                        winner = "Player 2 \nWins";
                    }
                    else
                    {
                        explosionTime = 0;
                        xyBoard[p2.getPosition().X, p2.getPosition().Y] -= p1.getBitM();
                        p2.setPosition(p2.getSpawn());
                        p1.getInventory()[i].X = -1;
                        p1.getInventory()[i].Y = -1;
                        endTurn();
                    }
                }
            }
        }

        public void detect()
        {
            if (Player.Player1Turn && p1.getActionPoints() > 1)
            {
                p1.subActionPoints(2);
                numOfStuff = 0;
                for (int i = 0; i < p2.getInventory().Length; i++)
                    if (isMovementValid(p1.getPosition(), p2.getInventory()[i].X, p2.getInventory()[i].Y))
                        numOfStuff++;
                //if (numOfStuff > 0)
                    //detectTime = 0;
                if (p1.getActionPoints() == 0)
                    endTurn();
            }
            else if (!Player.Player1Turn && p2.getActionPoints() > 1)
            {
                p2.subActionPoints(2);
                numOfStuff = 0;
                for (int i = 0; i < p1.getInventory().Length; i++)
                    if (isMovementValid(p2.getPosition(), p1.getInventory()[i].X, p1.getInventory()[i].Y))
                        numOfStuff++;
                //if (numOfStuff > 0)
                    detectTime = 0;
                if (p2.getActionPoints() == 0)
                    endTurn();
            }
        }

        public void defuse()
        {
            flagDistance = 0;
            if (Player.Player1Turn && p1.getActionPoints() > 2)
            {
                p1.subActionPoints(3);
                for (int i = 1; i < p2.getInventory().Length; i++)
                    if (p2.getInventory()[i].X == playerDestination.X && p2.getInventory()[i].Y == playerDestination.Y)
                    {
                        calculateFlagDistance(p1, playerDestination, p2.getInventory()[0]);
                        p2.getInventory()[i] = new Point(-1, -1);
                    }

                destinationIsSelected = false;
                xyBoard[playerDestination.X, playerDestination.Y] -= 16;
                playerDestination = new Point(-1, -1);

                if (p1.getActionPoints() == 0)
                    endTurn();
            }
            else if (!Player.Player1Turn && p2.getActionPoints() > 2)
            {
                p2.subActionPoints(3);
                for (int i = 1; i < p1.getInventory().Length; i++)
                    if (p1.getInventory()[i].X == playerDestination.X && p2.getInventory()[i].Y == playerDestination.Y)
                    {
                        calculateFlagDistance(p2, playerDestination, p1.getInventory()[0]);
                        p1.getInventory()[i] = new Point(-1, -1);
                    }

                destinationIsSelected = false;
                xyBoard[playerDestination.X, playerDestination.Y] -= 16;
                playerDestination = new Point(-1, -1);

                if (p2.getActionPoints() == 0)
                    endTurn();
            }
            defusionTime = 0;
        }

        public void calculateFlagDistance(Player p, Point start, Point goal)
        {
            Point temp;
            flagDistance = 0;
            queue.Clear();
            discovered.Clear();
            frontier.Clear();

            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                temp = queue.Dequeue();
                discovered.Enqueue(temp);
                p.fillFlagArea(temp.X, temp.Y);

                //if (isMovementValid(temp, goal.X, goal.Y))
                if (temp.Equals(goal))
                {
                    //flagDistance++;
                    break;
                }
                else
                {
                    findNeighbors(temp);
                }
                if (queue.Count == 0)
                {
                    flagDistance++;
                    if (frontier.Contains(goal))
                    {
                        //flagDistance++;
                        while (frontier.Count != 0)
                        {
                            Point frontierTemp = frontier.Dequeue();
                            //queue.Enqueue(frontierTemp);
                            p.fillFlagArea(frontierTemp.X, frontierTemp.Y);
                        }
                        break;
                    }
                    while (frontier.Count != 0)
                    {
                        Point frontierTemp = frontier.Dequeue();
                        queue.Enqueue(frontierTemp);
                        //p.fillFlagArea(frontierTemp.X, frontierTemp.Y);
                    }
                }
            }
        }

        public void findNeighbors(Point a)
        {
            for (int i = -1; i < 2; i++)
            {
                for (int j = -1; j < 2; j++)
                {
                    if (i == 0 && j == 0)
                    {
                        continue;
                    }
                    else if (discovered.Contains(new Point(a.X + i, a.Y + j)))
                    {
                        continue;
                    }
                    else if (i != 0 && j != 0)
                    {
                        continue;
                    }
                    else if ((a.X + i < 0) || (a.X + i > 10) || ((a.Y % 2 == 1) && a.X + i > 9)
                        || (a.Y + j < 0) || (a.Y + j > 8))
                    {
                        continue;
                    }
                    frontier.Enqueue(new Point(a.X + i, a.Y + j));
                    discovered.Enqueue(new Point(a.X + i, a.Y + j));
                }
            }
            if (a.Y % 2 == 0)
            {
                if ((a.X - 1 < 0) || (a.X + 1 > 9) || (a.Y - 1 < 0) || (a.Y + 1 > 8))
                {
                    ;
                }
                else
                {
                    if (!discovered.Contains(new Point(a.X - 1, a.Y - 1)))
                    {
                        frontier.Enqueue(new Point(a.X - 1, a.Y - 1));
                        discovered.Enqueue(new Point(a.X - 1, a.Y - 1));
                    }
                    if (!discovered.Contains(new Point(a.X - 1, a.Y + 1)))
                    {
                        frontier.Enqueue(new Point(a.X - 1, a.Y + 1));
                        discovered.Enqueue(new Point(a.X - 1, a.Y + 1));
                    }
                }
            }
            else if (a.Y % 2 == 1)
            {
                if ((a.X - 1 < 0) || (a.X + 1 > 10) || (a.Y - 1 < 0) || (a.Y + 1 > 8))
                {
                    ;
                }
                else
                {
                    if (!discovered.Contains(new Point(a.X + 1, a.Y - 1)))
                    {
                        frontier.Enqueue(new Point(a.X + 1, a.Y - 1));
                        discovered.Enqueue(new Point(a.X + 1, a.Y - 1));
                    }
                    if (!discovered.Contains(new Point(a.X + 1, a.Y + 1)))
                    {
                        frontier.Enqueue(new Point(a.X + 1, a.Y + 1));
                        discovered.Enqueue(new Point(a.X + 1, a.Y + 1));
                    }
                }
            }
        }
    }
}