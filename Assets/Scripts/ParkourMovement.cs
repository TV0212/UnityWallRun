using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class ParkourMovement : MonoBehaviour
{
    GameObject player;
    StarterAssets.FirstPersonController controllerScr;
    CharacterController controller;
    StarterAssets.StarterAssetsInputs inputs;
    CinemachineVirtualCamera virtualCamera;

    Vector3 wallRunNormal;
    Vector3 impact;

    bool wallRunSuppressed = false;
    bool wallRunning = false;
    bool wallRunningR = false;
    bool wallRunningL = false;

    [SerializeField] float mass = 25.0f;
    float defaultGravity;
    [SerializeField] float cameraTiltSpeed = 50.0f;
    [SerializeField] float wallRunSpeed = 5;
    [SerializeField] float wallRunTargetGravity = -4.0f;
    [SerializeField] float wallRunJumpHeight = 1.5f;
    [SerializeField] float wallRunJumpOffForce = 300.0f;
    float timeTillWallJump = 0.05f;

    // Start is called before the first frame update
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        virtualCamera = GameObject.Find("PlayerFollowCamera").GetComponent<CinemachineVirtualCamera>();

        controllerScr = player.GetComponent<StarterAssets.FirstPersonController>();
        controller = player.GetComponent<CharacterController>();
        inputs = player.GetComponent<StarterAssets.StarterAssetsInputs>();

        defaultGravity = controllerScr.Gravity;
    }

    // Update is called once per frame
    void Update()
    {
        if (!wallRunSuppressed) WallRunUpdate();
        CameraTick();

        if (impact.magnitude > 0.2 && !controllerScr.Grounded)
        {
            controller.Move(impact * Time.deltaTime);
            // consumes the impact energy each cycle:
            impact = Vector3.Lerp(impact, Vector3.zero, 2 * Time.deltaTime);

        }
        else impact = Vector3.zero;

        if (timeTillWallJump <= 0.0f && inputs.jump) WallRunJump();
    }

    void AddImpact(Vector3 dir, float force)
    {
        dir.Normalize();
        if (dir.y < 0) dir.y = -dir.y; // reflect down force on the ground
        impact += dir.normalized * force / mass;
    }

    void WallRunUpdate()
    {
        Vector3 left;
        Vector3 right;
        WallRunVector(out right, out left);

        if (WallRunMovement(player.transform.position, right, -1.0f))
        {
            wallRunning = true;
            wallRunningL = false;
            wallRunningR = true;

            if (controller.velocity.y <= 0.0f) controllerScr.Gravity = wallRunTargetGravity;
            if (timeTillWallJump > 0.0f) timeTillWallJump -= Time.deltaTime;
        }
        else if (WallRunMovement(player.transform.position, left, 1.0f))
        {
            wallRunning = true;
            wallRunningL = true;
            wallRunningR = false;

            if (controller.velocity.y <= 0.0f) controllerScr.Gravity = wallRunTargetGravity;
            if (timeTillWallJump > 0.0f) timeTillWallJump -= Time.deltaTime;
        }
        else
        {
            WallRunEnd(0.0f);
        }
    }

    private void WallRunVector(out Vector3 right, out Vector3 left)
    {
        right = player.transform.position + player.transform.right;
        left = player.transform.position + -player.transform.right;
    }

    bool WallRunMovement(Vector3 start, Vector3 end, float wallRunDirection)
    {
        RaycastHit hitInfo;
        if (Physics.Linecast(start, end, out hitInfo))
        {
            if (IsValidWallVector(hitInfo.normal) && !controllerScr.Grounded)
            {
                wallRunNormal = hitInfo.normal;

                //Stick to Wall
                AddImpact(-wallRunNormal, 1.0f);
                //Move forward
                AddImpact(Vector3.Cross(wallRunNormal, Vector3.up), wallRunSpeed * wallRunDirection);

                return true;
            }
        }

        return false;
    }

    bool IsValidWallVector(Vector3 normal)
    {
        if (normal.y < 0.52 && normal.y > -0.52)
        {
            return true;
        }

        return false;
    }

    void WallRunEnd(float resetTime)
    {
        if (wallRunning)
        {
            wallRunning = false;
            wallRunningL = false;
            wallRunningR = false;

            controllerScr.Gravity = defaultGravity;

            SuppressWallRun(resetTime);
            timeTillWallJump = 0.05f;
        }
    }

    IEnumerator SuppressWallRun(float delay)
    {
        wallRunSuppressed = true;

        yield return new WaitForSeconds(delay);

        yield return null;
    }

    void CameraTick()
    {
        if (wallRunningL)
        {
            CameraTilt(-15.0f);
        }
        else if (wallRunningR)
        {
            CameraTilt(15.0f);
        }
        else
        {
            CameraTilt(0.0f);
        }
    }

    void CameraTilt(float rollDegree)
    {
        if (virtualCamera.m_Lens.Dutch < rollDegree - 0.5)
            virtualCamera.m_Lens.Dutch += cameraTiltSpeed * Time.deltaTime;
        else if (virtualCamera.m_Lens.Dutch > rollDegree + 0.5)
            virtualCamera.m_Lens.Dutch -= cameraTiltSpeed * Time.deltaTime;
        else
            virtualCamera.m_Lens.Dutch = rollDegree;
    }

    void WallRunJump()
    {
        if (wallRunning)
        {
            WallRunEnd(0.35f);
            Vector3 jumpDir = new Vector3(wallRunNormal.x * 1.5f, wallRunJumpHeight, wallRunNormal.z * 1.5f) + player.transform.forward;
            AddImpact(jumpDir, wallRunJumpOffForce);
        }
    }
}
