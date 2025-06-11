using Assets._1._Scripts;
using NaughtyAttributes;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FSMRunner : MonoBehaviour
{
    [Required]
    [SerializeField]
    private FSMBase FSM;
    [Required]
    [SerializeField]
    private Animator anim;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        FSM.Init(anim);
    }

    // Update is called once per frame
    void Update()
    {
        FSM.Update();
    }

    #region Getter & Setter
    public void SetBool(string name, bool value)
    {
        FSM.SetVariable(name, value);
    }

    public void SetInt(string name, int value)
    {
        FSM.SetVariable(name, value);
    }

    public void SetFloat(string name, float value)
    {
        FSM.SetVariable(name, value);
    }

    public void SetTrigger(string name)
    {
        FSM.SetVariable(name, true);
    }

    public int GetInt(string name)
    {
        return FSM.GetVariable<int>(name);
    }

    public float GetFloat(string name)
    {
        return FSM.GetVariable<float>(name);
    }

    public bool GetBool(string name)
    {
        return FSM.GetVariable<bool>(name);
    } 

    public void AddTriggerEvent(string name, System.Action action)
    {
        FSM.AddTriggerEvent(name, action);
    }

    public void RemoveTriggerEvent(string name, System.Action action)
    {
        FSM.RemoveTriggerEvent(name, action);
    }
    #endregion
}
