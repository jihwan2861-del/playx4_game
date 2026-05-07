using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region Serializable classes
[System.Serializable]
public class EnemyWaves 
{
    [Tooltip("time for wave generation from the moment the game started")]
    public float timeToStart;

    [Tooltip("Enemy wave's prefab")]
    public GameObject wave;
}

#endregion

public class LevelController : MonoBehaviour {

    //Serializable classes implements
    public EnemyWaves[] enemyWaves; 

    public GameObject powerUp;
    public float timeForNewPowerup;
    public GameObject[] planets;
    public float timeBetweenPlanets;
    public float planetsSpeed;
    List<GameObject> planetsList = new List<GameObject>();

    Camera mainCamera;   

    [Header("🎮 Global Game Settings (전체 난이도 관리)")]
    [Tooltip("모든 적 패턴(장판 폭격 포함)이 시작되기 전, 맨 처음 주어지는 준비 시간(초)입니다! 여기서 조절하세요.")]
    public float globalStartDelay = 5f; 

    [Header("Evasion Spawning System")]
    public bool enableRandomSpawning = true;
    public bool disableOriginalWaves = false; // 기본 웨이브 시스템 끄기 (요청에 따라 기본 웨이브 켬)
    public bool spawnOriginalWavesFromBottom = true; // 기본 웨이브를 뒤집어서 뒤에서도 생성할지 여부
    public GameObject[] customEnemyPrefabs;
    public float spawnInterval = 1.5f;
    public float randomEnemySpeed = 10f;

    [Header("Wall Spawning System")]
    public bool enableWallSpawning = true;
    public float wallSpawnStartDelay = 5f;
    public float wallSpawnInterval = 3f;
    public int wallObstacleCount = 7;
    public float wallObstacleSpeed = 8f;
    public float wallObstacleSpacing = 2.5f; // 장애물 사이의 간격



    [Header("Endless Laser Patterns (레이저 패턴 무한 랜덤)")]
    [Tooltip("패턴 메이커 툴로 구워낸 레이저 프리팹을 넣으세요. 랜덤으로 나옵니다!")]
    public GameObject[] laserPatternPrefabs;
    [Tooltip("패턴 랜덤 스폰 시작 대기 시간 (초)")]
    public float laserPatternStartDelay = 10f;
    [Tooltip("다음 패턴까지의 간격 (초)")]
    public float laserPatternInterval = 4f;

    [Header("Survival / Boss Settings")]
    public float frenzyStartTime = 60f; // 보스 출현까지 걸리는 기본 시간
    [HideInInspector] public float currentBossTimer; // 현재 남은 보스 출현 시간
    public bool isFrenzyPhase = false;
    public GameObject interactionButtonPrefab;
    public GameObject safeZonePrefab;
    
    [Tooltip("버튼이 생성될 위치들 (Hierachy에서 빈 오브젝트를 만들어 지정하세요. 비워두면 랜덤 생성됩니다)")]
    public Transform[] buttonSpawnPoints;
    
    public int totalButtonsToActivate = 4;
    private int activatedButtonsCount = 0;
    private bool isSafeZoneActive = false;
    private Coroutine laserCoroutine;
    public static LevelController instance;

    private void Awake()
    {
        if (instance == null)
            instance = this;
            
        // PC 빌드(exe 파일) 실행 시 해상도를 강제로 1920 x 1200 창모드로 고정합니다.
#if UNITY_STANDALONE
        Screen.SetResolution(1920, 1200, FullScreenMode.Windowed);
#endif
    }

    private void Start()
    {
        mainCamera = Camera.main;
        if (!disableOriginalWaves)
        {
            //for each element in 'enemyWaves' array creating coroutine which generates the wave
            for (int i = 0; i<enemyWaves.Length; i++) 
            {
                float finalDelay = globalStartDelay + enemyWaves[i].timeToStart;
                StartCoroutine(CreateEnemyWave(finalDelay, enemyWaves[i].wave, false));
                if (spawnOriginalWavesFromBottom)
                {
                    // 뒤에서도 정확히 거울처럼 대칭으로 나오게 함
                    StartCoroutine(CreateEnemyWave(finalDelay, enemyWaves[i].wave, true));
                }
            }
        }
        StartCoroutine(PowerupBonusCreation());
        StartCoroutine(PlanetsCreation());

        if (enableRandomSpawning)
        {
            StartCoroutine(RandomEnemySpawning());
        }

        if (enableWallSpawning)
        {
            StartCoroutine(WallSpawning());
        }

        if (laserPatternPrefabs != null && laserPatternPrefabs.Length > 0)
        {
            laserCoroutine = StartCoroutine(EndlessLaserPatternSpawning());
        }
        // 타이머 방식 보스 등장으로 변경
        currentBossTimer = frenzyStartTime;
    }

