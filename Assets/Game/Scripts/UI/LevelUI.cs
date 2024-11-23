using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LevelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_MoveCountText;
    [SerializeField] private GameObject m_GoalsGrid;
    [SerializeField] private GameObject goalPrefab;

    [Header("Goal Icons")]
    [SerializeField] private Sprite boxIcon;
    [SerializeField] private Sprite stoneIcon;
    [SerializeField] private Sprite vaseIcon;

    private Dictionary<string, GoalInfo> goals = new Dictionary<string, GoalInfo>();
    private Dictionary<string, GoalElement> goalElements = new Dictionary<string, GoalElement>();

    public void InitializeGoals(LevelData levelData)
    {
        ClearGoals();
        CountObstacles(levelData);
        CreateGoalElements();
    }

    private void ClearGoals()
    {
        goals.Clear();
        foreach (var element in goalElements.Values)
        {
            Destroy(element.gameObject);
        }
        goalElements.Clear();
    }

    private void CountObstacles(LevelData levelData)
    {
        foreach (string gridItem in levelData.grid)
        {
            if (IsObstacle(gridItem))
            {
                if (!goals.ContainsKey(gridItem))
                {
                    goals[gridItem] = new GoalInfo
                    {
                        type = gridItem,
                        icon = GetObstacleIcon(gridItem),
                        initialCount = 1,
                        remainingCount = 1
                    };
                }
                else
                {
                    var info = goals[gridItem];
                    info.initialCount++;
                    info.remainingCount++;
                    goals[gridItem] = info;
                }
            }
        }
    }

    private void CreateGoalElements()
    {
        foreach (var goal in goals)
        {
            GameObject goalObj = Instantiate(goalPrefab, m_GoalsGrid.transform);
            GoalElement element = goalObj.GetComponent<GoalElement>();
            element.Initialize(goal.Value.icon, goal.Value.remainingCount);
            goalElements[goal.Key] = element;
        }
    }

    public void UpdateObstacleCount(string obstacleType)
    {
        if (goals.ContainsKey(obstacleType))
        {
            var info = goals[obstacleType];
            info.remainingCount--;
            goals[obstacleType] = info;

            if (goalElements.ContainsKey(obstacleType))
            {
                goalElements[obstacleType].UpdateCount(info.remainingCount);
            }
        }
    }

    private bool IsObstacle(string gridItem)
    {
        return gridItem == "bo" || gridItem == "s" || gridItem == "v";
    }

    private Sprite GetObstacleIcon(string obstacleType)
    {
        return obstacleType switch
        {
            "bo" => boxIcon,
            "s" => stoneIcon,
            "v" => vaseIcon,
            _ => null
        };
    }

    public void UpdateMoveCount(int remainingMoves)
    {
        m_MoveCountText.text = remainingMoves.ToString();
    }

    public bool AreAllGoalsComplete()
    {
        foreach (var goal in goals.Values)
        {
            if (goal.remainingCount > 0)
                return false;
        }
        return true;
    }
}