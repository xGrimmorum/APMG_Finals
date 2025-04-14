using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class EnhancedMeshGenerator : MonoBehaviour
{
    public Text timerText, hpText, resultText;

    private List<GameObject> platforms = new List<GameObject>();
    private List<GameObject> enemies = new List<GameObject>();
    private List<GameObject> obstacles = new List<GameObject>();
    private List<GameObject> powerups = new List<GameObject>();
    private List<GameObject> fireballs = new List<GameObject>();
    private GameObject player, finishGoal;

    private Vector3 playerVelocity = Vector3.zero;
    private bool isGrounded = false;
    private float jumpForce = 15f;
    private float gravity = 20f;
    private float moveSpeed = 5f;
    private int maxHP = 3;
    private int currentHP;
    private float fireballTimer = 0f;
    private float invincibleTimer = 0f;
    private float gameTimer = 0f;
    private bool gameEnded = false;

    private float finishGoalSpawnTime = 20f;
    private bool finishGoalSpawned = false;

    private bool goalAppeared = false;
    private float goalSpawnTime = 20f;

    void Start()
    {
        currentHP = maxHP;
        GeneratePlayer();
        GenerateElevatedPlatforms();
        GenerateEnemies();
        GenerateObstacles();
        GeneratePowerups();
    }

    void Update()
    {
        if (gameEnded) return;

        gameTimer += Time.deltaTime;
        timerText.text = "Time: " + Mathf.FloorToInt(gameTimer);
        hpText.text = "HP: " + currentHP;

        if (!finishGoalSpawned && gameTimer > finishGoalSpawnTime)
        {
            SpawnFinishGoal();
            finishGoalSpawned = true;
        }

        HandlePlayerMovement();
        MoveFireballs();
        MoveEnemies();
        CheckFireballHitsEnemies();
        HandleInvincibility();
        CheckPowerupPickup();

        if (!goalAppeared && Time.time >= goalSpawnTime)
        {
            SpawnFinishGoal();
            goalAppeared = true;
        }

        MoveFinishGoal();


        if (currentHP <= 0 && !gameEnded)
        {
            GameOver();
        }
    }

    void GeneratePlayer()
    {
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.transform.position = new Vector3(0, 1, 0);
        player.name = "Player";
    }

    void HandlePlayerMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        Vector3 move = new Vector3(horizontal, 0, 0) * moveSpeed;

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            playerVelocity.y = jumpForce;

        playerVelocity.y -= gravity * Time.deltaTime;
        if (!isGrounded && playerVelocity.y < 0)
            playerVelocity.y -= gravity * 0.5f * Time.deltaTime;

        Vector3 totalMove = move * Time.deltaTime;
        totalMove.y = playerVelocity.y * Time.deltaTime;

        player.transform.position += totalMove;

        isGrounded = false;

        // Check platform collisions
        foreach (GameObject platform in platforms)
        {
            Vector3 p = platform.transform.position;
            Vector3 pScale = platform.transform.localScale;
            Vector3 playerPos = player.transform.position;

            bool withinX = Mathf.Abs(playerPos.x - p.x) < pScale.x / 2 + 0.5f;
            bool closeToTop = Mathf.Abs(playerPos.y - (p.y + pScale.y / 2 + 1f)) < 0.1f;

            if (withinX && closeToTop && playerVelocity.y <= 0)
            {
                isGrounded = true;
                Vector3 fixedPos = player.transform.position;
                fixedPos.y = p.y + pScale.y / 2 + 1f; // 1f = player half height
                player.transform.position = fixedPos;
                playerVelocity.y = 0;
            }
        }

        // Ground check for base level (floor)
        if (player.transform.position.y <= 1.01f)
        {
            isGrounded = true;
            Vector3 pos = player.transform.position;
            pos.y = 1.01f;
            player.transform.position = pos;
            playerVelocity.y = 0;
        }

        if (fireballTimer > 0) fireballTimer -= Time.deltaTime;
        if (invincibleTimer > 0) invincibleTimer -= Time.deltaTime;

        player.GetComponent<Renderer>().material.color = invincibleTimer > 0 ? Color.yellow : Color.white;

        if (fireballTimer <= 0 && Input.GetKeyDown(KeyCode.F))
            ShootFireball();
    }

    void ShootFireball()
    {
        GameObject fireball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireball.transform.localScale = Vector3.one * 0.3f;
        fireball.transform.position = player.transform.position + Vector3.right;
        fireball.AddComponent<Rigidbody>().useGravity = false;
        fireball.GetComponent<Renderer>().material.color = Color.red;
        fireballs.Add(fireball);
    }

    void CheckPowerupPickup()
    {
        for (int i = powerups.Count - 1; i >= 0; i--)
        {
            GameObject pu = powerups[i];
            if (Vector3.Distance(player.transform.position, pu.transform.position) < 1.2f)
            {
                Color color = pu.GetComponent<Renderer>().material.color;

                if (color == Color.red)
                    fireballTimer = 100f; // 100 seconds of fireball ability
                else if (color == Color.green)
                    currentHP++;

                Destroy(pu);
                powerups.RemoveAt(i);
            }
        }
    }


    void MoveFireballs()
    {
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            GameObject fb = fireballs[i];
            fb.transform.position += Vector3.right * 10f * Time.deltaTime;
            if (fb.transform.position.x > player.transform.position.x + 50)
            {
                Destroy(fb);
                fireballs.RemoveAt(i);
            }
        }
    }

    void MoveEnemies()
    {
        foreach (GameObject enemy in enemies)
        {
            enemy.transform.position += Vector3.left * 2f * Time.deltaTime;
            if (Vector3.Distance(enemy.transform.position, player.transform.position) < 1.5f && invincibleTimer <= 0)
            {
                currentHP--;
                invincibleTimer = 2f;
            }
        }
    }

    void CheckFireballHitsEnemies()
    {
        for (int i = fireballs.Count - 1; i >= 0; i--)
        {
            for (int j = enemies.Count - 1; j >= 0; j--)
            {
                if (Vector3.Distance(fireballs[i].transform.position, enemies[j].transform.position) < 1f)
                {
                    Destroy(enemies[j]);
                    enemies.RemoveAt(j);
                    Destroy(fireballs[i]);
                    fireballs.RemoveAt(i);
                    break;
                }
            }
        }
    }

    void HandleInvincibility() { /* Already handled in Update visuals */ }

    void GenerateElevatedPlatforms()
    {
        for (int i = 0; i < 5; i++)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.transform.position = new Vector3(5 * i, 2 + i % 2, 0);
            platform.transform.localScale = new Vector3(3, 0.5f, 3);
            platform.GetComponent<Renderer>().material.color = Color.gray;
            platforms.Add(platform);
        }
    }

    void GenerateEnemies()
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
            enemy.transform.position = new Vector3(15 + i * 5, 1, 0);
            enemy.GetComponent<Renderer>().material.color = Color.magenta;
            enemies.Add(enemy);
        }
    }

    void GenerateObstacles()
    {
        for (int i = 0; i < 2; i++)
        {
            GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            obs.transform.position = new Vector3(10 + i * 10, 1, 0);
            obs.GetComponent<Renderer>().material.color = Color.black;
            obstacles.Add(obs);
        }
    }

    void GeneratePowerups()
    {
        GameObject fireballPU = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        fireballPU.transform.position = new Vector3(7, 2, 0);
        fireballPU.GetComponent<Renderer>().material.color = Color.red;
        powerups.Add(fireballPU);

        GameObject lifePU = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        lifePU.transform.position = new Vector3(13, 2, 0);
        lifePU.GetComponent<Renderer>().material.color = Color.green;
        powerups.Add(lifePU);
    }

    void SpawnFinishGoal()
    {
        finishGoal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        finishGoal.transform.localScale = new Vector3(2f, 3f, 2f);
        finishGoal.transform.position = new Vector3(player.transform.position.x + 30f, 0, player.transform.position.z);

        var renderer = finishGoal.GetComponent<Renderer>();
        renderer.material.color = Color.yellow;

        var collider = finishGoal.GetComponent<Collider>();
        collider.isTrigger = true;
    }



    void MoveFinishGoal()
    {
        if (!goalAppeared || finishGoal == null) return;

        float speed = 5f;
        finishGoal.transform.position = Vector3.MoveTowards(
            finishGoal.transform.position,
            player.transform.position,
            speed * Time.deltaTime
        );

        // Optional win detection
        if (Vector3.Distance(finishGoal.transform.position, player.transform.position) < 1.5f)
        {
            Debug.Log("Level Complete!");
            // Add UI/scene change logic here
        }
    }

    void WinGame()
    {
        gameEnded = true;
        resultText.text = "YOU WIN!";
        resultText.color = Color.green;
    }

    void GameOver()
    {
        gameEnded = true;
        resultText.text = "GAME OVER";
        resultText.color = Color.red;
    }
}
