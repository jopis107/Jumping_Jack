using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

namespace JumpingJack.Player{
    [RequireComponent(typeof(CharacterController), typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour{
        [Header("Movement")]
        [SerializeField] private float initialPlayerSpeed = 4f;
        [SerializeField] private float maximumPlayerSpeed = 30f;
        [SerializeField] private float playerSpeedIncreaseRate = .1f;

        [Header("Jump & Gravity")]

        [Header("Jump & Gravity")]
        [SerializeField] private float jumpHeight = 2.0f;
        [SerializeField] private float initialGravityMagnitude = 9.81f;
        [SerializeField] private float maximumGravityMagnitude = 25f;
        [SerializeField] private float fallMultiplier = 2.5f;

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask turnLayer;
        [SerializeField] private LayerMask obstacleLayer;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private AnimationClip slideAnimationClip;

        [Header("Audio")]
        [SerializeField] 
        private AudioClip lightningPickupSound;

        private AudioSource audioSource;

        [Header("Visual Model")]
        [SerializeField] private Transform model;              // child mesh/animator (vizual)

        [Header("Score")]
        [SerializeField] private float scoreMultiplier = 10f;

        [Header("Events")]
        [SerializeField] private UnityEvent<Vector3> turnEvent;
        [SerializeField] private UnityEvent<int> gameOverEvent;
        [SerializeField] private UnityEvent<int> scoreUpdateEvent;

        [Header("Turning")]
        [SerializeField] private float turnSpeed = 15f;

        private Quaternion targetRotation; // U ovu varijablu spremamo ciljanu rotaciju

        // internals
        private bool isDead = false;
        private CharacterController controller;
        private PlayerInput playerInput;
        private InputAction turnAction, jumpAction, slideAction;

        private Vector3 movementDirection = Vector3.forward;
        private Vector3 velocity;
        private float playerSpeed;
        private float score;
        private bool sliding;
        private float airTime;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();
            audioSource = GetComponent<AudioSource>();
            if (!animator) animator = GetComponent<Animator>();
            if (!model && animator) model = animator.transform; // fallback
            if (animator) animator.applyRootMotion = false;

            turnAction = playerInput.actions["Turn"];
            jumpAction = playerInput.actions["Jump"];
            slideAction = playerInput.actions["Slide"];
        }

        private void OnEnable()
        {
            turnAction.performed += PlayerTurn;
            jumpAction.performed += PlayerJump;
            slideAction.performed += PlayerSlide;
        }
        private void OnDisable()
        {
            turnAction.performed -= PlayerTurn;
            jumpAction.performed -= PlayerJump;
            slideAction.performed -= PlayerSlide;
        }

        private void Start()
        {
            playerSpeed = initialPlayerSpeed;
            targetRotation = transform.rotation; // Postavi početnu rotaciju
        }

        private void Update()
        {
            if (isDead) return;

            bool grounded = IsGrounded();

            // score
            score += scoreMultiplier * Time.deltaTime;
            scoreUpdateEvent.Invoke((int)score);

            // translacija naprijed
            controller.Move(transform.forward * playerSpeed * Time.deltaTime);

            // gravitacija
            if (grounded && velocity.y < 0f) velocity.y = -2f;

            float speedPercentage = Mathf.InverseLerp(initialPlayerSpeed, maximumPlayerSpeed, playerSpeed);

            float currentGravity = Mathf.Lerp(initialGravityMagnitude, maximumGravityMagnitude, speedPercentage);

            float g = currentGravity * (velocity.y < 0f ? fallMultiplier : 1f);
            velocity.y -= g * Time.deltaTime;
            controller.Move(velocity * Time.deltaTime);

            //  detekcija pada s ploče
            if (!grounded && velocity.y < -0.1f) airTime += Time.deltaTime; else airTime = 0f;
            if (airTime > 0.5f) { GameOver(); return; }

            // ubrzavanje
            if (playerSpeed < maximumPlayerSpeed)
            {
                playerSpeed += playerSpeedIncreaseRate * Time.deltaTime;
                if (animator && animator.speed < 1.25f)
                    animator.speed += (1 / playerSpeed) * Time.deltaTime;
            }
        }

        private void LateUpdate()
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // -------- INPUTS --------
        private void PlayerJump(InputAction.CallbackContext ctx)
        {
            if (!IsGrounded() || sliding) return;
            float v0 = Mathf.Sqrt(2f * initialGravityMagnitude * Mathf.Max(0.01f, jumpHeight));
            velocity.y = v0;
        }

        private void PlayerSlide(InputAction.CallbackContext ctx)
        {
            if (!sliding && IsGrounded())
                StartCoroutine(Slide());
        }

        private IEnumerator Slide()
        {
            sliding = true;

            float originalHeight = controller.height;
            Vector3 originalCenter = controller.center;

            controller.height = originalHeight * 0.5f;
            controller.center = new Vector3(
                originalCenter.x,
                originalCenter.y - (originalHeight - controller.height) * 0.5f,
                originalCenter.z
            );

            if (animator) animator.SetTrigger("Slide");
            yield return new WaitForSeconds(slideAnimationClip.length / (animator ? animator.speed : 1f));

            controller.height = originalHeight;
            controller.center = originalCenter;
            sliding = false;
        }

        private void PlayerTurn(InputAction.CallbackContext context)
        {
            Vector3? turnPosition = CheckTurn(context.ReadValue<float>());
            if (!turnPosition.HasValue)
            {
                return;
            }

            movementDirection = Quaternion.AngleAxis(90 * context.ReadValue<float>(), Vector3.up) * movementDirection;
            turnEvent.Invoke(movementDirection);
            Turn(context.ReadValue<float>(), turnPosition.Value);
        }

        private Vector3? CheckTurn(float turnValue)
        {
            Collider[] hitColiders = Physics.OverlapSphere(transform.position, .1f, turnLayer);
            if (hitColiders.Length != 0)
            {
                Tile tile = hitColiders[0].transform.parent.GetComponent<Tile>();
                TileType type = tile.type;
                if ((type == TileType.LEFT && turnValue == -1) ||
                    (type == TileType.RIGHT && turnValue == 1) ||
                    (type == TileType.SIDEWAYS))
                {
                    return tile.pivot.position;
                }
            }
            return null;
        }


        private void Turn(float turnValue, Vector3 turnPosition)
        {
            Vector3 tempPlayerPosition = new Vector3(turnPosition.x, transform.position.y, turnPosition.z);
            controller.enabled = false;
            transform.position = tempPlayerPosition;
            controller.enabled = true;
            
            targetRotation *= Quaternion.Euler(0f, turnValue * 90f, 0f);
        }


        // -------- COMMON --------
        private bool IsGrounded() => controller.isGrounded;

        private void GameOver()
        {
            if (isDead) return;
            isDead = true;
            scoreUpdateEvent.Invoke((int)score);
            gameOverEvent.Invoke((int)score);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (((1 << hit.collider.gameObject.layer) & obstacleLayer.value) != 0)
                GameOver();
        }

        private void OnTriggerEnter(Collider other){
            if (other.CompareTag("SpeedBoost")){
                if (lightningPickupSound != null){
                    audioSource.PlayOneShot(lightningPickupSound);
                }
                Destroy(other.gameObject);   
            }
        }
    }
}
