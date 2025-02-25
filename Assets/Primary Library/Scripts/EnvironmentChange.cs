using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentChange : MonoBehaviour
{
    private int _case = 0;

    [SerializeField]
    private PlayerMovement _playerMovement;

    public Material skyBox2;
    public Material skyBox3;

    // Start is called before the first frame update
    void Start()
    {
        _playerMovement = GameObject.Find("Player").GetComponent<PlayerMovement>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

   public void switchEnvironment()
    {
        float zAxis = _playerMovement.ReturnZAxis();

        if (zAxis >= 150.0f && zAxis < 300.0f)
        {
            _case = 2;
            Debug.Log("Z Axis Case 2 ========" +zAxis);
        }
        else if (zAxis >= 300.0f)
        {
            _case = 3;
            Debug.Log("Z Axis Case 3 ========" + zAxis);
        }

        switch (_case)
        {
            case 2:
            funcChange();
            break;

            case 3:
            funcChange2();
            break;

            default:
            Debug.Log("Reached default val");
            break;

        }
    }

    void funcChange()
    {
        Debug.Log("Reached Case 2");
         
        RenderSettings.skybox = skyBox2;
       
    }

    void funcChange2()
    {
        Debug.Log("Reached Case 3");
        RenderSettings.skybox = skyBox3;
    }
}
