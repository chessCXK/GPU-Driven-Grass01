#ifndef COMMON
#define COMMON

struct ClusterData
{
    half3 center;
    half3 extends;
    uint clusterIndex;
    uint clusterKindIndex;
};

struct ClusterKindData
{
    uint argsIndex;
    uint kindResultStart;
    uint lodNum;
    uint elementNum;
    half4 lodRelative;
};

static const half3 axisMuti[8] =
{
    half3(1, 1, 1),
    half3(1, 1, -1),
    half3(1, -1, 1),
    half3(1, -1, -1),
    half3(-1, 1, 1),
    half3(-1, 1, -1),
    half3(-1, -1, 1),
    half3(-1, -1, -1)
};

half angle(half3 a, half3 b)
{
    half dotProduct = dot(normalize(a), normalize(b));
    half angle = acos(dotProduct) * (180.0 / 3.1415926);
    return angle;
}

#endif