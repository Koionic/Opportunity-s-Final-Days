﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhotoCamera : MonoBehaviour
{
    [SerializeField]
    float minZoom = 40f, maxZoom = 90f, zoomSpeed = 60f;

    public float zoomPercent;

    [SerializeField]
    float viewportMargin;

    [SerializeField]
    float losRadius;

    [SerializeField]
    float distanceMargin, distanceModifier;

    Camera roverCamera;

    Camera photoCamera;

    [SerializeField]
    Transform cameraTarget;

    public bool targetInRange;
    public bool targetInView;
    public bool targetObscured;

    bool takePhotoNextFrame;

    RoverController roverController;

    // Start is called before the first frame update
    void Start()
    {
        roverController = RoverController.instance;
        roverCamera = roverController.fpsCamera;
        photoCamera = GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        if (roverController.cameraMode)
        {
            UpdateCamera(Input.GetAxis("CameraHorizontal"));

            if (QuestController.instance != null)
            {
                if (QuestController.instance.currentQuestType == QuestType.Photo)
                {
                    if (cameraTarget != null)
                    {
                        CheckLineOfSight();
                        targetInView = CheckVisionOfTarget(cameraTarget.position, roverCamera.fieldOfView - viewportMargin);
                    }
                    else
                    {
                        cameraTarget = QuestController.instance.spawnedObject.transform;
                    }
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                TriggerPhoto(Screen.width, Screen.height);
            }
        }
    }

    void UpdateCamera (float zoomAmount)
    {
        if (zoomAmount != 0f)
        {
            float deltaZoom = Mathf.Clamp(roverCamera.fieldOfView + (zoomAmount * Time.deltaTime * zoomSpeed), minZoom, maxZoom);
            roverCamera.fieldOfView = deltaZoom;

            zoomPercent = 1f - Mathf.InverseLerp(minZoom, maxZoom, roverCamera.fieldOfView); 
        }
    }

    public bool CheckVisionOfTarget(Vector3 target, float searchAngle)
    {
        //creates a vector to compare against the vector between the player and the target
        Vector3 normalisedHeading = roverCamera.transform.forward;
        Vector3 normalisedTargetVector = (target - roverCamera.transform.position).normalized;
        float dotProduct = Vector3.Dot(normalisedHeading, normalisedTargetVector);
        float angle = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;


        if (angle < searchAngle)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //function that checks if the player is in the enemies physical line of sight
    void CheckLineOfSight()
    {
        RaycastHit raycast;
        
        //checks if anything is in the players direction
        if (Physics.SphereCast(roverCamera.transform.position, losRadius, roverCamera.transform.forward, out raycast))
        {
            //sets the bool to true if the player is hit by the ray
            if (raycast.collider.CompareTag("CameraTarget") && raycast.collider.gameObject == cameraTarget.gameObject)
            {
                targetObscured = false;

                float distanceBuffer = distanceMargin + (zoomPercent * distanceModifier);

                if (raycast.distance < distanceBuffer)
                {
                    targetInRange = true;
                }
                else
                {
                    targetInRange = false;
                }
            }
            else
            {
                targetObscured = true;
            }
        }
    }

    private void OnPostRender()
    {
        if (takePhotoNextFrame)
        {
            takePhotoNextFrame = false;

            TakePhoto();
        }
    }

    private void TriggerPhoto(int width, int height)
    {
        photoCamera.fieldOfView = roverCamera.fieldOfView;
        photoCamera.transform.rotation = roverCamera.transform.rotation;

        photoCamera.targetTexture = RenderTexture.GetTemporary(width, height, 16);
        takePhotoNextFrame = true;
    }

    void TakePhoto()
    {
        roverCamera.enabled = false;

        RenderTexture renderTexture = photoCamera.targetTexture;

        Texture2D renderResult = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
        renderResult.ReadPixels(rect, 0, 0);

        if (QuestController.instance != null)
        {
            if (QuestController.instance.currentQuestType == QuestType.Photo)
            {
                bool correct = targetInView && targetInRange && !targetObscured;

                Texture2D photo = renderResult;

                photo.Apply();

                QuestController.instance.currentQuest.CheckPhoto(photo, correct);
            }
        }

    /*    byte[] byteArray = renderResult.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/CameraScreenshot.png", byteArray); */

        RenderTexture.ReleaseTemporary(renderTexture);
        photoCamera.targetTexture = null;

        roverCamera.enabled = true;
    }

}
