using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

using MonoEngine.Class;
using MonoEngine.MonoUtils;

using MyMonoGame.Class;

using System;
using System.Collections.Generic;

namespace MonoEngine
{
    public sealed class MonoEngine : Game
    {
        static public SpriteBatch SpriteBatch { get; private set; }

        static public Texture2D WhitePixel { get; private set; }

        static public Vector2 ViewportSize
        {
            get
            {
                return new Vector2(Graphics.GraphicsDevice.Viewport.Width, Graphics.GraphicsDevice.Viewport.Height);
            }
            set
            {
                Graphics.PreferredBackBufferWidth = (int)value.X;
                Graphics.PreferredBackBufferHeight = (int)value.Y;
                Graphics.ApplyChanges();
            }
        }

        static public bool WindowResizable
        {
            get
            {
                return MainGameInstance.Window.AllowUserResizing;
            }
            set
            {
                MainGameInstance.Window.AllowUserResizing = value;
            }
        }

        static public bool IsFullScreen
        {
            get
            {
                return Graphics.IsFullScreen;
            }
            set
            {
                Graphics.IsFullScreen = value;
                Graphics.ApplyChanges();
            }
        }

        static public int TargetElaspedTime
        {
            get
            {
                return MainGameInstance.TargetElapsedTime.Milliseconds;
            }
            set
            {
                MainGameInstance.TargetElapsedTime = TimeSpan.FromMilliseconds(value);
            }
        }

        static public List<Renderer> RendererList { get; private set; }
        static public List<Behaviour> BehaviourList { get; private set; }
        static public List<GCObject> PendingGCList { get; private set; }
        static public ContentManager ResourceManager { get; private set; }

        static private MonoEngine MainGameInstance;
        static private PhysicalEngine physicalEngine;
        static private GraphicsDeviceManager Graphics;

        public const float METER_TO_PIXEL = 128.0f;
        public const float PIXEL_TO_METER = 1.0f / METER_TO_PIXEL;

        public const int DEFAULT_RENDERER_LIST_SIZE = 1024;
        public const int DEFAULT_BEHAVIOUR_LIST_SIZE = 1024;
        public const int DEFAULT_GC_LIST_SIZE = 512;

        public const int DEFAULT_WINDOW_WIDTH = 1280;
        public const int DEFAULT_WINDOW_HEIGHT = 720;
        public const string DEFAULT_WINDOW_TITLE = "My Test";

        public MonoEngine()
        {
            Graphics = new GraphicsDeviceManager(this);

            physicalEngine = new PhysicalEngine();

            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            ResourceManager = Content;
            MainGameInstance = this;

            Graphics.PreferredBackBufferHeight = DEFAULT_WINDOW_HEIGHT;
            Graphics.PreferredBackBufferWidth = DEFAULT_WINDOW_WIDTH;
            Graphics.SynchronizeWithVerticalRetrace = false;

            IsFixedTimeStep = true;
            TargetElapsedTime = TimeSpan.FromMilliseconds(1000 / 60);

            Window.AllowUserResizing = true;
            Window.Title = DEFAULT_WINDOW_TITLE;
        }

        protected override void LoadContent()
        {
            SpriteBatch = new SpriteBatch(GraphicsDevice);
            WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
            WhitePixel.SetData(new[] { Color.White });

            RendererList = new List<Renderer>(DEFAULT_RENDERER_LIST_SIZE);
            BehaviourList = new List<Behaviour>(DEFAULT_BEHAVIOUR_LIST_SIZE);
            PendingGCList = new List<GCObject>(DEFAULT_GC_LIST_SIZE);

            LoadScene();
        }

        protected override void Update(GameTime gameTime)
        {
            FixedUpdateWorld(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            ClearScreen(Color.CornflowerBlue);

            UpdateGCObject();
            UpdateWorld(gameTime);

            BeginRenderPass();
            RenderWorld();
            EndRenderPass();

            base.Draw(gameTime);
        }

        private void BeginRenderPass()
        {
            Matrix viewMatrix = (Camera.Main && Camera.Main.Enabled) ? Camera.Main.GetViewMatrix() : Matrix.Identity;
            SpriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, viewMatrix);
        }

