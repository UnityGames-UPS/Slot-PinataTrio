// VIKING GAME - PHYSICS TRIGGER STOPPER FOR COLLIDER-BASED WHEEL — NOT USED IN THIS GAME.
// THE GREEN PIÑATA WHEEL NOW STOPS VIA DOTWEEN ANGLE CALCULATION IN BonusController.StopWheel().

// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;

// public class Stopper : MonoBehaviour
// {
//     [SerializeField]
//     private BonusController _controller;

//     private void OnTriggerEnter2D(Collider2D collision)
//     {
//         if (!_controller.isCollision)
//         {
//             _controller.isCollision = true;
//             Debug.Log("collision done");
//             _controller.StopWheel();
//         }
//     }

//     private void OnTriggerExit2D(Collider2D collision)
//     {
//         if (!_controller.isCollision)
//         {
//             _controller.isCollision = true;
//             Debug.Log("collision done");
//             _controller.StopWheel();
//         }
//     }

//     private void OnTriggerStay2D(Collider2D collision)
//     {
//         if (!_controller.isCollision)
//         {
//             _controller.isCollision = true;
//             Debug.Log("collision done");
//             _controller.StopWheel();
//         }
//     }
// }
