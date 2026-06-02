using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace Hanzzz.MeshDemolisher
{

public static class Constant
{
    public const double EPSILON_D = 0.0000001d;
    public const float EPSILON_F = 0.0001f;
    //public const double EPSILON_F = 0f; // exact predicates don't care about rounding errors
}

public interface IPointLocation
{
    public abstract int GetAxisCount();
    public abstract double GetAxis(int n);
    public abstract Point3D ToPoint3D();
}

public class Point2D
{
    public double x;
    public double y;

    public Point2D(double x,double y)
    {
        this.x = x;
        this.y = y;
    }
}

public class Point3D : IEquatable<Point3D>, IPointLocation
{
    public double x;
    public double y;
    public double z;

    public Point3D(double x,double y,double z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public Point3D(Vector3 point)
    {
        this.x = point.x;
        this.y = point.z;
        this.z = point.y;
    }
    
    public bool Equals(Point3D other)
    {
        return Math.Abs(this.x-other.x)<=Constant.EPSILON_D && Math.Abs(this.y-other.y)<=Constant.EPSILON_D && Math.Abs(this.z-other.z)<=Constant.EPSILON_D;
    }

    public int GetAxisCount()
    {
        return 3;
    }
    public double GetAxis(int n)
    {
        switch (n)
        {
            case 0: return x;
            case 1: return y;
            case 2: return z;
            default: throw new Exception();
        }
    }
    public Point3D ToPoint3D()
    {
        return this;
    }

    public Vector3 ToVector3()
    {
        return new Vector3((float)x, (float)z,(float)y);
    }

    public void Normalize()
    {
        double magnitude = Point3D.Magnitude(this);
        x /= magnitude;
        y /= magnitude;
        z /= magnitude;
    }

    public override string ToString()
    {
        return $"{x.ToString("F4")}, {y.ToString("F4")}, {z.ToString("F4")}";
    }

    public static Point3D operator+(Point3D a, Point3D b)
    {
        return new Point3D(a.x+b.x, a.y+b.y, a.z+b.z);
    }
    public static Point3D operator-(Point3D a, Point3D b)
    {
        return new Point3D(a.x-b.x, a.y-b.y, a.z-b.z);
    }
    public static Point3D operator*(Point3D a, Point3D b)
    {
        return new Point3D(a.x*b.x, a.y*b.y, a.z*b.z);
    }
    public static Point3D operator*(Point3D a, double b)
    {
        return new Point3D(a.x*b, a.y*b, a.z*b);
    }
    public static Point3D operator/(Point3D a, double b)
    {
        return new Point3D(a.x/b, a.y/b, a.z/b);
    }

    public static double Dot(Point3D a, Point3D b)
    {
        return a.x*b.x + a.y*b.y + a.z*b.z;
    }
    public static Point3D Cross(Point3D a, Point3D b)
    {
        return new Point3D(a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x);
    }
    public static double SquareMagnitude(Point3D a)
    {
        return a.x*a.x + a.y*a.y + a.z*a.z;
    }
    public static double Magnitude(Point3D a)
    {
        return Math.Sqrt(a.x*a.x + a.y*a.y + a.z*a.z);
    }
}

public class Point3DImplicit : IEquatable<Point3DImplicit>, IPointLocation
{
    public Point3D p0;
    public Point3D p1;
    public double t;

    public Point3DImplicit(Point3D p0, Point3D p1, double t)
    {
        this.p0 = p0;
        this.p1 = p1;
        this.t = t;
    }

    public bool Equals(Point3DImplicit other)
    {
        return (p0.Equals(other.p0) && p1.Equals(other.p1) && t == other.t) || (p0.Equals(other.p1) && p1.Equals(other.p0) && t == 1d-other.t);
    }

    public int GetAxisCount()
    {
        return 3;
    }
    public double GetAxis(int n)
    {
        throw new Exception();
    }
    public double GetFirstAxis(int n)
    {
        switch (n)
        {
            case 0: return p0.x;
            case 1: return p0.y;
            case 2: return p0.z;
            default: throw new Exception();
        }
    }
    public double GetSecondAxis(int n)
    {
        switch (n)
        {
            case 0: return p1.x;
            case 1: return p1.y;
            case 2: return p1.z;
            default: throw new Exception();
        }
    }
    public Point3D ToPoint3D()
    {
        return p0*(1-t)+p1*(t);
    }

    public override string ToString()
    {
        return $"{p0}->{p1}, {t}";
    }
}


public enum Sign
{
    NEGATIVE = -1,
    ZERO = 0,
    POSITIVE = 1,
}


public static class PointComputation
{
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    private const string dllName = "IndirectPredicates_Mac";
#else
    private const string dllName = "IndirectPredicates_Windows";
#endif

    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern void Initialize();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern void Reset();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern void AddExplicitPoint(double[] p);
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern void AddImplicitPoint(double[] p0, double[] p1, double t);
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern int Orient3D();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern int InCircumsphere();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern bool LineCrossInnerTriangle();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern bool LineCrossTriangle();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern int DotProductSign();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern bool SegmentCrossInnerSegment();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern bool PointInInnerSegment();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern bool InnerSegmentCrossInnerTriangle();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern bool PointInInnerTriangle();
    [System.Runtime.InteropServices.DllImport(dllName)]
    private static extern bool PointInTriangle();
#else

    private static readonly List<Point3D> managedPoints = new List<Point3D>();

    private static void Initialize()
    {
    }

    private static void ResetManagedPoints()
    {
        managedPoints.Clear();
    }

    private static int ManagedOrient3D()
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        Point3D d = managedPoints[3];
        double det = Point3D.Dot(a - d, Point3D.Cross(b - d, c - d));
        if(det > Constant.EPSILON_D)
        {
            return -1;
        }
        if(det < -Constant.EPSILON_D)
        {
            return 1;
        }
        return 0;
    }

    private static int ManagedInCircumsphere()
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        Point3D d = managedPoints[3];
        Point3D p = managedPoints[4];
        Point3D center = CircumsphereFromFourPoints(a, b, c, d);
        double radiusSquared = Point3D.SquareMagnitude(a - center);
        double delta = Point3D.SquareMagnitude(p - center) - radiusSquared;
        if(delta < -Constant.EPSILON_D)
        {
            return -1;
        }
        if(delta > Constant.EPSILON_D)
        {
            return 1;
        }
        return 0;
    }

    private static int ManagedDotProductSign()
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        double dot = Point3D.Dot(a - c, b - c);
        if(dot < -Constant.EPSILON_D)
        {
            return -1;
        }
        if(dot > Constant.EPSILON_D)
        {
            return 1;
        }
        return 0;
    }

    private static bool ManagedPointInInnerSegment()
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        Point3D bc = c - b;
        double bcLengthSquared = Point3D.SquareMagnitude(bc);
        if(bcLengthSquared <= Constant.EPSILON_D)
        {
            return false;
        }
        double t = Point3D.Dot(a - b, bc) / bcLengthSquared;
        return t > Constant.EPSILON_D && t < 1d - Constant.EPSILON_D;
    }

    private static bool ManagedSegmentCrossInnerSegment()
    {
        return ManagedSegmentsCross(true);
    }

    private static bool ManagedPointInInnerTriangle()
    {
        return ManagedPointInTriangle(true);
    }

    private static bool ManagedPointInTriangle()
    {
        return ManagedPointInTriangle(false);
    }

    private static bool ManagedLineCrossTriangle()
    {
        return ManagedLineCrossTriangle(false);
    }

    private static bool ManagedLineCrossInnerTriangle()
    {
        return ManagedLineCrossTriangle(true);
    }

    private static bool ManagedInnerSegmentCrossInnerTriangle()
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        Point3D d = managedPoints[3];
        Point3D e = managedPoints[4];
        var (hit, t) = LinePlaneIntersection(a, b, c, d, e);
        if(t <= Constant.EPSILON_D || t >= 1d - Constant.EPSILON_D)
        {
            return false;
        }
        return ManagedPointInTriangle(hit, c, d, e, true);
    }

    private static bool ManagedSegmentsCross(bool requireInteriorIntersection)
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        Point3D d = managedPoints[3];
        int o1 = ManagedOrientOnPoints(a, b, c, d);
        int o2 = ManagedOrientOnPoints(a, b, d, c);
        int o3 = ManagedOrientOnPoints(c, d, a, b);
        int o4 = ManagedOrientOnPoints(c, d, b, a);
        if(requireInteriorIntersection)
        {
            return o1 * o2 < 0 && o3 * o4 < 0;
        }
        return o1 * o2 <= 0 && o3 * o4 <= 0 && (o1 != 0 || o2 != 0 || o3 != 0 || o4 != 0);
    }

    private static bool ManagedPointInTriangle(bool requireInterior)
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        Point3D d = managedPoints[3];
        return ManagedPointInTriangle(a, b, c, d, requireInterior);
    }

    private static bool ManagedPointInTriangle(Point3D point, Point3D b, Point3D c, Point3D d, bool requireInterior)
    {
        int o0 = ManagedOrientOnPoints(b, c, d, point);
        int o1 = ManagedOrientOnPoints(c, d, b, point);
        int o2 = ManagedOrientOnPoints(d, b, c, point);
        if(requireInterior)
        {
            return o0 > 0 && o1 > 0 && o2 > 0;
        }
        return o0 >= 0 && o1 >= 0 && o2 >= 0;
    }

    private static bool ManagedLineCrossTriangle(bool requireInterior)
    {
        Point3D a = managedPoints[0];
        Point3D b = managedPoints[1];
        Point3D c = managedPoints[2];
        Point3D d = managedPoints[3];
        Point3D e = managedPoints[4];
        var (hit, t) = LinePlaneIntersection(a, b, c, d, e);
        if(requireInterior)
        {
            if(t <= Constant.EPSILON_D || t >= 1d - Constant.EPSILON_D)
            {
                return false;
            }
            return ManagedPointInTriangle(hit, c, d, e, true);
        }
        if(t < -Constant.EPSILON_D || t > 1d + Constant.EPSILON_D)
        {
            return false;
        }
        return ManagedPointInTriangle(hit, c, d, e, false);
    }

    private static int ManagedOrientOnPoints(Point3D a, Point3D b, Point3D c, Point3D d)
    {
        double det = Point3D.Dot(a - d, Point3D.Cross(b - d, c - d));
        if(det > Constant.EPSILON_D)
        {
            return -1;
        }
        if(det < -Constant.EPSILON_D)
        {
            return 1;
        }
        return 0;
    }
