#if UNITY_EDITOR
namespace CahtFramework.Pikachu
{
    using UnityEngine;
    using System.Collections;

    public class PikachuTestTool : MonoBehaviour
    {
        [SerializeField] private BoardController board;
        [SerializeField] private float           stepDelay = 0.5f;

        private Coroutine testCoroutine;

        [ContextMenu("▶ Chạy Auto Play")]
        public void StartAutoPlay()
        {
            if (this.board == null)
            {
                Debug.LogWarning("[TestTool] Chưa ném BoardController vào kìa!");
                return;
            }

            if (this.testCoroutine != null) StopCoroutine(this.testCoroutine);
            this.testCoroutine = StartCoroutine(this.AutoPlayRoutine());
            Debug.Log("[TestTool] Bắt đầu tự chơi...");
        }

        [ContextMenu("■ Dừng Auto Play")]
        public void StopAutoPlay()
        {
            if (this.testCoroutine != null)
            {
                StopCoroutine(this.testCoroutine);
                this.testCoroutine = null;
                Debug.Log("[TestTool] Đã dừng.");
            }
        }

        private IEnumerator AutoPlayRoutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(this.stepDelay);

                var match = this.board.Editor_FindFirstValidMatch();
                if (match != null)
                {
                    this.board.Editor_SimulateClick(match.node1);
                    yield return new WaitForSeconds(0.1f); 
                    this.board.Editor_SimulateClick(match.node2);
                }
                else
                {
                    Debug.Log("<color=green>[TestTool] Hoàn thành test! Không còn cặp nào nối được nữa.</color>");
                    break;
                }
            }
        }
    }
}
#endif