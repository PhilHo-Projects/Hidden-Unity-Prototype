using UnityEngine;
using Core.Utility;

public class VFXTester : MonoBehaviour
{
    [SerializeField] private VFXPrefabContainer prefabContainer;
    [SerializeField] private float effectScale = 1.0f;
    
    private VFX _vfx;
    
    private void Start()
    {
        _vfx = new VFX(prefabContainer);
        _vfx.InitializePool();

        _vfx.TestCycleAllEffects(transform, effectScale);
    }
}