using System.Collections.Generic;
using Combat;
using NUnit.Framework;
using UnityEngine;

public class LockOnTargetServiceTests
{
    private const int EnemyLayer = 0;
    private const int PlayerLayer = 9;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Object.DestroyImmediate(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
    }

    [Test]
    public void Lock_Breaks_When_Target_Dies_Without_Fallback()
    {
        CreateCameraAndPlayer(out Camera camera, out Transform player);
        GameObject enemy = CreateEnemy(new Vector3(0f, 1f, 8f));

        LockOnTargetService service = CreateService(player, camera);
        Transform target = service.FindBestTarget();

        Assert.AreEqual(enemy.transform, target);

        HealthComponent health = target.GetComponent<HealthComponent>();
        health.ApplyDamage(1000f);

        bool isInvalid = service.TryGetInvalidReason(target, 0f, out LockInvalidReason reason);

        Assert.IsTrue(isInvalid);
        Assert.AreEqual(LockInvalidReason.Dead, reason);
        Assert.IsNull(service.FindBestTarget(target));
    }

    [Test]
    public void Lock_Switches_When_Target_Dies_With_Fallback()
    {
        CreateCameraAndPlayer(out Camera camera, out Transform player);
        CreateEnemy(new Vector3(0f, 1f, 8f));
        CreateEnemy(new Vector3(2f, 1f, 8f));

        LockOnTargetService service = CreateService(player, camera);
        Transform target = service.FindBestTarget();
        Assert.IsNotNull(target);

        HealthComponent health = target.GetComponent<HealthComponent>();
        health.ApplyDamage(1000f);

        bool isInvalid = service.TryGetInvalidReason(target, 0f, out LockInvalidReason reason);
        Transform replacement = service.FindBestTarget(target);

        Assert.IsTrue(isInvalid);
        Assert.AreEqual(LockInvalidReason.Dead, reason);
        Assert.IsNotNull(replacement);
        Assert.AreNotEqual(target, replacement);
    }

    [Test]
    public void Lock_Persists_During_Occlusion_Within_Grace()
    {
        CreateCameraAndPlayer(out Camera camera, out Transform player);
        CreateEnemy(new Vector3(0f, 1f, 8f));

        LockOnTargetService service = CreateService(player, camera, lineOfSightGraceSeconds: 0.5f);
        Transform target = service.FindBestTarget();
        Assert.IsNotNull(target);

        CreateOccluder(new Vector3(0f, 1f, 1f), new Vector3(3f, 3f, 1f));
        Physics.SyncTransforms();

        bool isInvalid = service.TryGetInvalidReason(target, 0.25f, out _);
        Assert.IsFalse(isInvalid);
    }

    [Test]
    public void Lock_Breaks_After_Occlusion_Exceeds_Grace()
    {
        CreateCameraAndPlayer(out Camera camera, out Transform player);
        CreateEnemy(new Vector3(0f, 1f, 8f));

        LockOnTargetService service = CreateService(player, camera, lineOfSightGraceSeconds: 0.5f);
        Transform target = service.FindBestTarget();
        Assert.IsNotNull(target);

        CreateOccluder(new Vector3(0f, 1f, 1f), new Vector3(3f, 3f, 1f));
        Physics.SyncTransforms();

        bool firstCheck = service.TryGetInvalidReason(target, 0.3f, out _);
        bool secondCheck = service.TryGetInvalidReason(target, 0.25f, out LockInvalidReason reason);

        Assert.IsFalse(firstCheck);
        Assert.IsTrue(secondCheck);
        Assert.AreEqual(LockInvalidReason.Occluded, reason);
    }

    [Test]
    public void Lock_Breaks_When_Target_OutOfBreakDistance()
    {
        CreateCameraAndPlayer(out Camera camera, out Transform player);
        GameObject enemy = CreateEnemy(new Vector3(0f, 1f, 8f));

        LockOnTargetService service = CreateService(player, camera, lockOnRange: 50f, lockBreakDistance: 10f);
        Transform target = service.FindBestTarget();
        Assert.IsNotNull(target);

        enemy.transform.position = new Vector3(0f, 1f, 20f);
        Physics.SyncTransforms();

        bool isInvalid = service.TryGetInvalidReason(target, 0f, out LockInvalidReason reason);

        Assert.IsTrue(isInvalid);
        Assert.AreEqual(LockInvalidReason.OutOfRange, reason);
    }

    [Test]
    public void Lock_Ignores_PlayerCollider_In_LOS_Raycast()
    {
        CreateCameraAndPlayer(out Camera camera, out Transform player);
        CreateEnemy(new Vector3(0f, 1f, 8f));

        LockOnTargetService service = CreateService(player, camera);

        Transform target = service.FindBestTarget();
        Assert.IsNotNull(target);

        bool isInvalid = service.TryGetInvalidReason(target, 0.1f, out _);
        Assert.IsFalse(isInvalid);
    }

    [Test]
    public void SwitchTarget_Chooses_Visible_Valid_Target_Only()
    {
        CreateCameraAndPlayer(out Camera camera, out Transform player);
        GameObject current = CreateEnemy(new Vector3(0f, 1f, 8f));
        GameObject visibleRight = CreateEnemy(new Vector3(3f, 1f, 8f));
        GameObject deadRight = CreateEnemy(new Vector3(5f, 1f, 8f));

        HealthComponent deadHealth = deadRight.GetComponent<HealthComponent>();
        deadHealth.ApplyDamage(1000f);

        LockOnTargetService service = CreateService(player, camera);
        Transform switched = service.FindSwitchTarget(current.transform, Vector2.right);

        Assert.AreEqual(visibleRight.transform, switched);
    }

    private LockOnTargetService CreateService(
        Transform player,
        Camera camera,
        float lockOnRange = 30f,
        float lockBreakDistance = 35f,
        float lineOfSightGraceSeconds = 0.5f)
    {
        return new LockOnTargetService(
            player,
            camera,
            1 << EnemyLayer,
            ~0,
            lockOnRange,
            lockBreakDistance,
            lineOfSightGraceSeconds,
            1.2f,
            false);
    }

    private void CreateCameraAndPlayer(out Camera camera, out Transform player)
    {
        GameObject playerGO = new GameObject("Player");
        playerGO.transform.position = Vector3.zero;
        playerGO.layer = PlayerLayer;
        CapsuleCollider playerCollider = playerGO.AddComponent<CapsuleCollider>();
        playerCollider.center = new Vector3(0f, 1f, 0f);
        playerCollider.height = 2f;
        player = playerGO.transform;
        spawnedObjects.Add(playerGO);

        GameObject cameraGO = new GameObject("TestCamera");
        camera = cameraGO.AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 2f, -6f);
        camera.transform.LookAt(player.position + Vector3.up);
        camera.fieldOfView = 60f;
        spawnedObjects.Add(cameraGO);
    }

    private GameObject CreateEnemy(Vector3 position)
    {
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "Enemy";
        enemy.transform.position = position;
        enemy.layer = EnemyLayer;
        enemy.AddComponent<HealthComponent>();
        spawnedObjects.Add(enemy);
        return enemy;
    }

    private void CreateOccluder(Vector3 position, Vector3 scale)
    {
        GameObject occluder = GameObject.CreatePrimitive(PrimitiveType.Cube);
        occluder.name = "Occluder";
        occluder.transform.position = position;
        occluder.transform.localScale = scale;
        occluder.layer = 8;
        spawnedObjects.Add(occluder);
    }
}
