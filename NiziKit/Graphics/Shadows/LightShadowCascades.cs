using System.Numerics;

namespace NiziKit.Graphics.Shadows;

/// <summary>
/// Computes per-cascade light view-projection matrices for cascaded shadow maps.
/// Uses a bounding-sphere approach with texel-snapping for stable, shimmer-free shadows.
/// </summary>
public static class LightShadowCascades
{
    /// <summary>
    /// Compute <paramref name="numCascades"/> light view-projection matrices.
    /// </summary>
    /// <param name="cameraView">Camera view matrix (world → view).</param>
    /// <param name="cameraProjection">Camera projection matrix.</param>
    /// <param name="lightDirection">World-space direction the light rays travel (e.g. (0,-1,0) for a sun overhead).</param>
    /// <param name="numCascades">Number of cascades to produce.</param>
    /// <param name="splitDistances">
    /// Array of <c>numCascades+1</c> linear view-space depths.
    /// Index 0 = camera near, index N = camera far.
    /// </param>
    /// <param name="shadowTextureSize">Shadow map texel resolution (width == height assumed).</param>
    /// <param name="zMultiplier">
    /// Multiplier applied to the bounding-sphere radius when positioning the light "eye",
    /// ensuring shadow casters behind the visible frustum are still captured.
    /// </param>
    public static Matrix4x4[] Compute(
        Matrix4x4 cameraView,
        Matrix4x4 cameraProjection,
        Vector3 lightDirection,
        int numCascades,
        float[] splitDistances,
        float shadowTextureSize,
        float zMultiplier = 3.0f)
    {
        Matrix4x4.Invert(cameraView * cameraProjection, out var invVP);

        // Direction FROM scene TOWARDS the light source.
        var dirToLight = Vector3.Normalize(-lightDirection);

        // Choose a stable up vector.
        var up = MathF.Abs(Vector3.Dot(dirToLight, Vector3.UnitY)) > 0.99f
            ? Vector3.UnitZ
            : Vector3.UnitY;

        // Rotation-only world → light-space transform (used for texel snapping).
        // Eye at origin, looking towards dirToLight → +Z in light space = dirToLight direction.
        var lightRotation = Matrix4x4.CreateLookAtLeftHanded(Vector3.Zero, dirToLight, up);
        Matrix4x4.Invert(lightRotation, out var invLightRotation);

        // Full camera frustum corners in world space (D3D NDC: Z ∈ [0,1]).
        var frustumCorners = GetFrustumCornersWorldSpace(invVP);

        var nearPlane = splitDistances[0];

        // invVP was built from cameraProjection whose far plane (camFarPlane) may differ from
        // splitDistances[numCascades] when shadow distance is capped (e.g. ShadowMaxDistance < cam.FarPlane).
        // The far frustum corners (NDC Z=1) correspond to camFarPlane, so the lerp parameter t
        // must be normalised against camFarPlane, not against splitDistances[numCascades].
        //
        // For a D3D left-handed perspective matrix (System.Numerics row-major):
        //   M33 = far / (far - near)
        //   M43 = -near * far / (far - near)
        // → camFarPlane = -M43 / (M33 - 1)
        var camFarPlane = -cameraProjection.M43 / (cameraProjection.M33 - 1f);
        var depthRange = camFarPlane - nearPlane;

        var matrices = new Matrix4x4[numCascades];

        for (var i = 0; i < numCascades; i++)
        {
            var cascadeNear = splitDistances[i];
            var cascadeFar = splitDistances[i + 1];

            // Interpolate full-frustum corners to get this cascade's sub-frustum.
            var tNear = (cascadeNear - nearPlane) / depthRange;
            var tFar = (cascadeFar - nearPlane) / depthRange;

            Span<Vector3> subCorners = stackalloc Vector3[8];
            for (var j = 0; j < 4; j++)
            {
                var nc = frustumCorners[j];      // near-plane corner
                var fc = frustumCorners[j + 4];  // far-plane corner
                subCorners[j] = Vector3.Lerp(nc, fc, tNear);
                subCorners[j + 4] = Vector3.Lerp(nc, fc, tFar);
            }

            // Bounding sphere: use average of corners as centre.
            var center = Vector3.Zero;
            foreach (var c in subCorners) center += c;
            center /= 8;

            // Grow the radius to fully enclose all 8 corners (sphere is stable across rotations).
            var radius = 0f;
            foreach (var c in subCorners)
                radius = MathF.Max(radius, Vector3.Distance(center, c));

            // Round UP to the next integer to avoid sub-texel radius changes.
            radius = MathF.Ceiling(radius);

            // ── Texel snapping ──────────────────────────────────────────────────────────
            // Snap the center to the texel grid in light space so the shadow map
            // doesn't shift by fractional texels between frames (eliminates shimmering).
            var texelSize = 2f * radius / shadowTextureSize;
            var centerLS = Vector3.Transform(center, lightRotation);
            centerLS.X = MathF.Floor(centerLS.X / texelSize) * texelSize;
            centerLS.Y = MathF.Floor(centerLS.Y / texelSize) * texelSize;
            var snappedCenter = Vector3.Transform(centerLS, invLightRotation);
            // ────────────────────────────────────────────────────────────────────────────

            // Place the light "camera" behind the scene along the light direction,
            // far enough that shadow casters beyond the visible frustum are included.
            var eyeDistance = radius * zMultiplier;
            var eye = snappedCenter + dirToLight * eyeDistance;
            var lightView = Matrix4x4.CreateLookAtLeftHanded(eye, snappedCenter, up);

            // Tight orthographic projection: near=0 to capture all shadow casters between
            // the light eye and the scene; far = just past the scene front.
            // This maximises depth-buffer precision vs. the old "eyeDistance * 2" far plane.
            var lightProj = Matrix4x4.CreateOrthographicOffCenterLeftHanded(
                -radius, radius,
                -radius, radius,
                0f,
                eyeDistance + radius * 1.1f);

            matrices[i] = lightView * lightProj;
        }

        return matrices;
    }

