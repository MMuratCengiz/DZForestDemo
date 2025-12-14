namespace ECS;

public enum Schedule
{
    First,
    PreUpdate,
    FixedUpdate,
    Update,
    PostUpdate,
    Last,

    PreRender,
    Render,
    PostRender
}
