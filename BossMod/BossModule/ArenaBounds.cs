﻿namespace BossMod;

// note: if arena bounds are changed, new instance is recreated
// max approx error can change without recreating the instance
// you can use hash-code to cache clipping results - it will change whenever anything in the instance changes
public abstract class ArenaBounds(WPos center, float halfSize)
{
    public WPos Center { get; init; } = center;
    public float HalfSize { get; init; } = halfSize; // largest horizontal/vertical dimension: radius for circle, max of width/height for rect

    // fields below are used for clipping
    public float MaxApproxError { get; private set; }

    private readonly Clip2D _clipper = new();
    public IEnumerable<WPos> ClipPoly => _clipper.ClipPoly;

    public List<(WPos, WPos, WPos)> ClipAndTriangulate(ClipperLib.PolyTree poly) => _clipper.ClipAndTriangulate(poly);
    public List<(WPos, WPos, WPos)> ClipAndTriangulate(IEnumerable<WPos> poly) => _clipper.ClipAndTriangulate(poly);

    private float _screenHalfSize;
    public float ScreenHalfSize
    {
        get => _screenHalfSize;
        set
        {
            if (_screenHalfSize != value)
            {
                _screenHalfSize = value;
                MaxApproxError = CurveApprox.ScreenError / value * HalfSize;
                _clipper.ClipPoly = BuildClipPoly();
            }
        }
    }

    public abstract IEnumerable<WPos> BuildClipPoly(float offset = 0); // positive offset increases area, negative decreases
    public abstract Pathfinding.Map BuildMap(float resolution = 0.5f);
    public abstract bool Contains(WPos p);
    public abstract float IntersectRay(WPos origin, WDir dir);
    public abstract WDir ClampToBounds(WDir offset, float scale = 1);
    public WPos ClampToBounds(WPos position) => Center + ClampToBounds(position - Center);

