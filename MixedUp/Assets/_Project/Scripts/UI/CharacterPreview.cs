using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MixedUp
{
    /// <summary>
    /// Shows the player's character inside the settings menu: a tiny studio far away from the level (own camera,
    /// character copy that follows the saved colours) rendered into a RenderTexture on a RawImage. Drag to turn it.
    /// The studio exists only while the panel is visible, and it is lit by the scene's sun so it matches the world.
    /// </summary>
    public class CharacterPreview : MonoBehaviour, IDragHandler
    {
        public GameObject characterPrefab;
        public RawImage target;
        public int width = 600, height = 760;
        public float cameraDistance = 4f;
        public Vector3 focusPoint = new Vector3(0f, 0.72f, 0f);
        public float fieldOfView = 24f;
        [Tooltip("How far the character sways left and right while idle.")]
        public float swayDegrees = 22f;

        static int studioCount;

        Camera studioCamera;
        RenderTexture texture;
        GameObject studio, character;
        float litYaw, dragYaw, lastDragTime;

        public GameObject Character => character;
        public Camera StudioCamera => studioCamera;

        void OnEnable() => Build();
        void OnDisable() => Teardown();

        void Build()
        {
            if (characterPrefab == null || target == null) return;
            Teardown();

            studio = new GameObject("CharacterStudio");
            studio.hideFlags = HideFlags.DontSave;
            // Far from anything in the level, and a different spot for every studio in case two panels exist.
            studio.transform.position = new Vector3(6000f + 25f * studioCount++, 3000f, 6000f);

            character = Instantiate(characterPrefab, studio.transform);
            character.transform.localPosition = Vector3.zero;

            // The front of the character faces the sun, so the face is the lit side.
            litYaw = SunFacingYaw();

            // Without a graphics device (batch mode with -nographics) there is nothing to render into.
            bool canRender = SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null;
            if (canRender)
            {
                texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { name = "CharacterPreview", antiAliasing = 4 };
                texture.Create();
                target.texture = texture;
            }

            var cameraObject = new GameObject("StudioCamera");
            cameraObject.transform.SetParent(studio.transform, false);
            studioCamera = cameraObject.AddComponent<Camera>();
            studioCamera.clearFlags = CameraClearFlags.SolidColor;
            studioCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            studioCamera.fieldOfView = fieldOfView;
            studioCamera.nearClipPlane = 0.1f;
            studioCamera.farClipPlane = 30f;
            studioCamera.targetTexture = texture;
            studioCamera.enabled = canRender;
            studioCamera.allowHDR = false;
            studioCamera.allowMSAA = true;
            PlaceCamera();
            ApplyYaw();
        }

        void Teardown()
        {
            if (target != null && target.texture == texture) target.texture = null;
            if (texture != null)
            {
                texture.Release();
                Destroy(texture);
                texture = null;
            }
            if (studio != null) Destroy(studio);
            studio = null;
            character = null;
            studioCamera = null;
        }

        static float SunFacingYaw()
        {
            var sun = RenderSettings.sun;
            if (sun == null)
            {
                foreach (var light in FindObjectsByType<Light>())
                {
                    if (light.type != LightType.Directional) continue;
                    sun = light;
                    break;
                }
            }
            if (sun == null) return 0f;

            Vector3 towardSun = -sun.transform.forward;
            towardSun.y = 0f;
            return towardSun.sqrMagnitude < 0.001f ? 0f : Mathf.Atan2(towardSun.x, towardSun.z) * Mathf.Rad2Deg;
        }

        void PlaceCamera()
        {
            // In front of the character, a little above, looking at the focus point.
            Vector3 focus = studio.transform.position + focusPoint;
            Vector3 position = focus + Quaternion.Euler(0f, litYaw, 0f) * new Vector3(0f, 0.28f, cameraDistance);
            studioCamera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position));
        }

        void ApplyYaw()
        {
            float idle = Mathf.Sin(Time.unscaledTime * 0.7f) * swayDegrees;
            if (Time.unscaledTime - lastDragTime > 2.5f) dragYaw = Mathf.MoveTowards(dragYaw, 0f, 90f * Time.unscaledDeltaTime);
            character.transform.rotation = Quaternion.Euler(0f, litYaw + dragYaw + (Time.unscaledTime - lastDragTime > 2.5f ? idle : 0f), 0f);
        }

        void Update()
        {
            if (character == null) return;
            ApplyYaw();
        }

        public void OnDrag(PointerEventData eventData)
        {
            dragYaw -= eventData.delta.x * 0.6f;
            lastDragTime = Time.unscaledTime;
        }
    }
}
