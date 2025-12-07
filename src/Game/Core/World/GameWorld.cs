using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.Player;
using TheLastMageStanding.Game.Core.Camera;

namespace TheLastMageStanding.Game.Core.World;

internal sealed class GameWorld
{
    private readonly PlayerCharacter _player;
    private readonly List<Enemy> _enemies = new();
    private readonly Random _random = new();
    private readonly WaveSettings _waveSettings =
        new(
            WaveIntervalSeconds: 4f,
            EnemiesPerWave: 4,
            SpawnRadiusMin: 220f,
            SpawnRadiusMax: 320f,
            EnemySpeed: 90f,
            EnemyHealth: 30f,
            EnemyDamage: 10f,
            EnemyContactCooldown: 1.1f,
            EnemyCollisionRadius: 6f);

    private Texture2D? _debugDot;
    private float _waveTimer;
    private int _waveNumber = 1;

    public GameWorld(Camera2D camera)
    {
        _player = new PlayerCharacter(camera);
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _player.LoadContent(graphicsDevice);
        _debugDot ??= CreatePixel(graphicsDevice);
    }

    public void Update(GameTime gameTime, InputState input)
    {
        var delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _player.Update(gameTime, input);
        HandlePlayerAttack();
        UpdateEnemies(delta);
        UpdateWaveSpawning(delta);
    }

    public void Draw(SpriteBatch spriteBatch)
    {
        _player.Draw(spriteBatch);
        if (_debugDot is null)
        {
            return;
        }

        foreach (var enemy in _enemies)
        {
            var enemyColor = Color.Lerp(Color.Red, Color.LimeGreen, MathHelper.Clamp(enemy.HealthRatio, 0f, 1f));
            var origin = new Vector2(0.5f, 0.5f);
            const float enemySize = 7f;
            spriteBatch.Draw(_debugDot, enemy.Position, null, enemyColor, 0f, origin, enemySize, SpriteEffects.None, 0f);

            const float barWidth = 24f;
            const float barHeight = 3f;
            var barPosition = enemy.Position + new Vector2(-barWidth * 0.5f, -12f);
            spriteBatch.Draw(_debugDot, barPosition, null, Color.DimGray, 0f, Vector2.Zero, new Vector2(barWidth, barHeight), SpriteEffects.None, 0f);
            spriteBatch.Draw(
                _debugDot,
                barPosition,
                null,
                enemyColor,
                0f,
                Vector2.Zero,
                new Vector2(barWidth * MathHelper.Clamp(enemy.HealthRatio, 0f, 1f), barHeight),
                SpriteEffects.None,
                0f);
        }
    }

    private void UpdateWaveSpawning(float delta)
    {
        _waveTimer += delta;
        if (_waveTimer < _waveSettings.WaveIntervalSeconds)
        {
            return;
        }

        _waveTimer = 0f;
        var enemyCount = _waveSettings.EnemiesPerWave + (_waveNumber - 1);
        SpawnWave(enemyCount);
        _waveNumber++;
    }

    private void SpawnWave(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var spawnPosition = GetSpawnPosition();
            _enemies.Add(
                new Enemy(
                    spawnPosition,
                    _waveSettings.EnemySpeed,
                    _waveSettings.EnemyHealth,
                    _waveSettings.EnemyDamage,
                    _waveSettings.EnemyContactCooldown,
                    _waveSettings.EnemyCollisionRadius));
        }
    }

    private Vector2 GetSpawnPosition()
    {
        var angle = _random.NextSingle() * MathF.Tau;
        var distance =
            _waveSettings.SpawnRadiusMin + (_waveSettings.SpawnRadiusMax - _waveSettings.SpawnRadiusMin) * _random.NextSingle();
        var offset = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * distance;
        return _player.Position + offset;
    }

    private void UpdateEnemies(float delta)
    {
        for (var i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            var direction = _player.Position - enemy.Position;
            var distance = direction.Length();
            if (distance > 0.001f)
            {
                direction /= distance;
                enemy.Position += direction * enemy.Speed * delta;
            }

            enemy.TickAttackTimer(delta);

            if (!_player.IsDead && distance <= enemy.CollisionRadius + _player.CollisionRadius && enemy.CanAttack)
            {
                _player.ApplyDamage(enemy.Damage);
                enemy.MarkAttack();
            }

            if (enemy.IsDead)
            {
                _enemies.RemoveAt(i);
                continue;
            }

            _enemies[i] = enemy;
        }
    }

    private void HandlePlayerAttack()
    {
        if (!_player.ConsumeAttackTrigger())
        {
            return;
        }

        for (var i = _enemies.Count - 1; i >= 0; i--)
        {
            var enemy = _enemies[i];
            var distance = Vector2.Distance(_player.Position, enemy.Position);
            if (distance > _player.AttackRange + enemy.CollisionRadius)
            {
                continue;
            }

            enemy.TakeDamage(_player.AttackDamage);
            if (enemy.IsDead)
            {
                _enemies.RemoveAt(i);
            }
            else
            {
                _enemies[i] = enemy;
            }
        }
    }

    private static Texture2D CreatePixel(GraphicsDevice graphicsDevice)
    {
        var texture = new Texture2D(graphicsDevice, 1, 1);
        texture.SetData(new[] { Color.White });
        return texture;
    }

    private readonly record struct WaveSettings(
        float WaveIntervalSeconds,
        int EnemiesPerWave,
        float SpawnRadiusMin,
        float SpawnRadiusMax,
        float EnemySpeed,
        float EnemyHealth,
        float EnemyDamage,
        float EnemyContactCooldown,
        float EnemyCollisionRadius);

    private sealed class Enemy
    {
        public Enemy(
            Vector2 position,
            float speed,
            float maxHealth,
            float damage,
            float attackCooldown,
            float collisionRadius)
        {
            Position = position;
            Speed = speed;
            MaxHealth = maxHealth;
            Health = maxHealth;
            Damage = damage;
            AttackCooldown = attackCooldown;
            CollisionRadius = collisionRadius;
        }

        public Vector2 Position { get; set; }
        public float Speed { get; }
        public float Health { get; private set; }
        public float MaxHealth { get; }
        public float Damage { get; }
        public float AttackCooldown { get; }
        public float CollisionRadius { get; }

        private float AttackTimer { get; set; }
        public bool IsDead => Health <= 0f;
        public float HealthRatio => MaxHealth <= 0f ? 0f : Health / MaxHealth;
        public bool CanAttack => AttackTimer <= 0f;

        public void TickAttackTimer(float delta)
        {
            AttackTimer = MathF.Max(0f, AttackTimer - delta);
        }

        public void MarkAttack()
        {
            AttackTimer = AttackCooldown;
        }

        public void TakeDamage(float amount)
        {
            Health = MathF.Max(0f, Health - amount);
        }
    }
}

