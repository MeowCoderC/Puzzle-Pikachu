namespace CahtFramework.Pikachu
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using DG.Tweening;

    public class BoardController : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private NodeView nodeViewPrefab;
        [SerializeField] private Transform boardContainer;
        [SerializeField] private Transform spawnOrigin;

        [Header("Visuals")] 
        [SerializeField] private LineRenderer pathLineRenderer;
        [SerializeField] private float        pathDisplayDuration = 0.3f;
        [SerializeField] private float        shiftDuration       = 0.2f;

        [Header("Settings")] 
        [SerializeField] private Vector2 nodeSpacing = new(1.1f, 1.1f);

        private Node[,]                    grid;
        private Dictionary<Node, NodeView> nodeToView = new();
        private Dictionary<NodeView, Node> viewToNode = new();

        private LevelData currentLevelData;
        private ThemeData currentTheme;

        private IInput           input;
        private IPathfinding     pathfinder;
        private IShuffleStrategy shuffleStrategy; // Module trộn bài

        private Node firstSelectedNode = null;
        private Vector3[] cachedPathPoints = new Vector3[4];

        private void Awake()
        {
            this.input = this.GetComponent<IInput>();
            if (this.input != null) this.input.OnNodeClicked += this.HandleNodeClicked;
            else Debug.LogError("[BoardController] No IInput implementation found!");

            this.pathfinder = new OrthogonalPathfinder(); 
            this.shuffleStrategy = new FisherYatesShuffleStrategy();

            if (this.pathLineRenderer != null) this.pathLineRenderer.enabled = false;
        }

        private void OnDestroy()
        {
            if (this.input != null) this.input.OnNodeClicked -= this.HandleNodeClicked;
        }

        public void ClearBoard()
        {
            this.StopAllCoroutines();
            DOTween.KillAll();

            if (this.pathLineRenderer != null) this.pathLineRenderer.enabled = false;

            this.firstSelectedNode = null;
            if (this.nodeToView != null) this.nodeToView.Clear();
            if (this.viewToNode != null) this.viewToNode.Clear();
            this.grid = null;

            if (this.boardContainer != null)
                for (var i = this.boardContainer.childCount - 1; i >= 0; i--)
                    Destroy(this.boardContainer.GetChild(i).gameObject);
        }

        private Vector3 GetWorldPosition(int x, int y)
        {
            var cols = this.currentLevelData.columns;
            var rows = this.currentLevelData.rows;

            var centerPos = this.spawnOrigin != null ? this.spawnOrigin.position : this.transform.position;
            var offset    = new Vector3((cols - 1) * this.nodeSpacing.x / 2f, (rows - 1) * this.nodeSpacing.y / 2f, 0);

            return new Vector3(x * this.nodeSpacing.x, y * this.nodeSpacing.y, 0) - offset + centerPos;
        }

        public void GenerateBoard(LevelData levelData, ThemeData themeData)
        {
            this.ClearBoard();

            this.currentLevelData = levelData;
            this.currentTheme     = themeData;

            var cols = levelData.columns;
            var rows = levelData.rows;
            this.grid = new Node[cols, rows];

            var totalNodes = cols * rows;
            var spawnPool  = new List<PieceData>();

            if (this.currentLevelData.HasGeneratorModule) 
                spawnPool = this.currentLevelData.generatorModule.GeneratePieces(totalNodes, levelData.availablePieces);

            var pieceIndex = 0;
            for (var x = 0; x < cols; x++)
            for (var y = 0; y < rows; y++)
            {
                var newNode = new Node(x, y);
                if (pieceIndex < spawnPool.Count)
                {
                    newNode.SetPiece(spawnPool[pieceIndex]);
                    pieceIndex++;
                }

                this.grid[x, y] = newNode;

                var position = this.GetWorldPosition(x, y);
                var view     = Instantiate(this.nodeViewPrefab, position, Quaternion.identity, this.boardContainer);
                view.name = $"Node_{x}_{y}";

                var skin = !newNode.IsEmpty && this.currentTheme != null ? this.currentTheme.GetSkinForPiece(newNode.Piece) : null;
                view.SetSkin(skin);

                this.nodeToView.Add(newNode, view);
                this.viewToNode.Add(view, newNode);
            }

            if (this.currentLevelData.HasEntranceModule)
            {
                if (this.input != null) this.input.DisableInput();
                this.currentLevelData.entranceModule.AnimateEntrance(new List<NodeView>(this.nodeToView.Values), () =>
                {
                    if (this.input != null) this.input.EnableInput();
                });
            }
        }


        private void HandleNodeClicked(NodeView clickedView)
        {
            if (!this.viewToNode.TryGetValue(clickedView, out var clickedNode) || clickedNode.IsEmpty) return;

            if (this.firstSelectedNode == null)
                this.SelectFirstNode(clickedNode);
            else if (this.firstSelectedNode == clickedNode)
                this.DeselectNode();
            else
                this.ProcessMatchAttempt(this.firstSelectedNode, clickedNode);
        }

        private void SelectFirstNode(Node node)
        {
            this.firstSelectedNode = node;
            this.nodeToView[node].SetSelected(true);
        }

        private void DeselectNode()
        {
            if (this.firstSelectedNode != null)
            {
                this.nodeToView[this.firstSelectedNode].SetSelected(false);
                this.firstSelectedNode = null;
            }
        }

        private void ProcessMatchAttempt(Node first, Node second)
        {
            if (first.Piece.ID == second.Piece.ID)
            {
                var path = this.pathfinder.FindPath(this.grid, first, second);
                if (path != null)
                {
                    this.ExecuteMatch(first, second, path);
                }
                else
                {
                    this.DeselectNode();
                    this.SelectFirstNode(second);
                }
            }
            else
            {
                this.DeselectNode();
                this.SelectFirstNode(second);
            }
        }

        private void ExecuteMatch(Node first, Node second, List<Vector2Int> path)
        {
            this.nodeToView[first].SetSelected(false);
            this.nodeToView[second].SetSelected(false);

            first.ClearPiece();
            second.ClearPiece();

            this.nodeToView[first].SetSkin(null);
            this.nodeToView[second].SetSkin(null);

            this.firstSelectedNode = null;

            this.StartCoroutine(this.MatchSequenceRoutine(path));
        }

        private IEnumerator MatchSequenceRoutine(List<Vector2Int> path)
        {
            if (this.input != null) this.input.DisableInput();

            yield return this.StartCoroutine(this.ShowPathRoutine(path));

            if (this.currentLevelData.HasShiftModule)
            {
                this.ApplyGravityShift();
                yield return new WaitForSeconds(this.shiftDuration);
            }

            if (!this.HasAnyValidMatch())
            {
                bool isBoardClear = true;
                foreach (var node in this.grid)
                {
                    if (node != null && !node.IsEmpty) { isBoardClear = false; break; }
                }

                if (!isBoardClear)
                {
                    Debug.Log("[BoardController] Dead End! Đang tự động xáo trộn...");
                    yield return this.StartCoroutine(this.ShuffleBoardRoutine());
                }
                else
                {
                    Debug.Log("<color=green>[BoardController] CLEAR MAP! YOU WIN!</color>");
                }
            }

            if (this.input != null) this.input.EnableInput();
        }


        private bool HasAnyValidMatch()
        {
            Dictionary<int, List<Node>> nodesByID = new();
            
            for (int x = 0; x < this.currentLevelData.columns; x++)
            {
                for (int y = 0; y < this.currentLevelData.rows; y++)
                {
                    Node node = this.grid[x, y];
                    if (!node.IsEmpty)
                    {
                        int id = node.Piece.ID;
                        if (!nodesByID.ContainsKey(id)) nodesByID[id] = new List<Node>();
                        nodesByID[id].Add(node);
                    }
                }
            }

            foreach (var kvp in nodesByID)
            {
                var group = kvp.Value;
                if (group.Count < 2) continue; 

                for (int i = 0; i < group.Count; i++)
                {
                    for (int j = i + 1; j < group.Count; j++)
                    {
                        if (this.pathfinder.FindPath(this.grid, group[i], group[j]) != null)
                            return true;
                    }
                }
            }
            return false;
        }

        public void ForceShuffleBoard() => this.StartCoroutine(this.ShuffleBoardRoutine());

        private IEnumerator ShuffleBoardRoutine()
        {
            List<Node> activeNodes = new List<Node>();
            List<PieceData> activePieces = new List<PieceData>();

            for (int x = 0; x < this.currentLevelData.columns; x++)
            {
                for (int y = 0; y < this.currentLevelData.rows; y++)
                {
                    Node node = this.grid[x, y];
                    if (!node.IsEmpty)
                    {
                        activeNodes.Add(node);
                        activePieces.Add(node.Piece);
                    }
                }
            }

            if (activeNodes.Count == 0) yield break;

            int maxAttempts = 50;
            int attempts = 0;
            do
            {
                this.shuffleStrategy.Shuffle(activePieces);
                
                for (int i = 0; i < activeNodes.Count; i++)
                {
                    activeNodes[i].SetPiece(activePieces[i]);
                }
                attempts++;
                
            } while (!this.HasAnyValidMatch() && attempts < maxAttempts);

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("[BoardController] Cảnh báo: Trộn 50 lần vẫn không tìm thấy đường. Map có thể bị lỗi thiết kế kẹt góc.");
            }

            float animationDuration = 0.4f;
            foreach (Node node in activeNodes)
            {
                NodeView view = this.nodeToView[node];
                
                view.transform.DOScale(Vector3.zero, animationDuration / 2f).OnComplete(() =>
                {
                    Sprite newSkin = this.currentTheme != null ? this.currentTheme.GetSkinForPiece(node.Piece) : null;
                    view.SetSkin(newSkin);
                    view.transform.DOScale(Vector3.one, animationDuration / 2f).SetEase(Ease.OutBack);
                });
            }

            yield return new WaitForSeconds(animationDuration);
        }


        private void ApplyGravityShift()
        {
            var hasChanged = this.currentLevelData.shiftModule.ApplyShift(this.grid, this.currentLevelData.columns, this.currentLevelData.rows);
            if (hasChanged) this.UpdateAllViews();
        }

        private void UpdateAllViews()
        {
            for (var x = 0; x < this.currentLevelData.columns; x++)
            for (var y = 0; y < this.currentLevelData.rows; y++)
            {
                var node = this.grid[x, y];
                var view = this.nodeToView[node];

                var skin = !node.IsEmpty && this.currentTheme != null ? this.currentTheme.GetSkinForPiece(node.Piece) : null;
                view.SetSkin(skin);

                if (!node.IsEmpty)
                {
                    var targetPos = this.GetWorldPosition(x, y);
                    view.transform.DOMove(targetPos, this.shiftDuration).SetEase(Ease.OutQuad);
                }
            }
        }

        private IEnumerator ShowPathRoutine(List<Vector2Int> path)
        {
            if (this.pathLineRenderer == null || path == null || path.Count == 0) yield break;

            int pointCount = path.Count;
            this.pathLineRenderer.positionCount = pointCount;

            for (var i = 0; i < pointCount; i++)
            {
                var worldPos = this.GetWorldPosition(path[i].x, path[i].y);
                worldPos.z = -1f; 
                this.cachedPathPoints[i] = worldPos;
            }

            this.pathLineRenderer.SetPositions(this.cachedPathPoints);
            this.pathLineRenderer.enabled = true;

            yield return new WaitForSeconds(this.pathDisplayDuration);
            this.pathLineRenderer.enabled = false;
        }
        