    // functions for clipping various shapes to bounds
    public List<(WPos, WPos, WPos)> ClipAndTriangulateCone(WPos center, float innerRadius, float outerRadius, Angle centerDirection, Angle halfAngle)
    {
        // TODO: think of a better way to do that (analytical clipping?)
        if (innerRadius >= outerRadius || innerRadius < 0 || halfAngle.Rad <= 0)
            return [];

        bool fullCircle = halfAngle.Rad >= MathF.PI;
        bool donut = innerRadius > 0;
        var points = (donut, fullCircle) switch
        {
            (false, false) => CurveApprox.CircleSector(center, outerRadius, centerDirection - halfAngle, centerDirection + halfAngle, MaxApproxError),
            (false, true) => CurveApprox.Circle(center, outerRadius, MaxApproxError),
            (true, false) => CurveApprox.DonutSector(center, innerRadius, outerRadius, centerDirection - halfAngle, centerDirection + halfAngle, MaxApproxError),
            (true, true) => CurveApprox.Donut(center, innerRadius, outerRadius, MaxApproxError),
        };
        return ClipAndTriangulate(points);
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateCircle(WPos center, float radius)
    {
        return ClipAndTriangulate(CurveApprox.Circle(center, radius, MaxApproxError));
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateDonut(WPos center, float innerRadius, float outerRadius)
    {
        if (innerRadius >= outerRadius || innerRadius < 0)
            return [];
        return ClipAndTriangulate(CurveApprox.Donut(center, innerRadius, outerRadius, MaxApproxError));
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateTri(WPos a, WPos b, WPos c)
    {
        return ClipAndTriangulate([a, b, c]);
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateIsoscelesTri(WPos apex, WDir height, WDir halfBase)
    {
        return ClipAndTriangulateTri(apex, apex + height + halfBase, apex + height - halfBase);
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateIsoscelesTri(WPos apex, Angle direction, Angle halfAngle, float height)
    {
        var dir = direction.ToDirection();
        var normal = dir.OrthoL();
        return ClipAndTriangulateIsoscelesTri(apex, height * dir, height * halfAngle.Tan() * normal);
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateRect(WPos origin, WDir direction, float lenFront, float lenBack, float halfWidth)
    {
        var side = halfWidth * direction.OrthoR();
        var front = origin + lenFront * direction;
        var back = origin - lenBack * direction;
        return ClipAndTriangulate([front + side, front - side, back - side, back + side]);
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateRect(WPos origin, Angle direction, float lenFront, float lenBack, float halfWidth)
    {
        return ClipAndTriangulateRect(origin, direction.ToDirection(), lenFront, lenBack, halfWidth);
    }

    public List<(WPos, WPos, WPos)> ClipAndTriangulateRect(WPos start, WPos end, float halfWidth)
    {
        var dir = (end - start).Normalized();
        var side = halfWidth * dir.OrthoR();
        return ClipAndTriangulate([start + side, start - side, end - side, end + side]);
    }

    public static (float, float, WPos) CalculateHalfSizeAndCenter(List<WPos> points)
    {
        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minZ = float.MaxValue;
        float maxZ = float.MinValue;

        for (int i = 0; i < points.Count; i += 2)
        {
            WPos p = points[i];
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Z < minZ) minZ = p.Z;
            if (p.Z > maxZ) maxZ = p.Z;

            if (i + 1 < points.Count)
            {
                p = points[i + 1];
                if (p.X < minX) minX = p.X;
                if (p.X > maxX) maxX = p.X;
                if (p.Z < minZ) minZ = p.Z;
                if (p.Z > maxZ) maxZ = p.Z;
            }
        }

        float halfWidth = (maxX - minX) / 2;
        float halfHeight = (maxZ - minZ) / 2;
        WPos center = new((minX + maxX) / 2, (minZ + maxZ) / 2);

        return (halfWidth, halfHeight, center);
    }
}

public class ArenaBoundsCircle(WPos center, float radius) : ArenaBounds(center, radius)
{
    public override IEnumerable<WPos> BuildClipPoly(float offset) => CurveApprox.Circle(Center, HalfSize + offset, MaxApproxError);

    public override Pathfinding.Map BuildMap(float resolution)
    {
        var map = new Pathfinding.Map(resolution, Center, HalfSize, HalfSize, new());
        map.BlockPixelsInside(ShapeDistance.InvertedCircle(Center, HalfSize), 0, 0);
        return map;
    }

    public override bool Contains(WPos position) => position.InCircle(Center, HalfSize);
    public override float IntersectRay(WPos origin, WDir dir) => Intersect.RayCircle(origin, dir, Center, HalfSize);

    public override WDir ClampToBounds(WDir offset, float scale)
    {
        var r = HalfSize * scale;
        if (offset.LengthSq() > r * r)
            offset *= r / offset.Length();
        return offset;
    }
}

public class ArenaBoundsSquare(WPos center, float halfWidth) : ArenaBounds(center, halfWidth)
{
    public override IEnumerable<WPos> BuildClipPoly(float offset)
    {
        var s = HalfSize + offset;
        yield return Center + new WDir(s, -s);
        yield return Center + new WDir(s, s);
        yield return Center + new WDir(-s, s);
        yield return Center + new WDir(-s, -s);
    }

    public override Pathfinding.Map BuildMap(float resolution) => new Pathfinding.Map(resolution, Center, HalfSize, HalfSize);
    public override bool Contains(WPos position) => WPos.AlmostEqual(position, Center, HalfSize);
    public override float IntersectRay(WPos origin, WDir dir) => Intersect.RayRect(origin, dir, Center, new(0, 1), HalfSize, HalfSize);

    public override WDir ClampToBounds(WDir offset, float scale)
    {
        var wh = HalfSize * scale;
        if (Math.Abs(offset.X) > wh)
            offset *= wh / Math.Abs(offset.X);
        if (Math.Abs(offset.Z) > wh)
            offset *= wh / Math.Abs(offset.Z);
        return offset;
    }
}

public class ArenaBoundsRect(WPos center, float halfWidth, float halfHeight, Angle rotation = new()) : ArenaBounds(center, MathF.Max(halfWidth, halfHeight))
{
    public float HalfWidth { get; init; } = halfWidth;
    public float HalfHeight { get; init; } = halfHeight;
    public Angle Rotation { get; init; } = rotation;

    public override IEnumerable<WPos> BuildClipPoly(float offset)
    {
        var n = Rotation.ToDirection(); // local 'z' axis
        var dx = n.OrthoL() * (HalfWidth + offset);
        var dz = n * (HalfHeight + offset);
        yield return Center + dx - dz;
        yield return Center + dx + dz;
        yield return Center - dx + dz;
        yield return Center - dx - dz;
    }

    public override Pathfinding.Map BuildMap(float resolution) => new Pathfinding.Map(resolution, Center, HalfWidth, HalfHeight, Rotation);
    public override bool Contains(WPos position) => position.InRect(Center, Rotation, HalfHeight, HalfHeight, HalfWidth);
    public override float IntersectRay(WPos origin, WDir dir) => Intersect.RayRect(origin, dir, Center, Rotation.ToDirection(), HalfWidth, HalfHeight);

    public override WDir ClampToBounds(WDir offset, float scale)
    {
        var n = Rotation.ToDirection();
        var dx = MathF.Abs(offset.Dot(n.OrthoL()));
        if (dx > HalfWidth * scale)
            offset *= HalfWidth * scale / dx;
        var dy = MathF.Abs(offset.Dot(n));
        if (dy > HalfHeight * scale)
            offset *= HalfHeight * scale / dy;
        return offset;
    }
}

//should work for any non self-intersecting polygon with a list of points
public class ArenaBoundsPolygon : ArenaBounds
{
    public float HalfWidth { get; private set; }
    public float HalfHeight { get; private set; }
    public readonly List<WPos> Points;

    public ArenaBoundsPolygon(List<WPos> points) : base(CalculateHalfSizeAndCenter(points).Item3, MathF.Max(CalculateHalfSizeAndCenter(points).Item1, CalculateHalfSizeAndCenter(points).Item2))
    {
        Points = points;
        (HalfWidth, HalfHeight, Center) = CalculateHalfSizeAndCenter(points);
    }

    public override IEnumerable<WPos> BuildClipPoly(float offset) => Points;

    public override bool Contains(WPos position) => position.InPolygon(Points);

    public override Pathfinding.Map BuildMap(float resolution)
    {
        var map = new Pathfinding.Map(resolution, CalculateHalfSizeAndCenter(Points).Item3, CalculateHalfSizeAndCenter(Points).Item1, CalculateHalfSizeAndCenter(Points).Item2);
        map.BlockPixelsInside(ShapeDistance.InvertedPolygon(Points), 0, 0);
        return map;
    }

    public override float IntersectRay(WPos origin, WDir dir)
    {
        float minDistance = float.MaxValue;
        for (int i = 0; i < Points.Count; i++)
        {
            int j = (i + 1) % Points.Count;
            WPos p0 = Points[i];
            WPos p1 = Points[j];
            float distance = Intersect.RaySegment(origin, dir, p0, p1);
            if (distance < minDistance)
                minDistance = distance;
        }
        return minDistance;
    }

    public override WDir ClampToBounds(WDir offset, float scale)
    {
        float minDistance = float.MaxValue;
        WPos closestPoint = default;

        for (int i = 0; i < Points.Count; i++)
        {
            int j = (i + 1) % Points.Count;
            WPos p0 = Points[i];
            WPos p1 = Points[j];
            WPos pointOnSegment = Intersect.ClosestPointOnSegment(p0, p1, Center + offset);
            float distance = (pointOnSegment - (Center + offset)).Length();

            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = pointOnSegment;
            }
        }

        return closestPoint - Center;
    }
}
