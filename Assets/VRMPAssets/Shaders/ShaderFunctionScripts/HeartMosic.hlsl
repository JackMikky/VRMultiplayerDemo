#ifndef HEARTMOSIC_INCLUDE
#define HEARTMOSIC_INCLUDE

float inHeart(float2 tex, float size)
{
    if (size == 0.0)
    {
        return 0.0;
    }
    tex = float2(tex.x, -tex.y);
    tex /= size;
    tex += float2(1.25, -1.25);
                
    float s = tex.x * tex.x - tex.y * -tex.y - 1.0;
    float f1 = s * s * s;
    float f2 = tex.x * tex.x * tex.y * tex.y * tex.y * -1.0;
    return step(f1, f2);
}

#endif // HEARTMOSIC_INCLUDE