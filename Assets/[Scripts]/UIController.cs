using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    [Header("UI Controls")]
    public GameObject panel;
    public Button returnButton;
    public Text returnText;
    public PlayerBehaviour controller;
    public MouseLook cameraController;

    // Start is called before the first frame update
    void Start()
    {
        panel.SetActive(false);//dont show panel on start
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        //show panel if ` is pressed
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            panel.SetActive(!panel.activeInHierarchy);
            controller.noFire = true;
            cameraController.lockCursor = false;
            Cursor.lockState = (panel.activeInHierarchy) ? CursorLockMode.None : CursorLockMode.Locked;
            controller.noFire = (panel.activeInHierarchy) ? true : false;
            cameraController.lockCursor = (panel.activeInHierarchy) ? false : true;
        }
    }
}
