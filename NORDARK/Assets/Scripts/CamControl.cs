using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamControl : MonoBehaviour
{
    private float movementTime = 5;
    private float rotationAmount = 1;
    private Vector3 zoomAmount = new Vector3(-10, -10, 0);

    private float maxZoom = 100;
    private float minZoom = -400;

    public Transform controllerTransform;
    public Transform cameraTransform;
    public Vector3 newPosition;
    public Quaternion newRotation;
    public Vector3 newZoom;
    public Camera myCamera;

    private Vector3 dragStartPosition;
    private Vector3 dragCurrentPosition;

    private Vector3 rotateStartPosition;
    private Vector3 rotateCurrentPosition;

    public Vector3 camStart;
    public Vector3 zoomStart;
    public Quaternion rotStart;
    // Start is called before the first frame update
    void Start()
    {
        camStart = transform.position;
        zoomStart = cameraTransform.localPosition;
        rotStart = transform.rotation;
        newPosition = transform.position;
        newRotation = transform.rotation;
        newZoom = cameraTransform.localPosition;
        
    }

    // Update is called once per frame
    void Update()
    {
            HandleMouseInput();
            HandleMovementInput();
    }

    void HandleMouseInput()
    {
        bool UIhit = GameObject.Find("Canvas").GetComponent<CanvasRaycast>().isUI;
        // Mouse Zoom
        if (Input.mouseScrollDelta.y!=0)
        {
            if (Input.mouseScrollDelta.y>0 && newZoom.z < maxZoom)
            {
                newZoom += Input.mouseScrollDelta.y * zoomAmount;
            }
            if (Input.mouseScrollDelta.y < 0 && newZoom.z > minZoom)
            {
                newZoom += Input.mouseScrollDelta.y * zoomAmount;
            }
        }

        // Mouse Pan    

        if (Input.GetMouseButtonDown(0))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);

            float hit;

            if(plane.Raycast(ray, out hit) & !UIhit)
            {
                dragStartPosition = ray.GetPoint(hit);
            }
        }

        if (Input.GetMouseButton(0))
        {
            Plane plane = new Plane(Vector3.up, Vector3.zero);

            Ray ray = myCamera.ScreenPointToRay(Input.mousePosition);

            float hit;

            if(plane.Raycast(ray, out hit) & !UIhit)
            {
                dragCurrentPosition = ray.GetPoint(hit);

                newPosition = transform.position + dragStartPosition - dragCurrentPosition;
            }
        }

        // Mouse Rotation

        if (Input.GetMouseButtonDown(2))
        {
            rotateStartPosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            rotateCurrentPosition = Input.mousePosition;
            
            Vector3 difference = rotateStartPosition - rotateCurrentPosition;

            rotateStartPosition = rotateCurrentPosition;

            newRotation *= Quaternion.Euler(Vector3.up * (-difference.x/5));

        }


    }

    void HandleMovementInput()
    {
        // Keyboard rotation (Using W A S D Keys)

        if(Input.GetKey(KeyCode.A))
        {
            newRotation *= Quaternion.Euler(Vector3.up * rotationAmount);
        }

        if(Input.GetKey(KeyCode.D))
        {
            newRotation *= Quaternion.Euler(Vector3.up * -rotationAmount);
        }

        if(Input.GetKey(KeyCode.W))
        {
            newRotation *= Quaternion.Euler(new Vector3(0, 0, 1) * rotationAmount);
        }

        if(Input.GetKey(KeyCode.S))
        {
            newRotation *= Quaternion.Euler(new Vector3(0, 0, -1) * rotationAmount);
        }

        transform.position = Vector3.Lerp(transform.position, newPosition, Time.deltaTime * movementTime);
        transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, Time.deltaTime * movementTime);
        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, newZoom, Time.deltaTime * movementTime);
    }

    public void Exit()
    {
        Application.Quit();
    }


}