#endif

    private static double[] t0 = new double[3];
    private static double[] t1 = new double[3];


    private static void ResetPoints()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        Reset();
#else
        ResetManagedPoints();
#endif
    }

    private static void AddPoint(IPointLocation p)
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        if(p is Point3D)
        {
            for(int i=0; i<3; i++)
            {
                t0[i] = ((Point3D)p).GetAxis(i);
            }
            AddExplicitPoint(t0);
        }
        else
        {
            for(int i=0; i<3; i++)
            {
                t0[i] = ((Point3DImplicit)p).GetFirstAxis(i);
                t1[i] = ((Point3DImplicit)p).GetSecondAxis(i);
            }
            AddImplicitPoint(t0,t1, ((Point3DImplicit)p).t);
        }
#else
        managedPoints.Add(p.ToPoint3D());
#endif
    }

    private static int EvaluateOrient3D()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return Orient3D();
#else
        return ManagedOrient3D();
#endif
    }

    private static int EvaluateInCircumsphere()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return InCircumsphere();
#else
        return ManagedInCircumsphere();
#endif
    }

    private static bool EvaluateLineCrossInnerTriangle()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return LineCrossInnerTriangle();
#else
        return ManagedLineCrossInnerTriangle();
#endif
    }

    private static bool EvaluateLineCrossTriangle()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return LineCrossTriangle();
