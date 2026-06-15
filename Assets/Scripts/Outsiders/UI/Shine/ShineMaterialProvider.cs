using UnityEngine;

namespace Outsiders.UI.Shine
{
    /// <summary>
    /// Provides a single shared material instance for all shine effects.
    /// Avoids creating separate materials per UI element.
    /// The shader reads per-instance data from UV channels (UV1, UV2, UV3).
    /// </summary>
    [CreateAssetMenu(fileName = "ShineMaterialProvider", menuName = "Outsiders/UI/Shine Material Provider")]
    public class ShineMaterialProvider : ScriptableObject
    {
        [SerializeField] private Shader _shineShader;

        private Material _sharedMaterial;

        public Material SharedMaterial
        {
            get
            {
                if (_sharedMaterial == null)
                {
                    CreateMaterial();
                }
                return _sharedMaterial;
            }
        }

        private void CreateMaterial()
        {
            if (_shineShader == null)
            {
                _shineShader = Shader.Find("Outsiders/UI/Shine");
            }

            if (_shineShader == null)
            {
                Debug.LogError("[ShineMaterialProvider] Shine shader not found.");
                return;
            }

            _sharedMaterial = new Material(_shineShader)
            {
                name = "UI-Shine-Shared (Instance)",
                hideFlags = HideFlags.DontSave
            };
        }

        private void OnDisable()
        {
            if (_sharedMaterial != null)
            {
                DestroyImmediate(_sharedMaterial);
                _sharedMaterial = null;
            }
        }
    }
}
