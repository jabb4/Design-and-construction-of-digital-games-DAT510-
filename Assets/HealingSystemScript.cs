using UnityEngine;
using UnityEngine.InputSystem;
namespace Combat
{
    public class HealingSystemScript : MonoBehaviour
    {
        public HealthComponent playerHealth;

        [SerializeField]
        private int amountBandages;
        [SerializeField]
        private int healAmount;


        // Time until bandage can be used again
        [SerializeField]
        private float healDelay = 3;

        private float healTime;

        // Update is called once per frame
        void Update()
        {
            if (healTime <= healDelay) healTime += Time.deltaTime;

            /* 
             TODO: 
             Update the ui so that the amount of bandages is displayed.
            
            */

            if (InputSystem.actions.FindAction("Player/Heal").IsPressed()){
                UseBandage();
            }
        }

        private void UseBandage()
        {
            if (healTime < healDelay) return;
            if (amountBandages < 1) return;

            healTime = 0;
            amountBandages--;

            playerHealth.Heal(healAmount);

        }
        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Bandage")
            {
                amountBandages++;
                Destroy(other.gameObject);
            }
        }
    }
}