namespace ECS;

public enum Schedule
{
    First,
    PreUpdate,
    FixedUpdate,
    Update,
    PostUpdate,
    Last,

    PrepareFrame,
    Render,
    PostRender
}
