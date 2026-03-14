namespace CahtFramework.Pikachu
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using DG.Tweening;

    public class BoardController : MonoBehaviour
    {
        [Header("References")] 
        [SerializeField] private NodeView     nodeViewPrefab;
        [SerializeField] private Transform    boardContainer;
        [SerializeField] private Transform    spawnOrigin;
        [SerializeField] private PathRenderer pathRenderer;

        [Header("Visuals")] 
        [SerializeField] private float shiftDuration = 0.2f;

        [Header("Settings")] 
        [SerializeField] private Vector2 nodeSpacing = new(1.1f, 1.1f);

        public BoardData Data { get; private set; } = new BoardData();

        private LevelData currentLevelData;
        private ThemeData currentTheme;

        private IInput            input;
        private IShuffleStrategy  shuffleStrategy; 
        private BoardLogicService boardLogic; 

        private Node      firstSelectedNode = null;
        private Vector3[] cachedPathPoints  = new Vector3[4];

        private void Awake()
        {
            this.input = this.GetComponent<IInput>();
            if (this.input != null) this.input.OnNodeClicked += this.HandleNodeClicked;
            else Debug.LogError("[BoardController] No IInput implementation found!");

            IPathfinding pathfinder = new OrthogonalPathfinder(); 
            this.boardLogic         = new BoardLogicService(pathfinder);
            this.shuffleStrategy    = new FisherYatesShuffleStrategy(); 
        }

        private void OnDestroy()
        {
            if (this.input != null) this.input.OnNodeClicked -= this.HandleNodeClicked;
        }

        public void SetShuffleStrategy(IShuffleStrategy newStrategy)
        {
            this.shuffleStrategy = newStrategy;
        }

        public void ClearBoard()
        {
            this.StopAllCoroutines();
            DOTween.KillAll();

            if (this.pathRenderer != null) this.pathRenderer.HidePath();

            this.firstSelectedNode = null;
            this.Data.Clear(); 

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

            this.Data.Initialize(levelData.columns, levelData.rows);

            var totalNodes = levelData.columns * levelData.rows;
            var spawnPool  = new List<PieceData>();

            if (this.currentLevelData.HasGeneratorModule) 
                spawnPool = this.currentLevelData.generatorModule.GeneratePieces(totalNodes, levelData.availablePieces);

            var pieceIndex = 0;
            for (var x = 0; x < this.Data.Columns; x++)
            for (var y = 0; y < this.Data.Rows; y++)
            {
                var newNode = new Node(x, y);
                if (pieceIndex < spawnPool.Count)
                {
                    newNode.SetPiece(spawnPool[pieceIndex]);
                    pieceIndex++;
                }

                var position = this.GetWorldPosition(x, y);
                var view     = Instantiate(this.nodeViewPrefab, position, Quaternion.identity, this.boardContainer);
                view.name = $"Node_{x}_{y}";

                var skin = !newNode.IsEmpty && this.currentTheme != null ? this.currentTheme.GetSkinForPiece(newNode.Piece) : null;
                view.SetSkin(skin);

                this.Data.RegisterNode(newNode, view);
            }

            if (this.currentLevelData.HasEntranceModule)
            {
                if (this.input != null) this.input.DisableInput();
                this.currentLevelData.entranceModule.AnimateEntrance(this.Data.GetAllViews(), () =>
                {
                    if (this.input != null) this.input.EnableInput();
                });
            }
        }

        private void HandleNodeClicked(NodeView clickedView)
        {
            Node clickedNode = this.Data.GetNode(clickedView);
            if (clickedNode == null || clickedNode.IsEmpty) return;

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
            this.Data.GetView(node).SetSelected(true);
        }

        private void DeselectNode()
        {
            if (this.firstSelectedNode != null)
            {
                this.Data.GetView(this.firstSelectedNode).SetSelected(false);
                this.firstSelectedNode = null;
            }
        }

        private void ProcessMatchAttempt(Node first, Node second)
        {
            var path = this.boardLogic.CheckMatchAndGetPath(this.Data, first, second);
            
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

        private void ExecuteMatch(Node first, Node second, List<Vector2Int> path)
        {
            NodeView view1 = this.Data.GetView(first);
            NodeView view2 = this.Data.GetView(second);

            view1.SetSelected(false);
            view2.SetSelected(false);

            first.ClearPiece();
            second.ClearPiece();

            view1.SetSkin(null);
            view2.SetSkin(null);

            this.firstSelectedNode = null;

            this.StartCoroutine(this.MatchSequenceRoutine(path));
        }

        private IEnumerator MatchSequenceRoutine(List<Vector2Int> path)
        {
            if (this.input != null) this.input.DisableInput();

            int pointCount = path.Count;
            for (var i = 0; i < pointCount; i++)
            {
                var worldPos = this.GetWorldPosition(path[i].x, path[i].y);
                worldPos.z = -1f; 
                this.cachedPathPoints[i] = worldPos;
            }
            
            if (this.pathRenderer != null)
            {
                yield return this.StartCoroutine(this.pathRenderer.ShowPathRoutine(this.cachedPathPoints, pointCount));
            }

            if (this.currentLevelData.HasShiftModule)
            {
                this.ApplyGravityShift();
                yield return new WaitForSeconds(this.shiftDuration);
            }

            if (!this.boardLogic.HasAnyValidMatch(this.Data))
            {
                bool isBoardClear = true;
                foreach (var node in this.Data.Grid)
                {
                    if (node != null && !node.IsEmpty) { isBoardClear = false; break; }
                }

                if (!isBoardClear)
                {
                    Debug.Log("[BoardController] Dead End! Automating shuffle...");
                    yield return this.StartCoroutine(this.ShuffleBoardRoutine());
                }
                else
                {
                    Debug.Log("<color=green>[BoardController] CLEAR MAP! YOU WIN!</color>");
                }
            }

            if (this.input != null) this.input.EnableInput();
        }

        public void ForceShuffleBoard() => this.StartCoroutine(this.ShuffleBoardRoutine());

        private IEnumerator ShuffleBoardRoutine()
        {
            List<Node> activeNodes = new List<Node>();
            List<PieceData> activePieces = new List<PieceData>();

            for (int x = 0; x < this.Data.Columns; x++)
            {
                for (int y = 0; y < this.Data.Rows; y++)
                {
                    Node node = this.Data.Grid[x, y];
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
                
            } while (!this.boardLogic.HasAnyValidMatch(this.Data) && attempts < maxAttempts);

            if (attempts >= maxAttempts)
            {
                Debug.LogWarning("[BoardController] Warning: No valid matches found after 50 shuffle attempts. Potential level design flaw or corner-lock.");
            }

            float animationDuration = 0.4f;
            foreach (Node node in activeNodes)
            {
                NodeView view = this.Data.GetView(node);
                
                view.transform.DOScale(Vector3.zero, animationDuration / 2f).OnComplete(() =>
                {
                    Sprite newSkin = this.currentTheme != null ? this.currentTheme.GetSkinForPiece(node.Piece) : null;
                    view.SetSkin(newSkin);
                    view.transform.DOScale(Vector3.one, animationDuration / 2f).SetEase(Ease.OutBack);
                });
            }

            yield return new WaitForSeconds(animationDuration);
        }

        public void ShowHint()
        {
            if (this.input != null) this.input.DisableInput();

            NodePair match = this.boardLogic.FindValidMatch(this.Data);
            
            if (match != null)
            {
                NodeView view1 = this.Data.GetView(match.node1);
                NodeView view2 = this.Data.GetView(match.node2);

                view1.SetSelected(true);
                view2.SetSelected(true);

                Sequence hintSeq = DOTween.Sequence();
                hintSeq.Append(view1.transform.DOScale(1.2f, 0.2f).SetLoops(4, LoopType.Yoyo));
                hintSeq.Join(view2.transform.DOScale(1.2f, 0.2f).SetLoops(4, LoopType.Yoyo));
                
                hintSeq.OnComplete(() => {
                    view1.transform.localScale = Vector3.one;
                    view2.transform.localScale = Vector3.one;
                    
                    view1.SetSelected(false);
                    view2.SetSelected(false);

                    if (this.input != null) this.input.EnableInput();
                });
            }
            else
            {
                Debug.LogWarning("[BoardController] No hints available! Board should be shuffling.");
                if (this.input != null) this.input.EnableInput();
            }
        }

        private void ApplyGravityShift()
        {
            var hasChanged = this.currentLevelData.shiftModule.ApplyShift(this.Data.Grid, this.Data.Columns, this.Data.Rows);
            if (hasChanged) this.UpdateAllViews();
        }

        private void UpdateAllViews()
        {
            for (var x = 0; x < this.Data.Columns; x++)
            for (var y = 0; y < this.Data.Rows; y++)
            {
                var node = this.Data.Grid[x, y];
                var view = this.Data.GetView(node);

                var skin = !node.IsEmpty && this.currentTheme != null ? this.currentTheme.GetSkinForPiece(node.Piece) : null;
                view.SetSkin(skin);

                if (!node.IsEmpty)
                {
                    var targetPos = this.GetWorldPosition(x, y);
                    view.transform.DOMove(targetPos, this.shiftDuration).SetEase(Ease.OutQuad);
                }
            }
        }

#if UNITY_EDITOR
        public NodePair Editor_FindFirstValidMatch() => this.boardLogic.FindValidMatch(this.Data);

        public void Editor_SimulateClick(Node node)
        {
            NodeView view = this.Data.GetView(node);
            if (view != null) this.HandleNodeClicked(view);
        }
#endif
    }
}