namespace CahtFramework.Pikachu
{
    using UnityEngine;

    public class LevelManager : MonoBehaviour
    {
        [Header("Database")] 
        [SerializeField] private IODatabase levelDatabase;
        [SerializeField] private ThemeData currentTheme;

        [Header("Components")] 
        [SerializeField] private BoardController boardController;

        public int CurrentLevelIndex = 0;
        private LevelData currentLevelData;

        private void Start()
        {
            this.Preload();
            this.LoadLevel(this.CurrentLevelIndex);
        }

        public void Preload()
        {
            if (this.levelDatabase == null)
            {
                Debug.LogError("[LevelManager] Level Database is missing! Please assign it in the Inspector.");
                return;
            }

            Debug.Log($"[LevelManager] Preloaded {this.levelDatabase.Count} levels from database.");
        }

        public void LoadLevel(int index)
        {
            if (this.levelDatabase == null || this.levelDatabase.Count == 0) return;

            if (index < 0)
                index = this.levelDatabase.Count - 1;
            else if (index >= this.levelDatabase.Count) 
                index = 0;

            if (this.boardController != null) 
                this.boardController.ClearBoard();

            this.CurrentLevelIndex = index;
            this.currentLevelData = this.levelDatabase.GetDataByID<LevelData>(index);

            Debug.Log($"[LevelManager] Loading Level {index}: {this.currentLevelData.CodeName}");

            if (this.boardController != null) 
                this.boardController.GenerateBoard(this.currentLevelData, this.currentTheme);
        }

        public void ReloadLevel()
        {
            this.LoadLevel(this.CurrentLevelIndex);
        }

        public void NextLevel()
        {
            this.LoadLevel(this.CurrentLevelIndex + 1);
        }

        public void PrevLevel()
        {
            this.LoadLevel(this.CurrentLevelIndex - 1);
        }
    }
}