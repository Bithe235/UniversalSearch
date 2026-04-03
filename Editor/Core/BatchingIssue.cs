using System;

namespace BatchSight.Core
{
    public enum BatchingIssue
    {
        None,
        SkinnedMeshRenderer,
        MultiMaterial,
        NotStatic,
        DifferentLightmap,
        SRPIncompatible,
        MaterialPropertyBlock,
        UniqueMaterialInstance,
        PrefabInstance,
        LODMismatch,
        ShadowMismatch
    }
}
