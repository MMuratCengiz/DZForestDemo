namespace NiziKit.Animation;

public enum AnimatorLayerBlendMode
{
    Override,
    Additive
}

public class AnimatorLayer
{
    public string Name { get; set; } = "Base Layer";
    public float Weight { get; set; } = 1.0f;
    public AnimatorLayerBlendMode BlendMode { get; set; } = AnimatorLayerBlendMode.Override;
    public List<AnimatorState> States { get; } = [];
    public AnimatorState? DefaultState { get; set; }
    public AnimatorState? AnyState { get; private set; }
    public HashSet<int>? BoneMask { get; set; }

    public AnimatorState AddState(string name)
    {
        var state = new AnimatorState { Name = name };
        States.Add(state);

        if (DefaultState == null)
        {
            DefaultState = state;
        }

        return state;
    }

    public AnimatorState? GetState(string name)
    {
        return States.Find(s => s.Name == name);
    }

    public void SetupAnyStateTransitions()
    {
        AnyState = new AnimatorState { Name = "Any State" };
    }

    public AnimatorTransition AddAnyStateTransition(AnimatorState destination)
    {
        AnyState ??= new AnimatorState { Name = "Any State" };
        return AnyState.AddTransition(destination);
    }

    public void SetBoneMask(params int[] boneIndices)
    {
        BoneMask = [..boneIndices];
    }

    public void ClearBoneMask()
    {
        BoneMask = null;
    }
}
