using Assets._1._Scripts;
using UnityEngine;

public class FSMRunner : MonoBehaviour
{
    [SerializeField]
    public FSMBase FSM;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        FSM.Awake();
    }

    // Update is called once per frame
    void Update()
    {
        FSM.Update();
    }
}
