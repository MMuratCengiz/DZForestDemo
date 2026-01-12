using DenOfIz;

namespace NiziKit.Assets;

public sealed class GeometryBuilder
{
    public GeometryData BuildBox(float width, float height, float depth, BuildDesc flags = 0)
    {
        var desc = new BoxDesc
        {
            Width = width,
            Height = height,
            Depth = depth,
            BuildDesc = (uint)flags
        };
        return Geometry.BuildBox(ref desc);
    }

    public GeometryData BuildSphere(float diameter, uint tessellation = 16, BuildDesc flags = 0)
    {
        var desc = new SphereDesc
        {
            Diameter = diameter,
            Tessellation = tessellation,
            BuildDesc = (uint)flags
        };
        return Geometry.BuildSphere(ref desc);
    }

    public GeometryData BuildGeoSphere(float diameter, uint tessellation = 3, BuildDesc flags = 0)
    {
        var desc = new GeoSphereDesc
        {
            Diameter = diameter,
            Tessellation = tessellation,
            BuildDesc = (uint)flags
        };
        return Geometry.BuildGeoSphere(ref desc);
    }

    public GeometryData BuildCylinder(float diameter, float height, uint tessellation = 16, BuildDesc flags = 0)
    {
        var desc = new CylinderDesc
        {
            Diameter = diameter,
            Height = height,
            Tessellation = tessellation,
            BuildDesc = (uint)flags
        };
        return Geometry.BuildCylinder(ref desc);
    }

    public GeometryData BuildCone(float diameter, float height, uint tessellation = 16, BuildDesc flags = 0)
    {
        var desc = new ConeDesc
        {
            Diameter = diameter,
            Height = height,
            Tessellation = tessellation,
            BuildDesc = (uint)flags
        };
        return Geometry.BuildCone(ref desc);
    }

    public GeometryData BuildTorus(float diameter, float thickness, uint tessellation = 16, BuildDesc flags = 0)
    {
        var desc = new TorusDesc
        {
            Diameter = diameter,
            Thickness = thickness,
            Tessellation = tessellation,
            BuildDesc = (uint)flags
        };
        return Geometry.BuildTorus(ref desc);
    }

    public GeometryData BuildTetrahedron(BuildDesc flags = 0)
    {
        var desc = new TetrahedronDesc { BuildDesc = (uint)flags };
        return Geometry.BuildTetrahedron(ref desc);
    }

    public GeometryData BuildOctahedron(BuildDesc flags = 0)
    {
        var desc = new OctahedronDesc { BuildDesc = (uint)flags };
        return Geometry.BuildOctahedron(ref desc);
    }

    public GeometryData BuildDodecahedron(BuildDesc flags = 0)
    {
        var desc = new DodecahedronDesc { BuildDesc = (uint)flags };
        return Geometry.BuildDodecahedron(ref desc);
    }

    public GeometryData BuildIcosahedron(BuildDesc flags = 0)
    {
        var desc = new IcosahedronDesc { BuildDesc = (uint)flags };
        return Geometry.BuildIcosahedron(ref desc);
    }
}