using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestTowerAnimation : MonoBehaviour
{
    public Transform weapon;
    public Transform weaponBase;
    public Transform arrowObject;

    public float arrowSpeed;
    public float arkHeight;

    public float origRotationSpeed = 60f;

    public float rotationSpeed = 60f;
    public float weaponRotationSpeed = 60f;

    public float radius;

    private Vector3 target;
    private Vector3 calculatedVelocity;
    private float gravity;

    //private Transform testTarget;

    public DynamicSettings baseSetting;
    public DynamicSettings weaponSettings;

    private SecondOrderDynamicsVector baseDynamics;
    private SecondOrderDynamicsVector weaponDynamics;

    private Vector3 currentBase;
    private Vector3 currentWeapon;

    private Vector3 baseTarget;
    private Vector3 weaponTarget;


    public Transform baseTargetDebug;
    public Transform currentBaseDebug;

    public Transform weaponTargetDebug;
    public Transform currentWeaponDebug;
    public Transform targetDebug;

    public bool newCode = false;

    // Start is called before the first frame update
    void Start()
    {
        baseDynamics = new SecondOrderDynamicsVector(baseSetting.F, baseSetting.Z, baseSetting.R, weaponBase.forward);
        weaponDynamics = new SecondOrderDynamicsVector(weaponSettings.F, weaponSettings.Z, weaponSettings.R, weapon.forward);
        currentWeapon = weaponTarget = weapon.forward;
        currentBase = baseTarget = weaponBase.forward;
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.N)) {
            target = transform.position + Quaternion.Euler(0f,Random.Range(0f,360f),0f) * (Vector3.forward * Random.Range(radius - 2, radius + 2));
            targetDebug.position = target;
            if (!fts.solve_ballistic_arc_lateral(arrowObject.position, arrowSpeed, target, Vector3.zero, arkHeight, out calculatedVelocity, out gravity, out var impactPoint)) {
                Debug.LogError("ballistic error");
            }
            Quaternion targetRotation = Quaternion.LookRotation(calculatedVelocity);
            baseTarget = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f) * Vector3.forward;
            weaponTarget = Quaternion.Euler(targetRotation.eulerAngles.x, 0f, 0f) * Vector3.forward;
            //currentBase = baseTarget;
            //currentTarget = weaponTarget;
        }

        RotateGun();

        baseTargetDebug.forward = baseTarget;
        currentBaseDebug.forward = currentBase;

        weaponTargetDebug.forward = weaponTarget;
        currentWeaponDebug.forward = currentWeapon;
}

    void RotateGun() {
        currentBase = Vector3.RotateTowards(currentBase, baseTarget, rotationSpeed * Mathf.Deg2Rad * Time.deltaTime, 1f);
        currentWeapon = Vector3.RotateTowards(currentWeapon, weaponTarget, weaponRotationSpeed * Mathf.Deg2Rad * Time.deltaTime, 1f * Time.deltaTime);

        if (newCode) {
            weaponBase.forward = baseDynamics.Update(Time.deltaTime, currentBase);
            weapon.localRotation = Quaternion.LookRotation(weaponDynamics.Update(Time.deltaTime, currentWeapon));
        } else {
            baseDynamics.Update(Time.deltaTime, currentBase);
            weaponDynamics.Update(Time.deltaTime, currentWeapon);

            Quaternion targetRotation = Quaternion.LookRotation(calculatedVelocity);
            Quaternion baseRotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
            Quaternion crossBowRotation = Quaternion.Euler(targetRotation.eulerAngles.x, 0f, 0f);

            weaponBase.rotation = Quaternion.RotateTowards(weaponBase.rotation, baseRotation, origRotationSpeed * Time.deltaTime);
            weapon.localRotation = Quaternion.RotateTowards(weapon.localRotation, crossBowRotation, origRotationSpeed * Time.deltaTime);
        }
    }
}
