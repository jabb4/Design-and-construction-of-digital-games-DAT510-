using UnityEngine;
using UnityEngine.InputSystem;
namespace Combat
{
    public class HealingSystemScript : MonoBehaviour
    {
        public HealthComponent playerHealth;

        [SerializeField]
        private int amountBandages;
        public static event System.Action<int> OnBandagesChanged;

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

            if (InputSystem.actions.FindAction("Player/Heal").IsPressed()){
                UseBandage();
            }
        }

        private void UseBandage()
        {
            if (healTime < healDelay) return;
            if (amountBandages < 1) return;

            healTime = 0;

            DecreaseBandages(1);
            OnBandagesChanged?.Invoke(amountBandages);

            playerHealth.Heal(healAmount);

        }
        void OnTriggerEnter(Collider other)
        {
            if (other.tag == "Bandage")
            {
                IncreaseBandages(1);

                Destroy(other.gameObject);
            }
        }
        void IncreaseBandages(int amuont)
        {
            amountBandages += amountBandages;
            OnBandagesChanged?.Invoke(amountBandages);
        }
        void DecreaseBandages(int amount)
        {
            amountBandages -= amountBandages;
            OnBandagesChanged?.Invoke(amountBandages);
        }
        void SetBandages(int amount)
        {
            amountBandages = amount;
            OnBandagesChanged?.Invoke(amountBandages);
        }
        
    }
}