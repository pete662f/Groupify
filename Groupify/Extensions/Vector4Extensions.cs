using System.Numerics;

namespace Groupify.Extensions;

public class Vector4Extensions
{
    public static float Sum(Vector4 v) => v.X + v.Y + v.Z + v.W;
}