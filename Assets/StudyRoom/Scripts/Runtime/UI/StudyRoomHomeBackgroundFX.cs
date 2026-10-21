using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class StudyRoomHomeBackgroundFX : MonoBehaviour
{
    public GameObject prefab;
    public Vector3 renderPosition = new Vector3(20000, 20000, 20000);
    private GameObject rig;
    private Camera fxCamera;
    private RenderTexture texture;
    private RawImage image;

    private void Start()
    {
        if (!prefab) return;
        image = GetComponent<RawImage>();
        rig = Instantiate(prefab);
        rig.name = "Home FX Render Rig";
        rig.transform.position = renderPosition;
        rig.transform.rotation = Quaternion.identity;
        rig.transform.localScale = Vector3.one;
        // Keep the render rig with its owner when a loading canvas survives a scene change.
        rig.transform.SetParent(transform, true);
        foreach (var child in rig.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 30;
        // The supplied prefab includes a global volume and audio listener intended for its demo scene.
        foreach (var volume in rig.GetComponentsInChildren<UnityEngine.Rendering.Volume>(true)) volume.enabled = false;
        foreach (var listener in rig.GetComponentsInChildren<AudioListener>(true)) listener.enabled = false;
        fxCamera = rig.GetComponentInChildren<Camera>();
        if (!fxCamera) { rig.SetActive(false); return; }
        fxCamera.tag = "Untagged";
        fxCamera.cullingMask = 1 << 30;
        fxCamera.clearFlags = CameraClearFlags.SolidColor;
        var backgroundColor = fxCamera.backgroundColor;
        backgroundColor.a = 1f;
        fxCamera.backgroundColor = backgroundColor;
        image.color = Color.white;
        ResizeTexture();
        foreach (var particles in rig.GetComponentsInChildren<ParticleSystem>())
        {
            var main = particles.main;
            main.useUnscaledTime = true;
            particles.Simulate(5f, false, true);
            particles.Play();
        }
    }

    private void Update() { if (fxCamera) ResizeTexture(); }

    private void ResizeTexture()
    {
        int width = Mathf.Clamp(Screen.width, 1, 1920);
        int height = Mathf.Max(1, Mathf.RoundToInt(width * (float)Screen.height / Mathf.Max(1, Screen.width)));
        if (texture && texture.width == width && texture.height == height) return;
        ReleaseTexture();
        texture = new RenderTexture(width, height, 24) { name = "Home Background FX" };
        texture.Create();
        fxCamera.targetTexture = texture;
        image.texture = texture;
    }

    private void ReleaseTexture()
    {
        if (!texture) return;
        if (fxCamera) fxCamera.targetTexture = null;
        texture.Release();
        Destroy(texture);
    }

    private void OnEnable() { if (rig) rig.SetActive(true); }
    private void OnDisable() { if (rig) rig.SetActive(false); }
    private void OnDestroy()
    {
        ReleaseTexture();
        if (rig) Destroy(rig);
    }
}
