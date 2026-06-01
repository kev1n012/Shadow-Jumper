using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(CompositeCollider2D))]
public class ShadowCaster2DCreator : MonoBehaviour
{
    [SerializeField] private bool selfShadows = true;
    [SerializeField] private Transform lightSource;
    [SerializeField] private Material frozenShadowMaterial;
    [SerializeField] private float shadowLength = 10f;
    [SerializeField] private string frozenShadowLayer = "FrozenShadow";

    private CompositeCollider2D tilemapCollider;
    private GameObject frozenShadowRoot;
    private bool shadowsFrozen = false;
    private int groundMask;

    static readonly FieldInfo meshField = typeof(ShadowCaster2D).GetField("m_Mesh", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly FieldInfo shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash", BindingFlags.NonPublic | BindingFlags.Instance);
    static readonly MethodInfo generateShadowMeshMethod = typeof(ShadowCaster2D)
                                    .Assembly
                                    .GetType("UnityEngine.Rendering.Universal.ShadowUtility")
                                    .GetMethod("GenerateShadowMesh", BindingFlags.Public | BindingFlags.Static);

    private void Awake()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Static;

        tilemapCollider = GetComponent<CompositeCollider2D>();
        groundMask = LayerMask.GetMask("Ground");
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (!shadowsFrozen)
                FreezeShadows();
            else
                UnfreezeShadows();
        }
    }

    private void FreezeShadows()
    {
        if (frozenShadowRoot != null)
            Destroy(frozenShadowRoot);

        // Root object with Rigidbody2D + CompositeCollider2D to merge all shadow colliders
        frozenShadowRoot = new GameObject("FrozenShadows");
        frozenShadowRoot.transform.position = Vector3.zero;
        frozenShadowRoot.transform.rotation = Quaternion.identity;
        frozenShadowRoot.transform.localScale = Vector3.one;
        frozenShadowRoot.layer = LayerMask.NameToLayer(frozenShadowLayer);

        // CompositeCollider2D requires a Rigidbody2D on the same object
        Rigidbody2D rootRb = frozenShadowRoot.AddComponent<Rigidbody2D>();
        rootRb.bodyType = RigidbodyType2D.Static;

        CompositeCollider2D composite = frozenShadowRoot.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

        Vector3 lightPos = lightSource != null ? lightSource.position : Vector3.zero;

        foreach (Transform child in transform)
        {
            ShadowCaster2D sc = child.GetComponent<ShadowCaster2D>();
            if (sc == null) continue;

            Vector3[] shapePath = (Vector3[])shapePathField.GetValue(sc);
            if (shapePath == null || shapePath.Length == 0) continue;

            // Disable the live shadow caster
            sc.enabled = false;

            // Convert to world space
            Vector3[] worldPath = new Vector3[shapePath.Length];
            for (int i = 0; i < shapePath.Length; i++)
                worldPath[i] = transform.TransformPoint(shapePath[i]);

            // Raycast each vertex from the light, stop at walls
            Vector3[] projectedPath = new Vector3[shapePath.Length];
            for (int i = 0; i < worldPath.Length; i++)
            {
                Vector2 origin = new Vector2(worldPath[i].x, worldPath[i].y);
                Vector2 lightPos2D = new Vector2(lightPos.x, lightPos.y);
                Vector2 dir = (origin - lightPos2D).normalized;

                RaycastHit2D hit = Physics2D.Raycast(origin, dir, shadowLength, groundMask);
                if (hit.collider != null)
                    projectedPath[i] = new Vector3(hit.point.x, hit.point.y, worldPath[i].z);
                else
                    projectedPath[i] = worldPath[i] + new Vector3(dir.x, dir.y, 0) * shadowLength;
            }

            // Visual mesh
            Mesh shadowMesh = BuildShadowMesh(worldPath, projectedPath);

            GameObject frozen = new GameObject("frozen_" + child.name);
            frozen.transform.parent = frozenShadowRoot.transform;
            frozen.transform.position = Vector3.zero;
            frozen.transform.rotation = Quaternion.identity;
            frozen.transform.localScale = Vector3.one;
            frozen.layer = LayerMask.NameToLayer(frozenShadowLayer);

            MeshFilter mf = frozen.AddComponent<MeshFilter>();
            mf.mesh = shadowMesh;
            MeshRenderer mr = frozen.AddComponent<MeshRenderer>();
            mr.material = frozenShadowMaterial;
            mr.sortingLayerName = "Default";
            mr.sortingOrder = -1;

            // Attach one convex quad collider per edge instead of one big polygon.
            // CompositeCollider2D requires simple, non-self-intersecting polygons.
            AttachShadowColliders(frozen, worldPath, projectedPath);
        }

        shadowsFrozen = true;
    }

    /// <summary>
    /// Creates one PolygonCollider2D quad per edge of the shadow shape.
    /// Each quad is convex and wound CCW so CompositeCollider2D can merge them correctly.
    /// </summary>
    private void AttachShadowColliders(GameObject parent, Vector3[] worldPath, Vector3[] projectedPath)
    {
        int count = worldPath.Length;

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            Vector2 a = worldPath[i];
            Vector2 b = worldPath[next];
            Vector2 c = projectedPath[next];
            Vector2 d = projectedPath[i];

            // Skip degenerate quads (caster edge is a point)
            if (Vector2.Distance(a, b) < 0.001f) continue;

            GameObject quadObj = new GameObject("shadow_col_" + i);
            quadObj.transform.parent = parent.transform;
            quadObj.transform.localPosition = Vector3.zero;
            quadObj.transform.localRotation = Quaternion.identity;
            quadObj.transform.localScale = Vector3.one;
            quadObj.layer = parent.layer;

            PolygonCollider2D col = quadObj.AddComponent<PolygonCollider2D>();
            col.usedByComposite = true;

            // Enforce CCW winding which CompositeCollider2D requires
            col.SetPath(0, EnsureCCW(new Vector2[] { a, d, c, b }));
        }
    }

    /// <summary>
    /// Returns the points wound counter-clockwise (positive area in Unity's Y-up space).
    /// Reverses the array in-place if it is currently CW and returns it.
    /// </summary>
    private Vector2[] EnsureCCW(Vector2[] points)
    {
        float area = 0f;
        for (int i = 0; i < points.Length; i++)
        {
            int j = (i + 1) % points.Length;
            area += points[i].x * points[j].y;
            area -= points[j].x * points[i].y;
        }
        // area > 0 means CCW in Unity's Y-up coordinate system
        if (area < 0)
            System.Array.Reverse(points);
        return points;
    }

    private Mesh BuildShadowMesh(Vector3[] casterVerts, Vector3[] projectedVerts)
    {
        int count = casterVerts.Length;
        Vector3[] vertices = new Vector3[count * 2];
        for (int i = 0; i < count; i++)
        {
            vertices[i] = casterVerts[i];
            vertices[i + count] = projectedVerts[i];
        }

        int[] triangles = new int[count * 6];
        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;
            int t = i * 6;
            triangles[t + 0] = i;
            triangles[t + 1] = next;
            triangles[t + 2] = i + count;
            triangles[t + 3] = next;
            triangles[t + 4] = next + count;
            triangles[t + 5] = i + count;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void UnfreezeShadows()
    {
        foreach (Transform child in transform)
        {
            ShadowCaster2D sc = child.GetComponent<ShadowCaster2D>();
            if (sc != null)
                sc.enabled = true;
        }

        if (frozenShadowRoot != null)
        {
            Destroy(frozenShadowRoot);
            frozenShadowRoot = null;
        }

        shadowsFrozen = false;
    }

    public void Create()
    {
        DestroyOldShadowCasters();
        tilemapCollider = GetComponent<CompositeCollider2D>();

        for (int i = 0; i < tilemapCollider.pathCount; i++)
        {
            Vector2[] pathVertices = new Vector2[tilemapCollider.GetPathPointCount(i)];
            tilemapCollider.GetPath(i, pathVertices);
            GameObject shadowCaster = new GameObject("shadow_caster_" + i);
            shadowCaster.transform.parent = gameObject.transform;
            ShadowCaster2D shadowCasterComponent = shadowCaster.AddComponent<ShadowCaster2D>();
            shadowCasterComponent.selfShadows = this.selfShadows;

            Vector3[] testPath = new Vector3[pathVertices.Length];
            for (int j = 0; j < pathVertices.Length; j++)
                testPath[j] = pathVertices[j];

            shapePathField.SetValue(shadowCasterComponent, testPath);
            shapePathHashField.SetValue(shadowCasterComponent, Random.Range(int.MinValue, int.MaxValue));
            meshField.SetValue(shadowCasterComponent, new Mesh());
            generateShadowMeshMethod.Invoke(shadowCasterComponent,
                new object[] { meshField.GetValue(shadowCasterComponent), shapePathField.GetValue(shadowCasterComponent) });
        }
    }

    public void DestroyOldShadowCasters()
    {
        var tempList = transform.Cast<Transform>().ToList();
        foreach (var child in tempList)
        {
            DestroyImmediate(child.gameObject);
        }
    }
}

[CustomEditor(typeof(ShadowCaster2DCreator))]
public class ShadowCaster2DTileMapEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Create"))
        {
            var creator = (ShadowCaster2DCreator)target;
            creator.Create();
        }
        if (GUILayout.Button("Remove Shadows"))
        {
            var creator = (ShadowCaster2DCreator)target;
            creator.DestroyOldShadowCasters();
        }
        EditorGUILayout.EndHorizontal();
    }
}