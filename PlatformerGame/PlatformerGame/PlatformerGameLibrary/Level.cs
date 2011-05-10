﻿#region FileDescription
/* Level.cs
 * 
 * Represents a level in the game.
 * 
 * Uses the XNA Platformer Starter Kit class of the same name as a starting framework.
 * 
 */
#endregion

#region UsingStatements
using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
#endregion

namespace PlatformerGameLibrary
{
    public class Level : IDisposable
    {
        #region Fields and Properties

        [ContentSerializer]
        public string Name;

        [ContentSerializer(Optional = true)]
        public Texture2D Background;

        //[ContentSerializer(Optional = true)]
        //public string LevelMusic;
//        public Song LevelMusic;

        [ContentSerializer(Optional = true)]
        public Song LevelSong;

        // Keep track of all the types of tiles in this level
        [ContentSerializer]
        public Tile[] TileTypes;

        [ContentSerializer]
        public Vector2 MapSize;

        // Store the locations and tile type for every tile location
        [ContentSerializer]
        public TileMap[] TileArray;


        Dictionary<string, Texture2D> tileTextureDict;

        // 2D tilemap of tiles in the level
        Tile[,] tiles;
        // Various layers that make up the level
        Texture2D[] layers;
        // Blank Texture to serve as the background if none are provided
        Texture2D blankLayer;
        // Rectangle representing the entire level size (not the viewport size)
        Rectangle levelRectangle;

        // Player, enemies, and items in the level
        [ContentSerializerIgnore]
        public Player Player
        {
            get { return player; }
            set { player = value; }
        }
        Player player;

        List<Enemy> enemies;


        // Key positions in the level
        private static readonly Point InvalidPosition = new Point(-1, -1);
        Vector2 startPos;
        Point exitPos;

         // Level completion properties
        [ContentSerializerIgnore]
        public bool ReachedExit
        {
            get { return reachedExit; }
        }
        bool reachedExit;

        [ContentSerializerIgnore]
        public TimeSpan TimeRemaining
        {
            get { return timeRemaining; }
        }
        TimeSpan timeRemaining;
        bool isTimed; // Do we want a time limit for this level?

        [ContentSerializerIgnore]
        public ContentManager Content
        {
            get { return content; }
        }
        ContentManager content;

        [ContentSerializerIgnore]
        public GraphicsDevice GraphicsDevice
        {
            get { return graphicsDevice; }
        }
        GraphicsDevice graphicsDevice;


        Vector2 cameraPos;
        Vector2 maxCameraPos;
        int scrollBoundaryLeft;
        int scrollBoundaryRight;
        int scrollBoundaryTop;
        int scrollBoundaryBottom;
        Matrix cameraTransform;
        
        #endregion


        #region Initialization

        public void Initialize(GraphicsDevice device, IServiceProvider services, bool isTimeLimit)
        {
            content = new ContentManager(services, "Content");

            graphicsDevice = device;

            enemies = new List<Enemy>();
            exitPos = InvalidPosition;

            isTimed = isTimeLimit;
            timeRemaining = TimeSpan.FromSeconds(100);

            // Load the background(s)
            if (Background != null)
            {
                layers = new Texture2D[1];
                layers[0] = Background;
            }
            else
            {
                layers = new Texture2D[0];
                blankLayer = content.Load<Texture2D>("blank");
            }
            
            // Build a dictionary of all tile names and their texture locations
            tileTextureDict = new Dictionary<string, Texture2D>();
            foreach (Tile tile in TileTypes)
            {
                tileTextureDict.Add(tile.Name, tile.Texture);
            }

            // Load the tiles
            LoadTiles();

            levelRectangle = new Rectangle(0, 0, Tile.Width * Width, graphicsDevice.Viewport.Height);

            cameraPos = new Vector2(Player.Position.X, Player.Position.Y);
            float maxCamX = Tile.Width * Width - graphicsDevice.Viewport.Width;
            float maxCamY = Tile.Height * Height - graphicsDevice.Viewport.Height;
            maxCameraPos = new Vector2(maxCamX, maxCamY);

            cameraTransform = Matrix.CreateTranslation(-cameraPos.X, -cameraPos.Y, 0f);

            scrollBoundaryLeft = (int)(graphicsDevice.Viewport.Width * (0.4));
            scrollBoundaryRight = graphicsDevice.Viewport.Width - scrollBoundaryLeft;
            scrollBoundaryTop = (int)(graphicsDevice.Viewport.Height * (0.4));
            scrollBoundaryBottom = graphicsDevice.Viewport.Height - scrollBoundaryTop;
        }

