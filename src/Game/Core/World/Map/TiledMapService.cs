using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Tiled;
using MonoGame.Extended.Tiled.Renderers;
using TheLastMageStanding.Game.Core.Ecs;

namespace TheLastMageStanding.Game.Core.World.Map;

internal sealed class TiledMapService : IDisposable
{
    private readonly TiledMap _map;
    private readonly TiledMapRenderer _renderer;

    public TiledMap Map => _map;

    private TiledMapService(TiledMap map, TiledMapRenderer renderer)
    {
        _map = map;
        _renderer = renderer;
    }

    public static TiledMapService Load(ContentManager content, GraphicsDevice graphicsDevice, string assetName)
    {
        var map = content.Load<TiledMap>(assetName);
        var renderer = new TiledMapRenderer(graphicsDevice, map);

        return new TiledMapService(map, renderer);
    }

    /// <summary>
    /// Loads static collision regions from the TMX map into the ECS world.
    /// Call this after creating the map service to populate world colliders.
    /// </summary>
    public void LoadCollisionRegions(EcsWorld world)
    {
        StaticColliderLoader.LoadCollisionRegions(_map, world);
    }

    public void Update(GameTime gameTime)
    {
        _renderer.Update(gameTime);
    }

    public void Draw(Matrix viewMatrix)
    {
        _renderer.Draw(viewMatrix);
    }

    public Vector2 GetPlayerSpawnOrDefault(Vector2 fallback)
    {
        return FindObjectCenter("player_start") ?? fallback;
    }

    private Vector2? FindObjectCenter(string name)
    {
        foreach (var layer in _map.ObjectLayers)
        {
            var match = layer.Objects.FirstOrDefault(
                obj => string.Equals(obj.Name, name, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                continue;
            }

            var position = match.Position;
            var size = match.Size;
            if (size != SizeF.Empty)
            {
                position += new Vector2(size.Width, size.Height) * 0.5f;
            }

            return position;
        }

        return null;
    }

    public void Dispose()
    {
        _renderer.Dispose();
    }
}

