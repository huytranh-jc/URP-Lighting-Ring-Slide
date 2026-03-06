using UnityEngine;
using UnityEditor;

/// <summary>
/// Unity 6 Editor Tool — Tạo ô bàn cờ (checkerboard) từ model truyền vào
/// Đặt file này vào thư mục Editor/ trong project
/// </summary>
public class CheckerboardGridGenerator : EditorWindow
{
    // ── Inspector Fields ──────────────────────────────────────────
    private GameObject modelA;          // Model cho ô chẵn (trắng)
    private GameObject modelB;          // Model cho ô lẻ (đen) — để null = bỏ trống
    private Vector2Int gridSize = new Vector2Int(8, 8);
    private float cellSpacing = 1f;     // Khoảng cách giữa các ô
    private Vector3 startPosition = Vector3.zero;
    private string parentName = "CheckerboardGrid";
    private bool centerGrid = true;     // Căn giữa toàn bộ grid

    // ── Preview ───────────────────────────────────────────────────
    private Vector2 scrollPos;
    private int totalObjects;

    // ── Open Window ───────────────────────────────────────────────
    [MenuItem("Tools/Checkerboard Grid Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<CheckerboardGridGenerator>("Checkerboard Grid");
        window.minSize = new Vector2(360, 520);
    }

    // ── GUI ───────────────────────────────────────────────────────
    private void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        DrawHeader();
        DrawModelSection();
        DrawGridSection();
        DrawOptionsSection();
        DrawPreviewInfo();
        DrawButtons();

        EditorGUILayout.EndScrollView();
    }

    private void DrawHeader()
    {
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter
        };
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("♟ Checkerboard Grid Generator", titleStyle);
        EditorGUILayout.Space(4);
        DrawSeparator();
    }

    private void DrawModelSection()
    {
        EditorGUILayout.LabelField("Models", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        modelA = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Model A (ô chẵn)", "Model đặt ở các ô màu sáng"),
            modelA, typeof(GameObject), false);

        modelB = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Model B (ô lẻ)", "Model đặt ở các ô màu tối — để trống = skip"),
            modelB, typeof(GameObject), false);

        DrawSeparator();
    }

    private void DrawGridSection()
    {
        EditorGUILayout.LabelField("Grid Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        // Vector2Int field cho grid size
        gridSize = EditorGUILayout.Vector2IntField(
            new GUIContent("Grid Size (X, Y)", "Số cột × số hàng"),
            gridSize);

        // Clamp tối thiểu 1x1
        gridSize.x = Mathf.Max(1, gridSize.x);
        gridSize.y = Mathf.Max(1, gridSize.y);

        cellSpacing = EditorGUILayout.FloatField(
            new GUIContent("Cell Spacing", "Khoảng cách giữa tâm các ô"),
            cellSpacing);
        cellSpacing = Mathf.Max(0.01f, cellSpacing);

        DrawSeparator();
    }

    private void DrawOptionsSection()
    {
        EditorGUILayout.LabelField("Options", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        centerGrid = EditorGUILayout.Toggle(
            new GUIContent("Center Grid", "Căn giữa grid tại Start Position"),
            centerGrid);

        startPosition = EditorGUILayout.Vector3Field(
            new GUIContent("Start Position", "Vị trí góc trái-dưới (hoặc tâm nếu Center Grid)"),
            startPosition);

        parentName = EditorGUILayout.TextField(
            new GUIContent("Parent Object Name", "Tên của GameObject cha chứa toàn bộ grid"),
            parentName);

        DrawSeparator();
    }

    private void DrawPreviewInfo()
    {
        int even = 0, odd = 0;
        for (int y = 0; y < gridSize.y; y++)
            for (int x = 0; x < gridSize.x; x++)
                if ((x + y) % 2 == 0) even++; else odd++;

        totalObjects = (modelA != null ? even : 0) + (modelB != null ? odd : 0);

        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.IntField("Tổng ô grid", gridSize.x * gridSize.y);
            EditorGUILayout.IntField("Ô chẵn (Model A)", even);
            EditorGUILayout.IntField("Ô lẻ (Model B)", odd);
            EditorGUILayout.IntField("Objects sẽ tạo", totalObjects);
        }

        DrawSeparator();
    }

    private void DrawButtons()
    {
        EditorGUILayout.Space(4);

        bool canGenerate = modelA != null;

        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 13 };
            if (GUILayout.Button("▶  Generate Grid", btnStyle, GUILayout.Height(36)))
                GenerateGrid();
        }

        if (!canGenerate)
        {
            EditorGUILayout.HelpBox("Cần ít nhất Model A để tạo grid.", MessageType.Warning);
        }

        EditorGUILayout.Space(4);

        if (GUILayout.Button("🗑  Xóa Grid Cũ ('" + parentName + "')", GUILayout.Height(28)))
            DeleteExistingGrid();

        EditorGUILayout.Space(4);
    }

    // ── Core: Generate Grid ────────────────────────────────────────
    private void GenerateGrid()
    {
        // Xóa grid cũ nếu có
        DeleteExistingGrid();

        // Tạo parent object
        GameObject parent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(parent, "Create Checkerboard Grid");
        parent.transform.position = startPosition;

        // Tính offset để căn giữa
        Vector3 offset = Vector3.zero;
        if (centerGrid)
        {
            offset = new Vector3(
                -(gridSize.x - 1) * cellSpacing * 0.5f,
                0f,
                -(gridSize.y - 1) * cellSpacing * 0.5f
            );
        }

        int created = 0;

        for (int y = 0; y < gridSize.y; y++)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                bool isEven = (x + y) % 2 == 0;
                GameObject prefab = isEven ? modelA : modelB;

                if (prefab == null) continue;

                Vector3 localPos = new Vector3(
                    x * cellSpacing + offset.x,
                    0f,
                    y * cellSpacing + offset.z
                );

                // Instantiate từ prefab
                GameObject cell = PrefabUtility.InstantiatePrefab(prefab, parent.transform) as GameObject;
                if (cell == null)
                    cell = Instantiate(prefab, parent.transform);

                cell.transform.localPosition = localPos;
                cell.name = $"Cell_{x}_{y}_{(isEven ? "A" : "B")}";

                Undo.RegisterCreatedObjectUndo(cell, "Create Cell");
                created++;
            }
        }

        // Select parent
        Selection.activeGameObject = parent;
        EditorGUIUtility.PingObject(parent);

        Debug.Log($"[CheckerboardGrid] ✅ Tạo {created} objects ({gridSize.x}×{gridSize.y} grid) → '{parentName}'");
    }

    // ── Helper: Xóa grid cũ ────────────────────────────────────────
    private void DeleteExistingGrid()
    {
        GameObject existing = GameObject.Find(parentName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            Debug.Log($"[CheckerboardGrid] 🗑 Đã xóa grid cũ '{parentName}'");
        }
    }

    // ── Helper: Separator Line ─────────────────────────────────────
    private void DrawSeparator()
    {
        EditorGUILayout.Space(4);
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        EditorGUILayout.Space(4);
    }
}