    private void Update()
    {
        if (!isFrenzyPhase && currentBossTimer > 0)
        {
            currentBossTimer -= Time.deltaTime;
            // UI 업데이트 추가 가능
            if (currentBossTimer <= 0)
            {
                currentBossTimer = 0;
                StartFrenzyPhase();
            }
        }
    }

    public void ReduceBossTimer(float timeToReduce)
    {
        if (isFrenzyPhase) return;

        currentBossTimer -= timeToReduce;
        if (currentBossTimer < 0) currentBossTimer = 0;
        
        Debug.Log($"⏳ [보스 출현 단축] 남은 시간: {currentBossTimer:F1}초");
    }

    void StartFrenzyPhase()
    {
        isFrenzyPhase = true;
        Debug.Log("🚨 [폭주 모드 시작] 화면 초기화 후 레이저 폭발!");

        // 1. 화면에 남아있는 모든 적 제거 (먼저 깨끗이 비웁니다)
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            Destroy(enemy);
        }

        // 2. 레이저 주기를 2초로 설정하고 딜레이 없이 즉시 시작
        laserPatternInterval = 2.0f;
        if (laserCoroutine != null) StopCoroutine(laserCoroutine);
        laserCoroutine = StartCoroutine(EndlessLaserPatternSpawning(true));