    /// <summary>
    /// Computes practical (log/linear blend) split distances for <paramref name="numCascades"/> cascades.
    /// </summary>
    /// <param name="nearPlane">Camera near plane.</param>
    /// <param name="farPlane">Camera far plane.</param>
    /// <param name="numCascades">Number of cascades.</param>
    /// <param name="lambda">Blend factor: 0 = fully linear, 1 = fully logarithmic.</param>
    /// <returns>Array of <c>numCascades + 1</c> split distances.</returns>
    public static float[] ComputeSplitDistances(float nearPlane, float farPlane, int numCascades, float lambda = 0.75f)
    {
        var distances = new float[numCascades + 1];
        distances[0] = nearPlane;
        distances[numCascades] = farPlane;

        for (var i = 1; i < numCascades; i++)
        {
            var linear = nearPlane + (farPlane - nearPlane) * i / numCascades;
            var log = nearPlane * MathF.Pow(farPlane / nearPlane, (float)i / numCascades);
            distances[i] = lambda * log + (1f - lambda) * linear;
        }

        return distances;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────────

    private static Vector3[] GetFrustumCornersWorldSpace(Matrix4x4 invViewProj)
    {
        // D3D / left-handed NDC: XY ∈ [-1,+1], Z ∈ [0,1].
        ReadOnlySpan<Vector4> ndc = stackalloc Vector4[]
        {
            new(-1,  1, 0, 1), new( 1,  1, 0, 1), // near: top-left,     top-right
            new(-1, -1, 0, 1), new( 1, -1, 0, 1), // near: bottom-left,  bottom-right
            new(-1,  1, 1, 1), new( 1,  1, 1, 1), // far:  top-left,     top-right
            new(-1, -1, 1, 1), new( 1, -1, 1, 1), // far:  bottom-left,  bottom-right
        };

        var corners = new Vector3[8];
        for (var i = 0; i < 8; i++)
        {
            var world = Vector4.Transform(ndc[i], invViewProj);
            corners[i] = new Vector3(world.X, world.Y, world.Z) / world.W;
        }
        return corners;
    }
}
