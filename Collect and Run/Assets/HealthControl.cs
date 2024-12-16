using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthControl : MonoBehaviour
{
    // Start is called before the first frame update
    int health = 3;
    public event Action<int> OnHealthChanged;

    public void decreaseLive()
    {
        health--;
        // Gérer dans le gameManager
        /*
        if(live_left == 0)
        {
            gameOver = true;
            // TODO Arrêter le jeu
        }*/
    }

    public int Health
    {
        get => health;
        set
        {
            if (health != value)
            {
                health = value;
                // Déclencher l'événement
                OnHealthChanged?.Invoke(health);
            }
        }
    }
}
