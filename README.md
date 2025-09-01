# Jumping Jack - Tehnička dokumentacija
# Uvod

Ovaj dokument služi kao tehnička dokumentacija za 3D beskonačnu trkačku igru "Jumping Jack". Dokumentacija detaljno opisuje arhitekturu projekta, korištene tehnologije, te objašnjava sve ključne događaje i mehanike u igri kroz opis, vizualni prikaz i isječke koda.

# Sadržaj

Korištene tehnologije

Kako pokrenuti projekt

Arhitektura projekta

Događaji i mehanike u igri

1. Pokretanje igre i Glavni izbornik

2. Početak trčanja i progresivno ubrzanje

3. Skok

4. Klizanje

5. Skretanje na raskrižju

6. Sudar s preprekom

7. Kraj igre

# Korištene tehnologije 

Game Engine: Unity 2022.3.x

Programski jezik: C#

Sustav za unos: Unity Input System

IDE: Visual Studio

Kako pokrenuti projekt

Klonirajte repozitorij: git clone https://github.com/jopis107/Jumping_Jack.git

Otvorite projekt u Unity Hubu (verzija 2022.3 ili novija).

U Project prozoru, otvorite scenu MainMenu iz mape Assets/Scenes.

Pritisnite Play za pokretanje igre.

Arhitektura projekta

Logika igre podijeljena je u dvije glavne skripte:

`PlayerController.cs`: Upravlja svim akcijama igrača (kretanje, skok, klizanje, sudari).

`TileSpawner.cs`: Odgovorna za proceduralno generiranje beskonačne staze.

Skripte komuniciraju pomoću Unity Events sustava kako bi se smanjila direktna ovisnost i kod učinio čišćim.

Događaji i mehanike u igri
1. Pokretanje igre i Glavni izbornik
Opis: Nakon pokretanja aplikacije, igraču se prikazuje glavni izbornik. Klikom na gumb "PLAY", pokreće se glavna scena i započinje igra.

Vizualni prikaz:

<img width="552" height="928" alt="image" src="https://github.com/user-attachments/assets/7cd0bce3-c9d5-4c35-91a3-b6231f655a80" />


Kod:
Logika za prebacivanje scena nalazi se u skripti `MainMenuManager.cs`
```
public void StartGame()
{
    SceneManager.LoadScene("SampleScene");
}
```
2. Početak trčanja i progresivno ubrzanje
Opis: Čim igra započne, lik automatski počinje trčati prema naprijed početnom brzinom. Kako vrijeme prolazi, brzina lika se postepeno povećava do definirane maksimalne brzine, čime se povećava i težina igre.

Vizualni prikaz:

<img width="554" height="926" alt="image" src="https://github.com/user-attachments/assets/3fd90859-6ed8-49f8-889d-7232d4fcc80a" />



Kod `PlayerController.cs`:
Logika za kretanje i ubrzavanje nalazi se unutar `Update()` metode.
```
private void Update()
{
    // ... ostali kod ...

    // 1) translacija naprijed
    controller.Move(transform.forward * playerSpeed * Time.deltaTime);

    // ... ostali kod ...

    // 6) ubrzavanje
    if (playerSpeed < maximumPlayerSpeed)
    {
        playerSpeed += playerSpeedIncreaseRate * Time.deltaTime;
    }
}
```
3. Skok
Opis: Pritiskom na tipku 'W', igrač može izvesti skok kako bi izbjegao niske prepreke. Skok je moguć samo ako se lik nalazi na tlu. Visina skoka i jačina gravitacije su podesivi parametri u Inspector prozoru.

Vizualni prikaz:

<img width="463" height="826" alt="image" src="https://github.com/user-attachments/assets/7c73c7bf-115c-4f42-a85d-5531ef2f24f7" />


Kod `PlayerController.cs`:
Skok se aktivira putem InputAction događaja i primjenjuje vertikalnu brzinu na lika.
```
private void PlayerJump(InputAction.CallbackContext ctx)
{
    if (!IsGrounded() || sliding) return;
    float v0 = Mathf.Sqrt(2f * initialGravityMagnitude * Mathf.Max(0.01f, jumpHeight));
    velocity.y = v0;
}

private bool IsGrounded() => controller.isGrounded;
```
4. Klizanje
Opis: Pritiskom na tipku 'S', igrač može klizati kako bi izbjegao visoke prepreke. Tijekom klizanja, visina collidera lika se privremeno smanjuje. Akcija traje onoliko dugo koliko i animacija klizanja.

Vizualni prikaz:

