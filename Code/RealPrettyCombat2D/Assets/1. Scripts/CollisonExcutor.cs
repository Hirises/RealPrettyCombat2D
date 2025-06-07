using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Assets._1._Scripts
{
    public class CollisonExcutor : MonoBehaviour
    {
        [SerializeField]
        public UnityEvent<Collider2D> OnTriggerEnter;
        [SerializeField]
        public UnityEvent<Collider2D> OnTriggerExit;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            OnTriggerEnter?.Invoke(collision);
        }

        private void OnTriggerExit2D(Collider2D collision)
        {
            OnTriggerExit?.Invoke(collision);
        }
    }
}