        /// <summary>
        /// TODO: MODIFY TO LOAD BASED ON XML
        /// </summary>
        /// <param name="path"></param>
        private void LoadTiles()
        {
            int height = (int)MapSize.X;
            int width = (int)MapSize.Y;

            // Create the tile grid
            tiles = new Tile[width, height];

            // Loop over every tile position and load each tile
            for (int y = 0; y < height; ++y)
            {
                for (int x = 0; x < width; ++x)
                {
                    TileMap curTile = TileArray[y * (width) + x];
                    tiles[x, y] = LoadTileType(curTile);
                }
            }

            // Make sure the level has a beginning and an end
            if (Player == null)
            {
                throw new Exception("No starting point");
            }
            if (exitPos == InvalidPosition)
            {
                throw new Exception("No exit in the level");
            }

        }

        private Tile LoadTileType(TileMap curTile)
        {
            // TODO: This will need to handle loading of all tile types, loading each type appropriately

            int y = (int)curTile.Position.X;
            int x = (int)curTile.Position.Y;

            switch (curTile.Name)
            {
                // Blank space
                case "Blank Tile":
                    return new Tile(null, TileCollision.Passable, false);

                // Player
                case "Player":
                    return LoadStartTile(curTile.Name, x, y);

                //-------------- PLATFORMS --------------//
                // Solid block
                case "Ground":
                    return LoadTile(curTile.Name, TileCollision.Impassable, false);

                // Platform
                case "Platform":
                    return LoadTile(curTile.Name, TileCollision.Platform, false);

                // Exit
                case "LevelEnd":
                    return LoadExitTile(curTile.Name, x, y);

                //-------------- ENEMIES --------------//

                //
                case "WalkingEnemy":
                    return LoadTile(curTile.Name, TileCollision.Passable, true);

                // Error for unsupported tile found
                default:
                    throw new Exception("Invalid tile character in level");
            }
        }

        private Tile LoadTile(string name, TileCollision collision, bool isDamage)
        {
            // TODO: for multiple tilesets, add in "Tiles/" + the tileset name we're using..add this variable
            return new Tile(tileTextureDict[name], collision, isDamage);            
            //return new Tile(content.Load<Texture2D>(name), collision, isDamage);
        }

        private Tile LoadStartTile(string name, int x, int y)
        {
            if (Player != null)
            {
                throw new Exception("A level may not have multiple players");
            }

            startPos = RectangleExtensions.GetBottomCenter(GetBounds(x, y));
            player = new Player(this, startPos, tileTextureDict[name]);

            return new Tile(null, TileCollision.Passable, false);
        }

        private Tile LoadExitTile(string name, int x, int y)
        {
            if (exitPos != InvalidPosition)
            {
                throw new Exception("A level may not have multiple exits");
            }

            exitPos = GetBounds(x, y).Center;

            return LoadTile(name, TileCollision.Passable, false);
        }


        public void Dispose()
        {
            // TODO: make sure we RESET backgrounds, timer, and everything else correctly
            player = null;
            exitPos = InvalidPosition;

            content.Unload();
        }

        #endregion


        #region Bounds and Collision

        public TileCollision GetCollision(int x, int y)
        {
            // Prevent moving outside the left and right level boundaries
            if (x < 0 || x >= Width)
            {
                return TileCollision.Impassable;
            }

            // Allow being above or below the level boundaries
            if (y < 0 || y >= Height)
            {
                return TileCollision.Passable;
            }

            return tiles[x, y].Collision;
        }

        /// <summary>
        /// Gets the bounding rectangle of a tile in world space.
        /// </summary>        
        public Rectangle GetBounds(int x, int y)
        {
            return new Rectangle(x * Tile.Width, y * Tile.Height, Tile.Width, Tile.Height);
        }

        /// <summary>
        /// Width of level measured in tiles.
        /// </summary>
        public int Width
        {
            get { return tiles.GetLength(0); }
        }

        /// <summary>
        /// Height of the level measured in tiles.
        /// </summary>
        public int Height
        {
            get { return tiles.GetLength(1); }
        }

        #endregion


        #region Update

        public void Update(GameTime gameTime)
        {
            if (!Player.IsAlive || TimeRemaining == TimeSpan.Zero)
            {

            }
            else if (ReachedExit)
            {

            }
            else
            {
                if (isTimed) timeRemaining -= gameTime.ElapsedGameTime;

                player.Update(gameTime);

                UpdateEnemies(gameTime);

                // Check if the player fell down a pit
                if (Player.BoundingRectangle.Top >= Height * Tile.Height)
                {
                    PlayerDeath(null);
                }

                // Check if they reached the exit
                if (player.IsAlive && player.BoundingRectangle.Contains(exitPos))
                {
                    ExitReached();
                }
            }

            if (timeRemaining < TimeSpan.Zero)
            {
                timeRemaining = TimeSpan.Zero;
            }
        }

