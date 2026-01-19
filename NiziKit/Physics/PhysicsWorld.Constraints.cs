using BepuPhysics;
using BepuPhysics.Constraints;

namespace NiziKit.Physics;

public sealed partial class PhysicsWorld
{
    public ConstraintHandle AddConstraint<TDescription>(BodyHandle bodyA, BodyHandle bodyB, TDescription description, int? ownerId = null)
        where TDescription : unmanaged, ITwoBodyConstraintDescription<TDescription>
    {
        var handle = _simulation.Solver.Add(bodyA, bodyB, description);
        _allConstraints.Add(handle);
        if (ownerId.HasValue)
        {
            if (!_constraintsByOwner.TryGetValue(ownerId.Value, out var list))
            {
                list = new List<ConstraintHandle>();
                _constraintsByOwner[ownerId.Value] = list;
            }
            list.Add(handle);
        }
        return handle;
    }

    public ConstraintHandle AddConstraint<TDescription>(BodyHandle body, TDescription description, int? ownerId = null)
        where TDescription : unmanaged, IOneBodyConstraintDescription<TDescription>
    {
        var handle = _simulation.Solver.Add(body, description);
        _allConstraints.Add(handle);
        if (ownerId.HasValue)
        {
            if (!_constraintsByOwner.TryGetValue(ownerId.Value, out var list))
            {
                list = new List<ConstraintHandle>();
                _constraintsByOwner[ownerId.Value] = list;
            }
            list.Add(handle);
        }
        return handle;
    }

    public void UpdateConstraint<TDescription>(ConstraintHandle handle, TDescription description)
        where TDescription : unmanaged, IConstraintDescription<TDescription>
    {
        _simulation.Solver.ApplyDescription(handle, description);
    }

    public void RemoveConstraint(ConstraintHandle handle)
    {
        if (_simulation.Solver.ConstraintExists(handle))
        {
            _simulation.Solver.Remove(handle);
        }
        _allConstraints.Remove(handle);
    }

    public void RemoveConstraintsForOwner(int ownerId)
    {
        if (_constraintsByOwner.TryGetValue(ownerId, out var list))
        {
            foreach (var handle in list)
            {
                if (_simulation.Solver.ConstraintExists(handle))
                {
                    _simulation.Solver.Remove(handle);
                }
                _allConstraints.Remove(handle);
            }
            _constraintsByOwner.Remove(ownerId);
        }
    }
}
