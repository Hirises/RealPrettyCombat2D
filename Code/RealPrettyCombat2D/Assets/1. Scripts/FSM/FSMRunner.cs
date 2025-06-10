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
}