#else
        return ManagedLineCrossTriangle();
#endif
    }

    private static int EvaluateDotProductSign()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return DotProductSign();
#else
        return ManagedDotProductSign();
#endif
    }

    private static bool EvaluatePointInInnerSegment()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return PointInInnerSegment();
#else
        return ManagedPointInInnerSegment();
#endif
    }

    private static bool EvaluateSegmentCrossInnerSegment()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return SegmentCrossInnerSegment();
#else
        return ManagedSegmentCrossInnerSegment();
#endif
    }

    private static bool EvaluateInnerSegmentCrossInnerTriangle()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return InnerSegmentCrossInnerTriangle();
#else
        return ManagedInnerSegmentCrossInnerTriangle();
#endif
    }

    private static bool EvaluatePointInInnerTriangle()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return PointInInnerTriangle();
#else
        return ManagedPointInInnerTriangle();
#endif
    }

    private static bool EvaluatePointInTriangle()
    {
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX || UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX
        return PointInTriangle();
#else
        return ManagedPointInTriangle();
#endif
    }

    public static void Init()
    {
        Initialize();
    }

    // left hand rule, curl fingers from a->b->c, positive if thumb points toward d
    public static Sign Orient(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);

        int res = EvaluateOrient3D(); // this lib decided to use right hand rule
        if(res<0)
        {
            return Sign.POSITIVE;
        }
        if(res>0)
        {
            return Sign.NEGATIVE;
        }
        return Sign.ZERO;
    }

    // positive if p in a,b,c,d; zero if on, negative if out. orient(a,b,c,d) is positive
    public static Sign InCircumsphere(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d, IPointLocation p)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);
        AddPoint(p);

        int res = EvaluateInCircumsphere(); // this lib decided to use right hand rule
        if(res<0)
        {
            return Sign.POSITIVE;
        }
        if(res>0)
        {
            return Sign.NEGATIVE;
        }
        return Sign.ZERO;
    }

    //positive if a->b corsses c,d,e
    public static Sign LineCrossInnerTriangle(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d, IPointLocation e)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);
        AddPoint(e);

        bool res = EvaluateLineCrossInnerTriangle();
        if(res)
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }
    public static Sign LineCrossTriangle(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d, IPointLocation e)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);
        AddPoint(e);

        bool res = EvaluateLineCrossTriangle();
        if(res)
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }

    // sign of (a-c) dot (b-c)
    public static Sign DotProductSign(IPointLocation a, IPointLocation b, IPointLocation c)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        int res = EvaluateDotProductSign();
        if(res<0)
        {
            return Sign.NEGATIVE;
        }
        if(res>0)
        {
            return Sign.POSITIVE;
        }
        return Sign.ZERO;
    }

    // positive if a in inner b->c
    public static Sign PointInInnerSegment(IPointLocation a, IPointLocation b, IPointLocation c)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        bool res = EvaluatePointInInnerSegment();
        if(res)
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }

    // positive if inner a->b crosses inner of c->d. a,b,c,d must be coplanar
    public static Sign SegmentCrossInnerSegment(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);
        bool res = EvaluateSegmentCrossInnerSegment();
        if(res)
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }

    // positive if interior of a->b inersects interior of c,d,e at a single point
    public static Sign InnerSegmentCrossInnerTriangle(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d, IPointLocation e)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);
        AddPoint(e);
        bool res = EvaluateInnerSegmentCrossInnerTriangle();
        if(res)
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }

    // positive if a is in the interior of b, c, d; a,b,c,d must be coplanar
    public static Sign PointInInnerTriangle(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);
        bool res = EvaluatePointInInnerTriangle();
        if(res)
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }

    // positive if a is in b, c, d; a,b,c,d must be coplanar
    public static Sign PointInTriangle(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d)
    {
        ResetPoints();
        AddPoint(a);
        AddPoint(b);
        AddPoint(c);
        AddPoint(d);
        bool res = EvaluatePointInTriangle();
        if(res)
        {
            return Sign.POSITIVE;
        }
        return Sign.NEGATIVE;
    }


    public static Point3D CircumsphereFromFourPoints(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d)
    {
        static double U(IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d, IPointLocation e, IPointLocation f, IPointLocation g, IPointLocation h)
        {
            return (a.GetAxis(2) - b.GetAxis(2)) * (c.GetAxis(0) * d.GetAxis(1) - d.GetAxis(0) * c.GetAxis(1))
                - (e.GetAxis(2) - f.GetAxis(2)) * (g.GetAxis(0) * h.GetAxis(1) - h.GetAxis(0) * g.GetAxis(1));
        }
        static double D(int x, int y, IPointLocation a, IPointLocation b, IPointLocation c)
        {
            return a.GetAxis(x) * (b.GetAxis(y) - c.GetAxis(y)) +
                    b.GetAxis(x) * (c.GetAxis(y) - a.GetAxis(y)) +
                    c.GetAxis(x) * (a.GetAxis(y) - b.GetAxis(y));
        }

        static double E(int x, int y, IPointLocation a, IPointLocation b, IPointLocation c, IPointLocation d, double ra, double rb, double rc, double rd, double uvw)
        {
            return ( ra * D(x, y, b, c, d) - rb * D(x, y, c, d, a) +
                        rc * D(x, y, d, a, b) - rd * D(x, y, a, b, c) ) / uvw;
        }

        double u = U(a, b, c, d, b, c, d, a);
        double v = U(c, d, a, b, d, a, b, c);
        double w = U(a, c, d, b, b, d, a, c);
        double uvw = 2 * (u + v + w);
        if(Math.Abs(uvw) < Constant.EPSILON_D)
        {
            throw new Exception();
        }

        static double sq(IPointLocation p)
        {
            return p.GetAxis(0) * p.GetAxis(0) + p.GetAxis(1) * p.GetAxis(1) + p.GetAxis(2) * p.GetAxis(2);
        }

        int x = 0;
        int y = 1;
        int z = 2;
        double ra = sq(a);
        double rb = sq(b);
        double rc = sq(c);
        double rd = sq(d);
        double x0 = E(y, z, a, b, c, d, ra, rb, rc, rd, uvw);
        double y0 = E(z, x, a, b, c, d, ra, rb, rc, rd, uvw);
        double z0 = E(x, y, a, b, c, d, ra, rb, rc, rd, uvw);

        return new Point3D(x0, y0, z0);
    }

    public static (Point3D origin, Point3D xAxis, Point3D yAxis, List<Point3D> sortedPoints) RotationSort(List<Point3D> points)
    {
        var res = RotationIndexSort(points);
        points = points.Select((x,i)=>points[res.mapping[i]]).ToList();
        return (res.origin, res.xAxis, res.yAxis, points);
    }

    public static (Point3D origin, Point3D xAxis, Point3D yAxis, List<int> mapping) RotationIndexSort(List<Point3D> points)
    {
        if(points.Count < 3)
        {
            throw new Exception();
        }

        return RotationIndexSort(points, Point3D.Cross(points[1]-points[0],points[2]-points[0]));
    }

    public static (Point3D origin, Point3D xAxis, Point3D yAxis, List<int> mapping) RotationIndexSort(List<Point3D> points, Point3D centerPlaneNormal)
    {
        if(points.Count < 3)
        {
            throw new Exception();
        }

        Point3D xAxis;
        if(0f != centerPlaneNormal.x)
        {
            xAxis = new Point3D(-centerPlaneNormal.y/centerPlaneNormal.x,1d,0d);
        }
        else if(0f != centerPlaneNormal.y)
        {
            xAxis = new Point3D(0d,-centerPlaneNormal.z/centerPlaneNormal.y,1d);
        }
        else
        {
            xAxis = new Point3D(1d,0d,-centerPlaneNormal.x/centerPlaneNormal.z);
        }
        xAxis.Normalize();
        Point3D yAxis = Point3D.Cross(centerPlaneNormal, xAxis);
        yAxis.Normalize();
        Point3D origin = points.Aggregate(new Point3D(0d,0d,0d), (sum, next)=>sum+next) / (double)points.Count();

        List<Point2D> points2D = new List<Point2D>();
        foreach(Point3D point in points)
        {
            points2D.Add(new Point2D(Point3D.Dot(xAxis,point-origin),Point3D.Dot(yAxis,point-origin)));
        }

        List<int> mapping = points2D
                    .Select((x,i) => new KeyValuePair<Point2D,int>(x,i))
                    .OrderBy(x=>(x.Key.y<0d?2d*Math.PI : 0d) + Math.Atan2(x.Key.y,x.Key.x))
                    .Select(x=>x.Value)
                    .ToList();
        return (origin, xAxis, yAxis, mapping);
    }

    public static (Point3D, double) LinePlaneIntersection(IPointLocation l0, IPointLocation l1, IPointLocation p0, IPointLocation p1, IPointLocation p2)
    {
        return LinePlaneIntersection(l0.ToPoint3D(),l1.ToPoint3D(),p0.ToPoint3D(),p1.ToPoint3D(),p2.ToPoint3D());
    }

    public static (Point3D, double) LinePlaneIntersection(Point3D l0, Point3D l1, Point3D p0, Point3D p1, Point3D p2)
    {
        Point3D pNormal = Point3D.Cross(p1-p0, p2-p0);
        Point3D u = l1-l0;
        double dot = Point3D.Dot(pNormal, u);

        Point3D v = l0-p0;
        double t = - Point3D.Dot(pNormal, v) / dot;
        u = u * t;
        return (l0 + u, t);
    }
}

}
