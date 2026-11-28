using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using MonoEngine;
using MonoEngine.Class;
using MonoEngine.MonoUtils;

using Engine = MonoEngine.MonoEngine;

using System;
using System.Collections.Generic;

namespace MyMonoGame.Class
{
    public class PlayerScript : Behaviour
    {
        public float Speed = 5.0f;

        public float MoveBackwardPenaltyFactor = 0.7f;

        public float GroundAccel = 6.0f;
        public float AirAccel = 1.0f;

        public float GroundFric = 10.0f;
        public float AirFric = 0.5f;

        public float Gravity = 9.8f;
        public float WallSlideGravity = 3.0f;
        public float JumpForce = 5.0f;

        public float MaxSafeVerticalSpeed = 6.0f;
        public float MaxSafeHorizontalSpeed = 8.0f;

        public float VerticalVelocityDamageScale = 2.0f;
        public float HorizontalVelocityDamageScale = 1.0f;

        public Transform transform;
        public ColliderBox colliderBox;

        public Vector2 Velocity = Vector2.Zero;

        public bool IsWallSliding { get; private set; }
        public bool IsFacingRight { get; private set; }
        public bool IsGrounded { get; private set; }

        private float maxHealth = 100.0f;
        public float MaxHealth
        {
            get => maxHealth;
            set
            {
                maxHealth = value;
                health = MathF.Min(health, maxHealth);
            }
        }

        private float health = 100.0f;
        public float Health
        {
            get => health;
            set
            {
                health = MathF.Min(value, maxHealth);
            }
        }

        private float maxSlopeAngle = MathF.Cos(45.0f * MathFEx.Deg2Rad);
        public float MaxSlopeAngle
        {
            get => MathF.Acos(maxSlopeAngle);
            set => maxSlopeAngle = MathF.Cos(value);
        }

        private float maxWallSlidingThreshold = MathF.Cos(15.0f * MathFEx.Deg2Rad);
        public float MaxWallSlidingThreshold
        {
            get => MathF.Acos(maxWallSlidingThreshold);
            set => maxWallSlidingThreshold = MathF.Cos(value);
        }

        private SpriteRenderer rendererSprite;
        private GunScript gunScript;
        private AudioSource audioSource;

        private Vector2 cameraVelocity = Vector2.Zero;

        private float JumpNormalX = 0.0f;
        private float damageInThisFrame = 0.0f;
        private float deathProgress = 0.0f;
        private float moveInput = 0.0f;
        private bool jumpRequested = false;
        private bool wasCollision = false;

        public override void Awake()
        {
            transform = GetOrAddComponent<Transform>();
            rendererSprite = GetOrAddComponent<SpriteRenderer>();
            colliderBox = GetOrAddComponent<ColliderBox>();
            audioSource = GetOrAddComponent<AudioSource>();

            colliderBox.IsStatic = false;
            audioSource.Is3D = true;
        }

        public override void Start()
        {
            GetComponentInChildren(out gunScript);

            Texture2D playerTexture = Engine.ResourceManager.Load<Texture2D>("ExperimentPlushieConstruction");
            SoundEffect bgm = Engine.ResourceManager.Load<SoundEffect>("Ice");

            rendererSprite.Texture = playerTexture;
            rendererSprite.Origin = new Vector2(0.5f, 0.5f);

            transform.Position = new Vector2(45.0f, 0.0f);
            transform.Scale = new Vector2(0.78f, 0.97f);

            colliderBox.ScaleOffset = new Vector2(0.7f, 0.8f);

            audioSource.Clip = bgm;
            audioSource.Loop = true;
            audioSource.Play();

            IsGrounded = false;
            IsFacingRight = false;
        }

        private bool IsAlive()
        {
            return health > 0;
        }

        private bool IsMouseOnTheRight(float x)
        {
            Camera camera = Camera.Main;
            if (!camera) return x > transform.Position.X;
            
            return camera.ScreenToWorld(new Vector2(x, 0.0f)).X > transform.Position.X;
        }

        private void ProcessMove(KeyboardState keyboardState)
        {
            moveInput = 0.0f;

            if (keyboardState.IsKeyDown(Keys.A))
                moveInput -= 1.0f;
            else if (keyboardState.IsKeyDown(Keys.D))
                moveInput += 1.0f;

            if (keyboardState.IsKeyDown(Keys.Space))
                jumpRequested = true;
        }

        private void ProcessLook(MouseState mouseState)
        {
            IsFacingRight = IsMouseOnTheRight(mouseState.X);
            rendererSprite.FlipX = IsFacingRight;
        }

        private void ProcessWeapon(KeyboardState keyboardState, MouseState mouseState)
        {
            if (!gunScript) return;

            if (mouseState.LeftButton == ButtonState.Pressed)
            {
                Camera camera = Camera.Main;
                if (!camera) return;

                Vector2 mouseWorldPos = camera.ScreenToWorld(new Vector2(mouseState.X, mouseState.Y));

                Vector2 origin = transform.Position;
                Vector2 direction = Vector2Ex.DirectionTo(origin, mouseWorldPos);

                gunScript.Shoot(origin, direction);
            }
        }

        private void ProcessInput(KeyboardState keyboardState, MouseState mouseState)
        {
            if (!IsAlive()) return;

            ProcessLook(mouseState);
            ProcessWeapon(keyboardState, mouseState);
            ProcessMove(keyboardState);
        }

        private void ProcessInputVelocity(float deltaTime)
        {
            bool isMovingBackward = ((IsFacingRight && moveInput < 0) || (!IsFacingRight && moveInput > 0)) && IsGrounded;

            float speed = Speed * (isMovingBackward ? MoveBackwardPenaltyFactor : 1.0f);
            float accel = IsGrounded ? GroundAccel : AirAccel;

            Velocity.X += moveInput * speed * accel * deltaTime;
        }