<img width="459" height="823" alt="image" src="https://github.com/user-attachments/assets/0414b9e7-67cc-46e4-89f3-4fafb02df9ec" />


Kod `PlayerController.cs`:
Klizanje je implementirano kao korutina (Coroutine) kako se ne bi blokirao ostatak igre.
```
private void PlayerSlide(InputAction.CallbackContext ctx)
{
    if (!sliding && IsGrounded())
        StartCoroutine(Slide());
}

private IEnumerator Slide()
{
    sliding = true;

    float originalHeight = controller.height;
    controller.height = originalHeight * 0.5f;

    if (animator) animator.SetTrigger("Slide");

    yield return new WaitForSeconds(slideAnimationClip.length / animator.speed);

    controller.height = originalHeight;
    
    sliding = false;
}
```

5. Skretanje na raskrižju

Opis: Na kraju svake dionice staze nalazi se raskrižje. Pritiskom na tipku 'A' (lijevo) ili 'D' (desno), igrač može skrenuti. Skripta provjerava je li igrač u zoni za skretanje i je li odabrao ispravan smjer. Rotacija lika je glatka za bolji vizualni dojam.

Vizualni prikaz:

<img width="241" height="425" alt="image" src="https://github.com/user-attachments/assets/cb61296f-eceb-4fdf-9d62-a65a1998a4e7" />

<img width="237" height="423" alt="image" src="https://github.com/user-attachments/assets/c4a46661-64de-46c2-9e8b-58ac80f4106d" />


Kod `PlayerController.cs`:
Logika skretanja podijeljena je u tri dijela: detekcija unosa, provjera zone i izvođenje rotacije.
```
// 1) INPUT – okida skretanje
private void PlayerTurn(InputAction.CallbackContext context)
{
    Vector3? turnPosition = CheckTurn(context.ReadValue<float>());
    if (!turnPosition.HasValue) return;

    turnEvent.Invoke(movementDirection);
    Turn(context.ReadValue<float>(), turnPosition.Value);
}

// 2) PROVJERA ZONE ZA SKRETANJE
private Vector3? CheckTurn(float turnValue)
{
    Collider[] hitColiders = Physics.OverlapSphere(transform.position, .1f, turnLayer);
}

// 3) IZVEDBA SKRETANJA
private void Turn(float turnValue, Vector3 turnPosition)
{
    transform.position = new Vector3(turnPosition.x, transform.position.y, turnPosition.z);
    
    // Postavi novu CILJANU rotaciju koja će se glatko izvršiti u LateUpdate()
    targetRotation *= Quaternion.Euler(0f, turnValue * 90f, 0f);
}

private void LateUpdate()
{
    transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
}
```

6. Sudar s preprekom
Opis: Ako igrač udari u prepreku, igra je gotova. Sudar se detektira provjerom layera objekta s kojim se igrač sudario.

Vizualni prikaz:

<img width="236" height="416" alt="image" src="https://github.com/user-attachments/assets/207654c1-1250-47e0-bd06-21d7f8326538" />


Kod `PlayerController.cs`:
Koristi se OnControllerColliderHit metoda koja se automatski poziva pri svakom sudaru.
```
private void OnControllerColliderHit(ControllerColliderHit hit)
{
    // Provjeri je li layer objekta s kojim smo se sudarili jednak obstacleLayeru
    if (((1 << hit.collider.gameObject.layer) & obstacleLayer.value) != 0)
        GameOver();
}
```

7. Kraj igre
Opis: Igra završava sudarom, padom sa staze ili pogrešnim skretanjem. Prikazuje se "Game Over" zaslon s konačnim rezultatom.

Vizualni prikaz:

<img width="234" height="414" alt="image" src="https://github.com/user-attachments/assets/3bc87adf-24fc-4988-afe3-435cd500a618" />


Kod `PlayerController.cs`:
Metoda GameOver() zaustavlja igru i aktivira događaj koji će prikazati UI za kraj igre.
```
private void GameOver()
{
    if (isDead) return;
    isDead = true;
    scoreUpdateEvent.Invoke((int)score);
    gameOverEvent.Invoke((int)score);
}
```
Gdje je `GameOver.cs`:
```
public class GameOver : MonoBehaviour{
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TextMeshProUGUI leaderboardScoreText;

    private void Start(){
        Time.timeScale = 1f;
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    public void StopGame(int score){
        if (leaderboardScoreText) leaderboardScoreText.text = score.ToString();
        if (gameOverPanel) gameOverPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ReloadScene(){
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
```
