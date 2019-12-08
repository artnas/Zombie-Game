using System;
using System.Collections;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

public class PlayerCharacter : MonoBehaviour
{
    public static PlayerCharacter Instance;

    private Animation _animation;
    private LineRenderer _shootLineRenderer;
    
    public readonly int MaxHealth = 3;
    public int CurrentHealth;

    public bool CanShoot = true;
    public bool IsMoving = false;

    void Start()
    {
        _animation = GetComponentInChildren<Animation>();
        _shootLineRenderer = GetComponentInChildren<LineRenderer>(true);
        Instance = this;
        
        var visionRadiusTransform = transform.Find("Radius");
        visionRadiusTransform.SetParent(null);
        visionRadiusTransform.localScale = Vector3.one * (shootingRange * 2f);
        visionRadiusTransform.SetParent(transform);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate()
    {
        if (!IsMoving && CanShoot)
        {
            UpdateShooting();
        }
    }

    private void OnDestroy()
    {
        Instance = null;
    }
    
#region Shooting

    private float shootingRange = 7;
    private float shootingInterval = 1f;
    private float lastShotTime = 0;

    private IEnumerator Shoot(Zombie zombie)
    {
        _animation.Play("Player-Gun-Shot");
        
        var rotation = Quaternion.LookRotation(zombie.transform.position - transform.position, Vector3.up);
        transform.rotation = rotation;

        var fromToVector = rotation * Vector3.forward;

        _shootLineRenderer.gameObject.SetActive(true);
        _shootLineRenderer.SetPositions(new Vector3[]{transform.position + Vector3.up * 4f + fromToVector * 2, zombie.transform.position + Vector3.up * 4f});
        
        yield return new WaitForSeconds(0.1f);

        _shootLineRenderer.gameObject.SetActive(false);
        
        zombie.Die();
    }

    private void UpdateShooting()
    {
        var nextShotTime = lastShotTime + shootingInterval;
        if (nextShotTime > Time.time)
        {
            return;
        }
			
        var minDistance = float.MaxValue;
        Zombie closestZombie = null;

        foreach (var zombie in GameManager.Instance.Zombies)
        {
            if (!zombie.IsAlive) continue;
				
            var distance = Vector3.Distance(zombie.transform.position, PlayerCharacter.Instance.transform.position);

            if (distance <= shootingRange && distance < minDistance)
            {
                minDistance = distance;
                closestZombie = zombie;
            }
        }

        if (closestZombie)
        {
            lastShotTime = Time.time;
            StartCoroutine(Shoot(closestZombie));
        }
    }
		
#endregion
}