        private void ProcessJumpRequest()
        {
            if (!jumpRequested) return;
            jumpRequested = false;

            if (!IsGrounded && !IsWallSliding) return;

            Vector2 jumpForce = Vector2.Zero;

            if (IsGrounded)
            {
                jumpForce.Y = JumpForce;
                IsGrounded = false;
            }

            if (IsWallSliding)
            {
                jumpForce.X = JumpNormalX * JumpForce * 0.15f;
                jumpForce.Y = JumpForce * 0.8f;
            }

            ApplyJumpForce(jumpForce);
        }

        private void ApplyJumpForce(Vector2 jumpForce)
        {
            Velocity.X += jumpForce.X;
            Velocity.Y = jumpForce.Y;
        }

        private void ApplyFriction(float deltaTime)
        {
            float fric = IsGrounded ? GroundFric : AirFric;
            Velocity.X *= MathF.Exp(-fric * deltaTime);
        }

        private void ApplyGravity(float deltaTime)
        {
            if (IsWallSliding)
            {
                Velocity.Y = MathFEx.MoveTowards(Velocity.Y, -WallSlideGravity, 5.0f * deltaTime);
                return;
            }

            if (!IsGrounded)
                Velocity.Y -= Gravity * deltaTime;
        }

        private void ApplyVelocity(float deltaTime)
        {
            transform.Translate(Velocity * deltaTime);
        }

        private void ApplyPhysicsDamage()
        {
            float overbounceX = MathF.Abs(Velocity.X) - MaxSafeHorizontalSpeed;
            if (overbounceX > 0)
            {
                damageInThisFrame += HorizontalVelocityDamageScale * (overbounceX * overbounceX);
            }

            float overbounceY = MathF.Abs(Velocity.Y) - MaxSafeVerticalSpeed;
            if (overbounceY > 0)
            {
                damageInThisFrame += VerticalVelocityDamageScale * (overbounceY * overbounceY);
            }
        }

        private void ResolveCollision(List<OverlapResult> results)
        {
            IsGrounded = false;
            IsWallSliding = false;
            JumpNormalX = 0.0f;

            bool hasCollision = results.Count != 0;

            if (!hasCollision)
            {
                wasCollision = false;
                return;
            }

            if (IsAlive() && !wasCollision)
                ApplyPhysicsDamage();

            foreach (var result in results)
            {
                transform.Position += result.MinimumTranslationVector;

                Vector2 normal = result.Normal;

                if (Vector2.Dot(normal, Vector2Ex.Up) > maxSlopeAngle)
                    IsGrounded = true;

                if (!IsGrounded)
                {
                    float WallNormalDot = Vector2.Dot(normal, Vector2Ex.Foward);
                    if (MathF.Abs(WallNormalDot) < maxWallSlidingThreshold) continue;
                    JumpNormalX = normal.X;
                    IsWallSliding = true;
                }
            }

            wasCollision = true;
        }

        private void ClipVelocity(List<OverlapResult> results, float overbounce = 1.0f)
        {
            bool hasCollision = results.Count != 0;
            if (!hasCollision) return;

            Vector2 velocity = Velocity;

            for (int i = 0; i < 4; i++)
            {
                foreach (var result in results)
                {
                    float normalVelocity = Vector2.Dot(velocity, result.Normal);

                    if (normalVelocity >= 0f) continue;

                    velocity -= result.Normal * (normalVelocity * overbounce);

                    float adjust = Vector2.Dot(velocity, result.Normal);
                    if (adjust < 0f)
                        velocity -= result.Normal * adjust;
                }
            }

            Vector2Ex.ApplyDeadZoneRef(ref velocity, 1e-3f);
            Velocity = velocity;
        }

        private void MoveAndSlide(float deltaTime)
        {
            ApplyVelocity(deltaTime);

            List<OverlapResult> collisionResult = PhysicalEngine.OverlapCollider(colliderBox.CollisionActor);
            ResolveCollision(collisionResult);
            ClipVelocity(collisionResult);
        }

        private void ApplyDamage()
        {
            Health -= damageInThisFrame;
            damageInThisFrame = 0.0f;
        }

        private void ProcessDamage()
        {
            if (!IsAlive()) return;

            ApplyDamage();
        }

        private void ProcessDeath(float deltaTime)
        {
            if (IsAlive()) return;

            deathProgress = MathF.Min(deathProgress + 2.0f * deltaTime, 1.0f);

            float smoothedProgress = MathFEx.EaseSineOut(deathProgress);

            float rotation = 90.0f * smoothedProgress * MathFEx.Deg2Rad;
            transform.Rotation = IsFacingRight ? rotation : -rotation;
            
            rendererSprite.TextureColor = Color.Lerp(Color.White, Color.Red, smoothedProgress * 0.6f);
        }

        private void CameraFollow(float deltaTime)
        {
            Camera camera = Camera.Main;
            if (!camera)
                return;

            camera.transform.Position = Vector2Ex.SmoothDamp(camera.transform.Position, transform.Position, ref cameraVelocity, 0.1f, deltaTime);
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);

            MouseState mouseState = Mouse.GetState();
            KeyboardState keyboardState = Keyboard.GetState();
            ProcessInput(keyboardState, mouseState);

            ProcessDamage();

            ProcessDeath(deltaTime);
            CameraFollow(deltaTime);
        }

        public override void FixedUpdate(GameTime gameTime)
        {
            float deltaTime = MathF.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.1f);

            if (IsAlive())
            {
                ProcessInputVelocity(deltaTime);
                ProcessJumpRequest();
            }

            ApplyGravity(deltaTime);
            ApplyFriction(deltaTime);
            MoveAndSlide(deltaTime);
        }
    }
}