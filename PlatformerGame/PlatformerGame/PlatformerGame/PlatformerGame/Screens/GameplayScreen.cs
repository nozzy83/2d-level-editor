﻿#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// The actual game
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Net;
using PlatformerGameLibrary;
#endregion

namespace PlatformerGame
{
    class GameplayScreen : GameScreen
    {
        ContentManager content;
        SpriteBatch spriteBatch;

        // The Content Builder and Content Manager are used to load textures
        // at runtime through the Content Pipeline
        ContentBuilder contentBuilder;

        /*
        // health bars
        Texture2D healthBar;

        // audio
        Song levelMusic;

        // timer
        TimeSpan countdownTimer;
        Vector2 timerPos;
        SpriteFont timerFont;

        // scrolling backgrounds
        float scrollSpeed;

        float scrollBoundaryLeft;
        float scrollBoundaryRight;
        float scrollBoundaryTop;
        float scrollBoundaryBottom;
        bool scrolledX;
        bool scrolledY;

        Level curLevel;
        int curLives;
        Vector2 livesPos;

        
        bool checkpointHit;
         * */

        // generate list of levels so we know where to go next
        // Store the folder containing all the levels for this game
        string baseLevelsPath;
        string tempLevelXNBPath;
        int levelIndex;
        List<string> allLevels;
        Level level;

        int numLives;

        public GameplayScreen(int levelIndex, string gameLevelsPath)
        {
            this.levelIndex = levelIndex;
            baseLevelsPath = gameLevelsPath;
        }

        public override void LoadContent()
        {
            contentBuilder = new ContentBuilder();

            // If we don't yet have a reference to the content manager, 
            // grab one from the game
            if (content == null)
                content = new ContentManager(ScreenManager.Game.Services, contentBuilder.OutputDirectory);
            tempLevelXNBPath = contentBuilder.OutputDirectory;

            spriteBatch = ScreenManager.SpriteBatch;
            //string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            //string relativePath = Path.Combine(assemblyLocation, "../../../../../PlatformerGameContent/Tiles/");
            //string contentPath = Path.GetFullPath(relativePath);

            // Given the folder path specified by the user, load all levels in that folder
            allLevels = new List<string>();

            DirectoryInfo di = new DirectoryInfo(baseLevelsPath);
            if (!di.Exists)
            {
                this.ExitScreen();
                return;
            }

            FileInfo[] files = di.GetFiles();
            foreach (FileInfo file in files)
            {
                // Add all the txt files to the level list
                if (file.Extension == ".xml")
                {
                    allLevels.Add(Path.GetFileNameWithoutExtension(file.FullName));
                }
            }

            CreateLevelXNB();

            // Load the first level from allLevels
            levelIndex--;
            //LoadNextLevel();
            //LoadLevelName("level0.txt");


            // Set the number of lives for the player
            numLives = 3;
            
           
            /*
            // level stuff
            curLevel = new Level(allLevels[levelIndex]);
            curLevel.LoadLevel(content, ScreenManager.GraphicsDevice);
            scrollBoundaryLeft = -(ScreenManager.GraphicsDevice.Viewport.Width / 5);
            scrollBoundaryRight = (ScreenManager.GraphicsDevice.Viewport.Width / 5);
            scrollBoundaryTop = (ScreenManager.GraphicsDevice.Viewport.Height / 5);
            scrollBoundaryBottom= -(ScreenManager.GraphicsDevice.Viewport.Height / 5);
            scrolledX = false;
            scrolledY = false;

            // timer stuff
            countdownTimer = new TimeSpan(0, 0, 60);
            timerPos = new Vector2((ScreenManager.GraphicsDevice.Viewport.Width / 2) - 30, 24);
            timerFont = content.Load<SpriteFont>("gameFont");

            // lives stuff
            livesPos = new Vector2((3 * ScreenManager.GraphicsDevice.Viewport.Width / 4), 24);

            scrollSpeed = raptor.runSpeed;

            // music
            levelMusic = content.Load<Song>("HeatVision");
            MediaPlayer.Play(levelMusic);
            MediaPlayer.IsRepeating = true;
            */


            base.LoadContent();
        }

        private void CreateLevelXNB()
        {
            contentBuilder.Clear();

            foreach (string file in allLevels)
            {
                string levelName = file;
                string levelPath = baseLevelsPath + file + ".xml";
                contentBuilder.Add(levelPath, levelName, null, "LevelProcessor");
            }
            string buildError = contentBuilder.Build();

            if (string.IsNullOrEmpty(buildError))
            {
                // We're good to game.
            }
            else
            {
                // Show the error
                // TODO: SHOW ERROR
                //MessageBox.Show(buildError, "Build Error");
            }
        }

