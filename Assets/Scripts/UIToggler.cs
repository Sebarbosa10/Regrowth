using UnityEngine;

public class UIToggler : MonoBehaviour
{
    [System.Serializable]
    public class UIAction
    {
        public enum ActionType { Open, Close, Toggle }
        public GameObject target;
        public ActionType action = ActionType.Toggle;
    }

    [SerializeField] private UIAction[] actions;

    public void Execute()
    {
        foreach (UIAction a in actions)
        {
            if (a.target == null) continue;

            switch (a.action)
            {
                case UIAction.ActionType.Open: a.target.SetActive(true); break;
                case UIAction.ActionType.Close: a.target.SetActive(false); break;
                case UIAction.ActionType.Toggle: a.target.SetActive(!a.target.activeSelf); break;
            }
        }
    }
}