        private void EndRenderPass()
        {
            SpriteBatch.End();
        }

        private void ClearScreen(Color clearColor)
        {
            GraphicsDevice.Clear(clearColor);
        }

        private void UpdateGCObject()
        {
            for (int i = PendingGCList.Count - 1; i >= 0; i--)
            {
                GCObject gcObj = PendingGCList[i];

                if (gcObj.IsObjectDestroyed())
                {
                    PendingGCList.SwapAndRemove(i);
                    continue;
                }

                if (gcObj.ShouldDestroy())
                {
                    gcObj.Destroy();
                    PendingGCList.SwapAndRemove(i);
                }
            }
        }

        private void FixedUpdateWorld(GameTime gameTime)
        {
            for (int i = 0; i < BehaviourList.Count; i++)
            {
                Behaviour behaviour = BehaviourList[i];

                if (!behaviour || !behaviour.Enabled) continue;

                behaviour.FixedUpdate(gameTime);
            }

            physicalEngine.UpdatePhysics(gameTime);
        }

        private void UpdateWorld(GameTime gameTime)
        {
            for (int i = BehaviourList.Count - 1; i >= 0; i--)
            {
                Behaviour behaviour = BehaviourList[i];

                if (!behaviour)
                {
                    BehaviourList.SwapAndRemove(i);
                    continue;
                }

                if (!behaviour.Enabled) continue;

                if (!behaviour.Started)
                {
                    behaviour.Started = true;
                    behaviour.Start();
                }

                behaviour.Update(gameTime);
            }
        }

        private void RenderWorld()
        {
            for (int i = RendererList.Count - 1; i >= 0; i--)
            {
                Renderer renderer = RendererList[i];

                if (!renderer)
                {
                    RendererList.SwapAndRemove(i);
                    continue;
                }

                if (!renderer.IsVisible() || !renderer.Enabled) continue;

                renderer.Draw();
            }
        }

        private void LoadScene()
        {
            GameObject playerObject = new GameObject("Player");
            playerObject.AddComponent<PlayerScript>();

            GameObject gunObject = new GameObject("Gun");
            gunObject.AddComponent<GunScript>();
            gunObject.Parent = playerObject;

            GameObject cameraObject = new GameObject("Camera");
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();

            GameObject groundObject = new GameObject("Ground");

            Transform groundTransform = groundObject.AddComponent<Transform>();
            groundTransform.Position = new Vector2(25.0f, -2.0f);
            groundTransform.Scale = new Vector2(50.0f, 0.5f);

            RectangleRenderer groundRectangleRenderer = groundObject.AddComponent<RectangleRenderer>();
            groundRectangleRenderer.Origin = new Vector2(0.5f, 0.5f);

            groundObject.AddComponent<ColliderBox>();

            GameObject rightWallObject = new GameObject("RightWall");

            Transform rightWallTransform = rightWallObject.AddComponent<Transform>();
            rightWallTransform.Position = new Vector2(50.25f, 23.25f);
            rightWallTransform.Scale = new Vector2(0.5f, 50.0f);

            RectangleRenderer rightWallRenderer = rightWallObject.AddComponent<RectangleRenderer>();
            rightWallRenderer.Origin = new Vector2(0.5f, 0.5f);

            rightWallObject.AddComponent<ColliderBox>();

            GameObject jumpTestWallObject = new GameObject("JumpTestWall");

            Transform jumpTestWallTransform = jumpTestWallObject.AddComponent<Transform>();
            jumpTestWallTransform.Position = new Vector2(48.25f, 23.75f);
            jumpTestWallTransform.Scale = new Vector2(0.5f, 49.0f);

            RectangleRenderer jumpTestWallRenderer = jumpTestWallObject.AddComponent<RectangleRenderer>();
            jumpTestWallRenderer.Origin = new Vector2(0.5f, 0.5f);

            jumpTestWallObject.AddComponent<ColliderBox>();
        }
    }
}