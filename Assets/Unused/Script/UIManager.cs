//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;


//// ���� �� ��� UI ��Ҹ� �����ϴ� �Ŵ��� Ŭ����
//public class UIManager : MonoBehaviour
//{
//    [SerializeField] private GameObject enemyUIPrefab;
//    [SerializeField] private GameObject operatorUIPrefab;
//    //[SerializeField] private Canvas worldSpaceCanvas; // ���� ĵ����

//    // UI��Ҹ� Key - Value�� ������
//    private Dictionary<int, GameObject> enemyUIElements = new Dictionary<int, GameObject>(); 
//    private Dictionary<int, GameObject> operatorUIElements = new Dictionary<int, GameObject>();

//    private static UIManager instance;
//    public static UIManager Instance
//    {
//        get
//        {
//            if (instance == null)
//            {
//                instance = FindObjectOfType<UIManager>();
//                if (instance == null)
//                {
//                    GameObject obj = new GameObject("UIManager");
//                    instance = obj.AddComponent<UIManager>();
//                }
//            }
//            return instance; 
//        }
//    }

//    private void Awake()
//    {
//        if (instance == null)
//        {
//            instance = this;
//            //DontDestroyOnLoad(gameObject);
//        }
//        else
//        {
//            Destroy(gameObject);
//        }

//        //if (worldSpaceCanvas == null)
//        //{   
//        //    // ���� ĵ���� ���� �� ���� �Ҵ�
//        //    GameObject canvasObj = new GameObject("WorldSpaceCanvas");
//        //    worldSpaceCanvas = canvasObj.AddComponent<Canvas>();
//        //    worldSpaceCanvas.renderMode = RenderMode.WorldSpace; // ���� ������
//        //    canvasObj.AddComponent<CanvasScaler>();
//        //    canvasObj.AddComponent<GraphicRaycaster>();
//        //}
//    }

//    /// <summary>
//    /// Enemy - EnemyUI ���� ��Ī. Enemy�� ������ �� ȣ���Ѵ�.
//    /// </summary>
//    public void CreateEnemyUI(Enemy enemy)
//    {
//        //GameObject uiElement = Instantiate(enemyUIPrefab, worldSpaceCanvas.transform);
//        GameObject uiElement = Instantiate(enemyUIPrefab, enemy.transform.position, Quaternion.identity);
//        EnemyUI enemyUI = uiElement.GetComponent<EnemyUI>();
//        //enemyUI.SetTarget(enemy);
//        enemyUIElements.Add(enemy.GetInstanceID(), uiElement);
//    }

//    public void CreateOperatorUI(Operator op)
//    {
//        GameObject uiElement = Instantiate(operatorUIPrefab, op.transform.position, Quaternion.identity);
//        OperatorUI operatorUI = uiElement.GetComponent<OperatorUI>();
//        //operatorUI.SetTarget(op);
//        enemyUIElements.Add(op.GetInstanceID(), uiElement);
//    }

//    public void RemoveEnemyUI (Enemy enemy)
//    {
//        int id = enemy.GetInstanceID();
//        if (enemyUIElements.TryGetValue(id, out GameObject uiElement))
//        {
//            Destroy(uiElement);
//            enemyUIElements.Remove(id);
//        }
//    }

//    public void RemoveOperatorUI(Operator op)
//    {
//        int id = op.GetInstanceID();
//        if (operatorUIElements.TryGetValue(id, out GameObject uiElement))
//        {
//            Destroy(uiElement);
//            operatorUIElements.Remove(id);
//        }
//    }

//    public void UpdateEnemyUI(Enemy enemy)
//    {
//        int id = enemy.GetInstanceID();

//        if (enemyUIElements.TryGetValue(id, out GameObject uiElement))
//        {
//            EnemyUI enemyUI = uiElement.GetComponent<EnemyUI>();
//            enemyUI.UpdateUI();
//        }
//    }

//    public void UpdateOperatorUI(Operator op)
//    {
//        int id = op.GetInstanceID();
//        if (operatorUIElements.TryGetValue(id, out GameObject uiElement))
//        {
//            OperatorUI operatorUI = uiElement.GetComponent<OperatorUI>();
//            operatorUI.UpdateUI();
//        }
//    }
//}
