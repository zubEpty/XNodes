using UnityEngine;

namespace Dispatch.Gameplay
{
    [ExecuteAlways]
    public class DispatchNodePlayerGlowBinder : MonoBehaviour
    {
        private enum MappingMode
        {
            AutoLargestAxes,
            LocalXZFloor,
            LocalXYQuad,
            LocalYZSide
        }

        private static readonly int PlayerUVId = Shader.PropertyToID("_PlayerUV");
        private static readonly int PlayerMotionId = Shader.PropertyToID("_PlayerMotion");

        [SerializeField] private Renderer surfaceRenderer;
        [SerializeField] private Transform player;
        [SerializeField] private MeshFilter surfaceMesh;
        [SerializeField] private MappingMode mappingMode = MappingMode.AutoLargestAxes;
        [SerializeField] private bool flipU;
        [SerializeField] private bool flipV;
        [SerializeField] private Vector2 uvOffset;
        [SerializeField] private Vector2 uvScale = Vector2.one;
        [SerializeField] private float motionSensitivity = 8f;
        [SerializeField] private float motionFadeSpeed = 8f;

        private MaterialPropertyBlock propertyBlock;
        private Vector2 previousUv;
        private float currentMotion;
        private bool hasPreviousUv;

        private void OnEnable()
        {
            if (surfaceRenderer == null)
            {
                surfaceRenderer = GetComponent<Renderer>();
            }

            if (surfaceMesh == null)
            {
                surfaceMesh = GetComponent<MeshFilter>();
            }

            if (player == null)
            {
                PlayerController playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    player = playerController.transform;
                }
            }
        }

        private void Reset()
        {
            surfaceRenderer = GetComponent<Renderer>();
            surfaceMesh = GetComponent<MeshFilter>();
        }

        private void LateUpdate()
        {
            if (surfaceRenderer == null || player == null)
            {
                return;
            }

            if (surfaceMesh == null)
            {
                surfaceMesh = surfaceRenderer.GetComponent<MeshFilter>();
            }

            if (surfaceMesh == null || surfaceMesh.sharedMesh == null)
            {
                return;
            }

            Bounds meshBounds = surfaceMesh.sharedMesh.bounds;
            Vector3 localPlayer = surfaceRenderer.transform.InverseTransformPoint(player.position);

            Vector2 uv = GetSurfaceUv(meshBounds, localPlayer);

            if (flipU)
            {
                uv.x = 1f - uv.x;
            }

            if (flipV)
            {
                uv.y = 1f - uv.y;
            }

            uv = Vector2.Scale(uv, uvScale) + uvOffset;
            uv = new Vector2(Mathf.Clamp01(uv.x), Mathf.Clamp01(uv.y));

            float targetMotion = 0f;
            if (hasPreviousUv && Application.isPlaying)
            {
                float uvSpeed = Vector2.Distance(uv, previousUv) / Mathf.Max(Time.deltaTime, 0.0001f);
                targetMotion = Mathf.Clamp01(uvSpeed * motionSensitivity);
            }

            currentMotion = Mathf.MoveTowards(currentMotion, targetMotion, Time.deltaTime * motionFadeSpeed);
            previousUv = uv;
            hasPreviousUv = true;

            propertyBlock ??= new MaterialPropertyBlock();
            surfaceRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetVector(PlayerUVId, new Vector4(uv.x, uv.y, 0f, 0f));
            propertyBlock.SetFloat(PlayerMotionId, currentMotion);
            surfaceRenderer.SetPropertyBlock(propertyBlock);
        }

        private Vector2 GetSurfaceUv(Bounds meshBounds, Vector3 localPlayer)
        {
            switch (mappingMode)
            {
                case MappingMode.LocalXZFloor:
                    return new Vector2(
                        Mathf.InverseLerp(meshBounds.min.x, meshBounds.max.x, localPlayer.x),
                        Mathf.InverseLerp(meshBounds.min.z, meshBounds.max.z, localPlayer.z));

                case MappingMode.LocalXYQuad:
                    return new Vector2(
                        Mathf.InverseLerp(meshBounds.min.x, meshBounds.max.x, localPlayer.x),
                        Mathf.InverseLerp(meshBounds.min.y, meshBounds.max.y, localPlayer.y));

                case MappingMode.LocalYZSide:
                    return new Vector2(
                        Mathf.InverseLerp(meshBounds.min.y, meshBounds.max.y, localPlayer.y),
                        Mathf.InverseLerp(meshBounds.min.z, meshBounds.max.z, localPlayer.z));

                default:
                    return GetAutoLargestAxesUv(meshBounds, localPlayer);
            }
        }

        private Vector2 GetAutoLargestAxesUv(Bounds meshBounds, Vector3 localPlayer)
        {
            Vector3 size = meshBounds.size;

            if (size.x >= size.y && size.z >= size.y)
            {
                return new Vector2(
                    Mathf.InverseLerp(meshBounds.min.x, meshBounds.max.x, localPlayer.x),
                    Mathf.InverseLerp(meshBounds.min.z, meshBounds.max.z, localPlayer.z));
            }

            if (size.x >= size.z && size.y >= size.z)
            {
                return new Vector2(
                    Mathf.InverseLerp(meshBounds.min.x, meshBounds.max.x, localPlayer.x),
                    Mathf.InverseLerp(meshBounds.min.y, meshBounds.max.y, localPlayer.y));
            }

            return new Vector2(
                Mathf.InverseLerp(meshBounds.min.y, meshBounds.max.y, localPlayer.y),
                Mathf.InverseLerp(meshBounds.min.z, meshBounds.max.z, localPlayer.z));
        }
    }
}
