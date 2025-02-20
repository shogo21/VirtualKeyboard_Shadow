using UnityEngine;
using jp.nyatla.nyartoolkit.cs.markersystem;
using NyARUnityUtils;
using ModifiedARUnityUtils;

public class ARMarkerDetector : MonoBehaviour
{

    private Texture2D texture;
    private NyARUnityMarkerSystem ms;
    private ModifiedARUnityTexture ar_texture;
    private int marker_id;
    private GameObject bg_panel;
    private Renderer bg_renderer;
    private GameObject ar_obj;
    private GameObject ar_next_obj;
    private GameObject ar_above_obj;

    private bool isActive = false;
    public Vector2 markerPosition = new Vector2(0, 0);
    public Vector2 nextPosition = new Vector2(0, 0);
    public Vector2 abovePosition = new Vector2(0, 0);
    public bool markerTiltWarning = false;
    public bool isDetected = false;

    private bool log_flag = false;

    void Awake()
    {
        this.bg_panel = this.transform.Find("ProjectPlane").gameObject;
        this.bg_renderer = this.bg_panel.GetComponent<Renderer>();
        this.ar_obj = this.transform.Find("ARObject").gameObject;
        this.ar_next_obj = this.transform.Find("ARObject/Next").gameObject;
        this.ar_above_obj = this.transform.Find("ARObject/Above").gameObject;
    }

    void Update()
    {
        if (this.isActive)
        {
            this.ms.update(this.ar_texture);
            if (this.ms.isExist(marker_id))
            {
                this.ms.setTransform(marker_id, this.ar_obj.transform);
                this.isDetected = true;
                this.calcMarkerPosition();
                this.DetectMarkerTilt();
            }
            else
            {
                this.isDetected = false;
                this.resetMarkerPosition();
            }

            if (this.log_flag)
            {
                Logger.Logging(new ARMarkerLog(this.markerPosition, this.nextPosition));
                this.log_flag = false;
            }
        }
    }

    public void WakeUp(Texture2D tex)
    {
        this.texture = tex;

        this.ar_texture = new ModifiedARUnityTexture(this.texture);
        NyARMarkerSystemConfig config = new NyARMarkerSystemConfig(this.ar_texture.width, this.ar_texture.height);

        this.ms = new NyARUnityMarkerSystem(config);
        marker_id = this.ms.addNyIdMarker(1, 80);

        this.ms.setARBackgroundTransform(this.bg_panel.transform);

        this.isActive = true;
    }

    public void TextureUpdated()
    {
        this.ar_texture.update();
        this.log_flag = true;
    }

    private void calcMarkerPosition()
    {
        RaycastHit[] hits = Physics.RaycastAll(
            new Ray(this.transform.position, this.ar_obj.transform.position - this.transform.position)
        );
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == this.bg_panel)
            {
                float x = hit.point.x / this.bg_renderer.bounds.size.x;
                float y = hit.point.y / this.bg_renderer.bounds.size.y;
                this.markerPosition = new Vector2(x, y);
                break;
            }
        }

        hits = Physics.RaycastAll(
            new Ray(this.transform.position, this.ar_next_obj.transform.position - this.transform.position)
        );
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == this.bg_panel)
            {
                float x = hit.point.x / this.bg_renderer.bounds.size.x;
                float y = hit.point.y / this.bg_renderer.bounds.size.y;
                this.nextPosition = new Vector2(x, y);
                break;
            }
        }

        hits = Physics.RaycastAll(
            new Ray(this.transform.position, this.ar_above_obj.transform.position - this.transform.position)
        );
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject == this.bg_panel)
            {
                float x = hit.point.x / this.bg_renderer.bounds.size.x;
                float y = hit.point.y / this.bg_renderer.bounds.size.y;
                this.abovePosition = new Vector2(x, y);
                break;
            }
        }
    }

    private void DetectMarkerTilt()
    {
        Vector2 horizontal = (this.nextPosition - this.markerPosition) * this.bg_renderer.bounds.size;
        Vector2 vertical = (this.abovePosition - this.markerPosition) * this.bg_renderer.bounds.size;
        float diff = Mathf.Abs(horizontal.magnitude - vertical.magnitude) / Mathf.Max(horizontal.magnitude, vertical.magnitude);
        float angle = Vector2.Angle(horizontal, vertical);
        if (diff > 0.1f || Mathf.Abs(angle - 90f) > 10f)
        {
            this.markerTiltWarning = true;
        }
        else
        {
            this.markerTiltWarning = false;
        }
    }

    private void resetMarkerPosition()
    {
        this.markerPosition = new Vector2(0, 0);
        this.nextPosition = new Vector2(0, 0);
        this.abovePosition = new Vector2(0, 0);
        this.markerTiltWarning = false;
    }

}
