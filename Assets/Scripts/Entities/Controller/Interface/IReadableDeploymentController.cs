using System;
using UnityEngine;

public interface IReadableDeploymentController
{
    bool IsDeployed { get; }
    bool IsPreviewMode { get; }
    Tile? CurrentTile { get; }
    int DeploymentOrder { get; }
    Vector2Int GridPos { get; }

    event Action<Tile> OnTileChanged;
    // event Action<DeployableUnitEntity> OnDeployed;
    // event Action<DeployableUnitEntity> OnUndeployed;  
}