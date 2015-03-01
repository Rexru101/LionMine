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
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D hexBoard, hexSquareR, hexSquareB, square, larg, goltana;
        MouseState currentMouse, prevMouse;
        
        const int HEIGHT = 790;
        const int WIDTH = 750;
        bool newGame = false;
        bool player1 = true, player2;

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
            //hexSquareR = Content.Load<Texture2D>(@"Sprites/hexSquareRed");
            //hexSquareB = Content.Load<Texture2D>(@"Sprites/hexSquareBlue");
            larg = Content.Load<Texture2D>(@"Sprites/LargFlag");
            goltana = Content.Load<Texture2D>(@"Sprites/GoltanaFlag");
            square = Content.Load<Texture2D>(@"Sprites/pixel");

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
            prevMouse = currentMouse;
            currentMouse = Mouse.GetState();
            if (Keyboard.GetState().IsKeyDown(Keys.Enter))
                this.Exit();
            if (IsActive && currentMouse.LeftButton == ButtonState.Released
                && prevMouse.LeftButton == ButtonState.Pressed)
                checkClick();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.White);
            spriteBatch.Begin();

            spriteBatch.Draw(hexBoard, Vector2.Zero, Color.White);
            spriteBatch.Draw(larg, new Vector2(rectArr[0, 2].X + 2, rectArr[0, 2].Y), null, Color.White, 0f, Vector2.Zero,
                1f, SpriteEffects.None, 0);
            spriteBatch.Draw(goltana, new Vector2(rectArr[20, 2].X, rectArr[20, 2].Y), null, Color.White, 0f, Vector2.Zero,
                1f, SpriteEffects.None, 0);
            
            if (newGame)
                drawNewGame();

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
            spriteBatch.Draw(larg, new Vector2(rectArr[0, 2].X + 2, rectArr[0, 2].Y), null, Color.White, 0f, Vector2.Zero,
                1f, SpriteEffects.None, 0);
            spriteBatch.Draw(goltana, new Vector2(rectArr[20, 2].X, rectArr[20, 2].Y), null, Color.White, 0f, Vector2.Zero,
                1f, SpriteEffects.None, 0);
        }

        public void drawSquare()
        {
            for (int i = 0; i < 21; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (boardState[i, j] == 1)
                        spriteBatch.Draw(square, new Vector2(rectArr[i, j].X, rectArr[i, j].Y), rectArr[i, j], Color.Black,
                            0f, Vector2.Zero, 1f, SpriteEffects.None, 1);
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
                        boardState[i, j] = 1;
                    }
                }
            }
        }
    }
}