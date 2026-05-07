using System.Collections.Generic;
using System.IO;
using System.Linq;
using MmoGame.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MmoGame.Editor
{
    /// <summary>
    /// Visual map authoring tool. Loads a JSON manifest into the scene via
    /// MapLoader (with MapPieceMarker stamps), lets the author add/move/edit
    /// pieces and tilings through a palette + inspector, then writes back to
    /// JSON. JSON remains source of truth — scene is just a working copy.
    /// </summary>
    public class MapEditorWindow : EditorWindow
    {
        const string MapsFolder = "Assets/Resources/Maps";
        const string PreviewRootPrefix = "[MapEditor]";

        // ----- session state -----
        string _currentMapName;
        string _currentMapPath;
        MapManifest _manifest;
        bool _dirty;
        GameObject _previewRoot;

        // ----- palette state -----
        List<SyntyCatalogScanner.CatalogEntry> _catalog;
        List<string> _categories;
        string _searchFilter = "";
        int _categoryFilterIdx = 0;
        Vector2 _paletteScroll;
        string _placePrefabName;
        float _placeRotationY;
        float _placeUniformScale = 1f;
        string _placeGroup = "";
        float _snapStep = 0f;            // 0 disables snap
        bool _snapToGround = true;       // raycast hit y is used; toggle uses 0

        // ----- inspector state -----
        int _selectedPieceIdx = -1;
        int _selectedTilingIdx = -1;
        Vector2 _rightScroll;
        bool _tilingsExpanded = true;
        readonly Dictionary<int, bool> _tilingFold = new();

        [MenuItem("MmoGame/Map Editor")]
        public static void Open()
        {
            var w = GetWindow<MapEditorWindow>("Map Editor");
            w.minSize = new Vector2(720, 480);
            w.Show();
        }

        // -----------------------------------------------------------------
        //  Lifecycle
        // -----------------------------------------------------------------

        void OnEnable()
        {
            RefreshCatalog();
            SceneView.duringSceneGui += OnSceneGUI;
            Selection.selectionChanged += OnSelectionChanged;
            Undo.undoRedoPerformed += Repaint;
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
            Selection.selectionChanged -= OnSelectionChanged;
            Undo.undoRedoPerformed -= Repaint;
        }

        void RefreshCatalog()
        {
            _catalog = SyntyCatalogScanner.ScanAll();
            _categories = new List<string> { "All" };
            _categories.AddRange(_catalog.Select(e => e.category).Distinct().OrderBy(c => c));
        }

        // -----------------------------------------------------------------
        //  Top-level GUI
        // -----------------------------------------------------------------

        void OnGUI()
        {
            DrawTopBar();
            EditorGUILayout.Space(2);

            if (_manifest == null)
            {
                EditorGUILayout.HelpBox(
                    "No map loaded.\n• Pick one from the dropdown above, or\n• Click 'New Map' to create a fresh manifest.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.BeginHorizontal();
            DrawLeftPanel();
            DrawRightPanel();
            EditorGUILayout.EndHorizontal();
        }

        void DrawTopBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Map dropdown
            var maps = ListMapNames();
            int curIdx = _currentMapName == null ? -1 : maps.IndexOf(_currentMapName);
            int newIdx = EditorGUILayout.Popup(curIdx, maps.ToArray(), EditorStyles.toolbarDropDown, GUILayout.Width(160));
            if (newIdx != curIdx && newIdx >= 0 && newIdx < maps.Count)
            {
                if (ConfirmDiscardIfDirty()) LoadMap(maps[newIdx]);
            }

            if (GUILayout.Button("New", EditorStyles.toolbarButton, GUILayout.Width(40)))
                NewMap();

            if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(55)))
                if (_currentMapName != null && ConfirmDiscardIfDirty()) LoadMap(_currentMapName);

            using (new EditorGUI.DisabledScope(!_dirty || _manifest == null))
                if (GUILayout.Button(_dirty ? "Save *" : "Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    SaveMap();

            if (GUILayout.Button("Validate", EditorStyles.toolbarButton, GUILayout.Width(65)))
                MapValidator.ValidateAll();

            if (GUILayout.Button("Rebuild Catalog", EditorStyles.toolbarButton, GUILayout.Width(110)))
            {
                SyntyCatalogScanner.Rebuild();
                RefreshCatalog();
            }

            GUILayout.FlexibleSpace();

            if (_manifest != null)
            {
                int p = _manifest.pieces?.Length ?? 0;
                int t = _manifest.tilings?.Length ?? 0;
                int instances = _previewRoot != null ? _previewRoot.GetComponentsInChildren<MapPieceMarker>(true).Length : 0;
                GUILayout.Label($"pieces:{p}  tilings:{t}  instances:{instances}", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndHorizontal();
        }

        // -----------------------------------------------------------------
        //  Left panel — palette
        // -----------------------------------------------------------------

        void DrawLeftPanel()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(290));

            EditorGUILayout.LabelField("Palette", EditorStyles.boldLabel);

            // Search + category
            EditorGUILayout.BeginHorizontal();
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField);
            EditorGUILayout.EndHorizontal();
            _categoryFilterIdx = EditorGUILayout.Popup("Category", _categoryFilterIdx, _categories.ToArray());

            // Place options
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("Place options", EditorStyles.miniBoldLabel);
            _placeGroup = EditorGUILayout.TextField("Group", _placeGroup);
            _placeRotationY = EditorGUILayout.FloatField("Rotation Y", _placeRotationY);
            _placeUniformScale = Mathf.Max(0.01f, EditorGUILayout.FloatField("Scale", _placeUniformScale));
            _snapStep = Mathf.Max(0f, EditorGUILayout.FloatField("Snap step (0=off)", _snapStep));
            _snapToGround = EditorGUILayout.Toggle("Snap to ground", _snapToGround);

            if (_placePrefabName != null)
            {
                EditorGUILayout.HelpBox(
                    $"Place mode: '{_placePrefabName}'\n" +
                    "• Click in scene to place\n• [R] rotate +90°\n• [Esc] cancel",
                    MessageType.Info);
                if (GUILayout.Button("Cancel place mode")) _placePrefabName = null;
            }

            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField($"Prefabs ({FilteredCatalog().Count()})", EditorStyles.miniBoldLabel);

            _paletteScroll = EditorGUILayout.BeginScrollView(_paletteScroll, GUILayout.ExpandHeight(true));
            foreach (var entry in FilteredCatalog())
            {
                bool active = entry.name == _placePrefabName;
                var bg = GUI.backgroundColor;
                if (active) GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button(
                        new GUIContent($"{entry.name}\n  size {entry.size.x:F1}×{entry.size.y:F1}×{entry.size.z:F1}"),
                        GUILayout.Height(32)))
                {
                    _placePrefabName = active ? null : entry.name;
                    SceneView.RepaintAll();
                }
                GUI.backgroundColor = bg;
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndVertical();
        }

        IEnumerable<SyntyCatalogScanner.CatalogEntry> FilteredCatalog()
        {
            string cat = _categories != null && _categoryFilterIdx < _categories.Count ? _categories[_categoryFilterIdx] : "All";
            string s = _searchFilter?.Trim().ToLowerInvariant() ?? "";
            return _catalog.Where(e =>
                (cat == "All" || e.category == cat) &&
                (s.Length == 0 || e.name.Contains(s)));
        }

        // -----------------------------------------------------------------
        //  Right panel — inspector + tilings
        // -----------------------------------------------------------------

        void DrawRightPanel()
        {
            EditorGUILayout.BeginVertical();
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            DrawSelectedPiece();
            EditorGUILayout.Space(6);
            DrawTilingsList();

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawSelectedPiece()
        {
            EditorGUILayout.LabelField("Selected piece", EditorStyles.boldLabel);
            if (_selectedPieceIdx < 0 || _manifest.pieces == null || _selectedPieceIdx >= _manifest.pieces.Length)
            {
                EditorGUILayout.HelpBox("Click a placed piece in the scene to edit its values here.", MessageType.None);
                return;
            }

            var p = _manifest.pieces[_selectedPieceIdx];
            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("prefab", p.prefab);
            var pos = Vec3Field("position", ToVec3(p.position, Vector3.zero));
            var rot = Vec3Field("rotation", ToVec3(p.rotation, Vector3.zero));
            var scl = Vec3Field("scale",    ToVec3(p.scale, Vector3.one));
            string parent = EditorGUILayout.TextField("parent group", p.parent ?? "");
            string note = EditorGUILayout.TextField("note", p.note ?? "");

            if (EditorGUI.EndChangeCheck())
            {
                p.position = new[] { pos.x, pos.y, pos.z };
                p.rotation = new[] { rot.x, rot.y, rot.z };
                p.scale    = new[] { scl.x, scl.y, scl.z };
                p.parent = string.IsNullOrEmpty(parent) ? null : parent;
                p.note   = string.IsNullOrEmpty(note) ? null : note;
                MarkDirty();
                ApplySelectedPieceTransformToScene();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Duplicate")) DuplicateSelectedPiece();
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("Delete")) DeleteSelectedPiece();
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        void DrawTilingsList()
        {
            _tilingsExpanded = EditorGUILayout.Foldout(_tilingsExpanded,
                $"Tilings ({_manifest.tilings?.Length ?? 0})", true, EditorStyles.foldoutHeader);
            if (!_tilingsExpanded) return;

            int count = _manifest.tilings?.Length ?? 0;
            for (int i = 0; i < count; i++)
            {
                var t = _manifest.tilings[i];
                if (!_tilingFold.ContainsKey(i)) _tilingFold[i] = false;

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.BeginHorizontal();
                _tilingFold[i] = EditorGUILayout.Foldout(_tilingFold[i], $"[{i}] {t.prefab}  ({t.note ?? "—"})", true);
                GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
                if (GUILayout.Button("✕", GUILayout.Width(24))) { DeleteTiling(i); GUI.backgroundColor = Color.white; EditorGUILayout.EndHorizontal(); EditorGUILayout.EndVertical(); break; }
                GUI.backgroundColor = Color.white;
                EditorGUILayout.EndHorizontal();

                if (_tilingFold[i])
                {
                    EditorGUI.BeginChangeCheck();
                    string prefab = EditorGUILayout.TextField("prefab", t.prefab ?? "");
                    var min  = Vec3Field("min",      ToVec3(t.min, Vector3.zero));
                    var max  = Vec3Field("max",      ToVec3(t.max, Vector3.zero));
                    var step = Vec3Field("step",     ToVec3(t.step, Vector3.one));
                    var rot  = Vec3Field("rotation", ToVec3(t.rotation, Vector3.zero));
                    string parent = EditorGUILayout.TextField("parent", t.parent ?? "");
                    string note   = EditorGUILayout.TextField("note", t.note ?? "");
                    if (EditorGUI.EndChangeCheck())
                    {
                        t.prefab = prefab;
                        t.min  = new[] { min.x, min.y, min.z };
                        t.max  = new[] { max.x, max.y, max.z };
                        t.step = new[] { step.x, step.y, step.z };
                        t.rotation = new[] { rot.x, rot.y, rot.z };
                        t.parent = string.IsNullOrEmpty(parent) ? null : parent;
                        t.note   = string.IsNullOrEmpty(note) ? null : note;
                        MarkDirty();
                        RebuildPreview();
                    }
                }
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("+ Add tiling"))
            {
                AddTiling();
            }
        }

        // -----------------------------------------------------------------
        //  Scene interaction (click-to-place + selection sync)
        // -----------------------------------------------------------------

        void OnSceneGUI(SceneView sv)
        {
            if (_manifest == null) return;
            var e = Event.current;

            if (_placePrefabName != null)
            {
                HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Escape) { _placePrefabName = null; e.Use(); Repaint(); return; }
                    if (e.keyCode == KeyCode.R) { _placeRotationY = (_placeRotationY + 90f) % 360f; e.Use(); Repaint(); return; }
                }

                var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (TryGroundHit(ray, out var hit))
                {
                    var p = ApplySnap(hit);
                    Handles.color = new Color(0.4f, 0.8f, 1f, 0.9f);
                    Handles.SphereHandleCap(0, p, Quaternion.identity, 0.6f, EventType.Repaint);
                    Handles.DrawWireDisc(p, Vector3.up, 1f);
                    Handles.Label(p + Vector3.up * 1.5f, $"{_placePrefabName}\nrot Y {_placeRotationY:F0}°  scale {_placeUniformScale:F2}");

                    if (e.type == EventType.MouseDown && e.button == 0)
                    {
                        AddPiece(_placePrefabName, p, _placeRotationY, _placeUniformScale, _placeGroup);
                        e.Use();
                    }
                }
                sv.Repaint();
                return;
            }

            // Live-edit: when user moves a marked GameObject in the scene, mirror back to manifest.
            var sel = Selection.activeGameObject;
            MapPieceMarker selMarker = sel != null ? sel.GetComponentInParent<MapPieceMarker>() : null;
            if (selMarker != null && selMarker.kind == MapMarkerKind.Piece &&
                selMarker.pieceIndex >= 0 && _manifest.pieces != null && selMarker.pieceIndex < _manifest.pieces.Length)
            {
                var p = _manifest.pieces[selMarker.pieceIndex];
                var prevPos = ToVec3(p.position, Vector3.zero);
                var prevRot = ToVec3(p.rotation, Vector3.zero);
                var prevScl = ToVec3(p.scale, Vector3.one);
                var curPos = selMarker.transform.localPosition;
                var curRot = selMarker.transform.localEulerAngles;
                var curScl = selMarker.transform.localScale;
                if (prevPos != curPos || prevRot != curRot || prevScl != curScl)
                {
                    p.position = new[] { curPos.x, curPos.y, curPos.z };
                    p.rotation = new[] { curRot.x, curRot.y, curRot.z };
                    p.scale    = new[] { curScl.x, curScl.y, curScl.z };
                    MarkDirty();
                    Repaint();
                }

                // Floating actions toolbar above the selection.
                DrawSelectionToolbar(selMarker);

                // Hotkeys when a piece is selected.
                if (e.type == EventType.KeyDown)
                {
                    if (e.keyCode == KeyCode.Delete || e.keyCode == KeyCode.Backspace)
                    { DeleteSelectedPiece(); e.Use(); return; }
                    if (e.keyCode == KeyCode.D && e.shift)
                    { DuplicateSelectedPiece(); e.Use(); return; }
                    if (e.keyCode == KeyCode.G)
                    { DropSelectedToGround(); e.Use(); return; }
                    if (e.keyCode == KeyCode.R && !e.control && !e.command && !e.alt)
                    { RotateSelected(90f); e.Use(); return; }
                }
            }

            // Right-click context menu on any marked GameObject in the scene.
            if (e.type == EventType.MouseDown && e.button == 1)
            {
                var marker = PickMarkerUnderMouse(e.mousePosition);
                if (marker != null && marker.kind == MapMarkerKind.Piece)
                {
                    Selection.activeGameObject = marker.gameObject;
                    ShowPieceContextMenu(marker);
                    e.Use();
                }
            }
        }

        void DrawSelectionToolbar(MapPieceMarker marker)
        {
            var screenPos = HandleUtility.WorldToGUIPoint(marker.transform.position + Vector3.up * 2.5f);
            Handles.BeginGUI();
            var rect = new Rect(screenPos.x - 130, screenPos.y, 260, 22);
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);
            GUILayout.BeginArea(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4));
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(new GUIContent("Dup", "Shift+D"), EditorStyles.miniButtonLeft)) DuplicateSelectedPiece();
            if (GUILayout.Button(new GUIContent("Drop", "G — drop to ground"), EditorStyles.miniButtonMid)) DropSelectedToGround();
            if (GUILayout.Button(new GUIContent("Rot 90°", "R"), EditorStyles.miniButtonMid)) RotateSelected(90f);
            if (GUILayout.Button(new GUIContent("Reset", "reset rotation/scale"), EditorStyles.miniButtonMid)) ResetSelectedTransform();
            if (GUILayout.Button(new GUIContent("Group", "move to group..."), EditorStyles.miniButtonMid)) ShowGroupMenu(marker);
            if (GUILayout.Button(new GUIContent("Del", "Delete"), EditorStyles.miniButtonRight)) DeleteSelectedPiece();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            Handles.EndGUI();
        }

        MapPieceMarker PickMarkerUnderMouse(Vector2 mousePos)
        {
            var picked = HandleUtility.PickGameObject(mousePos, false);
            if (picked == null) return null;
            return picked.GetComponentInParent<MapPieceMarker>();
        }

        void ShowPieceContextMenu(MapPieceMarker marker)
        {
            var menu = new GenericMenu();
            menu.AddDisabledItem(new GUIContent($"{marker.prefabName}  (#{marker.pieceIndex})"));
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Duplicate  Shift+D"), false, DuplicateSelectedPiece);
            menu.AddItem(new GUIContent("Drop to ground  G"), false, DropSelectedToGround);
            menu.AddItem(new GUIContent("Rotate +90°  R"), false, () => RotateSelected(90f));
            menu.AddItem(new GUIContent("Rotate -90°"), false, () => RotateSelected(-90f));
            menu.AddItem(new GUIContent("Reset rotation"), false, () => SetSelectedRotationY(0f));
            menu.AddItem(new GUIContent("Reset scale"), false, () => SetSelectedScale(1f));
            menu.AddSeparator("");
            // Move to existing group submenu
            foreach (var g in CollectGroups())
                menu.AddItem(new GUIContent($"Move to group/{(string.IsNullOrEmpty(g) ? "(root)" : g)}"), false, () => SetSelectedGroup(g));
            menu.AddItem(new GUIContent("Move to group/(new...)"), false, () => PromptNewGroup());
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Delete  Del"), false, DeleteSelectedPiece);
            menu.ShowAsContext();
        }

        void ShowGroupMenu(MapPieceMarker marker)
        {
            var menu = new GenericMenu();
            foreach (var g in CollectGroups())
                menu.AddItem(new GUIContent(string.IsNullOrEmpty(g) ? "(root)" : g), false, () => SetSelectedGroup(g));
            menu.AddItem(new GUIContent("(new...)"), false, () => PromptNewGroup());
            menu.ShowAsContext();
        }

        IEnumerable<string> CollectGroups()
        {
            var set = new HashSet<string>();
            if (_manifest.pieces != null) foreach (var p in _manifest.pieces) set.Add(p.parent ?? "");
            if (_manifest.tilings != null) foreach (var t in _manifest.tilings) set.Add(t.parent ?? "");
            return set.OrderBy(s => s);
        }

        // -----------------------------------------------------------------
        //  Selection actions
        // -----------------------------------------------------------------

        void DropSelectedToGround()
        {
            if (_selectedPieceIdx < 0) return;
            var p = _manifest.pieces[_selectedPieceIdx];
            var pos = ToVec3(p.position, Vector3.zero);
            var ray = new Ray(pos + Vector3.up * 100f, Vector3.down);
            if (Physics.Raycast(ray, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore))
            {
                // Skip the selected object itself (prevent self-hit).
                var picked = hit.collider.GetComponentInParent<MapPieceMarker>();
                if (picked != null && picked.pieceIndex == _selectedPieceIdx)
                {
                    // Try ignoring this object — re-raycast from below it.
                    ray = new Ray(pos + Vector3.down * 0.05f, Vector3.down);
                    if (!Physics.Raycast(ray, out hit, 500f, ~0, QueryTriggerInteraction.Ignore)) { pos.y = 0f; }
                    else pos.y = hit.point.y;
                }
                else pos.y = hit.point.y;
            }
            else pos.y = 0f;
            p.position = new[] { pos.x, pos.y, pos.z };
            ApplySelectedPieceTransformToScene();
            MarkDirty(); Repaint();
        }

        void RotateSelected(float deltaY)
        {
            if (_selectedPieceIdx < 0) return;
            var p = _manifest.pieces[_selectedPieceIdx];
            var rot = ToVec3(p.rotation, Vector3.zero);
            rot.y = (rot.y + deltaY + 360f) % 360f;
            p.rotation = new[] { rot.x, rot.y, rot.z };
            ApplySelectedPieceTransformToScene();
            MarkDirty(); Repaint();
        }

        void SetSelectedRotationY(float y)
        {
            if (_selectedPieceIdx < 0) return;
            var p = _manifest.pieces[_selectedPieceIdx];
            var rot = ToVec3(p.rotation, Vector3.zero); rot.y = y;
            p.rotation = new[] { rot.x, rot.y, rot.z };
            ApplySelectedPieceTransformToScene();
            MarkDirty(); Repaint();
        }

        void SetSelectedScale(float s)
        {
            if (_selectedPieceIdx < 0) return;
            var p = _manifest.pieces[_selectedPieceIdx];
            p.scale = new[] { s, s, s };
            ApplySelectedPieceTransformToScene();
            MarkDirty(); Repaint();
        }

        void ResetSelectedTransform()
        {
            SetSelectedRotationY(0f);
            SetSelectedScale(1f);
        }

        void SetSelectedGroup(string group)
        {
            if (_selectedPieceIdx < 0) return;
            _manifest.pieces[_selectedPieceIdx].parent = string.IsNullOrEmpty(group) ? null : group;
            MarkDirty();
            RebuildPreview();
            SelectPieceInScene(_selectedPieceIdx);
        }

        void PromptNewGroup()
        {
            // Inline prompt window — Unity has no built-in input dialog.
            MapEditorTextPrompt.Open("New group name", "", name =>
            {
                if (string.IsNullOrWhiteSpace(name)) return;
                SetSelectedGroup(name.Trim());
            });
        }

        bool TryGroundHit(Ray ray, out Vector3 hit)
        {
            // Prefer real geometry under the cursor (so you can stack on roofs);
            // fall back to y=0 plane.
            if (Physics.Raycast(ray, out var rh, 5000f)) { hit = rh.point; return true; }
            var plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float d)) { hit = ray.GetPoint(d); return true; }
            hit = default; return false;
        }

        Vector3 ApplySnap(Vector3 p)
        {
            if (!_snapToGround) p.y = 0f;
            if (_snapStep > 0.001f)
            {
                p.x = Mathf.Round(p.x / _snapStep) * _snapStep;
                p.z = Mathf.Round(p.z / _snapStep) * _snapStep;
            }
            return p;
        }

        void OnSelectionChanged()
        {
            var sel = Selection.activeGameObject;
            if (sel == null) { _selectedPieceIdx = -1; Repaint(); return; }
            var marker = sel.GetComponentInParent<MapPieceMarker>();
            if (marker == null) { _selectedPieceIdx = -1; Repaint(); return; }
            if (marker.kind == MapMarkerKind.Piece) _selectedPieceIdx = marker.pieceIndex;
            else _selectedPieceIdx = -1;
            Repaint();
        }

        // -----------------------------------------------------------------
        //  Manifest mutations
        // -----------------------------------------------------------------

        void AddPiece(string prefabName, Vector3 pos, float rotY, float scale, string group)
        {
            var arr = _manifest.pieces ?? System.Array.Empty<MapPiece>();
            var list = arr.ToList();
            list.Add(new MapPiece
            {
                prefab = prefabName,
                position = new[] { pos.x, pos.y, pos.z },
                rotation = new[] { 0f, rotY, 0f },
                scale    = new[] { scale, scale, scale },
                parent   = string.IsNullOrEmpty(group) ? null : group,
            });
            _manifest.pieces = list.ToArray();
            _selectedPieceIdx = list.Count - 1;
            MarkDirty();
            RebuildPreview();
            // Keep newly placed piece selected for instant tweaks.
            SelectPieceInScene(_selectedPieceIdx);
        }

        void DuplicateSelectedPiece()
        {
            if (_selectedPieceIdx < 0) return;
            var src = _manifest.pieces[_selectedPieceIdx];
            var copy = new MapPiece
            {
                prefab = src.prefab,
                position = (float[])src.position?.Clone(),
                rotation = (float[])src.rotation?.Clone(),
                scale    = (float[])src.scale?.Clone(),
                parent   = src.parent,
                note     = src.note,
            };
            // Offset by 2 units on X so the duplicate doesn't perfectly overlap.
            if (copy.position != null && copy.position.Length >= 3) copy.position[0] += 2f;
            var list = _manifest.pieces.ToList();
            list.Add(copy);
            _manifest.pieces = list.ToArray();
            _selectedPieceIdx = list.Count - 1;
            MarkDirty();
            RebuildPreview();
            SelectPieceInScene(_selectedPieceIdx);
        }

        void DeleteSelectedPiece()
        {
            if (_selectedPieceIdx < 0) return;
            var list = _manifest.pieces.ToList();
            list.RemoveAt(_selectedPieceIdx);
            _manifest.pieces = list.ToArray();
            _selectedPieceIdx = -1;
            MarkDirty();
            RebuildPreview();
        }

        void AddTiling()
        {
            var list = (_manifest.tilings ?? System.Array.Empty<MapTiling>()).ToList();
            list.Add(new MapTiling
            {
                prefab = _placePrefabName ?? "env_tile_grass_01",
                min  = new[] { -10f, 0f, -10f },
                max  = new[] {  10f, 0f,  10f },
                step = new[] {   2f, 0f,   2f },
                rotation = new[] { 0f, 0f, 0f },
                parent = string.IsNullOrEmpty(_placeGroup) ? null : _placeGroup,
            });
            _manifest.tilings = list.ToArray();
            _tilingFold[list.Count - 1] = true;
            MarkDirty();
            RebuildPreview();
        }

        void DeleteTiling(int idx)
        {
            var list = _manifest.tilings.ToList();
            list.RemoveAt(idx);
            _manifest.tilings = list.ToArray();
            _tilingFold.Clear();
            MarkDirty();
            RebuildPreview();
        }

        void ApplySelectedPieceTransformToScene()
        {
            if (_previewRoot == null) return;
            var markers = _previewRoot.GetComponentsInChildren<MapPieceMarker>(true);
            foreach (var m in markers)
            {
                if (m.kind == MapMarkerKind.Piece && m.pieceIndex == _selectedPieceIdx)
                {
                    var p = _manifest.pieces[_selectedPieceIdx];
                    m.transform.localPosition = ToVec3(p.position, Vector3.zero);
                    m.transform.localRotation = Quaternion.Euler(ToVec3(p.rotation, Vector3.zero));
                    m.transform.localScale    = ToVec3(p.scale, Vector3.one);
                    return;
                }
            }
        }

        void SelectPieceInScene(int idx)
        {
            if (_previewRoot == null) return;
            var markers = _previewRoot.GetComponentsInChildren<MapPieceMarker>(true);
            foreach (var m in markers)
                if (m.kind == MapMarkerKind.Piece && m.pieceIndex == idx)
                { Selection.activeGameObject = m.gameObject; return; }
        }

        // -----------------------------------------------------------------
        //  Map I/O
        // -----------------------------------------------------------------

        List<string> ListMapNames()
        {
            if (!Directory.Exists(MapsFolder)) return new List<string>();
            return Directory.GetFiles(MapsFolder, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(n => n)
                .ToList();
        }

        void NewMap()
        {
            string name = "untitled-" + System.DateTime.Now.Ticks;
            var path = $"{MapsFolder}/{name}.json";
            var manifest = new MapManifest { name = name, pieces = System.Array.Empty<MapPiece>(), tilings = System.Array.Empty<MapTiling>() };
            File.WriteAllText(path, JsonUtility.ToJson(manifest, true));
            AssetDatabase.ImportAsset(path);
            LoadMap(name);
        }

        void LoadMap(string mapName)
        {
            ClearPreview();
            _currentMapName = mapName;
            _currentMapPath = $"{MapsFolder}/{mapName}.json";
            var text = File.ReadAllText(_currentMapPath);
            _manifest = JsonUtility.FromJson<MapManifest>(text) ?? new MapManifest { name = mapName };
            _manifest.pieces ??= System.Array.Empty<MapPiece>();
            _manifest.tilings ??= System.Array.Empty<MapTiling>();
            _dirty = false;
            _selectedPieceIdx = -1;
            _selectedTilingIdx = -1;
            _tilingFold.Clear();
            RebuildPreview();
            Repaint();
        }

        void SaveMap()
        {
            if (_manifest == null || _currentMapPath == null) return;
            File.WriteAllText(_currentMapPath, JsonUtility.ToJson(_manifest, true));
            AssetDatabase.ImportAsset(_currentMapPath, ImportAssetOptions.ForceUpdate);
            _dirty = false;
            Debug.Log($"[MapEditor] Saved {_currentMapPath} ({_manifest.pieces.Length} pieces, {_manifest.tilings.Length} tilings).");
            Repaint();
        }

        bool ConfirmDiscardIfDirty()
        {
            if (!_dirty) return true;
            return EditorUtility.DisplayDialog("Unsaved changes",
                $"'{_currentMapName}' has unsaved edits. Discard them?", "Discard", "Cancel");
        }

        void MarkDirty() { _dirty = true; }

        // -----------------------------------------------------------------
        //  Preview management — uses MapLoader so markers are stamped
        //  identically to playmode.
        // -----------------------------------------------------------------

        void ClearPreview()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in roots)
                if (go.name.StartsWith(PreviewRootPrefix) || go.name.StartsWith("[Map:"))
                    DestroyImmediate(go);
            _previewRoot = null;
        }

        void RebuildPreview()
        {
            ClearPreview();
            if (_currentMapPath == null) return;
            // MapLoader reads from Resources, so flush our edits first.
            File.WriteAllText(_currentMapPath, JsonUtility.ToJson(_manifest, true));
            AssetDatabase.ImportAsset(_currentMapPath, ImportAssetOptions.ForceUpdate);
            Resources.UnloadUnusedAssets();
            _previewRoot = MapLoader.Load(_currentMapName, bakeNavMesh: false);
            if (_previewRoot != null) _previewRoot.name = $"{PreviewRootPrefix} {_currentMapName}";
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            // Auto-write on every preview rebuild keeps disk == in-memory state during a session;
            // but flag stays set until the user explicitly hits Save (in case they want Reload to revert).
        }

        // -----------------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------------

        static Vector3 ToVec3(float[] arr, Vector3 fb) =>
            arr == null || arr.Length < 3 ? fb : new Vector3(arr[0], arr[1], arr[2]);

        static Vector3 Vec3Field(string label, Vector3 v) => EditorGUILayout.Vector3Field(label, v);
    }

    /// <summary>
    /// Tiny modal-ish input window — Unity doesn't ship a built-in text prompt
    /// dialog. Used by Map Editor for group name entry, etc.
    /// </summary>
    public class MapEditorTextPrompt : EditorWindow
    {
        string _value;
        string _prompt;
        System.Action<string> _onAccept;

        public static void Open(string prompt, string initial, System.Action<string> onAccept)
        {
            var w = CreateInstance<MapEditorTextPrompt>();
            w._prompt = prompt;
            w._value = initial ?? "";
            w._onAccept = onAccept;
            w.titleContent = new GUIContent("Input");
            w.position = new Rect(Screen.currentResolution.width / 2f - 175, Screen.currentResolution.height / 2f - 40, 350, 80);
            w.ShowModalUtility();
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField(_prompt, EditorStyles.boldLabel);
            GUI.SetNextControlName("input");
            _value = EditorGUILayout.TextField(_value);
            EditorGUI.FocusTextInControl("input");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("OK") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return))
            {
                _onAccept?.Invoke(_value);
                Close();
            }
            if (GUILayout.Button("Cancel") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
                Close();
            EditorGUILayout.EndHorizontal();
        }
    }
}
