using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using TheLastMageStanding.Game.Core.Camera;
using TheLastMageStanding.Game.Core.Input;

namespace TheLastMageStanding.Game.Core.Ecs;

internal readonly record struct EcsUpdateContext(GameTime GameTime, float DeltaSeconds, InputState Input, Camera2D Camera, Vector2 MouseWorldPosition);

internal readonly record struct EcsDrawContext(SpriteBatch SpriteBatch, Camera2D Camera);

internal interface IEcsSystem
{
    void Initialize(EcsWorld world);
}

internal interface IUpdateSystem : IEcsSystem
{
    void Update(EcsWorld world, in EcsUpdateContext context);
}

internal interface IDrawSystem : IEcsSystem
{
    void Draw(EcsWorld world, in EcsDrawContext context);
}

internal interface ILoadContentSystem : IEcsSystem
{
    void LoadContent(EcsWorld world, GraphicsDevice graphicsDevice, ContentManager content);
}