        // 3. 버튼 스폰
        if (interactionButtonPrefab != null)
        {
            // 사용자가 직접 스폰 위치를 지정해둔 경우
            if (buttonSpawnPoints != null && buttonSpawnPoints.Length > 0)
            {
                totalButtonsToActivate = buttonSpawnPoints.Length; // 지정된 위치 개수만큼 목표 자동 조절
                for (int i = 0; i < buttonSpawnPoints.Length; i++)
                {
                    if (buttonSpawnPoints[i] == null) continue;
                    
                    GameObject btn = Instantiate(interactionButtonPrefab, buttonSpawnPoints[i].position, Quaternion.identity);
                    var interactScript = btn.AddComponent<InteractionPoint>();
                    interactScript.controller = this;
                }
            }
            else // 지정 안 했을 경우 (기존 랜덤 스폰)
            {
                for (int i = 0; i < totalButtonsToActivate; i++)
                {
                    Vector3 spawnPos = new Vector3(
                        Random.Range(PlayerMoving.instance.borders.minX, PlayerMoving.instance.borders.maxX),
                        Random.Range(PlayerMoving.instance.borders.minY, PlayerMoving.instance.borders.maxY),
                        0f
                    );
                    GameObject btn = Instantiate(interactionButtonPrefab, spawnPos, Quaternion.identity);
                    var interactScript = btn.AddComponent<InteractionPoint>();
                    interactScript.controller = this;
                }
            }
        }
    }

    public void OnButtonActivated()
    {
        activatedButtonsCount++;
        Debug.Log($"[버튼 활성화] ({activatedButtonsCount}/{totalButtonsToActivate})");

        if (activatedButtonsCount >= totalButtonsToActivate)
        {
            ActivateSafeZone();
        }
    }

    void ActivateSafeZone()
    {
        if (isSafeZoneActive) return; // 이미 활성화되었으면 중복 소환 안 함
        isSafeZoneActive = true;

        Debug.Log("🏁 [미션 완료] 세이프존 활성화! 레이저 중단 및 화면 청소.");
        
        // 1. 레이저 스폰 중단
        if (laserCoroutine != null) StopCoroutine(laserCoroutine);

        // 2. 화면에 있는 모든 적과 레이저 제거 (최적화)
        ClearAllEnemiesAndProjectiles();

        if (safeZonePrefab != null)
        {
            Instantiate(safeZonePrefab, Vector3.zero, Quaternion.identity);
        }
        else
        {
            // 사용자가 Inspector에 SafeZone 프리팹을 실수로 안 넣었을 경우를 대비한 자동 생성
            Debug.LogWarning("SafeZone Prefab이 비어있습니다! 임시 세이프존을 자동 생성합니다.");
            GameObject tempSafe = new GameObject("Temp_SafeZone");
            tempSafe.transform.position = Vector3.zero;
            tempSafe.AddComponent<SafeZone>();
        }
    }

    void ClearAllEnemiesAndProjectiles()
    {
        // "Enemy" 태그를 가진 모든 오브젝트 파괴
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies) Destroy(enemy);

        // "Projectile" 태그 등을 가진 오브젝트들도 파괴 (프리팹 설정에 따라 다를 수 있음)
        // 만약 레이저 패턴이 별도 오브젝트라면 그것들도 찾아서 지웁니다.
        var projectiles = GameObject.FindObjectsOfType<Projectile>();
        foreach (var p in projectiles)
        {
            if (p.enemyBullet) Destroy(p.gameObject);
        }
    }

    // 패턴 메이커 툴로 만든 프리팹을 랜덤으로 계속 소환하는 코루틴
    IEnumerator EndlessLaserPatternSpawning(bool skipInitialDelay = false)
    {
        if (!skipInitialDelay)
            yield return new WaitForSeconds(globalStartDelay + laserPatternStartDelay);
        
        while (true)
        {
            int rIndex = Random.Range(0, laserPatternPrefabs.Length);
            GameObject selectedPrefab = laserPatternPrefabs[rIndex];
            
            if (selectedPrefab != null)
            {
                // 레이저 패턴(GridStrikePattern 등)은 자체 스크립트에서 카메라 Viewport를 직접 계산해 소환하므로, 
                // 원점(0,0,0)에 회전 없이 깔끔하게 소환하는 것이 가장 안전하고 정상 작동합니다.
                Instantiate(selectedPrefab, Vector3.zero, Quaternion.identity);
            }
            
            yield return new WaitForSeconds(laserPatternInterval);
        }
    }

    //Create a new wave after a delay
    IEnumerator CreateEnemyWave(float delay, GameObject Wave, bool inverted) 
    {
        if (delay != 0)
            yield return new WaitForSeconds(delay);
            
        if (Player.instance != null && mainCamera != null)
        {
            // 카메라의 현재 위치를 기준으로 소환 (월드 좌표(0,0)에 고정되지 않게 함)
            Vector3 cameraPos = new Vector3(mainCamera.transform.position.x, mainCamera.transform.position.y, 0f);

            GameObject waveInstance = Instantiate(Wave, cameraPos, Quaternion.identity);
            
            if (inverted)
            {
                // 뒤집혀야 하는 경우에만 기존 회전값에 180도를 추가로 더해줍니다.
                waveInstance.transform.rotation *= Quaternion.Euler(0, 0, 180f);
            }
        }
    }

    //endless coroutine generating 'levelUp' bonuses. 
    IEnumerator PowerupBonusCreation() 
    {
        while (true) 
        {
            yield return new WaitForSeconds(timeForNewPowerup);
            Instantiate(
                powerUp,
                //Set the position for the new bonus: for X-axis - random position between the borders of 'Player's' movement; for Y-axis - right above the upper screen border 
                new Vector2(
                    Random.Range(PlayerMoving.instance.borders.minX, PlayerMoving.instance.borders.maxX), 
                    mainCamera.ViewportToWorldPoint(Vector2.up).y + powerUp.GetComponent<Renderer>().bounds.size.y / 2), 
                Quaternion.identity
                );
        }
    }

    IEnumerator PlanetsCreation()
    {
        //Create a new list copying the arrey
        for (int i = 0; i < planets.Length; i++)
        {
            planetsList.Add(planets[i]);
        }
        yield return new WaitForSeconds(10);
        while (true)
        {
            ////choose random object from the list, generate and delete it
            int randomIndex = Random.Range(0, planetsList.Count);
            GameObject newPlanet = Instantiate(planetsList[randomIndex]);
            planetsList.RemoveAt(randomIndex);
            //if the list decreased to zero, reinstall it
            if (planetsList.Count == 0)
            {
                for (int i = 0; i < planets.Length; i++)
                {
                    planetsList.Add(planets[i]);
                }
            }
            newPlanet.GetComponent<DirectMoving>().speed = planetsSpeed;

            yield return new WaitForSeconds(timeBetweenPlanets);
        }
    }

    IEnumerator RandomEnemySpawning()
    {
        yield return new WaitForSeconds(globalStartDelay); // 시작 딜레이 통합
        
        while (!isFrenzyPhase) // 폭주 모드가 아닐 때만 스폰
        {
            yield return new WaitForSeconds(spawnInterval);
            
            if (isFrenzyPhase) break;
            
            GameObject prefabToSpawn = null;
            if (customEnemyPrefabs != null && customEnemyPrefabs.Length > 0)
            {
                prefabToSpawn = customEnemyPrefabs[Random.Range(0, customEnemyPrefabs.Length)];
            }
            else if (enemyWaves != null && enemyWaves.Length > 0 && enemyWaves[0].wave != null)
            {
                // 프리팹 지정을 까먹었다면, 기존 웨이브 설정에서 첫번째 적을 몰래 훔쳐옵니다 (Fallback)
                var waveComp = enemyWaves[0].wave.GetComponent<Wave>();
                if (waveComp != null) prefabToSpawn = waveComp.enemy;
            }

            if (prefabToSpawn == null) continue;

            int edge = Random.Range(0, 4);
            Vector2 spawnPos = Vector2.zero;
            float rotZ = 0;

            // 카메라 경계를 바탕으로 화면 밖 위치 계산
            float minX = mainCamera.ViewportToWorldPoint(new Vector2(0, 0)).x - 2f;
            float maxX = mainCamera.ViewportToWorldPoint(new Vector2(1, 1)).x + 2f;
            float minY = mainCamera.ViewportToWorldPoint(new Vector2(0, 0)).y - 2f;
            float maxY = mainCamera.ViewportToWorldPoint(new Vector2(1, 1)).y + 2f;

            switch(edge)
            {
                case 0: // Top
                    spawnPos = new Vector2(Random.Range(minX, maxX), maxY);
                    break;
                case 1: // Bottom
                    spawnPos = new Vector2(Random.Range(minX, maxX), minY);
                    break;
                case 2: // Left
                    spawnPos = new Vector2(minX, Random.Range(minY, maxY));
                    break;
                case 3: // Right
                    spawnPos = new Vector2(maxX, Random.Range(minY, maxY));
                    break;
            }

            // 카메라(화면 중앙)를 향하는 방향 계산
            Vector2 targetPos = mainCamera.transform.position;
            Vector2 direction = (targetPos - spawnPos).normalized;
            
            // 유니티 2D에서 위쪽(Y축)이 앞을 향한다고 가정할 때 각도 계산
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            GameObject newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.Euler(0, 0, angle));
            
            // 기존 길따라가기 스크립트 제거 (직진만 하도록)
            var follow = newEnemy.GetComponent<FollowThePath>();
            if (follow != null) Destroy(follow);
            
            var directMove = newEnemy.GetComponent<DirectMoving>();
            if (directMove == null) directMove = newEnemy.AddComponent<DirectMoving>();
            directMove.speed = randomEnemySpeed;
        }
    }

    IEnumerator WallSpawning()
    {
        // 처음 5초 딜레이 (globalStartDelay 적용)
        yield return new WaitForSeconds(globalStartDelay);
        
        while (!isFrenzyPhase)
        {
            if (isFrenzyPhase) break;
            GameObject prefabToSpawn = null;
            if (customEnemyPrefabs != null && customEnemyPrefabs.Length > 0)
            {
                prefabToSpawn = customEnemyPrefabs[Random.Range(0, customEnemyPrefabs.Length)];
            }
            else if (enemyWaves != null && enemyWaves.Length > 0 && enemyWaves[0].wave != null)
            {
                var waveComp = enemyWaves[0].wave.GetComponent<Wave>();
                if (waveComp != null) prefabToSpawn = waveComp.enemy;
            }

            if (prefabToSpawn != null)
            {
                int edge = Random.Range(0, 4);
                float rotZ = 0;

                // 카메라 경계 계산
                float minX = mainCamera.ViewportToWorldPoint(new Vector2(0, 0)).x - 1f;
                float maxX = mainCamera.ViewportToWorldPoint(new Vector2(1, 1)).x + 1f;
                float minY = mainCamera.ViewportToWorldPoint(new Vector2(0, 0)).y - 1f;
                float maxY = mainCamera.ViewportToWorldPoint(new Vector2(1, 1)).y + 1f;

                float centerX = (minX + maxX) / 2f;
                float centerY = (minY + maxY) / 2f;

                for (int i = 0; i < wallObstacleCount; i++)
                {
                    Vector2 spawnPos = Vector2.zero;

                    // 화면 중앙을 기준으로 좌우/상하로 퍼지도록 오프셋 계산
                    float totalLength = (wallObstacleCount - 1) * wallObstacleSpacing;
                    float offset = (-totalLength / 2f) + (i * wallObstacleSpacing);

                    switch(edge)
                    {
                        case 0: // Top (가로로 일렬 배열, 아래로 비행)
                            spawnPos = new Vector2(centerX + offset, maxY + 2f);
                            rotZ = 180f;
                            break;
                        case 1: // Bottom (가로로 일렬 배열, 위로 비행)
                            spawnPos = new Vector2(centerX + offset, minY - 2f);
                            rotZ = 0f;
                            break;
                        case 2: // Left (세로로 일렬 배열, 오른쪽 비행)
                            spawnPos = new Vector2(minX - 2f, centerY + offset);
                            rotZ = -90f;
                            break;
                        case 3: // Right (세로로 일렬 배열, 왼쪽 비행)
                            spawnPos = new Vector2(maxX + 2f, centerY + offset);
                            rotZ = 90f;
                            break;
                    }

                    GameObject newEnemy = Instantiate(prefabToSpawn, spawnPos, Quaternion.Euler(0, 0, rotZ));
                    
                    var follow = newEnemy.GetComponent<FollowThePath>();
                    if (follow != null) Destroy(follow);
                    
                    var directMove = newEnemy.GetComponent<DirectMoving>();
                    if (directMove == null) directMove = newEnemy.AddComponent<DirectMoving>();
                    directMove.speed = wallObstacleSpeed;
                }
            }

            // 첫 스폰 이후로는 3초마다 반복
            yield return new WaitForSeconds(wallSpawnInterval);
        }
    }

}
