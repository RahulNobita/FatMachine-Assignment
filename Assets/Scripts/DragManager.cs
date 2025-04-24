using System.Collections;
using System.Linq;
using UnityEngine;

public class DragManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridSpawner gridSpawner;
    [SerializeField] private Camera cam;

    [Header("Drag Settings")]
    [SerializeField] private float dragSpeed = 10f;
    [SerializeField] private float spring = 100.0f;
    [SerializeField] private float damper = 5.0f;
    [SerializeField] private float maxDragDistance = 0.01f;
    [SerializeField] private float snapDuration = 0.2f;

    private Rigidbody draggedRigidbody;
    private SpringJoint joint;
    private RigidbodyConstraints originalConstraints;
    private GameObject anchorObject;

    private void Awake()
    {
        if (cam == null)
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
                Debug.LogWarning("Camera was not assigned to DragRigidbody3D. Using Main Camera instead.");
            }
        }

        if (gridSpawner == null)
        {
            Debug.LogError("GridSpawner not assigned to DragRigidbody3D. Drag functionality will not work correctly.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TryStartDrag();

        if (Input.GetMouseButtonUp(0))
            EndDrag();

        if (joint != null)
            UpdateDragPosition();
    }

    private void FixedUpdate()
    {
        ConstrainToGridBounds();
    }

    private void ConstrainToGridBounds()
    {
        if (draggedRigidbody == null) return;

        Bounds shapeBounds = GetShapeBounds(draggedRigidbody.transform);

        Bounds gridBounds = GetGridBounds();

        Vector3 offset = draggedRigidbody.position - shapeBounds.center;
        Vector3 clampedCenter = shapeBounds.center;

        clampedCenter.x = Mathf.Clamp(
            clampedCenter.x,
            gridBounds.min.x + shapeBounds.extents.x,
            gridBounds.max.x - shapeBounds.extents.x
        );

        clampedCenter.z = Mathf.Clamp(
            clampedCenter.z,
            gridBounds.min.z + shapeBounds.extents.z,
            gridBounds.max.z - shapeBounds.extents.z
        );

        draggedRigidbody.position = clampedCenter + offset;
    }

    private Bounds GetShapeBounds(Transform shapeRoot)
    {
        var renderers = shapeRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(shapeRoot.position, Vector3.zero);
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var renderer in renderers.Skip(1))
        {
            bounds.Encapsulate(renderer.bounds);
        }

        return bounds;
    }

    private void TryStartDrag()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;

        Rigidbody rb = hit.collider.GetComponentInParent<Rigidbody>();
        if (rb == null) return;

        draggedRigidbody = rb;

        originalConstraints = rb.constraints;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;

        CreateDragAnchor(hit.point, rb);
    }

    private void CreateDragAnchor(Vector3 position, Rigidbody connectedBody)
    {
        if (anchorObject != null)
        {
            Destroy(anchorObject);
        }

        anchorObject = new GameObject("DragAnchor");
        anchorObject.transform.position = position;

        joint = anchorObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedBody = connectedBody;
        joint.anchor = Vector3.zero;
        joint.connectedAnchor = connectedBody.transform.InverseTransformPoint(position);

        joint.spring = spring;
        joint.damper = damper;
        joint.maxDistance = maxDragDistance;
    }

    private void UpdateDragPosition()
    {
        if (joint == null || cam == null) return;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 targetPos = ray.GetPoint(5f); // 5 units along ray

        targetPos.y = joint.transform.position.y;

        joint.transform.position = Vector3.Lerp(
            joint.transform.position,
            targetPos,
            Time.deltaTime * dragSpeed
        );
    }

    private void EndDrag()
    {
        if (draggedRigidbody != null)
        {
            Vector3 nearestGridPos = FindNearestGridPosition(draggedRigidbody.position);

            StartCoroutine(SmoothSnap(draggedRigidbody.transform, nearestGridPos));

            draggedRigidbody = null;
        }

        if (joint != null)
        {
            Destroy(anchorObject);
            joint = null;
            anchorObject = null;
        }
    }

    private Vector3 FindNearestGridPosition(Vector3 currentPos)
    {
        if (gridSpawner == null) return currentPos;

        Vector3[,] grid = gridSpawner.GridPositions;
        Vector3 nearest = grid[0, 0];
        float minDistance = Vector3.Distance(currentPos, nearest);

        for (int x = 0; x < gridSpawner.Columns; x++)
        {
            for (int z = 0; z < gridSpawner.Rows; z++)
            {
                float dist = Vector3.Distance(currentPos, grid[x, z]);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = grid[x, z];
                }
            }
        }

        return nearest;
    }

    private IEnumerator SmoothSnap(Transform target, Vector3 finalPos)
    {
        Vector3 startPos = target.position;
        float elapsed = 0f;

        Rigidbody rb = target.GetComponent<Rigidbody>();

        while (elapsed < snapDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / snapDuration);

            float smoothT = Mathf.SmoothStep(0, 1, t);
            target.position = Vector3.Lerp(startPos, finalPos, smoothT);

            yield return null;
        }

        target.position = finalPos;

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private Bounds GetGridBounds()
    {
        if (gridSpawner == null)
        {
            Debug.LogError("GridSpawner not assigned!");
            return new Bounds(Vector3.zero, Vector3.one);
        }

        float tileSize = gridSpawner.TileSize;
        float width = gridSpawner.Columns * tileSize;
        float height = gridSpawner.Rows * tileSize;

        Vector3 center = gridSpawner.transform.position;
        return new Bounds(center, new Vector3(width, 0, height));
    }

    private void OnValidate()
    {
        dragSpeed = Mathf.Max(1f, dragSpeed);
        spring = Mathf.Max(10f, spring);
        damper = Mathf.Max(0.5f, damper);
        maxDragDistance = Mathf.Max(0.001f, maxDragDistance);
        snapDuration = Mathf.Max(0.05f, snapDuration);
    }
}