#if UNITY_EDITOR
        //Testing
        public NodePair Editor_FindFirstValidMatch()
        {
            if (this.grid == null) return null;

            Dictionary<int, List<Node>> nodesByID = new();
            
            for (int x = 0; x < this.currentLevelData.columns; x++)
            {
                for (int y = 0; y < this.currentLevelData.rows; y++)
                {
                    Node node = this.grid[x, y];
                    if (!node.IsEmpty)
                    {
                        int id = node.Piece.ID;
                        if (!nodesByID.ContainsKey(id)) nodesByID[id] = new List<Node>();
                        nodesByID[id].Add(node);
                    }
                }
            }

            foreach (var kvp in nodesByID)
            {
                var group = kvp.Value;
                if (group.Count < 2) continue; 

                for (int i = 0; i < group.Count; i++)
                {
                    for (int j = i + 1; j < group.Count; j++)
                    {
                        var path = this.pathfinder.FindPath(this.grid, group[i], group[j]);
                        if (path != null) return new NodePair { node1 = group[i], node2 = group[j] };
                    }
                }
            }
            return null;
        }

        public void Editor_SimulateClick(Node node)
        {
            if (this.nodeToView.TryGetValue(node, out NodeView view))
            {
                this.HandleNodeClicked(view);
            }
        }
        
        public class NodePair
        {
            public Node node1;
            public Node node2;
        }
#endif
    }
}