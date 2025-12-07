using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Config;
using TheLastMageStanding.Game.Core.Ecs;
using TheLastMageStanding.Game.Core.Input;
using TheLastMageStanding.Game.Core.World.Map;

namespace TheLastMageStanding.Game;

public class Game1 : Microsoft.Xna.Framework.Game
{
    private const int VirtualWidth = 960;
    private const int VirtualHeight = 540;
    private const int WindowScale = 2;

    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private RenderTarget2D _renderTarget = null!;
    private Camera2D _camera = null!;
    private InputState _input = null!;
    private EcsWorldRunner _ecs = null!;
    private TiledMapService _mapService = null!;
    private AudioSettingsConfig _audioSettings = null!;
    private Song _backgroundSong = null!;

    private const string HubMapAsset = "Tiles/Maps/HubMap";
    private const string FirstMapAsset = "Tiles/Maps/FirstMap";
    private const string MapEnvVar = "TLMS_MAP";

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        IsFixedTimeStep = true;
        TargetElapsedTime = TimeSpan.FromSeconds(1d / 60d);

        _graphics.PreferredBackBufferWidth = VirtualWidth * WindowScale;
        _graphics.PreferredBackBufferHeight = VirtualHeight * WindowScale;
    }

    protected override void Initialize()
    {
        _camera = new Camera2D(VirtualWidth, VirtualHeight);
        _audioSettings = AudioSettingsConfig.Default;
        _input = new InputState();
        _ecs = new EcsWorldRunner(_camera, _audioSettings);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _renderTarget = new RenderTarget2D(GraphicsDevice, VirtualWidth, VirtualHeight);

        var mapAsset = ResolveMapAsset();
        _mapService = TiledMapService.Load(Content, GraphicsDevice, mapAsset);

        var playerSpawn = _mapService.GetPlayerSpawnOrDefault(Vector2.Zero);
        _ecs.SetPlayerPosition(playerSpawn);
        _camera.LookAt(playerSpawn);

        _ecs.LoadContent(GraphicsDevice, Content);
        
        // Load collision regions from the map into the ECS world
        _mapService.LoadCollisionRegions(_ecs.World);

        _backgroundSong = Content.Load<Song>("Audio/Stage1Music");
        MediaPlayer.IsRepeating = true;
        _audioSettings.Apply();
        MediaPlayer.Play(_backgroundSong);
    }

    protected override void Update(GameTime gameTime)
    {
        _input.Update();

        _mapService.Update(gameTime);
        _ecs.Update(gameTime, _input);

        if (_ecs.ExitRequested)
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.SetRenderTarget(_renderTarget);
        GraphicsDevice.Clear(ClearOptions.Target, Color.CornflowerBlue, 1f, 0);

        _mapService.Draw(_camera.Transform);

        _spriteBatch.Begin(transformMatrix: _camera.Transform, samplerState: SamplerState.PointClamp);
        _ecs.Draw(_spriteBatch);
        _spriteBatch.End();

        // Draw UI to render target (screen space relative to virtual resolution)
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _ecs.DrawUI(_spriteBatch);
        _spriteBatch.End();

        GraphicsDevice.SetRenderTarget(null);
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(
            _renderTarget,
            destinationRectangle: new Rectangle(0, 0, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight),
            color: Color.White);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _mapService?.Dispose();
        }

        base.Dispose(disposing);
    }

    private static string ResolveMapAsset()
    {
        var envValue = Environment.GetEnvironmentVariable(MapEnvVar);
        if (string.Equals(envValue, "first", StringComparison.OrdinalIgnoreCase))
        {
            return FirstMapAsset;
        }

        return HubMapAsset;
    }
}
