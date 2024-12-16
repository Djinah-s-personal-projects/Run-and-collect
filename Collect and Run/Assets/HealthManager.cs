using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthManager : MonoBehaviour
{

    [SerializeField] private HealthControl healthManager;
    public bool gameOver = false;

    private void OnEnable()
    {
        healthManager.OnHealthChanged += HandleHealthChanged;
    }

    private void OnDisable()
    {
        healthManager.OnHealthChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(int newHealth)
    {
        if(newHealth == 0)
        {
            gameOver = true;
            // TODO ajouter le control de la fin du jeu 
            // Activation d'un panel you lost
        }
        
        Debug.Log("La santé a changé : " + newHealth);
    }
}