        // Returns true if a new level was successfully loaded
        public bool LoadNextLevel()
        {
            levelIndex++;
            if (allLevels.Count == 0)
            {
                // We never had any levels to load
                ScreenManager.RemoveScreen(this);
                return false;
            }
            else if (levelIndex >= allLevels.Count)
            {
                // That was the last level
                ScreenManager.AddScreen(new WinGameScreen(), null);
                ScreenManager.RemoveScreen(this);
                return false;
            }
            else
            {
                // Unload old level first
                if (level != null) level.Dispose();

                // Load the new level
                string levelName = allLevels[levelIndex];
                string levelPath = Path.Combine(tempLevelXNBPath, levelName);
                string fileNameOnly = Path.GetFileNameWithoutExtension(levelPath);
                level = content.Load<Level>(levelPath);
                level.Initialize(ScreenManager.GraphicsDevice, ScreenManager.Game.Services);
                return true;
            }
        }

        public void LoadLevelName(string levelName)
        {
            string levelPath = Path.Combine(tempLevelXNBPath, levelName);
            string fileNameOnly = Path.GetFileNameWithoutExtension(levelPath);

            // Unload old level first
            if (level != null) level.Dispose();

            // Load the new level
            level = content.Load<Level>(levelPath);
            level.Initialize(ScreenManager.GraphicsDevice, ScreenManager.Game.Services);
        }

        public void ReloadCurrentLevel()
        {
            // Just build the level again
            // Unload old level first
            if (level != null) level.Dispose();

            // Reload the current level
            string levelName = allLevels[levelIndex];
            string levelPath = Path.Combine(tempLevelXNBPath, levelName);
            string fileNameOnly = Path.GetFileNameWithoutExtension(levelPath);

            level = content.Load<Level>(levelPath);
            level.Initialize(ScreenManager.GraphicsDevice, ScreenManager.Game.Services);
        }

        /// <summary>
        /// Processes the user's input, to skip the splash screen.
        /// </summary>
        /// <param name="input">The InputState for the current frame</param>
        public override void HandleInput(InputState input)
        {
            PlayerIndex playerIndex;

            if (input.IsPauseGame(null))
            {
                // TODO: Actually pause and ask if they want to return to title screen instead of just doing it.

                this.ExitScreen();
            }

            if (input.IsNewKeyPress(Keys.E, null, out playerIndex))
            {
                level.PlayerDeath(null);
            }

            base.HandleInput(input);
        }

        public override void Update(GameTime gameTime, bool otherScreenHasFocus, bool coveredByOtherScreen)
        {
            if (level == null)
            {
                bool haveNewLevel = LoadNextLevel();
                if (!haveNewLevel) return;
            }

            level.Update(gameTime);

            if (level.TimeRemaining == TimeSpan.Zero || !level.Player.IsAlive)
            {
                // if the Player is dead or time ran out, see if they can respawn
                numLives--;
                if (numLives >= 0)
                {
                    ReloadCurrentLevel();

                }
                else
                {
                    ScreenManager.AddScreen(new GameOverScreen(), null);
                    this.ExitScreen();
                }
            }
            else if (level.ReachedExit)
            {
                LoadNextLevel();
            }

            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);
        }



        // TODO: Implement
        public void DrawHUD()
        {
        }



        /// <summary>
        /// Draw the game.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(Microsoft.Xna.Framework.GameTime gameTime)
        {
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            
            // Clear to white
            ScreenManager.GraphicsDevice.Clear(Color.White);

            ScreenManager.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            /*
            //--------Health Bar drawing--------//
            spriteBatch.Begin();

            // draw the health bar border
            spriteBatch.Draw(healthBar, new Rectangle(ScreenManager.GraphicsDevice.Viewport.Width / 4 - healthBar.Width / 2, 25,
                healthBar.Width, 32), new Rectangle(0, 0, healthBar.Width, healthBar.Height), Color.White);

            // draw the health bar inside
            spriteBatch.Draw(healthBar, new Rectangle((ScreenManager.GraphicsDevice.Viewport.Width / 4 - healthBar.Width / 2) + 4, 25 + 5,
                (int)((healthBar.Width - 10) * (raptor.health / 100.0f)) + 2, 32 - 10),
                new Rectangle(8, 10, healthBar.Width - 15, healthBar.Height - 20), Color.Red);

            spriteBatch.End();
            */
            
            //spriteBatch.Begin();

            level.Draw(gameTime, spriteBatch);

            //spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