        private void UpdateEnemies(GameTime gameTime)
        {
            foreach (Enemy e in enemies)
            {
                e.Update(gameTime);

                // TODO: see if an enemy hits a player

            }
        }

        public void PlayerDeath(Enemy killer)
        {
            player.Die(killer);
        }

        private void ExitReached()
        {
            player.BeatLevel();

            reachedExit = true;
        }

        public void StartNewLife()
        {
            Player.Reset(startPos);
        }


        private void UpdateCamera(Viewport viewport)
        {
            // Calculate the boundaries outside which we will scroll
            float boundLeft = cameraPos.X + scrollBoundaryLeft;
            float boundRight = cameraPos.X + scrollBoundaryRight;
            float boundTop = cameraPos.Y + scrollBoundaryTop;
            float boundBottom = cameraPos.Y + scrollBoundaryBottom;

            // Calculate how much to update the camera scroll
            Vector2 cameraUpdate = Vector2.Zero;
            if (Player.Position.X < boundLeft)
            {
                cameraUpdate.X = Player.Position.X - boundLeft;
            }
            else if (Player.Position.X > boundRight)
            {
                cameraUpdate.X = Player.Position.X - boundRight;
            }
            if (Player.Position.Y < boundTop)
            {
                cameraUpdate.Y = Player.Position.Y - boundTop;
            }
            else if (Player.Position.Y > boundBottom)
            {
                cameraUpdate.Y = Player.Position.Y - boundBottom;
            }

            // Update the camera position, but keep it within the bounds of the level
            cameraPos.X = MathHelper.Clamp(cameraPos.X + cameraUpdate.X, 0f, maxCameraPos.X);
            cameraPos.Y = MathHelper.Clamp(cameraPos.Y + cameraUpdate.Y, 0f, maxCameraPos.Y);
        }

        #endregion


        #region Draw

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Draw the background
            spriteBatch.Begin();
            // If they have provided one or more backgrounds, draw them
            // Otherwise clear the screen to white
            if (layers.Length > 0)
            {
                foreach (Texture2D layer in layers)
                {
                    spriteBatch.Draw(layer, graphicsDevice.Viewport.Bounds, Color.White);
                }
            }
            else
            {
                spriteBatch.Draw(blankLayer, levelRectangle, Color.White);
            }
            spriteBatch.End();

            // Scroll the camera if needed.
            UpdateCamera(spriteBatch.GraphicsDevice.Viewport);
            cameraTransform = Matrix.CreateTranslation(-cameraPos.X, -cameraPos.Y, 0f);
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, null, cameraTransform);

            DrawTiles(spriteBatch);

            spriteBatch.End();
        }

        public void DrawPlayerAndEnemies(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, null, null, null, null, cameraTransform);

            player.Draw(gameTime, spriteBatch);

            foreach (Enemy e in enemies)
            {
                e.Draw(gameTime, spriteBatch);
            }

            spriteBatch.End();
        }

        private void DrawTiles(SpriteBatch spriteBatch)
        {
            // Culling so that we only draw the visible tiles.
            int left = (int)Math.Floor(cameraPos.X / Tile.Width);
            int right = left + (graphicsDevice.Viewport.Width / Tile.Width) + 1;
            right = Math.Min(right, Width);
            // TODO: Culling for top and bottom
            for (int y = 0; y < Height; y++)
            {
                for (int x = left; x < right; x++)
                {
                    // If there is a visible tile here
                    Texture2D tex = tiles[x, y].Texture;
                    if (tex != null)
                    {
                        // Draw it
                        Vector2 pos = new Vector2(x, y) * Tile.Size;
                        Rectangle dest = new Rectangle((int)pos.X, (int)pos.Y, (int)Tile.Size.X, (int)Tile.Size.Y);
                        spriteBatch.Draw(tex, dest, Color.White);
                    }
                }
            }
        }

        #endregion
    }

    public class TileMap
    {
        #region Fields

        [ContentSerializer]
        public string Name;

        [ContentSerializer]
        public Vector2 Position;

        // grab texture from tiletype dict in level class
        Texture2D texture;
        [ContentSerializerIgnore]
        public Texture2D Texture
        {
            get { return texture; }
        }

        #endregion

        #region Initialization

        public void Initialize(GraphicsDevice device)
        {

        }

        #endregion
    }
}
